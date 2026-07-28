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
    private OpenTokCameraPosition _cameraPosition = OpenTokCameraPosition.Front;
    private bool _cameraTorch;
    private float _cameraZoomFactor = 1.0f;

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

    /// <summary>Raised about 20 times a second with the microphone's current level.</summary>
    /// <remarks>
    /// Drives a "you are muted" indicator or a level meter. Raised on the SDK's own thread and
    /// frequently, so do as little as possible in the handler.
    /// </remarks>
    public event EventHandler<OpenTokAudioLevelEventArgs>? AudioLevel;

    /// <summary>Raised when a moderator force-mutes this publisher.</summary>
    /// <remarks>
    /// <see cref="PublishAudio"/> is already false by the time this arrives — the SDK does the
    /// muting. The event exists so the UI can reflect it rather than showing an unmuted mic.
    /// </remarks>
    public event EventHandler? MuteForced;

    /// <summary>Which camera is being captured. Defaults to the front one.</summary>
    /// <remarks>
    /// Setting it before publishing sets a preference; after publishing it switches the live
    /// camera. Reading it back before capture starts reports the preference, not the hardware.
    /// </remarks>
    public OpenTokCameraPosition CameraPosition
    {
        get => _cameraPosition;
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // Only on a real change, and that is a contract the Android half depends on rather
            // than an optimisation: its SDK offers CycleCamera() — advance to the next camera —
            // and no way to select one, so it can only honour this by cycling when the value
            // differs. Both SDKs start on the front camera, which is what makes the tracked value
            // trustworthy from construction.
            if (_cameraPosition == value)
            {
                return;
            }

            _cameraPosition = value;
            SetCameraPositionNative(value);
        }
    }

    /// <summary>Whether the camera's torch is on. Defaults to off.</summary>
    /// <remarks>
    /// A preference, not a guarantee: front cameras generally have no torch, and setting this there
    /// does nothing. Reading it back once publishing reports the active camera's real state.
    /// </remarks>
    public bool CameraTorch
    {
        get => _cameraTorch;
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _cameraTorch = value;
            SetCameraTorchNative(value);
        }
    }

    /// <summary>The camera's zoom factor. 1.0 is no zoom.</summary>
    /// <remarks>
    /// Below 1.0 asks for ultra-wide, which not every device has; above 1.0 zooms in. Values
    /// outside the active camera's range are clamped by the SDK rather than rejected, so reading
    /// the property back is the only way to know what was applied.
    /// </remarks>
    public float CameraZoomFactor
    {
        get => _cameraZoomFactor;
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _cameraZoomFactor = value;
            SetCameraZoomFactorNative(value);
        }
    }

    /// <summary>Switches to the other camera.</summary>
    public void SwapCamera()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        CameraPosition = CameraPosition is OpenTokCameraPosition.Front
            ? OpenTokCameraPosition.Back
            : OpenTokCameraPosition.Front;
    }

    /// <summary>
    /// Sets the video transformations applied to the outgoing stream — background blur or
    /// replacement. Pass an empty sequence to remove them.
    /// </summary>
    /// <remarks>
    /// <b>Requires a transformers package</b> (<c>OpenTok.Net.Transformers.iOS</c> /
    /// <c>OpenTok.Net.Transformers.Android</c>), which is not a dependency of this one. Without it
    /// the SDK reports that the transformers library is not loaded — at runtime, from a call that
    /// compiled and linked. See <see cref="OpenTokTransformer"/>.
    /// </remarks>
    public void SetVideoTransformers(IEnumerable<OpenTokTransformer> transformers)
    {
        ArgumentNullException.ThrowIfNull(transformers);
        ObjectDisposedException.ThrowIf(_disposed, this);

        SetVideoTransformersNative(transformers.ToArray());
    }

    /// <summary>
    /// Sets the audio transformations applied to the outgoing stream — noise suppression. Pass an
    /// empty sequence to remove them.
    /// </summary>
    /// <remarks>Same package requirement as <see cref="SetVideoTransformers"/>.</remarks>
    public void SetAudioTransformers(IEnumerable<OpenTokTransformer> transformers)
    {
        ArgumentNullException.ThrowIfNull(transformers);
        ObjectDisposedException.ThrowIf(_disposed, this);

        SetAudioTransformersNative(transformers.ToArray());
    }

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

    private void OnAudioLevel(float level) =>
        AudioLevel?.Invoke(this, new OpenTokAudioLevelEventArgs(level));

    private void OnMuteForced()
    {
        // Keep the cached value honest: the SDK has already stopped sending audio, and a UI
        // reading PublishAudio to draw its mic button would otherwise show the wrong state until
        // the user toggled it.
        _publishAudio = false;
        MuteForced?.Invoke(this, EventArgs.Empty);
    }

    private partial void CreateNative(string? name);
    private partial void SetPublishAudioNative(bool value);
    private partial void SetPublishVideoNative(bool value);
    private partial void SetCameraPositionNative(OpenTokCameraPosition value);
    private partial void SetCameraTorchNative(bool value);
    private partial void SetCameraZoomFactorNative(float value);
    private partial void SetVideoTransformersNative(OpenTokTransformer[] transformers);
    private partial void SetAudioTransformersNative(OpenTokTransformer[] transformers);
    private partial void DisposeNative();
}
