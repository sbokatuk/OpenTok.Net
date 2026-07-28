using Com.Opentok.Android;

namespace OpenTok.Net;

/// <summary>The Android half of <see cref="OpenTokPublisher"/>, over <c>Com.Opentok.Android.Publisher</c>.</summary>
public sealed partial class OpenTokPublisher : IOpenTokVideoSource
{
    private Publisher? _publisher;

    /// <summary>The SDK-created preview view, valid from construction.</summary>
    public global::Android.Views.View? NativeView => _publisher?.View;

    /// <inheritdoc />
    /// <remarks>Never raised — see the iOS counterpart for why.</remarks>
#pragma warning disable CS0067
    public event EventHandler? NativeViewAvailable;
#pragma warning restore CS0067

    internal PublisherKit NativePublisher =>
        _publisher ?? throw new ObjectDisposedException(nameof(OpenTokPublisher));

    private partial void CreateNative(string? name)
    {
        var builder = new Publisher.Builder(OpenTokSession.AppContext);

        if (name is not null)
        {
            builder.Name(name);
        }

        // Build() returns a Publisher, not the inherited PublisherKit, and the chain stays on
        // Publisher.Builder — both restored by OpenTok.Net.Android's Additions/Builders.cs, since
        // Java's covariant return types do not survive class-parse.
        var publisher = builder.Build();

        publisher.StreamCreated += OnNativeStreamCreated;
        publisher.StreamDestroyed += OnNativeStreamDestroyed;
        publisher.Error += OnNativeError;
        publisher.AudioLevel += OnNativeAudioLevel;
        publisher.Mute += OnNativeMute;

        _publisher = publisher;
    }

    private partial void SetPublishAudioNative(bool value) => _publisher!.PublishAudio = value;

    private partial void SetPublishVideoNative(bool value) => _publisher!.PublishVideo = value;

    /// <summary>
    /// Selects the camera by cycling to the next one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Android's SDK has no "use this camera" call — only <c>CycleCamera</c>, which advances to the
    /// next one. The shared half of <see cref="OpenTokPublisher.CameraPosition"/> only reaches this
    /// when the value actually changed, so one cycle is the correct response.
    /// </para>
    /// <para>
    /// Reading the current camera and comparing would be the obvious alternative, but
    /// <c>Publisher.CameraId</c> and <c>Publisher.SwapCamera</c> are both deprecated in this SDK
    /// version while <c>CycleCamera</c> is not — so tracking the position in the façade is both the
    /// only non-deprecated route and, on a device with more than two cameras, no less accurate:
    /// the SDK exposes nothing finer-grained than "next".
    /// </para>
    /// </remarks>
    private partial void SetCameraPositionNative(OpenTokCameraPosition value) =>
        _publisher!.CycleCamera();

    private partial void SetCameraTorchNative(bool value) => _publisher!.SetCameraTorch(value);

    private partial void SetCameraZoomFactorNative(float value) => _publisher!.SetCameraZoomFactor(value);

    // PublisherKit.VideoTransformer and .AudioTransformer are *inner* Java classes, not static
    // nested ones, so each instance belongs to a PublisherKit. The binding surfaces that as a
    // leading `__self` parameter — hence the publisher being passed in as well as the name and
    // properties. There is no iOS equivalent of that argument; OTVideoTransformer is free-standing.
    private partial void SetVideoTransformersNative(OpenTokTransformer[] transformers) =>
        _publisher!.SetVideoTransformers(
            [.. transformers.Select(t => new PublisherKit.VideoTransformer(_publisher, t.Name, t.Properties))]);

    private partial void SetAudioTransformersNative(OpenTokTransformer[] transformers) =>
        _publisher!.SetAudioTransformers(
            [.. transformers.Select(t => new PublisherKit.AudioTransformer(_publisher, t.Name, t.Properties))]);

    private partial void DisposeNative()
    {
        if (_publisher is null)
        {
            return;
        }

        _publisher.StreamCreated -= OnNativeStreamCreated;
        _publisher.StreamDestroyed -= OnNativeStreamDestroyed;
        _publisher.Error -= OnNativeError;
        _publisher.AudioLevel -= OnNativeAudioLevel;
        _publisher.Mute -= OnNativeMute;

        // Detach the SDK's view from its parent before releasing the publisher that owns it —
        // a view left in a ViewGroup outlives its renderer and shows a frozen frame.
        (_publisher.View?.Parent as global::Android.Views.ViewGroup)?.RemoveView(_publisher.View);

        // Releases the camera. Not optional: without it the camera stays open until the process
        // ends, and the next publisher this app creates cannot acquire it.
        _publisher.Destroy();

        _publisher.Dispose();
        _publisher = null;
    }

    private void OnNativeStreamCreated(object? sender, PublisherKit.StreamCreatedEventArgs e)
    {
        if (e.Stream is not null)
        {
            OnStreamCreated(OpenTokSession.Convert(e.Stream));
        }
    }

    private void OnNativeStreamDestroyed(object? sender, PublisherKit.StreamDestroyedEventArgs e)
    {
        if (e.Stream is not null)
        {
            OnStreamDestroyed(OpenTokSession.Convert(e.Stream));
        }
    }

    private void OnNativeError(object? sender, PublisherKit.ErrorEventArgs e) =>
        OnFailed(OpenTokSession.Convert(e.Error));

    private void OnNativeAudioLevel(object? sender, PublisherKit.AudioLevelEventArgs e) =>
        OnAudioLevel(e.AudioLevel);

    private void OnNativeMute(object? sender, PublisherKit.MuteEventArgs e) => OnMuteForced();
}
