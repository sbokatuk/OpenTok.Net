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

    /// <summary>Raised when another client joins the session.</summary>
    /// <remarks>
    /// A connection is not a stream: a participant who has joined but not published raises this and
    /// never raises <see cref="StreamReceived"/>. Use it for a participant list; use
    /// <see cref="StreamReceived"/> for video.
    /// </remarks>
    public event EventHandler<OpenTokConnectionEventArgs>? ConnectionCreated;

    /// <summary>Raised when another client leaves the session.</summary>
    public event EventHandler<OpenTokConnectionEventArgs>? ConnectionDestroyed;

    /// <summary>Raised when a signal arrives — including one this client sent.</summary>
    /// <remarks>
    /// Check <see cref="OpenTokSignalEventArgs.FromSelf"/> before echoing it into a chat log.
    /// </remarks>
    public event EventHandler<OpenTokSignalEventArgs>? SignalReceived;

    /// <summary>Raised when recording of the session starts.</summary>
    public event EventHandler<OpenTokArchiveEventArgs>? ArchiveStarted;

    /// <summary>Raised when recording of the session stops.</summary>
    public event EventHandler<OpenTokArchiveEventArgs>? ArchiveStopped;

    /// <summary>Raised when the connection drops and the SDK begins trying to recover it.</summary>
    /// <remarks>
    /// Not a disconnect. The SDK reconnects publishers and subscribers by itself; either
    /// <see cref="Reconnected"/> or <see cref="Disconnected"/> follows. Worth surfacing in the UI,
    /// because media is interrupted meanwhile and users otherwise assume the app has frozen.
    /// </remarks>
    public event EventHandler? Reconnecting;

    /// <summary>Raised when the SDK has recovered a dropped connection.</summary>
    public event EventHandler? Reconnected;

    /// <summary>Raised when a moderator mutes the session, or lifts the mute state.</summary>
    /// <remarks>
    /// The boolean says which: <see langword="true"/> when streams were muted,
    /// <see langword="false"/> when a moderator turned the mute state off again.
    /// </remarks>
    public event EventHandler<OpenTokMuteForcedEventArgs>? MuteForced;

    /// <summary>
    /// What this client's token permits, once connected; <see langword="null"/> before that.
    /// </summary>
    /// <remarks>
    /// Read it after <see cref="Connected"/>. Hiding a moderator control the token cannot use is
    /// better than letting the call fail — the SDKs answer a role error rather than doing nothing.
    /// </remarks>
    public OpenTokCapabilities? Capabilities => _disposed ? null : GetCapabilitiesNative();

    /// <summary>
    /// Sends a signal to every client in the session, or to one of them.
    /// </summary>
    /// <param name="type">
    /// An application-defined type, so a receiver can tell a chat message from a "raise hand".
    /// Optional, but a session using more than one kind of signal needs it.
    /// </param>
    /// <param name="data">The payload. Limited to 8 KB by the platform.</param>
    /// <param name="to">
    /// The one client to send to, or <see langword="null"/> to send to everyone — including this
    /// client, which is what makes <see cref="OpenTokSignalEventArgs.FromSelf"/> necessary.
    /// </param>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The session is not connected.</exception>
    public void Signal(string? type, string? data, OpenTokConnection? to = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        RequireConnected(nameof(Signal));

        SignalNative(type, data, to);
    }

    /// <summary>
    /// Mutes every publisher in the session, optionally sparing some streams.
    /// </summary>
    /// <param name="except">Streams to leave unmuted — a moderator's own, typically.</param>
    /// <remarks>
    /// Requires a moderator token; see <see cref="Capabilities"/>. It also leaves the session in a
    /// muted <em>state</em>, so clients joining later are muted too, until
    /// <see cref="DisableForceMute"/> lifts it.
    /// </remarks>
    public void ForceMuteAll(IEnumerable<OpenTokStream>? except = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        RequireConnected(nameof(ForceMuteAll));

        ForceMuteAllNative(except?.ToArray() ?? []);
    }

    /// <summary>Lifts the session-wide mute state left by <see cref="ForceMuteAll"/>.</summary>
    public void DisableForceMute()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        RequireConnected(nameof(DisableForceMute));

        DisableForceMuteNative();
    }

    /// <summary>Mutes one publisher. Requires a moderator token.</summary>
    public void ForceMuteStream(OpenTokStream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ObjectDisposedException.ThrowIf(_disposed, this);
        RequireConnected(nameof(ForceMuteStream));

        ForceMuteStreamNative(stream);
    }

    /// <summary>Disconnects another client from the session. Requires a moderator token.</summary>
    public void ForceDisconnect(OpenTokConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ObjectDisposedException.ThrowIf(_disposed, this);
        RequireConnected(nameof(ForceDisconnect));

        ForceDisconnectNative(connection);
    }

    /// <summary>
    /// Turns on end-to-end encryption for this session, using a shared secret.
    /// </summary>
    /// <remarks>
    /// Every client in the session must set the <em>same</em> secret, and must set it before
    /// publishing or subscribing. A mismatch is not a connect failure — it is a subscriber error
    /// per stream, which is easy to misread as a network problem.
    /// </remarks>
    public void SetEncryptionSecret(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        ObjectDisposedException.ThrowIf(_disposed, this);

        SetEncryptionSecretNative(secret);
    }

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

    private void OnConnectionCreated(OpenTokConnection connection) =>
        ConnectionCreated?.Invoke(this, new OpenTokConnectionEventArgs(connection));

    private void OnConnectionDestroyed(OpenTokConnection connection) =>
        ConnectionDestroyed?.Invoke(this, new OpenTokConnectionEventArgs(connection));

    /// <summary>
    /// Raises <see cref="SignalReceived"/>, working out whether this client sent it.
    /// </summary>
    /// <remarks>
    /// The comparison is against the session's own connection id, which the platform half supplies
    /// — neither SDK flags a signal as self-sent, and both deliver it back to the sender. Doing it
    /// here means every app gets the answer rather than each one rediscovering the need for it.
    /// </remarks>
    private void OnSignalReceived(string? type, string? data, OpenTokConnection? from)
    {
        var fromSelf = from is not null &&
                       OwnConnectionIdNative() is { Length: > 0 } own &&
                       string.Equals(from.ConnectionId, own, StringComparison.Ordinal);

        SignalReceived?.Invoke(this, new OpenTokSignalEventArgs(type, data, from, fromSelf));
    }

    private void OnArchiveStarted(string archiveId, string? archiveName) =>
        ArchiveStarted?.Invoke(this, new OpenTokArchiveEventArgs(archiveId, archiveName));

    private void OnArchiveStopped(string archiveId) =>
        ArchiveStopped?.Invoke(this, new OpenTokArchiveEventArgs(archiveId, null));

    private void OnReconnecting() => Reconnecting?.Invoke(this, EventArgs.Empty);

    private void OnReconnected() => Reconnected?.Invoke(this, EventArgs.Empty);

    private void OnMuteForced(bool active) =>
        MuteForced?.Invoke(this, new OpenTokMuteForcedEventArgs(active));

    // The platform seam. Each is implemented exactly once per platform, under Platforms/.
    private partial void CreateNative();
    private partial void ConnectNative(string token);
    private partial void DisconnectNative();
    private partial void PublishNative(OpenTokPublisher publisher);
    private partial void UnpublishNative(OpenTokPublisher publisher);
    private partial void SubscribeNative(OpenTokSubscriber subscriber);
    private partial void UnsubscribeNative(OpenTokSubscriber subscriber);
    private partial void SignalNative(string? type, string? data, OpenTokConnection? to);
    private partial void ForceMuteAllNative(OpenTokStream[] except);
    private partial void DisableForceMuteNative();
    private partial void ForceMuteStreamNative(OpenTokStream stream);
    private partial void ForceDisconnectNative(OpenTokConnection connection);
    private partial void SetEncryptionSecretNative(string secret);
    private partial OpenTokCapabilities? GetCapabilitiesNative();
    private partial string? OwnConnectionIdNative();
    private partial void DisposeNative();
}

/// <summary>Carries whether a moderator muted the session or lifted the mute state.</summary>
public sealed class OpenTokMuteForcedEventArgs(bool active) : EventArgs
{
    /// <summary>
    /// <see langword="true"/> when streams were muted, <see langword="false"/> when a moderator
    /// turned the session's mute state off again.
    /// </summary>
    public bool Active { get; } = active;
}
