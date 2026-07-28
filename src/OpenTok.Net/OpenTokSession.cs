namespace OpenTok.Net;

/// <summary>
/// A connection to an OpenTok (Vonage Video API) session: connect, publish a local stream,
/// subscribe to remote ones.
/// </summary>
/// <remarks>
/// <para>
/// The whole of this class's public surface is here, in one file that compiles for every target
/// framework. Everything platform-specific lives behind the <c>partial void</c> hooks at the
/// bottom, implemented in <c>Platforms/iOS</c> and <c>Platforms/Android</c>. That split is the
/// point: the shape of the API, the state machine, and the argument checking are written once and
/// cannot drift between platforms, because there is only one copy of them.
/// </para>
/// <para>
/// Events are raised on the platform's own callback thread, which is not guaranteed to be the UI
/// thread on either platform. Marshal before touching UI.
/// </para>
/// <para>
/// Not thread-safe. Drive one session from one thread — in a UI app, the UI thread.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var session = new OpenTokSession(apiKey, sessionId);
/// session.Connected += (_, _) => session.Publish(publisher);
/// session.StreamReceived += (_, e) => session.Subscribe(new OpenTokSubscriber(e.Stream));
/// session.Connect(token);
/// </code>
/// </example>
public sealed partial class OpenTokSession : IDisposable
{
    private bool _disposed;

    /// <summary>Creates a session. Nothing happens on the network until <see cref="Connect"/>.</summary>
    /// <param name="apiKey">The Vonage Video API project key.</param>
    /// <param name="sessionId">The session to join.</param>
    /// <exception cref="ArgumentException">Either argument is null, empty or whitespace.</exception>
    public OpenTokSession(string apiKey, string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        ApiKey = apiKey;
        SessionId = sessionId;

        CreateNative();
    }

    /// <summary>The Vonage Video API project key this session was created with.</summary>
    public string ApiKey { get; }

    /// <summary>The session identifier this session was created with.</summary>
    public string SessionId { get; }

    /// <summary>Where the session is in its connection lifecycle.</summary>
    /// <remarks>
    /// Tracked by this class rather than read from the SDK — see
    /// <see cref="OpenTokConnectionState"/> for why the two platforms cannot both answer it.
    /// </remarks>
    public OpenTokConnectionState State { get; private set; } = OpenTokConnectionState.NotConnected;

    /// <summary>Raised when the session is connected and ready to publish and subscribe.</summary>
    public event EventHandler? Connected;

    /// <summary>Raised when the session has disconnected, whether requested or not.</summary>
    public event EventHandler? Disconnected;

    /// <summary>Raised when the SDK reports a session-level failure.</summary>
    /// <remarks>
    /// A failed connect arrives here, not as an exception from <see cref="Connect"/>: connecting is
    /// asynchronous on both platforms, so the call returns long before the outcome is known.
    /// </remarks>
    public event EventHandler<OpenTokErrorEventArgs>? Failed;

    /// <summary>Raised when another client starts publishing into this session.</summary>
    /// <remarks>Subscribe to the stream with <see cref="OpenTokSubscriber"/> to receive its media.</remarks>
    public event EventHandler<OpenTokStreamEventArgs>? StreamReceived;

    /// <summary>Raised when a remote stream ends.</summary>
    /// <remarks>
    /// Dispose the <see cref="OpenTokSubscriber"/> for that stream in response; the native
    /// subscriber is not usable once its stream is gone.
    /// </remarks>
    public event EventHandler<OpenTokStreamEventArgs>? StreamDropped;

    /// <summary>Connects to the session. Returns immediately; watch <see cref="Connected"/> and <see cref="Failed"/>.</summary>
    /// <param name="token">A token minted for this session.</param>
    /// <exception cref="ArgumentException"><paramref name="token"/> is null, empty or whitespace.</exception>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The session is already connecting or connected.</exception>
    public void Connect(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (State is not OpenTokConnectionState.NotConnected)
        {
            throw new InvalidOperationException($"The session is {State}; it can only connect from NotConnected.");
        }

        State = OpenTokConnectionState.Connecting;
        ConnectNative(token);
    }

