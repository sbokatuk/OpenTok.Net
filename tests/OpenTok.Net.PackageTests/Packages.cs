using System.IO.Compression;
using System.Xml.Linq;

namespace OpenTok.Net.PackageTests;

/// <summary>Locates the packed .nupkg files and describes what they are expected to contain.</summary>
public static class Packages
{
    public const string Core = "OpenTok.Net";
    public const string Maui = "OpenTok.Net.Maui";

    /// <summary>
    /// Every target framework both packages must carry.
    /// </summary>
    /// <remarks>
    /// Pinned rather than discovered, and this is the point of the suite: the two packages are
    /// produced by two SDK-band passes that are then merged (see build/BuildNugets.sh), and a pass
    /// that silently produced fewer target frameworks would still merge, still pack, and still
    /// install — for whoever happens not to use the missing one.
    /// </remarks>
    public static readonly string[] TargetFrameworks =
    [
        "net8.0-android34.0", "net9.0-android35.0", "net10.0-android36.0",
        "net8.0-ios18.0", "net9.0-ios18.0", "net10.0-ios26.0",
    ];

    /// <summary>The package ids in this repository, from build/packages.tsv's own ordering.</summary>
    public static readonly string[] Ids = [Core, Maui];

    public static IEnumerable<object[]> Frameworks =>
        TargetFrameworks.Select(tfm => new object[] { tfm });

    public static IEnumerable<object[]> PackageFrameworks =>
        Ids.SelectMany(id => TargetFrameworks.Select(tfm => new object[] { id, tfm }));

    public static IEnumerable<object[]> Packages_ =>
        Ids.Select(id => new object[] { id });

    /// <summary>Whether a target framework identifier is the iOS one.</summary>
    public static bool IsIos(string tfm) => tfm.Contains("-ios", StringComparison.Ordinal);

    /// <summary>The platform binding package a given target framework must depend on.</summary>
    public static string PlatformDependency(string tfm) =>
        IsIos(tfm) ? "OpenTok.Net.iOS" : "OpenTok.Net.Android";

    /// <summary>The dependency groups declared in a package's .nuspec, keyed by target framework.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<(string Id, string Version)>> DependencyGroups(string id)
    {
        using var package = OpenPackage(id);

        var entry = package.Entries.Single(e => e.FullName.EndsWith(".nuspec", StringComparison.Ordinal));
        using var stream = entry.Open();
        var document = XDocument.Load(stream);

        // The .nuspec namespace varies by the schema version NuGet wrote, so elements are matched
        // by local name rather than by a hard-coded namespace.
        return document.Descendants()
            .Where(e => e.Name.LocalName == "group")
            .ToDictionary(
                group => (string?)group.Attribute("targetFramework") ?? "",
                group => (IReadOnlyList<(string, string)>)group.Elements()
                    .Where(e => e.Name.LocalName == "dependency")
                    .Select(e => ((string?)e.Attribute("id") ?? "", (string?)e.Attribute("version") ?? ""))
                    .ToList());
    }

    public static string ArtifactsDirectory { get; } = ResolveArtifactsDirectory();

    public static string FindPackage(string id, string extension = ".nupkg")
    {
        var matches = Directory.Exists(ArtifactsDirectory)
            ? Directory.GetFiles(ArtifactsDirectory, $"{id}.*{extension}")
                .Where(f => IsVersionOf(Path.GetFileName(f), id, extension))
                .ToArray()
            : [];

        Assert.True(
            matches.Length > 0,
            $"No {id}.<version>{extension} found in '{ArtifactsDirectory}'. Run build/BuildNugets.sh first.");

        // A rebuilt working copy can leave several versions behind; test the newest.
        return matches.OrderByDescending(File.GetLastWriteTimeUtc).First();
    }

    /// <summary>
    /// Whether a file name is a version of exactly <paramref name="id"/>.
    /// </summary>
    /// <remarks>
    /// The glob for "OpenTok.Net.*" also matches OpenTok.Net.Maui, so a plain prefix test would
    /// have the core package's checks reading the MAUI package. Requiring the character after the
    /// id to be a digit is what separates "OpenTok.Net.2.34.1.1" from "OpenTok.Net.Maui.2.34.1.1".
    /// </remarks>
    private static bool IsVersionOf(string fileName, string id, string extension)
    {
        if (!fileName.StartsWith($"{id}.", StringComparison.Ordinal))
        {
            return false;
        }

        var remainder = fileName[(id.Length + 1)..^extension.Length];
        return remainder.Length > 0 && char.IsDigit(remainder[0]);
    }

    public static ZipArchive OpenPackage(string id, string extension = ".nupkg") =>
        ZipFile.OpenRead(FindPackage(id, extension));

    private static string ResolveArtifactsDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "global.json")))
        {
            directory = directory.Parent;
        }

        var root = directory?.FullName ?? AppContext.BaseDirectory;

        return Environment.GetEnvironmentVariable("OPENTOK_ARTIFACTS") is { Length: > 0 } configured
            ? Path.GetFullPath(configured, root)
            : Path.Combine(root, "artifacts");
    }
}
