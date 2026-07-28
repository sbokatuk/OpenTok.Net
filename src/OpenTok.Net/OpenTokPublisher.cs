namespace OpenTok.Net;

/// <summary>
/// This device's outgoing stream: camera and microphone, captured and rendered locally as soon as
/// the publisher exists, and sent to the session once <see cref="OpenTokSession.Publish"/> is
/// called.
/// </summary>
/// <remarks>
/// <para>
/// The local preview does not need a session. Both SDKs start capturing when the publisher is
/// constructed, so an app can show a camera preview — and let the user check their framing — before
/// any credentials exist. <see cref="OpenTokSession.Publish"/> is what turns that capture into a
/// stream other participants receive.
/// </para>
/// <para>
/// Construction opens the camera and microphone, so the app must already hold those runtime
/// permissions. This class does not request them: the request is a UI decision (when to ask, what
/// to say when refused) that belongs to the app, and both MAUI's <c>Permissions</c> and a
/// hand-rolled prompt are reasonable answers.
/// </para>
/// </remarks>
public sealed partial class OpenTokPublisher : IDisposable
{
    private bool _disposed;
    private bool _publishAudio = true;
    private bool _publishVideo = true;

    /// <summary>Creates the publisher and starts local capture.</summary>
    /// <param name="name">
    /// A name carried on the published stream, surfaced to other participants as
    /// <see cref="OpenTokStream.Name"/>. Optional.
    /// </param>
    public OpenTokPublisher(string? name = null)
    {
        Name = name;
        CreateNative(name);
    }

    /// <summary>The name given at construction, if any.</summary>
    public string? Name { get; }

    /// <summary>Whether the microphone is being sent. Defaults to <see langword="true"/>.</summary>
    /// <remarks>
    /// Setting this to <see langword="false"/> is a mute, not a teardown: the stream keeps
    /// existing and other participants keep receiving it, silently.
    /// </remarks>
    public bool PublishAudio
    {
        get => _publishAudio;
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _publishAudio = value;
            SetPublishAudioNative(value);
        }
    }

    /// <summary>Whether the camera is being sent. Defaults to <see langword="true"/>.</summary>
    public bool PublishVideo
    {
        get => _publishVideo;
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _publishVideo = value;
            SetPublishVideoNative(value);
        }
    }

    /// <summary>Raised once the stream is live in the session.</summary>
    public event EventHandler<OpenTokStreamEventArgs>? StreamCreated;

    /// <summary>Raised when the stream stops.</summary>
    public event EventHandler<OpenTokStreamEventArgs>? StreamDestroyed;

    /// <summary>Raised when the SDK reports a publisher-level failure.</summary>
    public event EventHandler<OpenTokErrorEventArgs>? Failed;

    /// <summary>Releases the camera, the microphone and the native renderer.</summary>
    /// <remarks>
    /// Required. A publisher that is never disposed holds the camera until the process ends, and on
    /// Android that blocks the next publisher — including the one the same app creates after
    /// navigating back to the call screen.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        DisposeNative();
        _disposed = true;
    }

    private void OnStreamCreated(OpenTokStream stream) =>
        StreamCreated?.Invoke(this, new OpenTokStreamEventArgs(stream));

    private void OnStreamDestroyed(OpenTokStream stream) =>
        StreamDestroyed?.Invoke(this, new OpenTokStreamEventArgs(stream));

    private void OnFailed(OpenTokError error) =>
        Failed?.Invoke(this, new OpenTokErrorEventArgs(error));

    private partial void CreateNative(string? name);
    private partial void SetPublishAudioNative(bool value);
    private partial void SetPublishVideoNative(bool value);
    private partial void DisposeNative();
}
