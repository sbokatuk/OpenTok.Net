using System.Xml.Linq;

namespace OpenTok.Net.PackageTests;

/// <summary>
/// Asserts the shape of the produced NuGet packages.
/// </summary>
/// <remarks>
/// These run against the packed <c>.nupkg</c> rather than the build output, so they catch packaging
/// regressions the compiler cannot see - a target framework the merge step dropped, a dependency
/// group that came out empty, a licence file that stopped being included.
/// </remarks>
public class PackageLayoutTests
{
    [Theory]
    [MemberData(nameof(Packages.Ids), MemberType = typeof(Packages))]
    public void Package_carries_an_assembly_for_every_expected_target_framework(string id)
    {
        using var package = Packages.OpenPackage(id);

        foreach (var tfm in Packages.ExpectedTargetFrameworks(id))
        {
            Assert.True(
                package.GetEntry($"lib/{tfm}/{id}.dll") is not null,
                $"{id} is missing 'lib/{tfm}/{id}.dll'. The net10 assets come from the second pack " +
                "pass and are grafted in by merge-packages.py, so a missing net10 target framework " +
                "usually means that step did not run.");
        }
    }

    [Theory]
    [MemberData(nameof(Packages.Ids), MemberType = typeof(Packages))]
    public void Package_carries_no_target_framework_it_should_not(string id)
    {
        using var package = Packages.OpenPackage(id);

        var expected = Packages.ExpectedTargetFrameworks(id).ToHashSet();

        var actual = package.Entries
            .Select(entry => entry.FullName.Split('/'))
            .Where(parts => parts.Length > 2 && parts[0] == "lib")
            .Select(parts => parts[1])
            .ToHashSet();

        // Equality, not containment, in both directions: a target framework silently disappearing
        // from the merge step is as bad as one appearing that nothing was built or tested for.
        Assert.Equal(expected.OrderBy(tfm => tfm), actual.OrderBy(tfm => tfm));
    }

    [Theory]
    [MemberData(nameof(Packages.Ids), MemberType = typeof(Packages))]
    public void Package_carries_documentation_for_every_assembly(string id)
    {
        using var package = Packages.OpenPackage(id);

        foreach (var tfm in Packages.ExpectedTargetFrameworks(id))
        {
            var entry = package.GetEntry($"lib/{tfm}/{id}.xml");

            Assert.True(entry is not null, $"{id} is missing 'lib/{tfm}/{id}.xml'.");
        }
    }

    [Theory]
    [MemberData(nameof(Packages.Ids), MemberType = typeof(Packages))]
    public void Package_declares_a_dependency_group_for_every_target_framework(string id)
    {
        using var package = Packages.OpenPackage(id);

        var groups = DependencyGroups(Packages.ReadNuspec(package, id));

        foreach (var tfm in Packages.ExpectedTargetFrameworks(id))
        {
            Assert.True(
                groups.ContainsKey(tfm),
                $"{id}'s nuspec declares no dependency group for '{tfm}'. NuGet reads a missing " +
                "group as 'this target framework needs nothing', so the platform package that head " +
                "actually needs (OpenTok.Net.iOS or OpenTok.Net.Android) would not be restored.");
        }
    }

    [Fact]
    public void The_facade_depends_on_OpenTok_Net_iOS_at_the_pinned_version_only_on_its_ios_head()
    {
        using var package = Packages.OpenPackage("OpenTok.Net");

        var groups = DependencyGroups(Packages.ReadNuspec(package, "OpenTok.Net"));

        foreach (var tfm in Packages.IosTargetFrameworks)
        {
            var declared = groups[tfm].ToDictionary(d => d.Id, d => d.Version);

            Assert.True(declared.TryGetValue("OpenTok.Net.iOS", out var version),
                $"OpenTok.Net ({tfm}) does not depend on OpenTok.Net.iOS.");

            // The exact version pinned in Directory.Build.props (OpenTokNetiOSVersion), not merely
            // "some version": a stray floating or mismatched reference here would still restore and
            // build, and only surface as a runtime mismatch against whatever this façade was tested
            // with.
            Assert.Equal(Packages.PinnediOSVersion, version);

            // A stray Android dependency on the ios head would resolve fine - NuGet does not care
            // that an Android package makes no sense on iOS - and only fail an app at link time.
            Assert.DoesNotContain("OpenTok.Net.Android", declared.Keys);
        }
    }

    [Fact]
    public void The_facade_depends_on_OpenTok_Net_Android_at_the_pinned_version_only_on_its_android_head()
    {
        using var package = Packages.OpenPackage("OpenTok.Net");

        var groups = DependencyGroups(Packages.ReadNuspec(package, "OpenTok.Net"));

        foreach (var tfm in Packages.AndroidTargetFrameworks)
        {
            var declared = groups[tfm].ToDictionary(d => d.Id, d => d.Version);

            Assert.True(declared.TryGetValue("OpenTok.Net.Android", out var version),
                $"OpenTok.Net ({tfm}) does not depend on OpenTok.Net.Android.");

            Assert.Equal(Packages.PinnedAndroidVersion, version);
            Assert.DoesNotContain("OpenTok.Net.iOS", declared.Keys);
        }
    }

    [Theory]
    [MemberData(nameof(Packages.Ids), MemberType = typeof(Packages))]
    public void Package_declares_the_MIT_licence_and_ships_its_text(string id)
    {
        using var package = Packages.OpenPackage(id);

        var nuspec = XDocument.Parse(Packages.ReadNuspec(package, id));
        var ns = nuspec.Root!.GetDefaultNamespace();

        Assert.Equal("MIT", nuspec.Descendants(ns + "license").Single().Value);

        // The code in this repository is MIT; the native SDK each package carries is Vonage's
        // proprietary OpenTok/Vonage Video API SDK, not something this repository can relicense -
        // see licenses/Vonage-OpenTok-SDK.md. NuGet has no slot for "this package's own code is MIT,
        // its payload is a third party's proprietary binary", so only the MIT text ships in-package.
        Assert.NotNull(package.GetEntry("licenses/MIT.txt"));
    }

    [Theory]
    [MemberData(nameof(Packages.Ids), MemberType = typeof(Packages))]
    public void Package_carries_a_readme_and_an_icon(string id)
    {
        using var package = Packages.OpenPackage(id);

        Assert.NotNull(package.GetEntry("README.md"));
        Assert.NotNull(package.GetEntry("icon.png"));
    }

    [Theory]
    [MemberData(nameof(Packages.Ids), MemberType = typeof(Packages))]
    public void Package_has_a_symbol_package(string id)
    {
        Assert.True(File.Exists(Packages.SnupkgPath(id)), $"'{Packages.SnupkgPath(id)}' does not exist.");
    }

    /// <summary>Maps each target framework to the dependencies its nuspec group declares.</summary>
    private static Dictionary<string, IReadOnlyList<(string Id, string Version)>> DependencyGroups(string nuspec)
    {
        var document = XDocument.Parse(nuspec);
        var ns = document.Root!.GetDefaultNamespace();

        return document
            .Descendants(ns + "group")
            .ToDictionary(
                group => group.Attribute("targetFramework")!.Value,
                group => (IReadOnlyList<(string, string)>)
                    [.. group.Elements(ns + "dependency")
                        .Select(dependency => (
                            dependency.Attribute("id")!.Value,
                            dependency.Attribute("version")!.Value))]);
    }
}
