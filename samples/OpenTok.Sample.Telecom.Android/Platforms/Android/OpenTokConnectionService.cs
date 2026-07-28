using Android.App;
using Android.Content;
using Android.OS;
using Android.Telecom;
using OpenTok.Net;

namespace OpenTok.Sample.Telecom.Android;

/// <summary>
/// The <c>android.telecom</c> integration: registers this app as a calling account and hands the
/// framework a <see cref="Connection"/> it can drive.
/// </summary>
/// <remarks>
/// <para>
/// This is Android's answer to CallKit, and the shape is genuinely different — which is why
/// <c>samples/OpenTok.Sample.CallKit.iOS</c> is a separate app rather than an <c>#if</c> branch.
/// On iOS the app owns a <c>CXProvider</c> object and calls it; here the *framework* binds a
/// service and asks it for connections, so the flow is inverted and the app's own objects are
/// created by the system.
/// </para>
/// <para>
/// What is shared is the audio handshake, and only that: both samples call
/// <see cref="OpenTokAudioSession"/> identically. On this platform that means telling the SDK to
/// stop requesting audio focus, because telecom holds it for the call.
/// </para>
/// <para>
/// <b>What a real app adds.</b> A push channel — FCM, typically — to learn about an incoming call,
/// and <c>TelecomManager.AddNewIncomingCall</c> from its handler. This sample calls that directly
/// from a button, because the rest of the flow is identical and a push setup would obscure it.
/// </para>
/// </remarks>
[Service(
    Exported = true,
    Permission = global::Android.Manifest.Permission.BindTelecomConnectionService,
    Name = "com.sbokatuk.opentok.sample.telecom.OpenTokConnectionService")]
[IntentFilter(["android.telecom.ConnectionService"])]
public sealed class OpenTokConnectionService : ConnectionService
{
    private const string AccountId = "opentok-sample";

    /// <summary>The live connection, so the app can end the call and the page can observe it.</summary>
    public static OpenTokConnection? Current { get; private set; }

    /// <summary>
    /// Registers this app's calling account with the telecom framework.
    /// </summary>
    /// <remarks>
    /// Idempotent and safe to call at every launch — registration is keyed by handle, and
    /// re-registering replaces rather than duplicates. Must happen before
    /// <see cref="ReportIncomingCall"/>, or the framework has no account to attribute the call to
    /// and rejects it.
    /// </remarks>
    public static PhoneAccountHandle Register(Context context)
    {
        var handle = new PhoneAccountHandle(
            new ComponentName(context, Java.Lang.Class.FromType(typeof(OpenTokConnectionService))),
            AccountId);

        var builder = new PhoneAccount.Builder(handle, "OpenTok Sample");

        // SelfManaged is the important one. A self-managed account keeps the call inside this app's
        // own UI — the system does not show the dialer's in-call screen, and the app is not
        // competing with the phone app for the call. The alternative, CallProvider, asks to *be* a
        // dialer, which needs the default-dialer role.
        //
        // Written out rather than chained because every builder setter is bound as returning a
        // nullable Builder.
        builder.SetCapabilities((int)PhoneAccountCapability.SelfManaged);
        builder.SetShortDescription("Video calls in the OpenTok sample");

        var account = builder.Build();

        var telecom = (TelecomManager?)context.GetSystemService(Context.TelecomService);
        telecom?.RegisterPhoneAccount(account);

        return handle;
    }

