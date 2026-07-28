using AVFoundation;
using OpenTok.Net.iOS;
using UIKit;

namespace OpenTok.Net;

/// <summary>The iOS half of <see cref="OpenTokPublisher"/>, over <c>OTPublisher</c>.</summary>
public sealed partial class OpenTokPublisher : IOpenTokVideoSource
{
    private OTPublisher? _publisher;
    private PublisherDelegate? _delegate;

    /// <summary>The SDK-created preview view, valid from construction.</summary>
    /// <remarks>
    /// Read-only and created by the SDK: OpenTok hands its render view <em>out</em>, unlike Agora,
    /// which takes an app-supplied view in. An app attaches this into its own layout —
    /// <c>OpenTok.Net.Maui</c>'s <c>OpenTokVideoView</c> is exactly that, done once.
    /// </remarks>
    public UIView? NativeView => _publisher?.View;

    /// <inheritdoc />
    /// <remarks>
    /// Never raised by a publisher: its preview view exists from construction, so there is no later
    /// moment at which it becomes available. Declared to satisfy
    /// <see cref="IOpenTokVideoSource"/>, whose other implementer — the subscriber — genuinely does
    /// have one.
    /// </remarks>
#pragma warning disable CS0067
    public event EventHandler? NativeViewAvailable;
#pragma warning restore CS0067

    internal OTPublisher NativePublisher =>
        _publisher ?? throw new ObjectDisposedException(nameof(OpenTokPublisher));

    private AudioLevelDelegate? _audioLevelDelegate;

    private partial void CreateNative(string? name)
    {
        // Constructing the publisher is what opens the camera and starts the preview — before any
        // session exists. See the class remarks.
        var settings = new OTPublisherSettings { Name = name };
        _delegate = new PublisherDelegate(this);
        _publisher = new OTPublisher(_delegate, settings);

        // A separate protocol from the main delegate, and a separate weakly-held property — so it
        // needs its own field to stay alive, exactly like the session's.
        _audioLevelDelegate = new AudioLevelDelegate(this);
        _publisher.AudioLevelDelegate = _audioLevelDelegate;
    }

    private partial void SetPublishAudioNative(bool value) => _publisher!.PublishAudio = value;

    private partial void SetPublishVideoNative(bool value) => _publisher!.PublishVideo = value;

    private partial void SetCameraPositionNative(OpenTokCameraPosition value) =>
        _publisher!.CameraPosition = value is OpenTokCameraPosition.Front
            ? AVCaptureDevicePosition.Front
            : AVCaptureDevicePosition.Back;

    private partial void SetCameraTorchNative(bool value) => _publisher!.CameraTorch = value;

    private partial void SetCameraZoomFactorNative(float value) => _publisher!.CameraZoomFactor = value;

    private partial void SetVideoTransformersNative(OpenTokTransformer[] transformers) =>
        _publisher!.VideoTransformers = [.. transformers.Select(t => new OTVideoTransformer(t.Name, t.Properties))];

    private partial void SetAudioTransformersNative(OpenTokTransformer[] transformers) =>
        _publisher!.AudioTransformers = [.. transformers.Select(t => new OTAudioTransformer(t.Name, t.Properties))];

    private partial void DisposeNative()
    {
        // Detach the SDK's view from whatever it was added to before releasing the publisher that
        // owns it — a view left in a superview outlives its renderer and draws a frozen last frame.
        _publisher?.View?.RemoveFromSuperview();

        _publisher?.Dispose();
        _publisher = null;
        _delegate = null;
        _audioLevelDelegate = null;
    }

    /// <summary>
    /// Translates <c>OTPublisherKitDelegate</c>'s callbacks onto the façade's events.
    /// </summary>
    /// <remarks>
    /// The callback parameter is <c>OTPublisherKit</c>, matching the header — the concrete
    /// <c>OTPublisher</c> would not receive a screen-share publisher, which the SDK constructs as
    /// the base type.
    /// </remarks>
    private sealed class PublisherDelegate(OpenTokPublisher owner) : OTPublisherKitDelegate
    {
        public override void DidFailWithError(OTPublisherKit publisher, OTError error) =>
            owner.OnFailed(OpenTokSession.Convert(error));

        public override void StreamCreated(OTPublisherKit publisher, OTStream stream) =>
            owner.OnStreamCreated(OpenTokSession.Convert(stream));

        public override void StreamDestroyed(OTPublisherKit publisher, OTStream stream) =>
            owner.OnStreamDestroyed(OpenTokSession.Convert(stream));

        public override void MuteForced(OTPublisherKit publisher) => owner.OnMuteForced();
    }

    /// <summary>Forwards <c>OTPublisherKitAudioLevelDelegate</c> onto the façade's event.</summary>
    private sealed class AudioLevelDelegate(OpenTokPublisher owner) : OTPublisherKitAudioLevelDelegate
    {
        public override void AudioLevelUpdated(OTPublisherKit publisher, float audioLevel) =>
            owner.OnAudioLevel(audioLevel);
    }
}
