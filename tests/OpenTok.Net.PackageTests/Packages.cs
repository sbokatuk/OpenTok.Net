using System.IO.Compression;

namespace OpenTok.Net.PackageTests;

/// <summary>What one package in this repository is supposed to be.</summary>
/// <param name="Id">The NuGet package id, which is also its directory under <c>src/</c>.</param>
/// <param name="Heads">Which target-framework family it ships - <c>android</c>, <c>ios</c> or <c>mobile</c>.</param>
/// <param name="Dependencies">Other packages in this repository it must declare a dependency on.</param>
public sealed record PackageSpec(string Id, string Heads, IReadOnlyList<string> Dependencies);

/// <summary>
/// Locates the packed <c>.nupkg</c> files and describes what each one is supposed to contain.
/// </summary>
/// <remarks>
/// The package set is read from <c>build/packages.tsv</c> rather than repeated here, so a package
/// added to the build but not to the tests fails as a missing file instead of passing unnoticed.
/// </remarks>
public static class Packages
{
    /// <summary>Every package this repository builds, in dependency order.</summary>
    public static readonly IReadOnlyList<PackageSpec> All = ReadManifest();

    public static readonly string[] AndroidTargetFrameworks =
    [
        "net8.0-android34.0",
        "net9.0-android35.0",
        "net10.0-android36.0",
    ];

    public static readonly string[] IosTargetFrameworks =
    [
        "net8.0-ios18.0",
        "net9.0-ios18.0",
        "net10.0-ios26.0",
    ];

    /// <summary>xunit member data: one row per package.</summary>
    public static TheoryData<string> Ids
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var package in All)
            {
                data.Add(package.Id);
            }

            return data;
        }
    }

    public static PackageSpec Spec(string id) => All.Single(package => package.Id == id);

    /// <summary>The target frameworks a package is expected to carry, by its declared head.</summary>
    public static string[] ExpectedTargetFrameworks(string id) => Spec(id).Heads switch
    {
        "android" => AndroidTargetFrameworks,
        "ios" => IosTargetFrameworks,
        "mobile" => [.. AndroidTargetFrameworks, .. IosTargetFrameworks],
        var heads => throw new InvalidOperationException($"Unknown heads '{heads}' for package {id} in packages.tsv."),
    };

    public static ZipArchive OpenPackage(string id)
    {
        var path = NupkgPath(id);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"'{path}' does not exist. Run ./build/BuildNugets.sh first.", path);
        }

        return ZipFile.OpenRead(path);
    }

    public static string NupkgPath(string id) => Path.Combine(ArtifactsDirectory, $"{id}.{Version}.nupkg");

    public static string SnupkgPath(string id) => Path.Combine(ArtifactsDirectory, $"{id}.{Version}.snupkg");

    public static string ReadNuspec(ZipArchive package, string id)
    {
        var entry = package.GetEntry($"{id}.nuspec")
            ?? throw new InvalidOperationException($"{id} has no {id}.nuspec.");

        using var stream = entry.Open();
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }

    /// <summary>The version OpenTok.Net - the only package this repository builds - was packed with.</summary>
    /// <remarks>
    /// Overridable so a CI job that packed a prerelease can point the tests at it without the
    /// version being written down in two places.
    /// </remarks>
    public static string Version =>
        Environment.GetEnvironmentVariable("OPENTOK_PACKAGE_VERSION") is { Length: > 0 } configured
            ? configured
            : $"{ReadProperty("OpenTokNativeVersion")}.{ReadProperty("OpenTokBindingRevision")}";

    /// <summary>The OpenTok.Net.iOS version this façade is pinned to, from Directory.Build.props.</summary>
    public static string PinnediOSVersion => ReadProperty("OpenTokNetiOSVersion");

    /// <summary>The OpenTok.Net.Android version this façade is pinned to, from Directory.Build.props.</summary>
    public static string PinnedAndroidVersion => ReadProperty("OpenTokNetAndroidVersion");

    /// <summary>The directory packages are read from.</summary>
    public static string ArtifactsDirectory =>
        Environment.GetEnvironmentVariable("OPENTOK_ARTIFACTS_DIR") is { Length: > 0 } configured
            ? configured
            : Path.Combine(RepositoryRoot, "artifacts");

    public static string RepositoryRoot
    {
        get
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

    private static IReadOnlyList<PackageSpec> ReadManifest()
    {
        var path = Path.Combine(RepositoryRoot, "build", "packages.tsv");
        var specs = new List<PackageSpec>();

        foreach (var line in File.ReadAllLines(path))
        {
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var columns = line.Split('\t');
            if (columns.Length < 3)
            {
                throw new InvalidOperationException($"Malformed row in packages.tsv: '{line}'");
            }

            var dependencies = columns[2] == "-"
                ? []
                : columns[2].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            specs.Add(new PackageSpec(columns[0], columns[1], dependencies));
        }

        if (specs.Count == 0)
        {
            throw new InvalidOperationException("packages.tsv listed no packages.");
        }

        return specs;
    }

    private static string ReadProperty(string name)
    {
        var props = File.ReadAllText(Path.Combine(RepositoryRoot, "Directory.Build.props"));
        var open = $"<{name}>";
        var close = $"</{name}>";

        var start = props.IndexOf(open, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException($"Directory.Build.props has no {open}");
        }

        start += open.Length;
        var end = props.IndexOf(close, start, StringComparison.Ordinal);

        return props[start..end].Trim();
    }
}
