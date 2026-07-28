using System.Text;
using OpenTok.Net;

namespace OpenTok.Sample.Maui;

/// <summary>
/// Connects to an OpenTok session, publishes this device's camera and microphone, and subscribes to
/// the first remote stream.
/// </summary>
/// <remarks>
/// <para>
/// The point of this file is what is <em>not</em> in it. There is no <c>#if IOS</c>, no
/// <c>#if ANDROID</c>, no video-view handler, no delegate subclass, no listener implementation, and
/// no <c>JavaCast</c> — compare the per-platform samples in the two binding repositories, each of
/// which needs all of that. Everything here compiles for both platforms from one source.
/// </para>
/// <para>
/// It is a deliberately faithful port of those two samples rather than a smaller demo, so the three
/// can be read against each other and the difference is the API, not the scope.
/// </para>
/// </remarks>
public partial class MainPage : ContentPage
{
    private readonly StringBuilder _status = new();

    private OpenTokSession? _session;
    private OpenTokPublisher? _publisher;
    private OpenTokSubscriber? _subscriber;

    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnConnectClicked(object? sender, EventArgs e)
    {
        var apiKey = ApiKeyEntry.Text?.Trim();
        var sessionId = SessionIdEntry.Text?.Trim();
        var token = TokenEntry.Text?.Trim();

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(token))
        {
            Append("enter an API key, a session id and a token first");
            return;
        }

        // The façade deliberately does not request permissions — when to ask, and what to do when
        // refused, is a UI decision. MAUI's Permissions API covers both platforms here.
        if (!await RequestCapturePermissionsAsync())
        {
            Append("camera and microphone permission denied");
            return;
        }

        var session = new OpenTokSession(apiKey, sessionId);
        session.Connected += OnSessionConnected;
        session.Disconnected += OnSessionDisconnected;
        session.Failed += OnSessionFailed;
        session.StreamReceived += OnStreamReceived;
        session.StreamDropped += OnStreamDropped;

        _session = session;
        ConnectButton.IsEnabled = false;
        Append("connecting…");

        // Asynchronous on both platforms: the outcome arrives on Connected or Failed.
        session.Connect(token);
    }

    private void OnDisconnectClicked(object? sender, EventArgs e)
    {
        TeardownPublisher();
        TeardownSubscriber();
        TeardownSession();

        SetConnected(false);
        Append("disconnected");
    }

    private void OnPublishClicked(object? sender, EventArgs e)
    {
        if (_session is null)
        {
            return;
        }

        var publisher = new OpenTokPublisher("opentok-net-sample");
        publisher.StreamCreated += OnPublisherStreamCreated;
        publisher.Failed += OnPublisherFailed;

        // The preview exists from construction, so the view can be pointed at it immediately —
        // OpenTokVideoView attaches the native view itself.
        LocalView.Source = publisher;

        _publisher = publisher;
        PublishButton.IsEnabled = false;

        _session.Publish(publisher);
        Append("publishing");
    }

    private void OnSessionConnected(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SetConnected(true);
            Append("connected");
        });

    private void OnSessionDisconnected(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SetConnected(false);
            Append("session disconnected");
        });

    private void OnSessionFailed(object? sender, OpenTokErrorEventArgs e) =>
        Append($"session error: {e.Error}");

    /// <summary>
    /// Subscribes to the first remote stream. There is one remote view in this sample, so later
    /// streams are logged and ignored.
    /// </summary>
    private void OnStreamReceived(object? sender, OpenTokStreamEventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Append($"remote stream created: {e.Stream}");

            if (_subscriber is not null || _session is null)
            {
                return;
            }

            var subscriber = new OpenTokSubscriber(e.Stream);
            subscriber.Failed += OnSubscriberFailed;

            // Assigned before the subscriber has any video: OpenTokVideoView waits for the
            // subscriber to report one rather than requiring the app to time this.
            RemoteView.Source = subscriber;

            _subscriber = subscriber;
            _session.Subscribe(subscriber);
        });

    private void OnStreamDropped(object? sender, OpenTokStreamEventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Append($"remote stream destroyed: {e.Stream.StreamId}");

            if (_subscriber?.Stream.StreamId == e.Stream.StreamId)
            {
                TeardownSubscriber();
            }
        });

    private void OnPublisherStreamCreated(object? sender, OpenTokStreamEventArgs e) =>
        Append("local stream published");

    private void OnPublisherFailed(object? sender, OpenTokErrorEventArgs e) =>
        Append($"publisher error: {e.Error}");

    private void OnSubscriberFailed(object? sender, OpenTokErrorEventArgs e) =>
        Append($"subscriber error: {e.Error}");

    private static async Task<bool> RequestCapturePermissionsAsync()
    {
        var camera = await Permissions.RequestAsync<Permissions.Camera>();
        var microphone = await Permissions.RequestAsync<Permissions.Microphone>();

        return camera == PermissionStatus.Granted && microphone == PermissionStatus.Granted;
    }

    private void SetConnected(bool connected)
    {
        ConnectButton.IsEnabled = !connected;
        DisconnectButton.IsEnabled = connected;
        PublishButton.IsEnabled = connected && _publisher is null;
    }

    private void TeardownPublisher()
    {
        if (_publisher is null)
        {
            return;
        }

        _publisher.StreamCreated -= OnPublisherStreamCreated;
        _publisher.Failed -= OnPublisherFailed;

        // Clear the view before disposing what it was showing.
        LocalView.Source = null;

        _session?.Unpublish(_publisher);
        _publisher.Dispose();
        _publisher = null;

        PublishButton.IsEnabled = _session is not null;
    }

    private void TeardownSubscriber()
    {
        if (_subscriber is null)
        {
            return;
        }

        _subscriber.Failed -= OnSubscriberFailed;
        RemoteView.Source = null;

        _session?.Unsubscribe(_subscriber);
        _subscriber.Dispose();
        _subscriber = null;
    }

    private void TeardownSession()
    {
        if (_session is null)
        {
            return;
        }

        _session.Connected -= OnSessionConnected;
        _session.Disconnected -= OnSessionDisconnected;
        _session.Failed -= OnSessionFailed;
        _session.StreamReceived -= OnStreamReceived;
        _session.StreamDropped -= OnStreamDropped;

        _session.Dispose();
        _session = null;
    }

    private void Append(string message) => MainThread.BeginInvokeOnMainThread(() =>
    {
        _status.AppendLine($"{DateTime.Now:HH:mm:ss}  {message}");
        StatusLabel.Text = _status.ToString();
        StatusScroll.ScrollToAsync(0, StatusLabel.Height, animated: false);
    });

    /// <summary>
    /// Releases the camera and the session when the page goes away.
    /// </summary>
    /// <remarks>
    /// Both platforms need this, for the same reason and now through the same call: a publisher
    /// that is never disposed holds the camera, and on Android that blocks the next one the app
    /// creates.
    /// </remarks>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        TeardownPublisher();
        TeardownSubscriber();
        TeardownSession();
    }
}
