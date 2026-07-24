using System.Text.RegularExpressions;

namespace OpenTok.Net.UnitTests;

/// <summary>
/// Checks the repository's own bookkeeping rather than compiled code.
/// </summary>
/// <remarks>
/// OpenTok.Net is a façade with no native code of its own - it depends on OpenTok.Net.iOS and
/// OpenTok.Net.Android, built and published from their own repositories - so there is no
/// cross-platform C# business logic here to unit test either, and the binding surface those
/// dependencies expose can only be exercised on a real device or simulator - see
/// tests/OpenTok.Net.DeviceTests. What <i>is</i> worth testing on a plain host, with nothing built,
/// is that the repository's own source of truth stays internally consistent: build/packages.tsv,
/// the project under src/, the .sln that lists it, and the version numbers in
/// Directory.Build.props. These run in the same `dotnet test` invocation as everything else and
/// catch a drifted manifest before a build ever needs to run.
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
    public void Every_dependency_in_packages_tsv_is_declared_earlier_in_the_file()
    {
        // build/BuildNugets.sh packs packages.tsv top to bottom and relies on this order to report a
        // failure in a dependency before the packages built on top of it repeat the same error.
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
    public void The_solution_lists_exactly_the_projects_packages_tsv_declares()
    {
        var slnPath = Path.Combine(RepositoryRoot, "src", "OpenTok.Net.sln");
        var sln = File.ReadAllText(slnPath);

        var expected = ReadPackagesTsv().Select(package => package.Id).ToHashSet();

        var referenced = Regex.Matches(sln, @"Project\(""\{[0-9A-Fa-f-]+\}""\)\s*=\s*""([^""]+)""")
            .Select(match => match.Groups[1].Value)
            .ToHashSet();

        // Equality, not containment, in both directions: a project left in the .sln after it was
        // dropped from packages.tsv would still open and build in an IDE, silently going stale.
        Assert.Equal(expected.OrderBy(id => id), referenced.OrderBy(id => id));
    }

    [Fact]
    public void Every_packable_project_carries_a_docs_readme_and_icon()
    {
        foreach (var package in ReadPackagesTsv())
        {
            var docs = Path.Combine(RepositoryRoot, "src", package.Id, "docs");

            Assert.True(File.Exists(Path.Combine(docs, "README.md")), $"{package.Id} has no docs/README.md.");
            Assert.True(File.Exists(Path.Combine(docs, "icon.png")), $"{package.Id} has no docs/icon.png.");
        }
    }

    [Theory]
    [InlineData("OpenTokNativeVersion")]
    [InlineData("OpenTokNetiOSVersion")]
    [InlineData("OpenTokNetAndroidVersion")]
    public void Version_properties_look_like_dotted_version_numbers(string property)
    {
        var value = ReadProperty(property);

        Assert.Matches(@"^\d+(\.\d+){1,3}$", value);
    }

    [Fact]
    public void The_binding_revision_is_a_plain_integer()
    {
        var value = ReadProperty("OpenTokBindingRevision");

        Assert.True(int.TryParse(value, out var revision) && revision >= 0,
            $"OpenTokBindingRevision is '{value}', not a non-negative integer.");
    }

    [Theory]
    [InlineData("OpenTokNetiOSVersion")]
    [InlineData("OpenTokNetAndroidVersion")]
    public void Pinned_platform_package_versions_are_not_lower_than_the_native_line_this_facade_names_itself_after(string property)
    {
        // Not a hard guarantee - OpenTok.Net.iOS and OpenTok.Net.Android are versioned
        // independently, per their own repositories - but a pinned version that regressed below
        // this façade's own OpenTokNativeVersion would almost certainly be a typo rather than an
        // intentional downgrade.
        var native = Version.Parse(PadToFour(ReadProperty("OpenTokNativeVersion")));
        var pinned = Version.Parse(PadToFour(ReadProperty(property)));

        Assert.True(pinned >= native, $"{property} ({pinned}) is lower than OpenTokNativeVersion ({native}).");

        static string PadToFour(string version)
        {
            var parts = version.Split('.');
            return string.Join('.', parts.Concat(Enumerable.Repeat("0", 4 - parts.Length)));
        }
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
