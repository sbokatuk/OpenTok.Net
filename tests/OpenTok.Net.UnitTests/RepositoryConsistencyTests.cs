using System.Text.RegularExpressions;

namespace OpenTok.Net.UnitTests;

/// <summary>
/// Checks the repository's own bookkeeping rather than compiled code.
/// </summary>
/// <remarks>
/// <para>
/// The cross-platform logic in <c>OpenTok.Net</c> cannot be unit tested on a plain host: every path
/// through it ends in a <c>partial void</c> implemented against a native SDK, so there is nothing to
/// exercise without a device — which is what <c>tests/OpenTok.Net.DeviceTests</c> is for, and why
/// the state machine and argument checks are asserted there.
/// </para>
/// <para>
/// What <em>is</em> worth checking on a host, with nothing built, is that this repository's several
/// sources of truth still agree: <c>build/packages.tsv</c>, the projects under <c>src/</c>, the
/// solution that lists them, and the version pins in <c>Directory.Build.props</c>. Each of these is
/// read by something that will not complain if it goes stale — the build scripts, the CI workflows,
/// the e2e runners — so a drifted manifest otherwise surfaces as a confusing failure much later.
/// These run in the same <c>dotnet test</c> invocation as everything else and need no workload.
/// </para>
/// </remarks>
public class RepositoryConsistencyTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Every_package_in_packages_tsv_has_a_matching_project_under_src()
    {
        foreach (var package in ReadPackagesTsv())
        {
            var project = Path.Combine(RepositoryRoot, "src", package.Id, $"{package.Id}.csproj");

            Assert.True(File.Exists(project), $"packages.tsv lists '{package.Id}' but '{project}' does not exist.");
        }
    }

    [Fact]
    public void Every_project_under_src_is_listed_in_packages_tsv()
    {
        // The other direction, which is the one that goes wrong silently: a project added under
        // src/ but not to packages.tsv is never packed, and the omission looks exactly like a
        // package that was not meant to ship yet.
        var declared = ReadPackagesTsv().Select(package => package.Id).ToHashSet();

        var found = Directory.EnumerateFiles(Path.Combine(RepositoryRoot, "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null)
            .Select(name => name!)
            .ToHashSet();

        Assert.Equal(declared.OrderBy(id => id), found.OrderBy(id => id));
    }

    [Fact]
    public void Every_dependency_in_packages_tsv_is_declared_earlier_in_the_file()
    {
        // build/BuildNugets.sh packs top to bottom and *relies* on this order rather than merely
        // preferring it: OpenTok.Net.Maui restores OpenTok.Net from the local artifacts feed, so
        // the core package has to have been packed and merged already. Out of order, the MAUI
        // package's restore fails with NU1102 against a version that does not exist yet.
        var seen = new HashSet<string>();

        foreach (var package in ReadPackagesTsv())
        {
            foreach (var dependency in package.Dependencies)
            {
                Assert.True(
                    seen.Contains(dependency),
                    $"'{package.Id}' depends on '{dependency}', which is not declared earlier in packages.tsv.");
            }

            seen.Add(package.Id);
        }
    }

    [Fact]
    public void Every_package_in_packages_tsv_declares_a_known_head()
    {
        string[] knownHeads = ["android", "ios", "mobile"];

        foreach (var package in ReadPackagesTsv())
        {
            Assert.Contains(package.Heads, knownHeads);
        }
    }

    [Fact]
    public void The_solution_lists_exactly_the_packages_plus_the_test_projects()
    {
        var sln = File.ReadAllText(Path.Combine(RepositoryRoot, "OpenTok.Net.sln"));

        // A .sln lists solution *folders* with the same Project(...) syntax as real projects,
        // distinguished only by the type GUID — `dotnet sln add` creates one per directory, so
        // "src" and "tests" appear here as entries. Filtered out by that well-known GUID rather
        // than by matching the C# project GUID, so a project of another type added later is
        // caught by this check rather than quietly ignored.
        const string solutionFolderTypeGuid = "2150E333-8FDC-42A3-9474-1A3956D46DE8";

        var referenced = Regex.Matches(sln, @"Project\(""\{([0-9A-Fa-f-]+)\}""\)\s*=\s*""([^""]+)""")
            .Where(match => !string.Equals(match.Groups[1].Value, solutionFolderTypeGuid, StringComparison.OrdinalIgnoreCase))
            .Select(match => match.Groups[2].Value)
            .ToHashSet();

        // Equality, not containment: a project left in the .sln after being dropped from
        // packages.tsv would still open and build in an IDE, silently going stale.
        //
        // The device tests are deliberately absent — they target -ios/-android and would make
        // `dotnet build OpenTok.Net.sln` require the mobile workloads, which is exactly what
        // keeping them out of the solution avoids. The sample is out for the same reason (MAUI).
        var expected = ReadPackagesTsv()
            .Select(package => package.Id)
            .Append("OpenTok.Net.PackageTests")
            .Append("OpenTok.Net.UnitTests")
            .ToHashSet();

        Assert.Equal(expected.OrderBy(id => id), referenced.OrderBy(id => id));
    }

    [Theory]
    [InlineData("OpenTokVersion")]
    [InlineData("OpenTokIosPackageVersion")]
    [InlineData("OpenTokAndroidPackageVersion")]
    [InlineData("OpenTokWinPackageVersion")]
    [InlineData("OpenTokClientPackageVersion")]
    public void Version_properties_are_literal_dotted_version_numbers(string property)
    {
        // Literal, not an MSBuild expression: .github/workflows/release.yml reads these with sed and
        // tests/OpenTok.Net.PackageTests compares them as strings. Neither evaluates MSBuild, so a
        // "$(OpenTokVersion).1" here would reach the published release notes verbatim.
        //
        // The same pattern rules out a prerelease, which is the other way a pin goes wrong. Every
        // pull request in the platform repositories publishes a <version>-beta.<pr>.<run>, and
        // pinning one is the obvious way to get this repository's Windows leg green while that
        // repository's release is still in flight — but whatever is pinned when a tag is cut is what
        // consumers of the umbrella resolve, and a released package that depends on something built
        // from a branch is not a release. The platform repository publishes first; this one re-pins
        // behind it.
        var value = ReadProperty(property);

        Assert.Matches(@"^\d+(\.\d+){1,3}$", value);
    }

    [Theory]
    [InlineData("OpenTokVersion")]
    [InlineData("OpenTokBindingRevision")]
    [InlineData("OpenTokIosPackageVersion")]
    [InlineData("OpenTokAndroidPackageVersion")]
    [InlineData("OpenTokWinPackageVersion")]
    [InlineData("OpenTokClientPackageVersion")]
    public void Version_properties_are_declared_exactly_once(string property)
    {
        // Everything that reads these pins outside MSBuild — build/pins.sh, build/check-upstream.sh,
        // release.yml, pr.yml, ReadProperty below — matches the element textually and keeps the
        // first hit. An XML comment is not a comment to any of them, so a second element written
        // into the prose above the real pin *is* the pin as far as they are concerned, and the two
        // disagree exactly where it is least visible: MSBuild keeps building against the last value
        // in the file while every report about the file describes the first.
        //
        // That is what an example beta above OpenTokWinPackageVersion did — the daily upstream check
        // reported the Windows package as pinned to a prerelease nuget.org had never carried, while
        // the packages were built against the release underneath it.
        var props = File.ReadAllText(Path.Combine(RepositoryRoot, "Directory.Build.props"));

        var occurrences = props.Split($"<{property}>").Length - 1;

        Assert.True(
            occurrences == 1,
            $"Directory.Build.props opens <{property}> {occurrences} times; it must appear once, comments included.");
    }

    [Fact]
    public void The_binding_revision_is_a_plain_integer()
    {
        var value = ReadProperty("OpenTokBindingRevision");

        Assert.True(int.TryParse(value, out var revision) && revision >= 0,
            $"OpenTokBindingRevision is '{value}', not a non-negative integer.");
    }

    [Fact]
    public void Both_pinned_platform_packages_wrap_the_same_native_sdk_line()
    {
        // The single most important invariant in this repository, and the reason it exists as a
        // façade at all. OpenTok.Net.iOS and OpenTok.Net.Android are versioned
        // <native SDK version>.<binding revision>, so their first three components are the native
        // SDK's. Those must match: a façade whose iOS half wraps 2.27.1 and whose Android half
        // wraps 2.34.1 builds, restores, packs and publishes without complaint, and then behaves
        // differently on the two platforms — which is precisely the drift a consumer takes this
        // package to avoid thinking about.
        //
        // The fourth components legitimately differ; each binding repository advances its own.
        var ios = NativeSdkLineOf(ReadProperty("OpenTokIosPackageVersion"));
        var android = NativeSdkLineOf(ReadProperty("OpenTokAndroidPackageVersion"));

        Assert.Equal(ios, android);
    }

    [Fact]
    public void The_facade_is_versioned_after_the_native_sdk_line_it_wraps()
    {
        // The package version's own first three components name the native SDK, the same way the
        // two platform packages' do — so "OpenTok.Net 2.34.1.x" means "wraps OpenTok SDK 2.34.1"
        // without having to look anything up.
        Assert.Equal(ReadProperty("OpenTokVersion"), NativeSdkLineOf(ReadProperty("OpenTokIosPackageVersion")));
    }

    [Theory]
    [InlineData("OpenTokSupportedIosVersion")]
    [InlineData("OpenTokSupportedAndroidVersion")]
    public void Platform_floors_are_declared(string property)
    {
        // Read by src/OpenTok.Net.props, the sample and the device tests. An empty value does not
        // fail a build — SupportedOSPlatformVersion simply falls back to the workload default — so
        // a typo here would quietly ship a package advertising the wrong floor.
        var value = ReadProperty(property);

        Assert.Matches(@"^\d+(\.\d+)?$", value);
    }

    /// <summary>The native SDK's own version: the first three components of a binding package version.</summary>
    private static string NativeSdkLineOf(string packageVersion)
    {
        var parts = packageVersion.Split('.');
        Assert.True(parts.Length >= 3, $"'{packageVersion}' is not a <sdk>.<revision> package version.");

        return string.Join('.', parts.Take(3));
    }

    private static string ReadProperty(string name)
    {
        var props = File.ReadAllText(Path.Combine(RepositoryRoot, "Directory.Build.props"));
        var open = $"<{name}>";
        var close = $"</{name}>";

        var start = props.IndexOf(open, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Directory.Build.props has no {open}.");

        start += open.Length;
        var end = props.IndexOf(close, start, StringComparison.Ordinal);

        return props[start..end].Trim();
    }

    private static IReadOnlyList<(string Id, string Heads, IReadOnlyList<string> Dependencies)> ReadPackagesTsv()
    {
        var path = Path.Combine(RepositoryRoot, "build", "packages.tsv");
        var packages = new List<(string, string, IReadOnlyList<string>)>();

        foreach (var line in File.ReadAllLines(path))
        {
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var columns = line.Split('\t');
            Assert.True(columns.Length >= 3, $"Malformed row in packages.tsv: '{line}'");

            var dependencies = columns[2] == "-"
                ? []
                : columns[2].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            packages.Add((columns[0], columns[1], dependencies));
        }

        Assert.NotEmpty(packages);

        return packages;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException(
                "Could not find the repository root by walking up from " + AppContext.BaseDirectory);
    }
}
