namespace OpenTok.Sample.Maui;

/// <summary>
/// The Windows half of <see cref="CaptureLifetime"/>: nothing to do.
/// </summary>
/// <remarks>
/// Windows places no restriction on camera or microphone access for a background desktop app. There
/// is no equivalent of Android 14's foreground-service requirement and no background-mode
/// declaration to make — a minimised window keeps capturing. So both halves are empty, and that is
/// the answer rather than a gap: this is the platform the shim exists to contrast with.
/// </remarks>
public static partial class CaptureLifetime
{
    private static partial void BeginPlatform()
    {
    }

    private static partial void EndPlatform()
    {
    }
}
