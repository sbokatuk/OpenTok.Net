#if ANDROID
using Android.Runtime;
using Com.Opentok.Android;
#endif
#if IOS
using OpenTok.Net.iOS;
using UIKit;
#endif
using System.Text;

namespace OpenTok.Maui.Sample;

/// <summary>
/// Connects to an OpenTok session, publishes this device's camera/microphone, and subscribes to
/// the first remote stream — built directly against each platform's own native binding surface
/// rather than a shared C# API.
///
/// OpenTok.Net is a façade over two unrelated native SDKs, not a cross-platform binding — see
/// <c>docs/native-surface.md</c>. <c>Com.Opentok.Android.Session</c> and <c>OTSession</c> have
/// nothing in common beyond both existing, so this file cannot be "one set of calls that happens
/// to compile on both heads". Each platform's connect/publish/subscribe flow is guarded by
/// <c>#if ANDROID</c> / <c>#if IOS</c>, the same way
/// <c>tests/OpenTok.Net.DeviceTests/SmokeTests.cs</c> and the platform-specific samples in
/// OpenTok.Net.Android and OpenTok.Net.iOS are.
/// </summary>
public partial class MainPage : ContentPage
{
    private readonly StringBuilder _status = new();

#if ANDROID
    private Session? _session;
    private Publisher? _publisher;
    private SubscriberKit? _subscriber;
#elif IOS
    private OTSession? _session;
    private SessionDelegate? _sessionDelegate;
    private OTPublisher? _publisher;
    private PublisherDelegate? _publisherDelegate;
    private OTSubscriber? _subscriber;
    private SubscriberDelegate? _subscriberDelegate;
#endif

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

        if (!await RequestCapturePermissionsAsync())
        {
            Append("camera and microphone permission denied");
            return;
        }

#if ANDROID
        var context = Platform.CurrentActivity ?? global::Android.App.Application.Context;
        var session = new Session.Builder(context, apiKey, sessionId).Build();

        session.Connected += (_, _) => MainThread.BeginInvokeOnMainThread(() =>
        {
            SetConnected(true);
            Append("connected");
        });
        session.Disconnected += (_, _) => MainThread.BeginInvokeOnMainThread(() =>
        {
            SetConnected(false);
            Append("session disconnected");
        });
        session.Error += (_, args) => Append($"session error: {args.P1.Message}");
        session.StreamDropped += (_, args) => MainThread.BeginInvokeOnMainThread(() =>
        {
            Append($"remote stream dropped: {args.P1.StreamId}");
            if (_subscriber?.Stream?.StreamId == args.P1.StreamId)
            {
                TeardownSubscriber();
            }
        });
        session.StreamReceived += (_, args) => MainThread.BeginInvokeOnMainThread(() =>
        {
            var stream = args.P1;
            Append($"remote stream received: {stream.StreamId}");

            // Only one remote view in this sample — the first stream gets it.
            if (_subscriber is not null)
            {
                return;
            }

            var subscriber = new Subscriber.Builder(context, stream).Build();
            subscriber.Connected += (_, _) => MainThread.BeginInvokeOnMainThread(() =>
            {
                var remoteHandler = (OpenTokVideoViewHandler)RemoteView.Handler!;
                remoteHandler.SetChild(subscriber.View!);
                Append("subscribed — remote video attached");
            });
            subscriber.Error += (_, subArgs) => Append($"subscriber error: {subArgs.P1.Message}");

            session.Subscribe(subscriber);
            _subscriber = subscriber;
        });

        _session = session;
        session.Connect(token);
        ConnectButton.IsEnabled = false;
        Append("connecting…");
#elif IOS
        _sessionDelegate = new SessionDelegate(this);
        var session = new OTSession(apiKey, sessionId, _sessionDelegate);
        session.ConnectWithToken(token, out var error);

        if (error is not null)
        {
            Append($"connectWithToken failed: {error.Code} {error.LocalizedDescription}");
            return;
        }

