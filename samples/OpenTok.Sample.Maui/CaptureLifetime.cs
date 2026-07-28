namespace OpenTok.Sample.Maui;

/// <summary>
/// Keeps the camera and microphone usable while the app is in the background.
/// </summary>
/// <remarks>
/// <para>
/// One of the few places a cross-platform sample genuinely has to branch, because the platforms do
/// not merely differ in API — they differ in whether anything is required at all.
/// </para>
/// <para>
/// <b>Android</b> refuses camera and microphone access to a background app, since Android 14,
/// unless a foreground service with the matching <c>foregroundServiceType</c> is running. Silently:
/// the app keeps running and captures nothing, which the other participant sees as a frozen frame.
/// <b>iOS</b> needs no equivalent for a foreground call, and the <c>voip</c>/<c>audio</c> background
/// modes a backgrounded call does need are declared in <c>Info.plist</c> rather than started at
/// runtime.
/// </para>
/// <para>
/// So this is not something <c>OpenTok.Net</c> could have unified: there is no shared operation to
/// name, only a shared moment at which to do platform-specific things. Hence a sample-level shim
/// rather than a façade API.
/// </para>
/// </remarks>
public static partial class CaptureLifetime
{
    /// <summary>Call when publishing starts.</summary>
    public static void Begin() => BeginPlatform();

    /// <summary>Call when publishing stops. Not when the app backgrounds — that is the point.</summary>
    public static void End() => EndPlatform();

    private static partial void BeginPlatform();
    private static partial void EndPlatform();
}
