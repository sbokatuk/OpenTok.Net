using AVFoundation;
using OpenTok.Net.iOS;

namespace OpenTok.Net;

/// <summary>The iOS half of <see cref="OpenTokAudioSession"/>, over <c>OTAudioSessionManager</c>.</summary>
/// <remarks>
/// Reached through <c>OTAudioDeviceManager.CurrentAudioSessionManager</c>, which returns the current
/// audio device when that device implements the protocol and null when it does not. The SDK's own
/// default device does implement it, which is what makes CallKit integration possible without
/// writing a custom <c>OTAudioDevice</c> — but a custom device that does not implement it will
/// return null here, and every method below is a no-op in that case rather than a crash.
/// </remarks>
public static partial class OpenTokAudioSession
{
    private static IOTAudioSessionManager? Manager => OTAudioDeviceManager.CurrentAudioSessionManager;

    private static partial void EnableCallingServicesModeNative() =>
        Manager?.EnableCallingServicesMode();

    // null asks for the SDK's default, which is AVAudioSessionModeVoiceChat — the right mode for
    // VoIP, and the one Vonage's own documentation uses.
    private static partial void PrepareForCallNative() =>
        Manager?.PreconfigureAudioSessionForCall(null);

    // AVAudioSession.SharedInstance rather than the instance CallKit hands to the delegate: iOS has
    // exactly one audio session per process, and provider:didActivateAudioSession: is documented to
    // pass that same shared instance. Taking it from here keeps the cross-platform signature free
    // of an iOS-only parameter for no loss.
    private static partial void NotifyActivatedNative() =>
        Manager?.AudioSessionDidActivate(AVAudioSession.SharedInstance());

    private static partial void NotifyDeactivatedNative() =>
        Manager?.AudioSessionDidDeactivate(AVAudioSession.SharedInstance());
}