        _session = session;
        ConnectButton.IsEnabled = false;
        Append("connecting…");
#endif
    }

    private void OnDisconnectClicked(object? sender, EventArgs e)
    {
        TeardownPublisher();
        TeardownSubscriber();

#if ANDROID
        _session?.Disconnect();
        _session = null;
#elif IOS
        if (_session is not null)
        {
            _session.Disconnect(out var error);
            if (error is not null)
            {
                Append($"disconnect returned: {error.Code} {error.LocalizedDescription}");
            }
        }

        _session = null;
        _sessionDelegate = null;
#endif

        SetConnected(false);
        Append("disconnected");
    }

    private void OnPublishClicked(object? sender, EventArgs e)
    {
        if (_session is null)
        {
            return;
        }

#if ANDROID
        var context = Platform.CurrentActivity ?? global::Android.App.Application.Context;

        // PublisherKit.Builder.Build() is what Publisher.Builder inherits — Java's own covariant
        // return (the runtime object is a Publisher) does not reach the C# static type, so this
        // needs an explicit cast back to the concrete type.
        var built = new Publisher.Builder(context).Name("opentok-net-maui-sample").Build();
        var publisher = built.JavaCast<Publisher>();
        publisher.Error += (_, args) => Append($"publisher error: {args.P1.Message}");
        publisher.StreamCreated += (_, _) => Append("local stream published");

        // The handler's platform view is a plain container — see OpenTokVideoView.cs for why,
        // unlike Agora's raw SurfaceView, OpenTok hands its own view back only once a
        // publisher/subscriber exists rather than wanting one supplied up front.
        var localHandler = (OpenTokVideoViewHandler)LocalView.Handler!;
        localHandler.SetChild(publisher.View!);

        _session.Publish(publisher);
        _publisher = publisher;
        PublishButton.IsEnabled = false;
        Append("publishing");
#elif IOS
        var settings = new OTPublisherSettings { Name = "opentok-net-maui-sample" };
        _publisherDelegate = new PublisherDelegate(this);
        var publisher = new OTPublisher(_publisherDelegate, settings);

        // OTPublisher.View exists as soon as the publisher does — the camera preview starts
        // rendering into it immediately, before session:publish:error: is ever called. Attaching
        // it here rather than waiting for a delegate callback is what every OpenTok sample does.
        AttachNativeView(publisher.View, LocalView.PlatformNativeView);

        _session.Publish(publisher, out var error);
        if (error is not null)
        {
            Append($"publish failed: {error.Code} {error.LocalizedDescription}");
            publisher.View?.RemoveFromSuperview();
            return;
        }

        _publisher = publisher;
        PublishButton.IsEnabled = false;
        Append("publishing");
#endif
    }

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
        PublishButton.IsEnabled = connected;
    }

    private void TeardownPublisher()
    {
#if ANDROID
        // Empty the container before releasing the view it was showing.
        (LocalView.Handler as OpenTokVideoViewHandler)?.Clear();

        // Releases the camera and the native renderer.
        _publisher?.Destroy();
        _publisher = null;
#elif IOS
        if (_publisher is not null && _session is not null)
        {
            _session.Unpublish(_publisher, out _);
        }

        _publisher?.View?.RemoveFromSuperview();
        _publisher = null;
        _publisherDelegate = null;
#endif
        PublishButton.IsEnabled = false;
    }

    private void TeardownSubscriber()
    {
#if ANDROID
        (RemoteView.Handler as OpenTokVideoViewHandler)?.Clear();

        if (_subscriber is not null)
        {
            _session?.Unsubscribe(_subscriber);
        }

        _subscriber = null;
#elif IOS
        if (_subscriber is not null && _session is not null)
        {
            _session.Unsubscribe(_subscriber, out _);
        }

        _subscriber?.View?.RemoveFromSuperview();
        _subscriber = null;
        _subscriberDelegate = null;
#endif
    }

#if IOS
    /// <summary>
    /// Adds an SDK-owned render view (publisher or subscriber) as a subview of one of this
    /// page's <see cref="OpenTokVideoView"/> containers, sized to fill it. Both views are
    /// SDK-created and read-only.
    /// </summary>
    private static void AttachNativeView(UIView? nativeView, UIView? container)
    {
        if (nativeView is null || container is null)
        {
            return;
        }

        nativeView.Frame = container.Bounds;
        nativeView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
        container.AddSubview(nativeView);
    }
