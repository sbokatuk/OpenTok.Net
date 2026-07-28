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

        _publisher = publisher;
    }

    private partial void SetPublishAudioNative(bool value) => _publisher!.PublishAudio = value;

    private partial void SetPublishVideoNative(bool value) => _publisher!.PublishVideo = value;

    private partial void DisposeNative()
    {
        if (_publisher is null)
        {
            return;
        }

        _publisher.StreamCreated -= OnNativeStreamCreated;
        _publisher.StreamDestroyed -= OnNativeStreamDestroyed;
        _publisher.Error -= OnNativeError;

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
}
