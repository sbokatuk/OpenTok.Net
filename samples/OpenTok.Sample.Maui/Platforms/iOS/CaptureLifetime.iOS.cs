namespace OpenTok.Sample.Maui;

/// <summary>
/// The iOS half: nothing to do.
/// </summary>
/// <remarks>
/// iOS has no foreground-service concept. A call that must keep running while the app is in the
/// background declares the <c>voip</c> and <c>audio</c> background modes in <c>Info.plist</c> — a
/// build-time declaration, not something started and stopped per call. See
/// <c>samples/OpenTok.Sample.CallKit.iOS</c>, whose plist declares both.
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