#endif

    private void Append(string message) => MainThread.BeginInvokeOnMainThread(() =>
    {
        _status.AppendLine($"{DateTime.Now:HH:mm:ss}  {message}");
        StatusLabel.Text = _status.ToString();
        StatusScroll.ScrollToAsync(0, StatusLabel.Height, animated: false);
    });

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        // Releases the session and its native views when the page goes away.
        if (Handler is null && _session is not null)
        {
            TeardownPublisher();
            TeardownSubscriber();
#if ANDROID
            _session.Disconnect();
#elif IOS
            _session.Disconnect(out _);
            _sessionDelegate = null;
#endif
            _session = null;
        }
    }

#if IOS
    /// <summary>
    /// Translates <c>OTSessionDelegate</c>'s (all-required) callbacks into UI updates and drives
    /// the subscribe side: the first remote stream this session reports gets subscribed to and
    /// rendered into <see cref="RemoteView"/>.
    /// </summary>
    private sealed class SessionDelegate(MainPage owner) : OTSessionDelegate
    {
        public override void DidConnect(OTSession session) => MainThread.BeginInvokeOnMainThread(() =>
        {
            owner.SetConnected(true);
            owner.Append("connected");
        });

        public override void DidDisconnect(OTSession session) => MainThread.BeginInvokeOnMainThread(() =>
        {
            owner.SetConnected(false);
            owner.Append("session disconnected");
        });

        public override void DidFailWithError(OTSession session, OTError error) =>
            owner.Append($"session error: {error.Code} {error.LocalizedDescription}");

        public override void StreamCreated(OTSession session, OTStream stream) =>
            MainThread.BeginInvokeOnMainThread(() =>
            {
                owner.Append($"remote stream created: {stream.StreamId}");

                // Only one remote view in this sample — the first stream gets it.
                if (owner._subscriber is not null)
                {
                    return;
                }

                owner._subscriberDelegate = new SubscriberDelegate(owner);
                var subscriber = new OTSubscriber(stream, owner._subscriberDelegate);
                session.Subscribe(subscriber, out var error);

                if (error is not null)
                {
                    owner.Append($"subscribe failed: {error.Code} {error.LocalizedDescription}");
                    return;
                }

                owner._subscriber = subscriber;
            });

        public override void StreamDestroyed(OTSession session, OTStream stream) =>
            MainThread.BeginInvokeOnMainThread(() =>
            {
                owner.Append($"remote stream destroyed: {stream.StreamId}");
                if (owner._subscriber?.Stream?.StreamId == stream.StreamId)
                {
                    owner.TeardownSubscriber();
                }
            });
    }

    /// <summary>Translates <c>OTPublisherKitDelegate</c>'s callbacks into UI updates.</summary>
    private sealed class PublisherDelegate(MainPage owner) : OTPublisherKitDelegate
    {
        public override void DidFailWithError(OTPublisher publisher, OTError error) =>
            owner.Append($"publisher error: {error.Code} {error.LocalizedDescription}");

        public override void StreamCreated(OTPublisher publisher, OTStream stream) =>
            owner.Append("local stream published");
    }

    /// <summary>
    /// Translates <c>OTSubscriberKitDelegate</c>'s callbacks into UI updates. The subscriber's
    /// render view only becomes valid once <see cref="DidConnectToStream"/> fires — attaching it
    /// any earlier would add nothing to the layout, unlike the publisher's view, which exists
    /// from construction.
    /// </summary>
    private sealed class SubscriberDelegate(MainPage owner) : OTSubscriberKitDelegate
    {
        public override void DidConnectToStream(OTSubscriber subscriber) => MainThread.BeginInvokeOnMainThread(() =>
        {
            AttachNativeView(subscriber.View, owner.RemoteView.PlatformNativeView);
            owner.Append("subscribed — remote video attached");
        });

        public override void DidFailWithError(OTSubscriber subscriber, OTError error) =>
            owner.Append($"subscriber error: {error.Code} {error.LocalizedDescription}");
    }
#endif
}
