using System.Text;
using OpenTok.Net;
using OpenTok.Net.Maui;

namespace OpenTok.Sample.Maui;

/// <summary>
/// A full OpenTok call: connect, publish, subscribe to every remote participant, chat over
/// signalling, drive the camera, and apply background transformers.
/// </summary>
/// <remarks>
/// <para>
/// The point of this file is what is <em>not</em> in it. There is no <c>#if IOS</c>, no
/// <c>#if ANDROID</c>, no video-view handler, no delegate subclass, no Java listener, and no
/// <c>JavaCast</c> — compare the per-platform samples in the two binding repositories, each of
/// which needs all of that for a fraction of these features.
/// </para>
/// <para>
/// <b>The transformers are deliberately unreferenced.</b> This project does not depend on
/// <c>OpenTok.Net.Transformers.iOS</c> / <c>.Android</c>, so the Blur and Suppress noise buttons
/// demonstrate the API <em>and</em> what happens without the package: the SDK reports that the
/// transformers library is not loaded. Adding either package to this .csproj makes them work, at a
/// cost of ~70 MB. That is the whole trade the split exists to offer, and it seemed more honest to
/// show it than to bundle the payload into a sample that is mostly about something else.
/// </para>
/// </remarks>
public partial class MainPage : ContentPage
{
    private readonly StringBuilder _status = new();

    /// <summary>Every remote participant, keyed by stream id — this is a multiparty call.</summary>
    private readonly Dictionary<string, Remote> _remotes = [];

    private OpenTokSession? _session;
    private OpenTokPublisher? _publisher;

    public MainPage()
    {
        InitializeComponent();
    }

    /// <summary>One remote participant: their subscriber and the view showing them.</summary>
    private sealed record Remote(OpenTokSubscriber Subscriber, View Container, OpenTokVideoView Video, ProgressBar Level);

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
        session.SignalReceived += OnSignalReceived;
        session.ConnectionCreated += OnConnectionCreated;
        session.ConnectionDestroyed += OnConnectionDestroyed;
        session.ArchiveStarted += OnArchiveStarted;
        session.ArchiveStopped += OnArchiveStopped;
        session.Reconnecting += OnReconnecting;
        session.Reconnected += OnReconnected;
        session.MuteForced += OnSessionMuteForced;

        _session = session;
        ConnectButton.IsEnabled = false;
        Append("connecting…");

