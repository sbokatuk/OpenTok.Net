namespace OpenTok.Sample.Maui;

/// <summary>The Android half: a camera + microphone foreground service.</summary>
public static partial class CaptureLifetime
{
    private static partial void BeginPlatform() =>
        OpenTokCaptureService.Start(global::Android.App.Application.Context);

    private static partial void EndPlatform() =>
        OpenTokCaptureService.Stop(global::Android.App.Application.Context);
}
