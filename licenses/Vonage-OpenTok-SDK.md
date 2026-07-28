# Native SDK licensing

The C# code in this repository - the façade project under `src/`, the MSBuild plumbing, the build
and CI scripts - is licensed under [MIT](MIT.txt). It is packaged separately from what it depends
on.

`OpenTok.Net`, the package this repository builds, depends on `OpenTok.Net.iOS` and
`OpenTok.Net.Android` - built and published from their own repositories, not this one. Both embed
native binaries owned by Vonage:

- `OpenTok.Net.iOS` embeds `OpenTok.xcframework`.
- `OpenTok.Net.Android` embeds the `opentok-android-sdk` `.aar` and its native dependencies.

Those binaries are **not** open source and carry no SPDX license identifier - they are Vonage's
proprietary OpenTok / Vonage Video API SDK, distributed under Vonage's own SDK terms. Neither this
repository nor the repositories that build `OpenTok.Net.iOS`/`OpenTok.Net.Android` relicense them.
See Vonage's terms directly:

- iOS: https://tokbox.com/developer/sdks/ios/
- Android: https://tokbox.com/developer/sdks/android/

`OpenTok.Net`'s `PackageLicenseExpression` states only `MIT` - the license of the façade code -
because NuGet's license expression field has no identifier for "depends on a proprietary
third-party binary." Consumers should independently confirm their use of the underlying OpenTok SDK
complies with Vonage's terms; installing `OpenTok.Net` does not substitute for that.
