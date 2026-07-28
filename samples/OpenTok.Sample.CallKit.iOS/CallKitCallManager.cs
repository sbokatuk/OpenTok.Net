using AVFoundation;
using CallKit;
using Foundation;
using OpenTok.Net;

namespace OpenTok.Sample.CallKit.iOS;

/// <summary>
/// Bridges CallKit and the OpenTok session: reports a call to the system, answers and ends it
/// through the system's own UI, and hands audio control over so the two do not fight.
/// </summary>
/// <remarks>
/// <para>
/// <b>What is CallKit's and what is OpenTok's.</b> CallKit owns the call's *identity and
/// lifecycle* — the full-screen incoming-call UI, the lock-screen answer button, the entry in
/// Recents, and crucially the audio session. OpenTok owns the *media*. The only place they meet is
/// audio, and that meeting is the whole reason this class exists: without
/// <see cref="OpenTokAudioSession"/>, both would try to configure and activate
/// <c>AVAudioSession</c>, and the symptom is a call that connects with no sound.
/// </para>
/// <para>
/// <b>Order matters.</b> Media must not start until CallKit says the audio session is active. That
/// is why <see cref="_pendingConnect"/> exists: answering only records the intent, and the session
/// is connected from <see cref="DidActivateAudioSession"/>. Connecting on answer instead appears to
/// work in the simulator and fails on a device the moment another call or Siri holds the session.
/// </para>
/// <para>
/// <b>What a real app adds.</b> PushKit. A production VoIP app receives a PushKit notification and
/// must call <see cref="ReportIncomingCall"/> from <c>DidReceiveIncomingPush</c> before that handler
/// returns, or iOS terminates the app. This sample triggers the same code path from a button
/// instead, because PushKit needs a VoIP certificate and a server; everything downstream of the
/// report is identical.
/// </para>
/// </remarks>
public sealed class CallKitCallManager : CXProviderDelegate
{
    private readonly CXProvider _provider;
    private readonly CXCallController _callController = new();

    private readonly Action<string> _log;
    private readonly Func<OpenTokSession?> _session;

    private NSUuid? _callId;
    private Action? _pendingConnect;

    public CallKitCallManager(Func<OpenTokSession?> session, Action<string> log)
    {
        _session = session;
        _log = log;

        var configuration = new CXProviderConfiguration
        {
            SupportsVideo = true,
            MaximumCallsPerCallGroup = 1,

            // Generic, not PhoneNumber: an OpenTok session id is not dialable, and declaring it as
            // a phone number puts an entry in Recents that redials into the phone app.
            //
            // An NSSet, not a C# collection — CXProviderConfiguration takes the Foundation type
            // directly, so a collection expression has nothing to build.
            SupportedHandleTypes = new NSSet<NSNumber>((NSNumber)(int)CXHandleType.Generic),
        };

        _provider = new CXProvider(configuration);

        // null queue means the main queue, which is what the rest of this sample runs on.
        _provider.SetDelegate(this, null);
    }

    /// <summary>Whether a call is currently reported to CallKit.</summary>
    public bool HasCall => _callId is not null;

    /// <summary>
    /// Reports an incoming call to iOS, which shows the system incoming-call UI.
    /// </summary>
    /// <param name="from">Who is calling, as shown on the call screen.</param>
    /// <param name="connect">
    /// Connects the OpenTok session. Held rather than run: it is invoked from
    /// <see cref="DidActivateAudioSession"/>, once CallKit has activated audio.
    /// </param>
    public void ReportIncomingCall(string from, Action connect)
    {
        if (HasCall)
        {
            _log("a call is already in progress");
            return;
        }

        var callId = new NSUuid();
        var update = new CXCallUpdate
        {
            RemoteHandle = new CXHandle(CXHandleType.Generic, from),
            HasVideo = true,
            SupportsGrouping = false,
            SupportsUngrouping = false,
            SupportsHolding = false,
        };

        _callId = callId;
        _pendingConnect = connect;

        _provider.ReportNewIncomingCall(callId, update, error =>
        {
            if (error is not null)
            {
                // The usual cause is Do Not Disturb or a blocked caller — iOS refuses the report
                // and the call must not proceed.
                _log($"CallKit refused the call: {error.LocalizedDescription}");
                _callId = null;
                _pendingConnect = null;
                return;
            }

            _log($"incoming call from {from} — answer it from the system UI");
        });
    }

