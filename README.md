# .NET and MAUI Bindings for Vonage Opentok WebRTC SDK (Xamarin Opentok and Xamarin Vonage previously)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

only Android: [![NuGet Badge](https://buildstats.info/nuget/OpenTok.Net.Android)](https://www.nuget.org/packages/OpenTok.Net.Android/)

only iOS: [![NuGet Badge](https://buildstats.info/nuget/OpenTok.Net.iOS)](https://www.nuget.org/packages/OpenTok.Net.iOS/)

OpenTok.Net: [![NuGet Badge](https://buildstats.info/nuget/OpenTok.Net)](https://www.nuget.org/packages/OpenTok.Net/)

### .Net 7 and .Net 8 support
For .net8.0-* choose packages version like 2.27.1.8*, for .net7.0-* choose versions of packages 2.27.1.7*

This repository provides `.NET` Bindings for `Vonage` formely `OpenTok` of tokbox.com Video and Audio WebRTC SDKs (`MAUI`) targeting platforms:
- **iOS**:
     - [Native ObjectiveC sample app](https://github.com/opentok/opentok-ios-sdk-samples).
     - [Native Swift sample app](https://github.com/opentok/opentok-ios-sdk-samples-swift).
     - [Release Notes](https://tokbox.com/developer/sdks/ios/release-notes.html).
- **Android**
     - [Native sample app](https://github.com/opentok/opentok-android-sdk-samples).
     - [Release Notes](https://tokbox.com/developer/sdks/android/release-notes.html).
     - [Download from Maven](https://central.sonatype.com/artifact/com.opentok.android/opentok-android-sdk?smo=true).
- **Windows**
     - working on referencing it into the nuget.
- **MacOS**
     - working on referencing it into the nuget.
     
## Detailed Repository Structure

### `Bindings/` Directory
- **`iOS/`**
  - **`.csproj`**: .NET bindings for the TokBox Vonage WideoRTC SDK for iOS.
    - NuGet package configuration is specified in `.csproj`
- **`Android/`**
  - **`.csproj`**: .NET bindings for the TokBox Vonage WideoRTC SDK for Android.
    - NuGet package configuration is specified in `.csproj`
    

### `SampleApps/` Directory
- **`MAUI/`**: A .NET MAUI application demonstrating the bindings.

### `NugetPackages/` Directory
- Hosts NuGet packages.

### Scripts and Automation
- **`.loadnative.sh`**: Automates downloading native frameworks and version change.
  - Invoke via `./loadnative.sh` in the terminal.
- **`.bind.sh`**: Automates sharpie binding call and generation of fresh APIDefinitions.cs and StructsAndEnums.cs.
  - Invoke via `./bind.sh` in the terminal.
- **`.changeversions.sh`**: Automates version change for SDK.
  - Invoke via `./changeversions.sh 1.1.1.1` in the terminal.
- **`.target.sh`**: Automates version build and nugets tests.
  - Invoke via `./target.sh VERSION="2.27.1" IOSVERSION="2.27.1" ANDROIDVERSION="2.27.1" BUILD="17" WEBRTC="99.10.35" MLTRANSFORMERS="3.1.44"` in the terminal.
- **`.cleanup.sh`**: Automates cleaning for SDK.
  - Invoke via `./cleanup.sh` in the terminal.
- **`.publish.sh`**: Automates for pushing SDK.
  - Invoke via `./publish.sh [apikey]` in the terminal, apikey - your from nuget.org.

## Objective Sharpie - A Closer Look
- Essential for translating native iOS APIs into .NET compatible bindings.
- Generates `APIDefinitions` and `StructsAndEnums` files for .NET bindings.

## Additional Resources and References

- **iOS SDK Online Reference**: [Vonage Video API on Tokbox.com](https://tokbox.com/developer/sdks/ios/)
- **Android SDK Reference**: [Vonage Video API on Tokbox.com](https://tokbox.com/developer/sdks/android/)
- **Objective Sharpie Documentation**: [Objective Sharpie Guide](https://learn.microsoft.com/en-us/xamarin/cross-platform/macios/binding/objective-sharpie/get-started)

### MIT License
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