    /// <summary>Disconnects from the session. A no-op when already disconnected.</summary>
    /// <remarks>
    /// Returns immediately. <see cref="Disconnected"/> is raised when the SDK confirms it.
    /// Unpublish and unsubscribe first if you want those torn down in an order you control;
    /// otherwise the SDK ends them along with the session.
    /// </remarks>
    public void Disconnect()
    {
        if (_disposed || State is OpenTokConnectionState.NotConnected or OpenTokConnectionState.Disconnecting)
        {
            return;
        }

        State = OpenTokConnectionState.Disconnecting;
        DisconnectNative();
    }

    /// <summary>Starts publishing this device's camera and microphone into the session.</summary>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The session is not connected.</exception>
    public void Publish(OpenTokPublisher publisher)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ObjectDisposedException.ThrowIf(_disposed, this);
        RequireConnected(nameof(Publish));

        PublishNative(publisher);
    }

    /// <summary>Stops publishing <paramref name="publisher"/>'s stream.</summary>
    public void Unpublish(OpenTokPublisher publisher)
    {
        ArgumentNullException.ThrowIfNull(publisher);

        if (_disposed || State is not OpenTokConnectionState.Connected)
        {
            return;
        }

        UnpublishNative(publisher);
    }

    /// <summary>Starts receiving the remote stream <paramref name="subscriber"/> was created for.</summary>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The session is not connected.</exception>
    public void Subscribe(OpenTokSubscriber subscriber)
    {
        ArgumentNullException.ThrowIfNull(subscriber);
        ObjectDisposedException.ThrowIf(_disposed, this);
        RequireConnected(nameof(Subscribe));

        SubscribeNative(subscriber);
    }

    /// <summary>Stops receiving <paramref name="subscriber"/>'s stream.</summary>
    public void Unsubscribe(OpenTokSubscriber subscriber)
    {
        ArgumentNullException.ThrowIfNull(subscriber);

        if (_disposed || State is not OpenTokConnectionState.Connected)
        {
            return;
        }

        UnsubscribeNative(subscriber);
    }

    /// <summary>Disconnects if still connected and releases the native session.</summary>
    /// <remarks>
    /// Required, not merely tidy: neither SDK releases a session because the managed wrapper became
    /// unreachable, and a session left connected keeps signalling.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Disconnect();
        DisposeNative();

        _disposed = true;
        State = OpenTokConnectionState.NotConnected;
    }

    private void RequireConnected(string operation)
    {
        if (State is not OpenTokConnectionState.Connected)
        {
            throw new InvalidOperationException(
                $"{operation} requires a connected session; this one is {State}. " +
                "Wait for the Connected event.");
        }
    }

    // Raised from the platform implementations. Each one both updates the state machine and
    // forwards the event, so the two cannot disagree — a platform partial never assigns State.

    private void OnConnected()
    {
        State = OpenTokConnectionState.Connected;
        Connected?.Invoke(this, EventArgs.Empty);
    }

    private void OnDisconnected()
    {
        State = OpenTokConnectionState.NotConnected;
        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    private void OnFailed(OpenTokError error)
    {
        // A failure before the connection is established leaves the session unusable, and the SDKs
        // do not follow it with a disconnect callback. Resetting here is what lets a caller retry
        // Connect on the same object rather than being stuck in Connecting forever.
        if (State is OpenTokConnectionState.Connecting)
        {
            State = OpenTokConnectionState.NotConnected;
        }

        Failed?.Invoke(this, new OpenTokErrorEventArgs(error));
    }

    private void OnStreamReceived(OpenTokStream stream) =>
        StreamReceived?.Invoke(this, new OpenTokStreamEventArgs(stream));

    private void OnStreamDropped(OpenTokStream stream) =>
        StreamDropped?.Invoke(this, new OpenTokStreamEventArgs(stream));

    // The platform seam. Each is implemented exactly once per platform, under Platforms/.
    private partial void CreateNative();
    private partial void ConnectNative(string token);
    private partial void DisconnectNative();
    private partial void PublishNative(OpenTokPublisher publisher);
    private partial void UnpublishNative(OpenTokPublisher publisher);
    private partial void SubscribeNative(OpenTokSubscriber subscriber);
    private partial void UnsubscribeNative(OpenTokSubscriber subscriber);
    private partial void DisposeNative();
}