    /// <summary>
    /// Ends the call from the app's side, as a "hang up" button does.
    /// </summary>
    /// <remarks>
    /// Requested through <see cref="CXCallController"/> rather than by calling
    /// <see cref="CXProvider.ReportCall"/> directly: a transaction is how the *app* asks for an
    /// end, and it comes back through <see cref="PerformEndCallAction"/> so that teardown happens
    /// in exactly one place regardless of who initiated it.
    /// </remarks>
    public void EndCall()
    {
        if (_callId is not { } callId)
        {
            return;
        }

        var transaction = new CXTransaction(new CXEndCallAction(callId));
        _callController.RequestTransaction(transaction, error =>
        {
            if (error is not null)
            {
                _log($"could not end the call: {error.LocalizedDescription}");
            }
        });
    }

    // ---- CXProviderDelegate -----------------------------------------------------------------

    /// <summary>
    /// The user answered from the system UI.
    /// </summary>
    /// <remarks>
    /// Deliberately does not connect the session. It configures audio and fulfils the action so
    /// CallKit will activate the session; the connect happens in
    /// <see cref="DidActivateAudioSession"/>. See the class remarks.
    /// </remarks>
    public override void PerformAnswerCallAction(CXProvider provider, CXAnswerCallAction action)
    {
        _log("answered");

        // Configure, do not activate — activating here is what causes the two-owners problem.
        OpenTokAudioSession.PrepareForCall();

        action.Fulfill();
    }

    /// <summary>The user ended the call, from the system UI or through <see cref="EndCall"/>.</summary>
    public override void PerformEndCallAction(CXProvider provider, CXEndCallAction action)
    {
        _log("ended");

        _session()?.Disconnect();

        _callId = null;
        _pendingConnect = null;

        action.Fulfill();
    }

    /// <summary>
    /// CallKit has activated the audio session — media can start.
    /// </summary>
    /// <remarks>
    /// Two things happen here and the order is deliberate: the SDK is told audio is live *before*
    /// the session connects, so that by the time media negotiation finishes the audio device is
    /// already running.
    /// </remarks>
    public override void DidActivateAudioSession(CXProvider provider, AVAudioSession audioSession)
    {
        _log("audio session activated");

        OpenTokAudioSession.NotifyActivated();

        // Cleared before running: a second activation (a held call resuming, for instance) must
        // not connect twice.
        var connect = _pendingConnect;
        _pendingConnect = null;
        connect?.Invoke();
    }

    /// <summary>CallKit has deactivated the audio session.</summary>
    public override void DidDeactivateAudioSession(CXProvider provider, AVAudioSession audioSession)
    {
        _log("audio session deactivated");

        OpenTokAudioSession.NotifyDeactivated();
    }

    /// <summary>
    /// The provider was reset — iOS discarded every call, and nothing may be fulfilled or reported
    /// against them.
    /// </summary>
    /// <remarks>
    /// Rare, but the one callback that must not be skipped: it fires when the system tears CallKit
    /// down underneath the app, and any state kept here is now stale.
    /// </remarks>
    public override void DidReset(CXProvider provider)
    {
        _log("CallKit provider reset");

        _session()?.Disconnect();
        _callId = null;
        _pendingConnect = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _provider.Invalidate();
            _provider.Dispose();
            _callController.Dispose();
        }

        base.Dispose(disposing);
    }
}
