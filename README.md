# .NET Bindings for Vonage Opentok WebRTC SDK (Xamarin.Opentok and Xamarin.Vonage previously)

This repository provides `.NET` Bindings for `Vonage` formely `OpenTok` of tokbox.com Video and Audio WebRTC SDKs (`MAUI.Vonage`) targeting platforms:
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
- **`.cleanup.sh`**: Automates cleaning for pushing SDK.
  - Invoke via `./cleanup.sh` in the terminal.

## Objective Sharpie - A Closer Look
- Essential for translating native iOS APIs into .NET compatible bindings.
- Generates `APIDefinitions` and `StructsAndEnums` files for .NET bindings.

## Additional Resources and References

- **iOS SDK Online Reference**: [Vonage Video API on Tokbox.com](https://tokbox.com/developer/sdks/ios/)
- **Android SDK Reference**: [Vonage Video API on Tokbox.com](https://tokbox.com/developer/sdks/android/)
- **Objective Sharpie Documentation**: [Objective Sharpie Guide](https://learn.microsoft.com/en-us/xamarin/cross-platform/macios/binding/objective-sharpie/get-started)

### Apache 2.0 License
[![License](https://img.shields.io/badge/License-Apache_2.0-yellowgreen.svg)](https://opensource.org/licenses/Apache-2.0)  
