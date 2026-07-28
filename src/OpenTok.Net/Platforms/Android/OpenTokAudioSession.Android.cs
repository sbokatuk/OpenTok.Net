using Com.Opentok.Android;

namespace OpenTok.Net;

/// <summary>
/// The Android half of <see cref="OpenTokAudioSession"/>, over
/// <c>BaseAudioDevice.AudioFocusManager</c>.
/// </summary>
/// <remarks>
/// <para>
/// Android has no audio-session protocol; the equivalent concern is <em>audio focus</em>, which the
/// SDK requests for itself by default. Under <c>android.telecom</c> the framework owns focus for the
/// call, so the SDK has to be told to stop asking — which is exactly what iOS's
/// <c>enableCallingServicesMode</c> means there, hence the shared name.
/// </para>
/// <para>
/// The manager comes from an <c>AudioDeviceManager</c>, which is cheap to construct and reads the
/// process-wide audio device the SDK is already using rather than creating one. It is fetched per
/// call rather than cached: an app may install a custom <c>BaseAudioDevice</c> at any point before
/// connecting, and a cached manager would then be talking to the device that was replaced.
/// </para>
/// </remarks>
public static partial class OpenTokAudioSession
{
    private static BaseAudioDevice.IAudioFocusManager? FocusManager
    {
        get
        {
            // Defensive rather than fastidious: this is driven by OS callbacks that can arrive
            // before the SDK has an audio device at all — a telecom Connection can be answered
            // before the app has connected a session — and a throw from a system callback is far
            // worse than doing nothing.
            try
            {
                using var manager = new AudioDeviceManager(OpenTokSession.AppContext);
                return manager.AudioFocusManager;
            }
            catch (Java.Lang.Exception)
            {
                return null;
            }
        }
    }

    // false: stop requesting focus, because the telecom framework holds it for the call.
    private static partial void EnableCallingServicesModeNative() =>
        FocusManager?.SetRequestAudioFocus(false);

    // Nothing to do. iOS separates "configure the session" from "activate it"; Android's focus
    // request is a single step, so there is no earlier moment to prepare for. See the
    // cross-platform doc comment for why this stays on the shared surface anyway.
    private static partial void PrepareForCallNative()
    {
    }

    private static partial void NotifyActivatedNative() => FocusManager?.AudioFocusActivated();

    private static partial void NotifyDeactivatedNative() => FocusManager?.AudioFocusDeactivated();
}
