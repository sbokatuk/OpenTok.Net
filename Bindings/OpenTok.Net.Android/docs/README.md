## OpenTok.Net.Android

Thank you for installing OpenTok.Net.Android, for details and issues reporting use our repository:
https://github.com/sbokatuk/OpenTok.Net

### .Net 7 and .Net 8 support
For .net8.0-* choose packages version like 2.27.1.8*, for .net7.0-* choose versions of packages 2.27.1.7*

### WIKI and API:

https://tokbox.com/developer/sdks/android/

### Requirements

The SDK is supported on high-speed Wi-Fi and 4G LTE networks.
The OpenTok Android SDK is supported on armeabi-v7a, armeabi64-v8a, x86, and x86_64 architectures.
The OpenTok Android SDK works with any Android 4.1+ device (Jelly Bean, API Level 16) that has a camera (for publishing video) and adequate CPU and memory support.

## Permissions

The OpenTok Android SDK uses following permissions:
android.permission.CAMERA -- If your app does not use the default video capturer and does not access the camera, you can remove this permission.
android.permission.INTERNET -- Required.
android.permission.RECORD_AUDIO -- If your app does not use the default audio device and does not access the microphone, you can remove this permission.
android.permission.MODIFY_AUDIO_SETTINGS -- If your app does not use the default audio device and does not access the microphone, you can remove this permission.
android.permission.BLUETOOTH -- The default audio device supports Bluetooth audio. If your app does not use the default audio device and does not use Bluetooth, you can remove this permission.
android.permission.BROADCAST_STICKY -- We have determined that this is unused by the OpenTok Android SDK, and we will remove this permission from an upcoming release.
android.permission.READ_PHONE_STATE -- The OpenTok Android SDK requests this permission in API level 22 and lower.
You do not need to add these to your app manifest. The OpenTok Android SDK adds them automatically. However, if you use Android 21+, certain permissions require you to prompt the user.
Your app can remove any of these permissions that will not be required. See this post and this Android documentation. For example, this removes the android.permission.CAMERA permission:
<uses-permission android:name="android.permission.CAMERA" tools:node="remove"/>