    /// <summary>
    /// Tells the framework a call is arriving. It responds by binding this service and asking for
    /// a connection through <see cref="OnCreateIncomingConnection"/>.
    /// </summary>
    public static void ReportIncomingCall(Context context, PhoneAccountHandle handle, string from)
    {
        var extras = new Bundle();

        // The caller's identity, as a tel: URI. Telecom insists on a Uri even for a self-managed
        // account that will never dial anything.
        extras.PutParcelable(
            TelecomManager.ExtraIncomingCallAddress,
            global::Android.Net.Uri.FromParts("tel", from, null));

        var telecom = (TelecomManager?)context.GetSystemService(Context.TelecomService);
        telecom?.AddNewIncomingCall(handle, extras);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Called by the framework, on its own thread, in response to <see cref="ReportIncomingCall"/>.
    /// The connection returned here is what the system will drive — answer, reject, disconnect —
    /// and it is the app's only handle on the call's lifecycle.
    /// </remarks>
    public override Connection? OnCreateIncomingConnection(
        PhoneAccountHandle? connectionManagerPhoneAccount,
        ConnectionRequest? request)
    {
        var connection = new OpenTokConnection();

        // Allowed: show the caller's address rather than hiding or withholding it.
        connection.SetAddress(request?.Address, global::Android.Telecom.Presentation.Allowed);

        // Ringing, not Active: the system shows the incoming-call affordance and waits for the
        // user. Setting Active here would connect the call without anyone answering it.
        connection.SetRinging();

        Current = connection;
        return connection;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Not used by this sample, which only demonstrates the incoming direction — but returning null
    /// (the base behaviour) would leave an outgoing call hanging, so it is answered explicitly.
    /// </remarks>
    public override Connection? OnCreateOutgoingConnection(
        PhoneAccountHandle? connectionManagerPhoneAccount,
        ConnectionRequest? request)
    {
        var connection = new OpenTokConnection();

        connection.SetAddress(request?.Address, global::Android.Telecom.Presentation.Allowed);
        connection.SetDialing();

        Current = connection;
        return connection;
    }

    internal static void Clear(OpenTokConnection connection)
    {
        if (ReferenceEquals(Current, connection))
        {
            Current = null;
        }
    }
}

/// <summary>
/// One telecom call. The framework calls into this; the app listens through
/// <see cref="Answered"/> and <see cref="Ended"/>.
/// </summary>
/// <remarks>
/// Deliberately holds no OpenTok state. Telecom callbacks arrive on the framework's thread and the
/// session has to be driven on the app's, so this raises events and lets the page decide — which
/// also keeps the sample's OpenTok code in one readable place.
/// </remarks>
public sealed class OpenTokConnection : Connection
{
    public OpenTokConnection()
    {
        // Tells the framework this app draws its own in-call UI, and that it can handle the audio
        // routing itself. Without the self-managed capability the system expects a dialer.
        ConnectionProperties = global::Android.Telecom.ConnectionProperties.SelfManaged;

        // No capabilities: the framework offers hold, mute and the rest only when the connection
        // advertises them, and this sample implements none of those. Declaring SupportHold without
        // an OnHold override produces a hold button that silently does nothing.
        ConnectionCapabilities = 0;

        AudioModeIsVoip = true;
    }

    /// <summary>Raised when the user answers, from the system UI or the notification.</summary>
    public event EventHandler? Answered;

    /// <summary>Raised when the call ends, whoever ended it.</summary>
    public event EventHandler? Ended;

    /// <inheritdoc />
    public override void OnAnswer()
    {
        // Active before the media exists: the framework owns the call's state machine, and leaving
        // it Ringing while OpenTok connects makes the system think the user never answered.
        SetActive();

        // Configure audio for the call. A no-op on this platform — Android's focus request is one
        // step — but called anyway so the two samples' lifecycles read the same.
        OpenTokAudioSession.PrepareForCall();
        OpenTokAudioSession.NotifyActivated();

        Answered?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public override void OnReject()
    {
        SetDisconnected(new DisconnectCause(Causes.Rejected));
        Finish();
    }

    /// <inheritdoc />
    public override void OnDisconnect()
    {
        SetDisconnected(new DisconnectCause(Causes.Local));
        Finish();
    }

    /// <inheritdoc />
    /// <remarks>
    /// The framework aborted the call before it was answered — a second incoming call taking
    /// priority, for instance. Ends the same way, but without a disconnect cause the user chose.
    /// </remarks>
    public override void OnAbort()
    {
        SetDisconnected(new DisconnectCause(Causes.Canceled));
        Finish();
    }

    private void Finish()
    {
        OpenTokAudioSession.NotifyDeactivated();

        Ended?.Invoke(this, EventArgs.Empty);

        OpenTokConnectionService.Clear(this);

        // Mandatory. A Connection that is disconnected but never destroyed leaks in the framework
        // and blocks the next call on this account.
        Destroy();
    }
}
