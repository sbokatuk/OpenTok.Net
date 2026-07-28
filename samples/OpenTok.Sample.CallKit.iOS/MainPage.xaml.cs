using System.Text;
using OpenTok.Net;

namespace OpenTok.Sample.CallKit.iOS;

/// <summary>
/// An OpenTok call driven by CallKit rather than by buttons in the app.
/// </summary>
/// <remarks>
/// <para>
/// The interesting part is not this page — it is <see cref="CallKitCallManager"/>, and the fact
/// that this page never connects the session itself. Answering happens in the system's call UI,
/// and the session is connected from CallKit's audio-activation callback.
/// </para>
/// <para>
/// This is an iOS-only sample on purpose. <c>CXProvider</c> and Android's
/// <c>ConnectionService</c> have nothing in common beyond their purpose, so
/// <c>samples/OpenTok.Sample.Telecom.Android</c> is a separate app rather than an
/// <c>#if ANDROID</c> branch of this one. What <em>is</em> shared is the audio handshake —
/// <see cref="OpenTokAudioSession"/> — which both samples call identically.
/// </para>
/// </remarks>
public partial class MainPage : ContentPage
{
    private readonly StringBuilder _status = new();

    private CallKitCallManager? _callKit;
    private OpenTokSession? _session;
    private OpenTokPublisher? _publisher;
    private OpenTokSubscriber? _subscriber;

    public MainPage()
    {
        InitializeComponent();

        // Once, before any call: tell the SDK a calling service owns the audio session. Everything
        // else CallKit does is undone by forgetting this one line.
        OpenTokAudioSession.EnableCallingServicesMode();

        _callKit = new CallKitCallManager(() => _session, Append);

        Append("ready — CallKit is in charge of audio");
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

        if (!await RequestCapturePermissionsAsync())
        {
            Append("camera and microphone permission denied");
            return;
        }

        // Built now, connected later. In a real app this whole method is the body of
        // DidReceiveIncomingPush and the credentials arrive in the push payload.
        var session = new OpenTokSession(apiKey, sessionId);
        session.Connected += OnSessionConnected;
        session.Disconnected += OnSessionDisconnected;
        session.Failed += OnSessionFailed;
        session.StreamReceived += OnStreamReceived;
        session.StreamDropped += OnStreamDropped;

        _session = session;

        ReportButton.IsEnabled = false;
        EndButton.IsEnabled = true;

        _callKit?.ReportIncomingCall(
            CallerEntry.Text?.Trim() is { Length: > 0 } caller ? caller : "Unknown",
            connect: () =>
            {
                // Reached from DidActivateAudioSession, not from the answer action.
                Append("connecting…");
                session.Connect(token);
            });
    }

    private void OnEndClicked(object? sender, EventArgs e)
    {
        // Through CallKit rather than straight to the session: the system's call UI has to be told,
        // or the call stays "active" in Recents and on the lock screen with nothing behind it.
        _callKit?.EndCall();
    }

    private void OnSessionConnected(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Append("connected — publishing");

            var publisher = new OpenTokPublisher("callkit-sample");
            publisher.Failed += (_, args) => Append($"publisher error: {args.Error}");

            LocalView.Source = publisher;
            _publisher = publisher;

            _session?.Publish(publisher);
        });

    private void OnSessionDisconnected(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Teardown();
            Append("disconnected");
        });

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

    private static async Task<bool> RequestCapturePermissionsAsync()
    {
        var camera = await Permissions.RequestAsync<Permissions.Camera>();
        var microphone = await Permissions.RequestAsync<Permissions.Microphone>();

        return camera == PermissionStatus.Granted && microphone == PermissionStatus.Granted;
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

        // End through CallKit first, so the system call goes away with the app's own state.
        _callKit?.EndCall();
        Teardown();

        _callKit?.Dispose();
        _callKit = null;
    }
}
