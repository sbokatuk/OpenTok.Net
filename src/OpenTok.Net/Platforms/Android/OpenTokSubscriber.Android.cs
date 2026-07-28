using Com.Opentok.Android;

// See OpenTokSession.Android.cs — System.IO.Stream is in scope via ImplicitUsings.
using OpenTokNativeStream = Com.Opentok.Android.Stream;

namespace OpenTok.Net;

/// <summary>The Android half of <see cref="OpenTokSubscriber"/>, over <c>Com.Opentok.Android.Subscriber</c>.</summary>
public sealed partial class OpenTokSubscriber : IOpenTokVideoSource
{
    private Subscriber? _subscriber;

    /// <summary>
    /// The SDK-created render view. Only meaningful once <see cref="OpenTokSubscriber.Connected"/>
    /// has been raised.
    /// </summary>
    public global::Android.Views.View? NativeView => _subscriber?.View;

    /// <inheritdoc />
    public event EventHandler? NativeViewAvailable;

    internal SubscriberKit NativeSubscriber =>
        _subscriber ?? throw new ObjectDisposedException(nameof(OpenTokSubscriber));

    private partial void CreateNative(OpenTokStream stream)
    {
        // Build() returns a Subscriber rather than the inherited SubscriberKit — see
        // OpenTok.Net.Android's Additions/Builders.cs.
        var subscriber = new Subscriber.Builder(
            OpenTokSession.AppContext,
            (OpenTokNativeStream)stream.NativeStream).Build();

        subscriber.Connected += OnNativeConnected;
        subscriber.SubscriberDisconnected += OnNativeDisconnected;
        subscriber.Error += OnNativeError;
        subscriber.AudioLevel += OnNativeAudioLevel;
        subscriber.Captions += OnNativeCaptions;

        _subscriber = subscriber;
    }

    private partial void SetSubscribeToAudioNative(bool value) => _subscriber!.SubscribeToAudio = value;

    private partial void SetSubscribeToVideoNative(bool value) => _subscriber!.SubscribeToVideo = value;

    private partial void SetSubscribeToCaptionsNative(bool value) => _subscriber!.SubscribeToCaptions = value;

    private partial void SetCaptionsTranslationLanguageNative(string? value) =>
        _subscriber!.CaptionsTranslationLanguage = value;

    private partial string? GetCaptionsTranslationLanguageNative() =>
        _subscriber?.CaptionsTranslationLanguage;

    private partial void SetAudioVolumeNative(double value) => _subscriber!.AudioVolume = value;

    private partial void DisposeNative()
    {
        if (_subscriber is null)
        {
            return;
        }

        _subscriber.Connected -= OnNativeConnected;
        _subscriber.SubscriberDisconnected -= OnNativeDisconnected;
        _subscriber.Error -= OnNativeError;
        _subscriber.AudioLevel -= OnNativeAudioLevel;
        _subscriber.Captions -= OnNativeCaptions;

        (_subscriber.View?.Parent as global::Android.Views.ViewGroup)?.RemoveView(_subscriber.View);

        // No Destroy() here, unlike the publisher: SubscriberKit.Destroy() is deprecated in this
        // SDK version, and unsubscribing is the whole teardown. A subscriber holds no camera.
        _subscriber.Dispose();
        _subscriber = null;
    }

    // SubscriberDisconnected, not Disconnected: SubscriberKit implements two listener interfaces
    // that each declare onDisconnected(SubscriberKit), and the binding renames both to keep them
    // apart — see OpenTok.Net.Android's Transforms/Metadata.xml. This is the subscriber-level one.
    private void OnNativeConnected(object? sender, SubscriberKit.ConnectedEventArgs e)
    {
        // Raised before Connected, so a handler reacting to either sees a valid NativeView.
        NativeViewAvailable?.Invoke(this, EventArgs.Empty);
        OnConnected();
    }

    private void OnNativeDisconnected(object? sender, SubscriberKit.SubscriberListenerDisconnectedEventArgs e) =>
        OnDisconnectedFromStream();

    private void OnNativeError(object? sender, SubscriberKit.ErrorEventArgs e) =>
        OnFailed(OpenTokSession.Convert(e.Error));

    private void OnNativeAudioLevel(object? sender, SubscriberKit.AudioLevelEventArgs e) =>
        OnAudioLevel(e.AudioLevel);

    private void OnNativeCaptions(object? sender, SubscriberKit.CaptionsEventArgs e) =>
        OnCaption(e.Text ?? "", e.IsFinal);
}
