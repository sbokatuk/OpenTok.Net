# What this repository does and does not cover

OpenTok.Net is a **façade**, not a binding: it does not project any native SDK into .NET itself.
`OpenTok.Net.iOS` (over `OpenTok.xcframework`) and `OpenTok.Net.Android` (over
`opentok-android-sdk.aar`) are built and published from their own repositories and consumed here as
ordinary pinned NuGet dependencies - see [`packaging.md`](packaging.md) and
`OpenTokNetiOSVersion`/`OpenTokNetAndroidVersion` in `../Directory.Build.props`.

This repository is *not* a cross-platform façade in the DatadogNet sense either: it makes no attempt
to unify the two native APIs into one shared C# surface. `Com.Opentok.Android.Session` on Android and
`OTSession` on iOS are separate, faithful projections of two unrelated native APIs, with nothing in
common beyond both existing. Reference the platform-specific type behind `#if ANDROID` / `#if IOS` in
shared code, the same way `tests/OpenTok.Net.DeviceTests/SmokeTests.cs` does.

## Finding the actual API surface

The binding surface lives in the OpenTok.Net.iOS and OpenTok.Net.Android packages, not in this
repository:

- [`OpenTok.Net.iOS` on nuget.org](https://www.nuget.org/packages/OpenTok.Net.iOS) - browse its
  assembly (e.g. in an IDE's Object Browser, or `ildasm`/`ikdasm` against the packed `.dll`) for the
  full `OTSession`/`OTPublisher`/`OTSubscriber` surface.
- [`OpenTok.Net.Android` on nuget.org](https://www.nuget.org/packages/OpenTok.Net.Android) - same,
  for `Com.Opentok.Android.Session`/`Publisher`/`Subscriber`.
- Vonage's own SDK reference: [iOS](https://tokbox.com/developer/sdks/ios/),
  [Android](https://tokbox.com/developer/sdks/android/) - translate Java/Objective-C names to their
  .NET binding equivalents mechanically (Java `getSessionId()` becomes C# `SessionId`, an
  Objective-C `initWithApiKey:sessionId:delegate:` becomes a matching C# constructor, and so on).

A worked example of both platforms' entry points is in
[`tests/OpenTok.Net.DeviceTests/SmokeTests.cs`](../tests/OpenTok.Net.DeviceTests/SmokeTests.cs):
constructing `OTSession`/`OTSessionSettings` on iOS and `Session.Builder`/`Session` on Android,
without ever calling `Connect`/`connect`.