        session.Connect(token);
    }

    private void OnDisconnectClicked(object? sender, EventArgs e)
    {
        TeardownPublisher();
        TeardownAllRemotes();
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
        publisher.MuteForced += OnPublisherMuteForced;
        publisher.AudioLevel += OnPublisherAudioLevel;

        // The preview exists from construction, so the view can be pointed at it immediately.
        LocalView.Source = publisher;

        _publisher = publisher;
        PublishButton.IsEnabled = false;
        SetPublishing(true);

        // Android refuses camera and microphone access to a background app without this, silently
        // — see CaptureLifetime. Started when publishing starts, not when the app backgrounds:
        // by then it is too late to ask.
        CaptureLifetime.Begin();

        _session.Publish(publisher);
        Append("publishing");
    }

    // ---- camera -----------------------------------------------------------------------------

    private void OnSwapCameraClicked(object? sender, EventArgs e)
    {
        if (_publisher is null)
        {
            return;
        }

        _publisher.SwapCamera();
        Append($"camera: {_publisher.CameraPosition}");

        // Front cameras generally have no torch, and the SDK simply ignores the request there —
        // so the button is reset rather than left claiming a torch that is not on.
        TorchButton.Text = "Torch on";
    }

    private void OnTorchClicked(object? sender, EventArgs e)
    {
        if (_publisher is null)
        {
            return;
        }

        _publisher.CameraTorch = !_publisher.CameraTorch;
        TorchButton.Text = _publisher.CameraTorch ? "Torch off" : "Torch on";

        // Read back rather than reporting what was asked for: this is a preference, and the active
        // camera decides.
        Append($"torch: {_publisher.CameraTorch}");
    }

    private void OnZoomChanged(object? sender, ValueChangedEventArgs e)
    {
        ZoomLabel.Text = $"Zoom {e.NewValue:0.0}×";

        if (_publisher is not null)
        {
            _publisher.CameraZoomFactor = (float)e.NewValue;
        }
    }

    private void OnMuteClicked(object? sender, EventArgs e)
    {
        if (_publisher is null)
        {
            return;
        }

        _publisher.PublishAudio = !_publisher.PublishAudio;
        MuteButton.Text = _publisher.PublishAudio ? "Mute" : "Unmute";
    }

    // ---- transformers -----------------------------------------------------------------------

    private void OnBlurClicked(object? sender, EventArgs e)
    {
        if (_publisher is null)
        {
            return;
        }

        // Without a transformers package this is where the SDK reports that the library is not
        // loaded — through the publisher's Failed event, not as an exception here. See the class
        // remarks.
        _publisher.SetVideoTransformers([OpenTokTransformer.BackgroundBlur(OpenTokBlurRadius.High)]);
        Append("background blur requested (needs the transformers package)");
    }

    private void OnNoiseClicked(object? sender, EventArgs e)
    {
        if (_publisher is null)
        {
            return;
        }

        _publisher.SetAudioTransformers([OpenTokTransformer.NoiseSuppression()]);
        Append("noise suppression requested (needs the transformers package)");
    }

    private void OnClearTransformersClicked(object? sender, EventArgs e)
    {
        if (_publisher is null)
        {
            return;
        }

        _publisher.SetVideoTransformers([]);
        _publisher.SetAudioTransformers([]);
        Append("transformers cleared");
    }

    // ---- signalling -------------------------------------------------------------------------

    private void OnSendClicked(object? sender, EventArgs e)
    {
        var message = MessageEntry.Text?.Trim();
        if (_session is null || string.IsNullOrEmpty(message))
        {
            return;
        }

        // Type and payload. The type lets a receiver tell chat from anything else the app sends
        // over the same channel.
        _session.Signal("chat", message);
        MessageEntry.Text = string.Empty;
    }

    private void OnSignalReceived(object? sender, OpenTokSignalEventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (e.Type != "chat")
            {
                return;
            }

            // Both SDKs deliver a signal back to its sender, so without this check every message
            // this client sends appears twice. The façade works out FromSelf; the app decides what
            // to do about it — here, labelling it rather than dropping it.
            var who = e.FromSelf ? "me" : e.From?.ConnectionId[..8] ?? "someone";
            Append($"[{who}] {e.Data}");
        });

    // ---- session events ---------------------------------------------------------------------

    private void OnSessionConnected(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SetConnected(true);

            // Only meaningful once connected — the token's role is what decides it.
            Append($"connected; token allows {_session?.Capabilities}");
        });

    private void OnSessionDisconnected(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SetConnected(false);
            Append("session disconnected");
        });

    private void OnSessionFailed(object? sender, OpenTokErrorEventArgs e) =>
        Append($"session error: {e.Error}");

    private void OnConnectionCreated(object? sender, OpenTokConnectionEventArgs e) =>
        Append($"joined: {e.Connection.ConnectionId[..8]}");

    private void OnConnectionDestroyed(object? sender, OpenTokConnectionEventArgs e) =>
        Append($"left: {e.Connection.ConnectionId[..8]}");

    private void OnArchiveStarted(object? sender, OpenTokArchiveEventArgs e) =>
        Append($"recording started: {e.ArchiveName ?? e.ArchiveId}");

    private void OnArchiveStopped(object? sender, OpenTokArchiveEventArgs e) =>
        Append($"recording stopped: {e.ArchiveId}");

    // Not a disconnect — the SDK is recovering, and media is interrupted meanwhile. Worth showing,
    // because otherwise the app looks frozen.
    private void OnReconnecting(object? sender, EventArgs e) => Append("reconnecting…");

    private void OnReconnected(object? sender, EventArgs e) => Append("reconnected");

    private void OnSessionMuteForced(object? sender, OpenTokMuteForcedEventArgs e) =>
        Append(e.Active ? "a moderator muted the session" : "the session mute state was lifted");

    // ---- publisher events -------------------------------------------------------------------

    private void OnPublisherStreamCreated(object? sender, OpenTokStreamEventArgs e) =>
        Append("local stream published");

    private void OnPublisherFailed(object? sender, OpenTokErrorEventArgs e) =>
        Append($"publisher error: {e.Error}");

    private void OnPublisherMuteForced(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // PublishAudio is already false — the SDK muted us. Reflect it rather than showing an
            // unmuted mic.
            MuteButton.Text = "Unmute";
            Append("a moderator muted this publisher");
        });

    private void OnPublisherAudioLevel(object? sender, OpenTokAudioLevelEventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() => LocalLevel.Progress = e.Level);

    // ---- remote participants ----------------------------------------------------------------

    /// <summary>
    /// Subscribes to every remote stream, one view each — a real multiparty call rather than the
    /// "first stream only" shortcut the platform samples take.
    /// </summary>
    private void OnStreamReceived(object? sender, OpenTokStreamEventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_session is null || _remotes.ContainsKey(e.Stream.StreamId))
            {
                return;
            }

            var subscriber = new OpenTokSubscriber(e.Stream);
            subscriber.Failed += OnSubscriberFailed;
            subscriber.Caption += OnSubscriberCaption;

            var video = new OpenTokVideoView { HeightRequest = 160, Source = subscriber };
            var level = new ProgressBar { WidthRequest = 140, Rotation = 270 };

            subscriber.AudioLevel += (_, args) =>
                MainThread.BeginInvokeOnMainThread(() => level.Progress = args.Level);

            var container = new Grid
            {
                ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)],
                ColumnSpacing = 8,
            };
            container.Add(video, 0);
            container.Add(level, 1);

            RemoteViews.Add(container);
            _remotes[e.Stream.StreamId] = new Remote(subscriber, container, video, level);

            _session.Subscribe(subscriber);

            UpdateRemoteHeading();
            Append($"subscribed to {e.Stream}");
        });

    private void OnStreamDropped(object? sender, OpenTokStreamEventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            TeardownRemote(e.Stream.StreamId);
            Append($"remote stream ended: {e.Stream.StreamId}");
        });

    private void OnSubscriberFailed(object? sender, OpenTokErrorEventArgs e) =>
        Append($"subscriber error: {e.Error}");

    private void OnSubscriberCaption(object? sender, OpenTokCaptionEventArgs e)
    {
        // Only final lines: interim ones arrive continuously while someone speaks, and appending
        // every one of them fills the log with half-sentences.
        if (e.IsFinal)
        {
            Append($"caption: {e.Text}");
        }
    }

    // ---- teardown ---------------------------------------------------------------------------

    // Behind a shim rather than calling MAUI's Permissions API directly, because Windows has no
    // runtime prompt to make — see CapturePermissions. Still one call from here, and still no
    // platform branching in this file.
    private static Task<bool> RequestCapturePermissionsAsync() => CapturePermissions.RequestAsync();

    private void SetConnected(bool connected)
    {
        ConnectButton.IsEnabled = !connected;
        DisconnectButton.IsEnabled = connected;
        PublishButton.IsEnabled = connected && _publisher is null;

        MessageEntry.IsEnabled = connected;
        SendButton.IsEnabled = connected;
    }

    private void SetPublishing(bool publishing)
    {
        SwapCameraButton.IsEnabled = publishing;
        TorchButton.IsEnabled = publishing;
        MuteButton.IsEnabled = publishing;
        ZoomSlider.IsEnabled = publishing;
        BlurButton.IsEnabled = publishing;
        NoiseButton.IsEnabled = publishing;
        ClearTransformersButton.IsEnabled = publishing;
    }

    /// <summary>
    /// Tells the SDK the app has gone to the background. Called from the window's Deactivated
    /// event — see App.xaml.cs.
    /// </summary>
    /// <remarks>
    /// Deliberately does <em>not</em> stop the foreground service: the service is what makes
    /// capturing in the background legal at all, and stopping it here would produce exactly the
    /// failure it exists to prevent.
    /// </remarks>
    public void PauseSession() => _session?.Pause();

    /// <summary>Tells the SDK the app is back in the foreground.</summary>
    public void ResumeSession() => _session?.Resume();

    private void UpdateRemoteHeading() => RemoteHeading.Text = $"Remote ({_remotes.Count})";

    private void TeardownPublisher()
    {
        if (_publisher is null)
        {
            return;
        }

        _publisher.StreamCreated -= OnPublisherStreamCreated;
        _publisher.Failed -= OnPublisherFailed;
        _publisher.MuteForced -= OnPublisherMuteForced;
        _publisher.AudioLevel -= OnPublisherAudioLevel;

        // Clear the view before disposing what it was showing.
        LocalView.Source = null;

        _session?.Unpublish(_publisher);
        _publisher.Dispose();
        _publisher = null;

        CaptureLifetime.End();

        SetPublishing(false);
        LocalLevel.Progress = 0;
        PublishButton.IsEnabled = _session is not null;
    }

    private void TeardownRemote(string streamId)
    {
        if (!_remotes.Remove(streamId, out var remote))
        {
            return;
        }

        remote.Subscriber.Failed -= OnSubscriberFailed;
        remote.Subscriber.Caption -= OnSubscriberCaption;

        remote.Video.Source = null;
        RemoteViews.Remove(remote.Container);

        _session?.Unsubscribe(remote.Subscriber);
        remote.Subscriber.Dispose();

        UpdateRemoteHeading();
    }

    private void TeardownAllRemotes()
    {
        // Materialised first: TeardownRemote mutates the dictionary.
        foreach (var streamId in _remotes.Keys.ToList())
        {
            TeardownRemote(streamId);
        }
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
        _session.SignalReceived -= OnSignalReceived;
        _session.ConnectionCreated -= OnConnectionCreated;
        _session.ConnectionDestroyed -= OnConnectionDestroyed;
        _session.ArchiveStarted -= OnArchiveStarted;
        _session.ArchiveStopped -= OnArchiveStopped;
        _session.Reconnecting -= OnReconnecting;
        _session.Reconnected -= OnReconnected;
        _session.MuteForced -= OnSessionMuteForced;

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
        TeardownAllRemotes();
        TeardownSession();
    }
}
