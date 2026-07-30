# OpenTok.Net

[![NuGet](https://img.shields.io/nuget/v/OpenTok.Net?label=OpenTok.Net)](https://www.nuget.org/packages/OpenTok.Net)
[![NuGet](https://img.shields.io/nuget/v/OpenTok.Net.Maui?label=OpenTok.Net.Maui)](https://www.nuget.org/packages/OpenTok.Net.Maui)
[![Targets: net8.0 | net9.0 | net10.0](https://img.shields.io/badge/targets-net8.0%20%7C%20net9.0%20%7C%20net10.0-512BD4)](#packages)
[![OpenTok SDK 2.34.1](https://img.shields.io/badge/OpenTok%20SDK-2.34.1-099DFD)](https://developer.vonage.com/en/video/client-sdk/overview)
[![Licence: MIT](https://img.shields.io/badge/licence-MIT-green)](LICENSE)

One `Session`/`Publisher`/`Subscriber` API over Vonage's (formerly TokBox's) OpenTok iOS, Android
and Windows SDKs, so a .NET or .NET MAUI app writes its calling code once.

```bash
dotnet add package OpenTok.Net.Maui
```

```csharp
using var session = new OpenTokSession(apiKey, sessionId);

session.Connected     += (_, _) => session.Publish(publisher);
session.StreamReceived += (_, e) =>
{
    var subscriber = new OpenTokSubscriber(e.Stream);
    RemoteView.Source = subscriber;      // OpenTokVideoView, from OpenTok.Net.Maui
    session.Subscribe(subscriber);
};

session.Connect(token);
```

No `#if IOS`, no `#if ANDROID`, no `#if WINDOWS`, no delegate subclass, no Java listener, no
`JavaCast`, and no hand-written video-view handler. `samples/OpenTok.Sample.Maui` is that code as a running app —
compare it with the per-platform samples in the two binding repositories, which are the same flow
written twice.

---

## Packages

| Package | Depends on | Use it when |
| --- | --- | --- |
| `OpenTok.Net` | [`OpenTok.Net.iOS`](https://github.com/sbokatuk/OpenTok.Net.iOS) / [`OpenTok.Net.Android`](https://github.com/sbokatuk/OpenTok.Net.Android) / [`OpenTok.Net.Win`](https://github.com/sbokatuk/OpenTok.Net.Win), per target framework | Always — `OpenTokSession`, `OpenTokPublisher`, `OpenTokSubscriber`. |
| `OpenTok.Net.Maui` | `OpenTok.Net` + `Microsoft.Maui.Controls` | You are building MAUI and want `OpenTokVideoView` rather than writing a handler. |

Both carry `net8.0`, `net9.0` and `net10.0` for iOS, Android and Windows — nine target frameworks
each.

### Windows

Windows arrives differently from the other two. Vonage's `OpenTok.Client` is already managed .NET,
so there is no binding to generate — but it ships video renderers only for WPF and Windows Forms,
and none at all on the `netstandard2.0` asset a modern .NET app resolves. .NET MAUI on Windows is
WinUI 3, so without help a MAUI app can connect, publish and subscribe and have nowhere to put the
picture. [`OpenTok.Net.Win`](https://github.com/sbokatuk/OpenTok.Net.Win) 2.34.1.4 supplies the WinUI
renderer; this façade uses it, and you do not reference it directly.

Two things about Windows that the other platforms do not ask of you:

* **x64 only.** `OpenTok.Client`'s native payload has no arm64 build. `OpenTok.Net.Win` fails the
  build with **OTW0001** rather than letting that become a `BadImageFormatException` after launch.
  Windows on ARM runs the x64 build under emulation.
* **Create the first OpenTok object on the UI thread.** The Windows SDK has an explicit context
  object with no iOS or Android equivalent, and the façade binds it to that thread's dispatcher
  queue so events arrive somewhere they can touch the UI. It is what `OpenTokSession` already asks
  for; on Windows it is enforced rather than advised.

Some of the shared API has no Windows equivalent and is a documented no-op there rather than an
exception — camera position, torch and zoom (desktop webcams have none), the end-to-end encryption
secret, `Pause()`/`Resume()`, and all of `OpenTokAudioSession` (Windows has no CallKit analogue). A
façade whose common API throws on one platform would not be a façade.

For background blur, background replacement or noise suppression, add the platform transformers
package too — `OpenTok.Net.Transformers.iOS` and/or `OpenTok.Net.Transformers.Android`. Neither is
a dependency of anything here, because each costs around 70 MB; `OpenTokTransformer` is present in
`OpenTok.Net` either way, and reports that the library is not loaded if you use it without them.

The split keeps MAUI out of the core package's dependency graph: a plain .NET for iOS or .NET for
Android app can take `OpenTok.Net` on its own and render video however it likes, reaching the
SDK's view through `IOpenTokVideoSource.NativeView`.

## What it covers

Connect, publish, subscribe — and the rest of what both SDKs actually do, all of it cross-platform:

| | |
| --- | --- |
| **Session** | connect / disconnect, reconnection events, participant join / leave, capabilities from the token |
| **Signalling** | send to everyone or to one participant, with `FromSelf` worked out for you |
| **Moderation** | force-mute all or one stream, lift the mute state, force-disconnect |
| **Publisher** | mute audio / video, camera position, torch, zoom, audio level, forced-mute notification |
| **Subscriber** | per-stream audio and video toggles, volume, audio level, captions and translation language |
| **Archiving** | recording started / stopped |
| **Encryption** | end-to-end encryption secret |
| **Transformers** | background blur, background replacement, noise suppression (needs a transformers package) |
| **Lifecycle** | `Pause()` / `Resume()` for backgrounding |
| **Calling services** | `OpenTokAudioSession` — hand audio to CallKit or `android.telecom` |

`samples/OpenTok.Sample.Maui` demonstrates every row of that table in one page, with no
per-platform code.

## Samples

| Sample | Platforms | Shows |
| --- | --- | --- |
| `OpenTok.Sample.Maui` | iOS, Android, Windows | Everything in the table above, one page, no per-platform code |
| `OpenTok.Sample.CallKit.iOS` | iOS | `CXProvider`, answering from the system call UI, audio handed to CallKit |
| `OpenTok.Sample.Telecom.Android` | Android | `ConnectionService` + `PhoneAccount`, and a camera/microphone foreground service |

The two calling-service samples are separate apps rather than branches of the first, deliberately.
`CXProvider` and `ConnectionService` share a purpose and nothing else — on iOS the app owns a
provider object and calls it; on Android the *framework* binds a service and asks the app for
connections, which inverts the flow entirely. The one thing they do share is
`OpenTokAudioSession`, which both call identically, and that is exactly the part the façade could
honestly unify.

**If your Android app can be backgrounded mid-call, you need a foreground service.** Since Android
14, a background app is refused camera and microphone access without one carrying the matching
`foregroundServiceType` — silently: the session stays connected and simply captures nothing, which
the *other* participant sees as a frozen frame. `OpenTok.Sample.Maui` includes one
(`Platforms/Android/OpenTokCaptureService.cs`) precisely because it is not optional.

## What the façade does and does not hide

It hides the parts that are accidentally different, and leaves alone the parts that genuinely are.

**Hidden.** iOS answers an `OTError` out parameter from every call while Android returns void and
reports through a listener; iOS wants a delegate object per protocol while Android wants either a
listener interface or an event; iOS hands back a `UIView` and Android an `Android.Views.View`;
Android needs a `Context` and iOS does not; Android's `Publisher.Builder.Build()` needs a
`JavaCast` and iOS's constructor does not. None of that is a real difference in behaviour, and all
of it is gone.

**Not hidden.** `OpenTokError.Code` is the platform's own number and is deliberately *not* unified —
the two SDKs number their errors differently and Vonage documents no correspondence, so mapping
them would mean inventing one. Log it; do not branch on it across platforms. Permissions are not
requested for you either: when to ask and what to do when refused is a UI decision.

`OpenTokConnectionState` is the intersection the two platforms can both answer for — iOS exposes six
states, Android exposes none and only fires callbacks — so the façade tracks it rather than asking
the SDK, and it means the same thing on both.

## Threading

Events are raised on whichever thread the native SDK used. That is not the UI thread on either
platform. Marshal before touching UI:

```csharp
session.Connected += (_, _) => MainThread.BeginInvokeOnMainThread(() => StatusLabel.Text = "connected");
```

`OpenTokVideoView` already does this internally, so setting `Source` from a callback is safe.

## Disposal is not optional

`OpenTokPublisher` holds the camera and microphone; `OpenTokSession` holds a live signalling
connection. Neither SDK releases anything because the managed wrapper became unreachable. A
publisher that is never disposed keeps the camera until the process ends — and on Android that
blocks the *next* publisher the same app creates, which presents as a call screen that works once
and then shows black.

## Building locally

The two platform binding packages are restored, not built here. `NuGet.config` points at sibling
checkouts' `artifacts/` directories ahead of nuget.org, so an unreleased binding change is picked up
by packing that repository first; with no siblings present, restore falls through to nuget.org.

```sh
./build/BuildNugets.sh
dotnet test tests/OpenTok.Net.PackageTests
```

No single .NET SDK builds net8, net9 *and* net10 for either platform, so `BuildNugets.sh` packs
twice (the installed SDK's band, then a `net10` pass from a scratch `global.json`) and merges the
results — see `build/merge-packages.py`. Packages are packed in `build/packages.tsv` order, and each
is merged immediately after its own two passes, because `OpenTok.Net.Maui`'s restore needs an
`OpenTok.Net` in `artifacts/` that already carries every target framework.

See [`docs/packaging.md`](docs/packaging.md) for why the pack order matters and how CI versions flow
into the inter-package reference.

## Tests

Three suites, two of which run anywhere:

- **`tests/OpenTok.Net.UnitTests`** checks the repository's own bookkeeping with nothing built —
  `build/packages.tsv` against the projects under `src/`, the solution against both, dependency
  ordering, and the version pins: each declared exactly once in `Directory.Build.props`, comments
  included, and each a released version rather than one of the platform repositories' betas.
  Including the invariant this repository exists for: **both pinned platform packages wrapping the
  same native SDK generation.**
- **`tests/OpenTok.Net.PackageTests`** inspects the packed `.nupkg` files: an assembly for every
  target framework, the right platform binding depended on for each one, an exact-version link
  between the two packages, and the same same-SDK-generation check read back out of the shipped
  `.nuspec`.
- **`tests/OpenTok.Net.DeviceTests`** is a bare app (no MAUI, no test framework) that runs on a
  simulator or emulator. **It contains no `#if` at all** — the same calls compiled once, driving
  `OTSession` on iOS and `Com.Opentok.Android.Session` on Android. That is the claim of this
  repository, executed: it proves the native payload loads *and* that the state machine, argument
  validation and disposal semantics behave identically on both runtimes. Nothing connects,
  publishes or subscribes, so no credentials, no network and no camera are involved.

```sh
./build/BuildNugets.sh
dotnet test tests/OpenTok.Net.UnitTests
dotnet test tests/OpenTok.Net.PackageTests
```

With a simulator or emulator already booted:

```sh
./.github/scripts/run-simulator-tests.sh 2.34.1.1 net9.0-ios18.0
```

```sh
OPENTOK_DEVICE_RID=android-arm64 ./.github/scripts/run-emulator-tests.sh 2.34.1.1 net9.0-android35.0
```

## CI

| Workflow | Trigger | What it does |
| --- | --- | --- |
| [`pr.yml`](.github/workflows/pr.yml) | pull request | Packs both packages as `<version>-beta.<pr>.<run>`, runs the unit and package tests, builds the sample against them, runs the device checks on an iOS simulator and an Android emulator (net8 and net10 legs in parallel), then publishes the beta to nuget.org. Forked PRs build and test but skip publishing, since they cannot read secrets. |
| [`release.yml`](.github/workflows/release.yml) | tag `v*` | The same build and tests at the tag's version, publishes to nuget.org, then creates a GitHub release — from `docs/release-notes/<version>.md` when a curated file exists, otherwise from the commit log since the previous tag. |

Both call the reusable [`build.yml`](.github/workflows/build.yml). The pack and simulator jobs run
on macOS (an `-ios` target framework needs Xcode to resolve reference assemblies, even for a project
with no native code); the emulator job runs on Linux, because the Android emulator needs KVM.

Publishing uses nuget.org trusted publishing (OIDC, no long-lived API key) against the `nuget.org`
environment.

## Sample

`samples/OpenTok.Sample.Maui` is a MAUI app for both platforms with no per-platform code: API
key/session/token entry, Connect/Disconnect/Publish, local and remote video, and a status log. It
consumes the packed packages from `./artifacts`, so pack first:

```sh
./build/BuildNugets.sh
dotnet build samples/OpenTok.Sample.Maui -f net9.0-ios18.0 -p:RuntimeIdentifier=iossimulator-arm64
```

```sh
dotnet build samples/OpenTok.Sample.Maui -f net9.0-android35.0
```

`OpenTok.Net.sln` contains the library projects and the tests but **not** the sample, so
`dotnet build OpenTok.Net.sln` does not require the MAUI workload.

## Requirements

- **iOS 15.0+** — OTXCFramework 2.34.1's own floor, raised from 13.0 by that SDK.
- **Android API 24+** — `opentok-android-sdk`'s own `minSdkVersion`.
- **JDK 17+** on `$JAVA_HOME` for any Android app build — `opentok-android-sdk`'s `classes.jar` is
  Java 17 class file format. See the Android binding repository's README for the details.

## Repository layout

`src/`, `samples/`, `tests/` and `build/` are this project. The `Bindings/`, `NugetPackages/` and
`SampleApps/` directories, and the `*.sh` scripts beside them, are the **superseded monorepo** —
the iOS and Android bindings moved to their own repositories, and the old `OpenTok.Net` package was
a nuspec whose `.targets` copied files into `lib/` at *consumer* build time rather than a real
cross-platform API. They are kept only so the history is readable and can be deleted.

## Licence

MIT — see [LICENSE](LICENSE). Vonage's own OpenTok SDKs are distributed under their own SDK licence
terms, reached transitively through the platform binding packages.
