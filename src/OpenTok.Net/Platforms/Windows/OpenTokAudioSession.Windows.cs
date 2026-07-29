namespace OpenTok.Net;

/// <summary>
/// The Windows half of <see cref="OpenTokAudioSession"/> — every member a documented no-op.
/// </summary>
/// <remarks>
/// <para>
/// This whole type exists for CallKit on iOS and <c>android.telecom</c> on Android: an OS-level
/// calling framework owns the audio route, and the app has to hand the SDK's audio session over to
/// it and be told when it has been activated. Windows has no such framework. There is no system
/// call UI to integrate with, nothing that takes ownership of the audio device out from under the
/// process, and correspondingly nothing in <c>OpenTok.Client</c> to call.
/// </para>
/// <para>
/// Empty rather than throwing, and this is the deliberate part. Code like this is ordinary in a
/// shared MAUI view model:
/// </para>
/// <code>
/// OpenTokAudioSession.EnableCallingServicesMode();
/// OpenTokAudioSession.PrepareForCall();
/// </code>
/// <para>
/// It has to be safe to run everywhere. A Windows head that threw would force the one thing this
/// façade exists to remove — an <c>#if WINDOWS</c> around a call whose absence changes nothing —
/// and would do it at runtime, on the platform least likely to be tested first.
/// </para>
/// </remarks>
public static partial class OpenTokAudioSession
{
    private static partial void EnableCallingServicesModeNative()
    {
    }

    private static partial void PrepareForCallNative()
    {
    }

    private static partial void NotifyActivatedNative()
    {
    }

    private static partial void NotifyDeactivatedNative()
    {
    }
}
