# OpenTok.Net

[![NuGet](https://img.shields.io/nuget/v/OpenTok.Net?label=nuget)](https://www.nuget.org/packages/OpenTok.Net)
[![release](https://github.com/sbokatuk/OpenTok.Net/actions/workflows/release.yml/badge.svg)](https://github.com/sbokatuk/OpenTok.Net/actions/workflows/release.yml)
[![Targets: net8.0 | net9.0 | net10.0](https://img.shields.io/badge/targets-net8.0%20%7C%20net9.0%20%7C%20net10.0-512BD4)](#package)
[![Licence: MIT](https://img.shields.io/badge/licence-MIT-green.svg)](#licence)

**.NET / .NET MAUI façade for the Vonage OpenTok (Video API) SDK**, for building live video, audio
and screen-sharing into Android and iOS apps from C#.

`OpenTok.Net` is the package you install. It carries no code and binds nothing itself - it depends
on [`OpenTok.Net.iOS`](https://www.nuget.org/packages/OpenTok.Net.iOS) (over
`OpenTok.xcframework`) on an `-ios` target framework and
[`OpenTok.Net.Android`](https://www.nuget.org/packages/OpenTok.Net.Android) (over
`opentok-android-sdk.aar`) on an `-android` one, both built and published from their own
repositories. This is *not* a cross-platform façade in the sense of unifying the two native APIs:
`OTSession` on iOS and `Com.Opentok.Android.Session` on Android are separate, faithful projections of
two unrelated native APIs, with nothing in common beyond both existing. See
[`docs/native-surface.md`](docs/native-surface.md) for where to find each side's actual API and
[`docs/packaging.md`](docs/packaging.md) for how the package resolves the right dependency per
platform.

```bash
dotnet add package OpenTok.Net
```

```csharp
#if ANDROID
using Com.Opentok.Android;
var session = new Session.Builder(Android.App.Application.Context, apiKey, sessionId).Build();
#elif IOS
var session = new OTSession(apiKey, sessionId, this);
#endif
```

---

## Contents

- [Package](#package)
- [Platforms](#platforms)
- [How this repository works](#how-this-repository-works)
- [Building locally](#building-locally)
- [Testing](#testing)
- [Upgrading](#upgrading)
- [Releasing](#releasing)
- [Licence](#licence)

---

## Package

One package, `OpenTok.Net`, defined in [`build/packages.tsv`](build/packages.tsv). It ships
`net8.0-android34.0`, `net8.0-ios18.0`, `net9.0-android35.0`, `net9.0-ios18.0`,
`net10.0-android36.0` and `net10.0-ios26.0`, and depends on:

| On target framework | Depends on |
| --- | --- |
| `-ios` | [`OpenTok.Net.iOS`](https://www.nuget.org/packages/OpenTok.Net.iOS), pinned in `Directory.Build.props` (`OpenTokNetiOSVersion`) |
| `-android` | [`OpenTok.Net.Android`](https://www.nuget.org/packages/OpenTok.Net.Android), pinned in `Directory.Build.props` (`OpenTokNetAndroidVersion`) |

There is no plain `net8.0`/`net9.0`/`net10.0` asset: neither platform package ships one either, so
there is nothing for a neutral `OpenTok.Net` asset to reference. Guard the reference by target
platform in a multi-headed app that also targets Windows or Mac Catalyst.

## Platforms

- **iOS** - [native Objective-C sample app](https://github.com/opentok/opentok-ios-sdk-samples),
  [native Swift sample app](https://github.com/opentok/opentok-ios-sdk-samples-swift),
  [release notes](https://tokbox.com/developer/sdks/ios/release-notes.html).
- **Android** - [native sample app](https://github.com/opentok/opentok-android-sdk-samples),
  [release notes](https://tokbox.com/developer/sdks/android/release-notes.html),
  [Maven](https://central.sonatype.com/artifact/com.opentok.android/opentok-android-sdk).
- **Windows / macOS** - not currently packaged; `OpenTok.Net.iOS`/`OpenTok.Net.Android` do not
  publish binaries for either.

A working sample MAUI app lives at [`samples/OpenTok.Maui.Sample`](samples/OpenTok.Maui.Sample). It
connects to a session, publishes the camera/microphone and subscribes to the first remote stream on
both platforms - built directly against each platform's own native binding surface behind
`#if ANDROID` / `#if IOS`, since this façade has no shared cross-platform API (see
[`docs/native-surface.md`](docs/native-surface.md)).

## How this repository works

```
src/            OpenTok.Net - the façade project, the only thing this repository packs, one .sln
build/          packages.tsv (the package manifest) and the two-pass pack pipeline
tests/          PackageTests (packed .nupkg layout), DeviceTests (simulator/emulator smoke
                checks against the pinned platform packages), UnitTests (repository bookkeeping)
samples/        OpenTok.Maui.Sample, a multi-headed MAUI app built against the packed package
docs/           native-surface.md, packaging.md, upgrade notes, curated release notes
.github/        build/pr/release workflows and the scripts they call
```

See [`docs/packaging.md`](docs/packaging.md) for the two-pass band build and the merge step. See
[`docs/native-surface.md`](docs/native-surface.md) for where the actual native API surface is
documented (not in this repository).

## Building locally

Requires macOS with Xcode and the `android`/`ios` .NET workloads for both the `net9` and `net10` SDK
bands (`dotnet workload install android ios`) - needed to resolve `OpenTok.Net`'s own `-ios`/
`-android` reference assemblies, even though it carries no native code itself.

```bash
./build/BuildNugets.sh                         # packs OpenTok.Net into ./artifacts
OPENTOK_PACKAGE_VERSION=$(sed -n 's:.*<OpenTokNativeVersion>\(.*\)</OpenTokNativeVersion>.*:\1:p' Directory.Build.props).$(sed -n 's:.*<OpenTokBindingRevision>\(.*\)</OpenTokBindingRevision>.*:\1:p' Directory.Build.props) \
  dotnet test tests/OpenTok.Net.PackageTests   # asserts the packed layout
```

## Testing

- **`tests/OpenTok.Net.PackageTests`** - asserts the shape of the packed `.nupkg` (target
  frameworks, the per-head dependency on `OpenTok.Net.iOS`/`OpenTok.Net.Android` at the exact pinned
  version, licence, readme/icon) against `build/packages.tsv` and `Directory.Build.props`. Runs on a
  plain host, no workload required, once the package is packed.
- **`tests/OpenTok.Net.DeviceTests`** - a minimal iOS/Android app that constructs real native
  objects through `OpenTok.Net.iOS`/`OpenTok.Net.Android` and reports a pass/fail verdict; run on an
  iOS simulator and an Android emulator in CI via `.github/scripts/run-simulator-tests.sh` /
  `run-emulator-tests.sh`. Never calls `Connect`/`connect` - it proves the pinned platform packages
  load and their native libraries link, without needing a real OpenTok API key or network access.
- **`tests/OpenTok.Net.UnitTests`** - checks the repository's own bookkeeping (`packages.tsv`
  against `src/`, the `.sln`, `Directory.Build.props` version properties, including that the pinned
  platform versions are not lower than this façade's own native line) on a plain host with nothing
  built.

## Upgrading

Package structure and versioning changed with 2.27.1.900 - see
[`docs/upgrade-to-2.27.1.900.md`](docs/upgrade-to-2.27.1.900.md) if you are on an older
`OpenTok.Net`.

## Releasing

Tag `vX.Y.Z.W` matching `Directory.Build.props`'s `OpenTokNativeVersion.OpenTokBindingRevision` to
trigger `.github/workflows/release.yml`: it rebuilds `OpenTok.Net`, publishes to nuget.org over OIDC
trusted publishing, and creates a GitHub release from `docs/release-notes/<version>.md` if one
exists, or a generated commit list otherwise. Every pull request publishes a `-beta.<pr>.<run>`
prerelease the same way, so a package can be tried before it merges.

## Licence

[![Licence: MIT](https://img.shields.io/badge/licence-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

The code in this repository - the façade project, MSBuild plumbing, build and CI scripts - is
[MIT](LICENSE). `OpenTok.Net.iOS` and `OpenTok.Net.Android`, which this package depends on, each
embed Vonage's proprietary OpenTok / Vonage Video API SDK, not open source; see
[`licenses/Vonage-OpenTok-SDK.md`](licenses/Vonage-OpenTok-SDK.md).
