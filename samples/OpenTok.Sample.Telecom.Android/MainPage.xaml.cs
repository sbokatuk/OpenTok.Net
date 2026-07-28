using System.Text;
using Android.Telecom;
using OpenTok.Net;

namespace OpenTok.Sample.Telecom.Android;

/// <summary>
/// An OpenTok call driven by <c>android.telecom</c> rather than by buttons in the app, with a
/// foreground service so capture survives backgrounding.
/// </summary>
/// <remarks>
/// <para>
/// The interesting parts are <see cref="OpenTokConnectionService"/> and
/// <see cref="OpenTokCaptureService"/>. This page never answers the call itself — the framework
/// does, through the system's call UI, and the session is connected from the connection's
/// <c>Answered</c> event.
/// </para>
/// <para>
/// Android-only on purpose; <c>samples/OpenTok.Sample.CallKit.iOS</c> is the other half. The one
/// thing both share is <see cref="OpenTokAudioSession"/>.
/// </para>
/// </remarks>
public partial class MainPage : ContentPage
{
    private readonly StringBuilder _status = new();

    private PhoneAccountHandle? _accountHandle;
    private OpenTokConnection? _connection;

    private OpenTokSession? _session;
    private OpenTokPublisher? _publisher;
    private OpenTokSubscriber? _subscriber;

    public MainPage()
    {
        InitializeComponent();

        // Once, before any call: tell the SDK the telecom framework owns audio focus. Everything
        // else here is undone by forgetting this one line — the SDK and telecom end up fighting
        // over focus and the call has no sound.
        OpenTokAudioSession.EnableCallingServicesMode();

        Append("ready — telecom is in charge of audio focus");
    }

    private async void OnReportClicked(object? sender, EventArgs e)
    {
        var apiKey = ApiKeyEntry.Text?.Trim();
        var sessionId = SessionIdEntry.Text?.Trim();
        var token = TokenEntry.Text?.Trim();

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(token))
        {
            Append("enter an API key, a session id and a token first");
            return;
        }

        if (!await RequestPermissionsAsync())
        {
            Append("camera, microphone or phone permission denied");
            return;
        }

        var context = Platform.CurrentActivity ?? global::Android.App.Application.Context;

        // Registration is idempotent; doing it here rather than at launch keeps the sample's
        // ordering visible — an account must exist before a call can be attributed to it.
        _accountHandle ??= OpenTokConnectionService.Register(context);

        var session = new OpenTokSession(apiKey, sessionId);
        session.Connected += OnSessionConnected;
        session.Disconnected += OnSessionDisconnected;
        session.Failed += OnSessionFailed;
        session.StreamReceived += OnStreamReceived;
        session.StreamDropped += OnStreamDropped;

        _session = session;

        ReportButton.IsEnabled = false;
        EndButton.IsEnabled = true;

        OpenTokConnectionService.ReportIncomingCall(
            context,
            _accountHandle,
            CallerEntry.Text?.Trim() is { Length: > 0 } caller ? caller : "0000000");

        // The framework creates the Connection asynchronously, so it is not available immediately.
        // Polling once on the next tick is enough for a sample; a real app would have the service
        // publish it.
        Dispatcher.Dispatch(() => AttachToConnection(token));

        Append("incoming call reported — answer it from the system UI");
    }

    private void AttachToConnection(string token)
    {
        if (OpenTokConnectionService.Current is not { } connection)
        {
            // Not yet bound. Try again shortly rather than failing — the framework binds the
            // service on its own schedule.
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(200), () => AttachToConnection(token));
            return;
        }

        _connection = connection;

        connection.Answered += (_, _) => MainThread.BeginInvokeOnMainThread(() =>
        {
            Append("answered — connecting");

            // Started here, not at report time: the service holds the process in the foreground
            // for the duration of the call, and there is no call to hold up until it is answered.
            OpenTokCaptureService.Start(global::Android.App.Application.Context);

            _session?.Connect(token);
        });

        connection.Ended += (_, _) => MainThread.BeginInvokeOnMainThread(() =>
        {
            Append("call ended");
            Teardown();
        });
    }

    private void OnEndClicked(object? sender, EventArgs e)
    {
        // Through the connection rather than straight to the session: the framework has to be told,
        // or the call stays "active" in the system's own state with nothing behind it.
        _connection?.OnDisconnect();
    }

    private void OnSessionConnected(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Append("connected — publishing");

            var publisher = new OpenTokPublisher("telecom-sample");
            publisher.Failed += (_, args) => Append($"publisher error: {args.Error}");

            LocalView.Source = publisher;
            _publisher = publisher;

            _session?.Publish(publisher);
        });

    private void OnSessionDisconnected(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() => Append("session disconnected"));

    private void OnSessionFailed(object? sender, OpenTokErrorEventArgs e) =>
        Append($"session error: {e.Error}");

    private void OnStreamReceived(object? sender, OpenTokStreamEventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_subscriber is not null || _session is null)
            {
                return;
            }

            var subscriber = new OpenTokSubscriber(e.Stream);
            subscriber.Failed += (_, args) => Append($"subscriber error: {args.Error}");

            RemoteView.Source = subscriber;
            _subscriber = subscriber;

            _session.Subscribe(subscriber);
            Append("subscribed");
        });

    private void OnStreamDropped(object? sender, OpenTokStreamEventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_subscriber?.Stream.StreamId != e.Stream.StreamId)
            {
                return;
            }

            RemoteView.Source = null;
            _session?.Unsubscribe(_subscriber);
            _subscriber.Dispose();
            _subscriber = null;
        });

    private static async Task<bool> RequestPermissionsAsync()
    {
        var camera = await Permissions.RequestAsync<Permissions.Camera>();
        var microphone = await Permissions.RequestAsync<Permissions.Microphone>();

        // READ_PHONE_STATE is what telecom checks before letting a self-managed account place or
        // receive calls. Granted separately from the capture permissions, and refused silently by
        // TelecomManager if missing.
        var phone = await Permissions.RequestAsync<Permissions.Phone>();

        return camera == PermissionStatus.Granted
            && microphone == PermissionStatus.Granted
            && phone == PermissionStatus.Granted;
    }

    private void Teardown()
    {
        LocalView.Source = null;
        RemoteView.Source = null;

        if (_publisher is not null)
        {
            _session?.Unpublish(_publisher);
            _publisher.Dispose();
            _publisher = null;
        }

        if (_subscriber is not null)
        {
            _session?.Unsubscribe(_subscriber);
            _subscriber.Dispose();
            _subscriber = null;
        }

        _session?.Dispose();
        _session = null;
        _connection = null;

        // Stopped only now: while it runs the process stays foreground and holds the camera.
        OpenTokCaptureService.Stop(global::Android.App.Application.Context);

        ReportButton.IsEnabled = true;
        EndButton.IsEnabled = false;
    }

    private void Append(string message) => MainThread.BeginInvokeOnMainThread(() =>
    {
        _status.AppendLine($"{DateTime.Now:HH:mm:ss}  {message}");
        StatusLabel.Text = _status.ToString();
        StatusScroll.ScrollToAsync(0, StatusLabel.Height, animated: false);
    });

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _connection?.OnDisconnect();
        Teardown();
    }
}
