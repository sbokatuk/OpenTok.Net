# How the package is built

## One package, and it is a façade

This repository builds exactly one package, `OpenTok.Net`, listed in
[`build/packages.tsv`](../build/packages.tsv) - the single source of truth every script and test
reads instead of keeping its own copy.

`OpenTok.Net` carries no code and no native reference of its own. It is an ordinary multi-targeted
project (`src/OpenTok.Net/OpenTok.Net.csproj`) whose only content is a conditional
`PackageReference`:

- on an `-ios` target framework, it depends on `OpenTok.Net.iOS`;
- on an `-android` target framework, it depends on `OpenTok.Net.Android`.

Both dependencies are pinned to exact versions in `Directory.Build.props`
(`OpenTokNetiOSVersion`/`OpenTokNetAndroidVersion`) - not a floating range, so a consumer never
resolves a platform package this façade was never built or tested against. **Both packages are built
and published from their own repositories, not this one.** Bump the pinned versions by hand when a
new release of either is worth picking up; there is nothing to download or rebuild here first.

This is the same structure DatadogNet uses for `DatadogNet.Maui`'s dependency on `DatadogNet.iOS`/
`DatadogNet.Android`, minus the part where DatadogNet's own repository *also* contains hand-written
cross-platform logic worth publishing as its own multi-headed package (`DatadogNet` itself). OpenTok.Net
has no such layer: `Com.Opentok.Android.Session` and `OTSession` are unrelated APIs (see
[`native-surface.md`](native-surface.md)), so there is nothing to unify and nothing for this façade
to do beyond routing the right platform package to the right target framework.

## Two pack passes, merged

No single installed .NET SDK builds `net8`, `net9` and `net10` `-ios`/`-android` target frameworks
together: each SDK's workload ships reference packs for the current target framework and the one
before it. `build/BuildNugets.sh` therefore packs `OpenTok.Net` twice - once on the `net9` band
(which covers `net8` and `net9`), once on the `net10` band - and `build/merge-packages.py` grafts
the `net10` `lib/<tfm>/` assets and their nuspec dependency group into the `net9`-band package
afterwards. This applies even though `OpenTok.Net` has no native code of its own: a plain
multi-targeted library still needs the matching reference assemblies for each `-ios`/`-android`
target framework, which come from the same per-band workloads. This is the same split and the same
merge script DatadogNet uses, copied close to verbatim.

`Directory.Build.props`'s `OpenTokSdkBand` MSBuild property (`net9` by default, `net10` when
`BuildNugets.sh` invokes the second pass) drives which `TargetFrameworks` the project actually
declares for that pass - see `OpenTokAndroidTargetFramework` / `OpenTokiOSTargetFramework`.

NuGet's target-framework compatibility rules mean the pinned platform packages do not need to ship
`net9`/`net10`-labelled assets of their own for this to work: an `-ios`/`-android` asset targeting an
older, compatible OS version resolves for a newer target framework in the same platform family (e.g.
a `net7.0-ios16.1` asset satisfies a `net9.0-ios18.0` reference) the same way it would for any other
NuGet package.

## Building locally

```bash
./build/BuildNugets.sh          # packs OpenTok.Net into ./artifacts
dotnet test tests/OpenTok.Net.PackageTests   # asserts the packed .nupkg layout
```

`NuGet.config` adds `./artifacts` as a package source, so the device tests and the sample resolve
the locally-packed `OpenTok.Net` without needing it published anywhere - while `OpenTok.Net.iOS` and
`OpenTok.Net.Android` resolve from nuget.org at the versions pinned in `Directory.Build.props`, the
same way they would for any consumer.
