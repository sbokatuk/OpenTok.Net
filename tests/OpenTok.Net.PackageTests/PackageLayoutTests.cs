namespace OpenTok.Net.PackageTests;

/// <summary>
/// Asserts the shape of the two packed packages: every target framework present, an assembly in
/// each, and — the part that actually matters for a façade — the right platform binding depended on
/// for each one, at a version that cannot drift between platforms.
/// </summary>
public class PackageLayoutTests
{
    [Theory]
    [MemberData(nameof(Packages.PackageFrameworks), MemberType = typeof(Packages))]
    public void Package_carries_an_assembly_for_every_target_framework(string id, string tfm)
    {
        using var package = Packages.OpenPackage(id);

        var expected = $"lib/{tfm}/{id}.dll";
        Assert.True(package.GetEntry(expected) is not null, $"missing '{expected}'.");
    }

    [Theory]
    [MemberData(nameof(Packages.Frameworks), MemberType = typeof(Packages))]
    public void Core_depends_on_the_binding_for_the_platform_of_each_target_framework(string tfm)
    {
        var groups = Packages.DependencyGroups(Packages.Core);

        Assert.True(groups.ContainsKey(tfm), $"OpenTok.Net declares no dependency group for {tfm}.");

        var expected = Packages.PlatformDependency(tfm);
        var other = Packages.IsIos(tfm) ? "OpenTok.Net.Android" : "OpenTok.Net.iOS";

        // Both directions. The wrong binding present is the loud failure — it does not restore. The
        // right one *absent* is the quiet one: the façade's own assembly still ships, and the app
        // fails at load with a missing assembly rather than at restore.
        Assert.Contains(groups[tfm], d => d.Id == expected);
        Assert.DoesNotContain(groups[tfm], d => d.Id == other);
    }

    [Fact]
    public void Both_platforms_wrap_the_same_native_sdk_generation()
    {
        var groups = Packages.DependencyGroups(Packages.Core);

        // The failure this package exists to prevent, asserted directly: an iOS half on one SDK
        // generation and an Android half on another. It is invisible at build time and at restore
        // — both packages exist, both resolve — and shows up as a feature that works on one
        // platform and silently does not on the other.
        //
        // Compared on the first three components, which are the native SDK's own version; the
        // fourth is each binding repository's independent revision counter and legitimately
        // differs.
        var versions = Packages.TargetFrameworks
            .Select(tfm => (Tfm: tfm, Dependency: groups[tfm].Single(d => d.Id == Packages.PlatformDependency(tfm))))
            .Select(x => (x.Tfm, x.Dependency.Id, Sdk: NativeSdkVersionOf(x.Dependency.Version)))
            .ToList();

        var distinct = versions.Select(v => v.Sdk).Distinct().ToList();

        Assert.True(
            distinct.Count == 1,
            "the platform bindings wrap different native SDK versions: " +
            string.Join(", ", versions.Select(v => $"{v.Tfm} -> {v.Id} {v.Sdk}")));
    }

    [Theory]
    [MemberData(nameof(Packages.Frameworks), MemberType = typeof(Packages))]
    public void Maui_depends_on_the_core_package_at_an_exact_version(string tfm)
    {
        var groups = Packages.DependencyGroups(Packages.Maui);

        Assert.True(groups.ContainsKey(tfm), $"OpenTok.Net.Maui declares no dependency group for {tfm}.");

        var core = groups[tfm].SingleOrDefault(d => d.Id == Packages.Core);
        Assert.True(core.Id is not null, $"OpenTok.Net.Maui does not depend on {Packages.Core} for {tfm}.");

        // "[x.y.z.w]" — an exact range, not a minimum. The two ship together and share the
        // per-platform IOpenTokVideoSource, so a handler resolved against a different version of it
        // than the one at runtime would compile and then fail to find the interface.
        Assert.StartsWith("[", core.Version, StringComparison.Ordinal);
        Assert.EndsWith("]", core.Version, StringComparison.Ordinal);
    }

    /// <summary>
    /// The native SDK version out of a binding package version — the first three components of
    /// <c>&lt;native SDK version&gt;.&lt;binding revision&gt;</c>, ignoring any prerelease suffix.
    /// </summary>
    private static string NativeSdkVersionOf(string version)
    {
        var bare = version.Trim('[', ']');

        var suffix = bare.IndexOf('-');
        if (suffix >= 0)
        {
            bare = bare[..suffix];
        }

        var parts = bare.Split('.');
        Assert.True(parts.Length >= 3, $"'{version}' is not a <sdk>.<revision> package version.");

        return string.Join('.', parts.Take(3));
    }
}
