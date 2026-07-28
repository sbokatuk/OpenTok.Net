namespace OpenTok.Net;

/// <summary>
/// A receiver for one remote stream. Create one per stream reported by
/// <see cref="OpenTokSession.StreamReceived"/> and hand it to <see cref="OpenTokSession.Subscribe"/>.
/// </summary>
/// <remarks>
/// A subscriber is bound to a single stream for its whole life — there is no reassigning it — and
/// stops being usable when that stream ends. Dispose it in response to
/// <see cref="OpenTokSession.StreamDropped"/>.
/// </remarks>
public sealed partial class OpenTokSubscriber : IDisposable
{
    private bool _disposed;
    private bool _subscribeToAudio = true;
    private bool _subscribeToVideo = true;

    /// <summary>Creates a subscriber for <paramref name="stream"/>.</summary>
    /// <remarks>
    /// Creating it does not start receiving; <see cref="OpenTokSession.Subscribe"/> does.
    /// </remarks>
    public OpenTokSubscriber(OpenTokStream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        Stream = stream;
        CreateNative(stream);
    }

    /// <summary>The stream this subscriber receives.</summary>
    public OpenTokStream Stream { get; }

    /// <summary>
    /// Raised once media is actually flowing. The remote video is only worth showing from here on.
    /// </summary>
    /// <remarks>
    /// Unlike a publisher's preview, which exists from construction, a subscriber has nothing
    /// decoded to render until this fires — which is why <c>OpenTokVideoView</c> waits for it
    /// rather than attaching the native view immediately.
    /// </remarks>
    public event EventHandler? Connected;

    /// <summary>Raised when the subscriber stops receiving the stream.</summary>
    public event EventHandler? DisconnectedFromStream;

    /// <summary>Raised when the SDK reports a subscriber-level failure.</summary>
    public event EventHandler<OpenTokErrorEventArgs>? Failed;

    /// <summary>Whether this stream's audio is being received. Defaults to <see langword="true"/>.</summary>
    public bool SubscribeToAudio
    {
        get => _subscribeToAudio;
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _subscribeToAudio = value;
            SetSubscribeToAudioNative(value);
        }
    }

    /// <summary>
    /// Whether this stream's video is being received. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Turning video off for a participant who is not on screen is the single most effective
    /// bandwidth saving available to a multi-party call — the media router stops sending it
    /// entirely rather than sending frames that are decoded and discarded.
    /// </remarks>
    public bool SubscribeToVideo
    {
        get => _subscribeToVideo;
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _subscribeToVideo = value;
            SetSubscribeToVideoNative(value);
        }
    }

    /// <summary>Releases the native subscriber and its renderer.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        DisposeNative();
        _disposed = true;
    }

    private void OnConnected() => Connected?.Invoke(this, EventArgs.Empty);

    private void OnDisconnectedFromStream() => DisconnectedFromStream?.Invoke(this, EventArgs.Empty);

    private void OnFailed(OpenTokError error) =>
        Failed?.Invoke(this, new OpenTokErrorEventArgs(error));

    private partial void CreateNative(OpenTokStream stream);
    private partial void SetSubscribeToAudioNative(bool value);
    private partial void SetSubscribeToVideoNative(bool value);
    private partial void DisposeNative();
}
