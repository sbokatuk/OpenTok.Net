# How the packages are built

## Two packages

[`build/packages.tsv`](../build/packages.tsv) is the single source of truth every script, workflow
and test reads instead of keeping its own copy:

| Package | What it is |
| --- | --- |
| `OpenTok.Net` | The cross-platform API — `OpenTokSession`, `OpenTokPublisher`, `OpenTokSubscriber`. |
| `OpenTok.Net.Maui` | `OpenTokVideoView` and its handlers. Depends on `OpenTok.Net`. |

They are split so that MAUI stays out of the core package's dependency graph. A plain .NET for iOS
or .NET for Android app can take `OpenTok.Net` alone and render video itself through
`IOpenTokVideoSource.NativeView`; only apps that want the ready-made view pay for
`Microsoft.Maui.Controls`.

Neither binds a native SDK. `OpenTok.Net.iOS` and `OpenTok.Net.Android` — the packages that actually
wrap `OpenTok.xcframework` and `opentok-android-sdk.aar` — are built and published from their own
repositories and consumed here as exact pinned dependencies
(`OpenTokIosPackageVersion` / `OpenTokAndroidPackageVersion` in `Directory.Build.props`). There is
nothing native to download or rebuild before packing.

`OpenTok.Net` resolves exactly one of them per target framework, through a conditional
`PackageReference`: the iOS binding on an `-ios` head, the Android binding on an `-android` one.
That routing is what the old fat `OpenTok.Net` package tried to do by unzipping both platform
`.nupkg` outputs and re-zipping their `lib/` and `native/` trees into a single nuspec, with a "copy
`native/` into `lib/<tfm>` on the consumer's first build" MSBuild target to make up for the binding
SDK's own packaging being lost along the way. NuGet already resolves one dependency per target
framework; none of that is needed.

## The invariant worth naming

Both pinned platform packages must wrap the **same native SDK generation**. Their versions are
`<native SDK version>.<binding revision>`, so the first three components have to match; the fourth
legitimately differs, because each binding repository advances its own.

A mismatch is invisible: both packages exist, both restore, everything builds, packs and publishes —
and the app then behaves differently on the two platforms, which is exactly the drift a consumer
takes this package to stop thinking about. It is asserted in two places, deliberately:
`RepositoryConsistencyTests` checks the pins in `Directory.Build.props` before anything is built,
and `PackageLayoutTests` checks the dependency groups in the packed `.nuspec` afterwards.

## Two pack passes, merged

No single installed .NET SDK builds `net8`, `net9` and `net10` `-ios`/`-android` target frameworks
together: each SDK's workloads ship reference packs for the current target framework and the one
before it. `build/BuildNugets.sh` therefore packs each project twice — once on the `net9` band
(covering `net8` and `net9`), once on the `net10` band — and `build/merge-packages.py` grafts the
`net10` `lib/<tfm>/` assets and their nuspec dependency group into the `net9`-band package
afterwards. This applies even though neither project has native code of its own: a plain
multi-targeted library still needs the matching reference assemblies for each head, and those come
from the same per-band workloads.

`OpenTokSdkBand` (`net9` by default, `net10` for the second pass) selects which `TargetFrameworks`
the project declares for that pass — see [`src/OpenTok.Net.props`](../src/OpenTok.Net.props).

**Packing is per package, not per band.** `BuildNugets.sh` runs both passes *and the merge* for
`OpenTok.Net` before touching `OpenTok.Net.Maui`, because the MAUI project references the core one
as a package rather than a project, and even its `net9`-band pass cross-targets four target
frameworks at once. It therefore needs an `OpenTok.Net` in `artifacts/` that already carries every
one of them; a merge deferred to the end of the run cannot supply that in time. `packages.tsv`'s row
order is what encodes this, and a consistency test enforces that dependencies come first.

## Versions in a CI build

`BuildNugets.sh` takes an optional version argument, which CI uses to stamp a unique prerelease
(`2.34.1.1-beta.<pr>.<run>`), and passes through as `-p:Version=`.

That does **not** flow into `$(VersionPrefix)`, so `OpenTok.Net.Maui`'s reference to `OpenTok.Net`
has to be derived from `$(Version)` when it is set — otherwise a beta MAUI package would depend on a
stable core package this build never produced, and restore would either fail with NU1102 or, worse,
silently resolve a real older release from nuget.org. See the comment on
`OpenTokCorePackageVersion` in `src/OpenTok.Net.Maui/OpenTok.Net.Maui.csproj`.

## Building locally

```bash
./build/BuildNugets.sh                        # packs both into ./artifacts
dotnet test tests/OpenTok.Net.UnitTests       # repository bookkeeping; needs nothing built
dotnet test tests/OpenTok.Net.PackageTests    # asserts the packed .nupkg layout
```

`NuGet.config` adds `./artifacts` as a package source, so the device tests and the sample resolve the
locally-packed packages without anything being published. It also adds the two sibling binding
repositories' `artifacts/` directories ahead of nuget.org, so an unreleased binding change can be
tested end to end by packing that repository first. Missing sibling directories are ignored by
NuGet, and a checkout without them simply restores the pinned versions from nuget.org.
