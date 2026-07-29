using Microsoft.UI.Xaml;
using OpenTok.Net.Win.Rendering;
using WindowsPublisher = OpenTok.Publisher;

namespace OpenTok.Net;

/// <summary>
/// The Windows half of <see cref="OpenTokPublisher"/>, over <c>OpenTok.Publisher</c>.
/// </summary>
/// <remarks>
/// <para>
/// The camera controls are the interesting part of this file, and they are the part that does
/// nothing. <see cref="OpenTokPublisher.CameraPosition"/>, <c>CameraTorch</c> and
/// <c>CameraZoomFactor</c> are phone concepts: OpenTok.Client exposes a <c>VideoCapturer</c> and no
/// notion of a front or back camera, a torch, or optical zoom, because desktop webcams do not have
/// them. They are accepted and ignored here rather than throwing — see each one for why that is the
/// right call for a façade.
/// </para>
/// <para>
/// The view is created here rather than taken from the SDK. On iOS and Android the SDK owns a native
/// view; on Windows it renders into an <c>IVideoRenderer</c> supplied at build time, so the façade
/// constructs an <c>OpenTokVideoView</c> and hands it its own renderer.
/// </para>
/// </remarks>
public sealed partial class OpenTokPublisher : IOpenTokVideoSource
{
    private WindowsPublisher? _publisher;
    private OpenTokVideoView? _view;
    private Context? _context;

    /// <inheritdoc />
    public FrameworkElement? NativeView => _view;

    /// <inheritdoc />
    public event EventHandler? NativeViewAvailable;

    internal WindowsPublisher NativePublisher =>
        _publisher ?? throw new ObjectDisposedException(nameof(OpenTokPublisher));

    private partial void CreateNative(string? name)
    {
        _context = OpenTokWindowsContext.Acquire();

        // UniformToFill: a self-view is nearly always a small tile, where letterboxing wastes most
        // of it. Matches what the iOS and Android heads ask their native views for.
        _view = new OpenTokVideoView { Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill };

        _publisher = new WindowsPublisher.Builder(_context)
        {
            Renderer = _view.Renderer,
            Name = name,
        }.Build();

        _publisher.AudioLevel += (_, e) => OnAudioLevel(e.AudioLevel);

        // Raised even though the view was ready before this returns. A consumer written against
        // iOS or Android subscribes to this and waits; never raising it would leave that consumer
        // waiting forever on the one platform where the view was there all along.
        NativeViewAvailable?.Invoke(this, EventArgs.Empty);
    }

    private partial void SetPublishAudioNative(bool value) => _publisher!.PublishAudio = value;

    private partial void SetPublishVideoNative(bool value) => _publisher!.PublishVideo = value;

    private partial void SetCameraPositionNative(OpenTokCameraPosition value)
    {
        // No front/back on a desktop. OpenTok.Client selects a capture device through
        // VideoCapturer, which enumerates whatever cameras exist without classifying them — there
        // is nothing here to map "front" or "back" onto.
        //
        // Ignored rather than thrown, deliberately: a shared view model that sets CameraPosition on
        // startup is ordinary, and a façade whose common API throws on one platform is not a façade.
        // The property still reports back what was set, so the app's own state stays consistent.
        _ = value;
    }

    private partial void SetCameraTorchNative(bool value)
    {
        // Webcams have no torch. Same reasoning as CameraPosition above.
        _ = value;
    }

    private partial void SetCameraZoomFactorNative(float value)
    {
        // No zoom control in OpenTok.Client's capture API. Same reasoning again.
        _ = value;
    }

    private partial void SetVideoTransformersNative(OpenTokTransformer[] transformers) =>
        _publisher!.VideoTransformers =
            [.. transformers.Select(t => new VideoTransformer(t.Name, t.Properties))];

    private partial void SetAudioTransformersNative(OpenTokTransformer[] transformers) =>
        _publisher!.AudioTransformers =
            [.. transformers.Select(t => new AudioTransformer(t.Name, t.Properties))];

    private partial void DisposeNative()
    {
        _publisher?.Dispose();
        _publisher = null;

        // After the publisher, not before: the SDK can deliver one last frame into the renderer
        // while the publisher is being torn down, and a disposed renderer simply drops it.
        _view?.Dispose();
        _view = null;

        if (_context is not null)
        {
            _context = null;
            OpenTokWindowsContext.Release();
        }
    }
}
