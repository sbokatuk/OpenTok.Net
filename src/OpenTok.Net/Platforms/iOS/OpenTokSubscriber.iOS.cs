using OpenTok.Net.iOS;
using UIKit;

namespace OpenTok.Net;

/// <summary>The iOS half of <see cref="OpenTokSubscriber"/>, over <c>OTSubscriber</c>.</summary>
public sealed partial class OpenTokSubscriber : IOpenTokVideoSource
{
    private OTSubscriber? _subscriber;
    private SubscriberDelegate? _delegate;
    private AudioLevelDelegate? _audioLevelDelegate;
    private CaptionsDelegate? _captionsDelegate;

    /// <summary>
    /// The SDK-created render view. Only meaningful once <see cref="OpenTokSubscriber.Connected"/>
    /// has been raised — before that there is nothing decoded and attaching it shows nothing.
    /// </summary>
    public UIView? NativeView => _subscriber?.View;

    /// <inheritdoc />
    public event EventHandler? NativeViewAvailable;

    internal OTSubscriberKit NativeSubscriber =>
        _subscriber ?? throw new ObjectDisposedException(nameof(OpenTokSubscriber));

    private partial void CreateNative(OpenTokStream stream)
    {
        _delegate = new SubscriberDelegate(this);

        // stream.NativeStream is the OTStream this OpenTokStream was read from — see the remarks
        // on that property for why the native object is carried rather than re-resolved by id.
        _subscriber = new OTSubscriber((OTStream)stream.NativeStream, _delegate);

        // Separate protocols, separate weakly-held properties, separate fields to keep them alive.
        _audioLevelDelegate = new AudioLevelDelegate(this);
        _subscriber.AudioLevelDelegate = _audioLevelDelegate;

        _captionsDelegate = new CaptionsDelegate(this);
        _subscriber.CaptionsDelegate = _captionsDelegate;
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
        _subscriber?.View?.RemoveFromSuperview();

        _subscriber?.Dispose();
        _subscriber = null;
        _delegate = null;
        _audioLevelDelegate = null;
        _captionsDelegate = null;
    }

    /// <summary>Forwards <c>OTSubscriberKitAudioLevelDelegate</c> onto the façade's event.</summary>
    private sealed class AudioLevelDelegate(OpenTokSubscriber owner) : OTSubscriberKitAudioLevelDelegate
    {
        public override void AudioLevelUpdated(OTSubscriberKit subscriber, float audioLevel) =>
            owner.OnAudioLevel(audioLevel);
    }

    /// <summary>Forwards <c>OTSubscriberKitCaptionsDelegate</c> onto the façade's event.</summary>
    private sealed class CaptionsDelegate(OpenTokSubscriber owner) : OTSubscriberKitCaptionsDelegate
    {
        public override void Caption(OTSubscriberKit subscriber, string text, bool isFinal) =>
            owner.OnCaption(text, isFinal);
    }

    private sealed class SubscriberDelegate(OpenTokSubscriber owner) : OTSubscriberKitDelegate
    {
        public override void DidConnectToStream(OTSubscriberKit subscriber)
        {
            // The view only becomes worth attaching now — this is the moment IOpenTokVideoSource
            // exists to announce. Raised before Connected so a handler that reacts to either sees
            // a NativeView that is already valid.
            owner.NativeViewAvailable?.Invoke(owner, EventArgs.Empty);
            owner.OnConnected();
        }

        public override void DidFailWithError(OTSubscriberKit subscriber, OTError error) =>
            owner.OnFailed(OpenTokSession.Convert(error));

        public override void DidDisconnectFromStream(OTSubscriberKit subscriber) =>
            owner.OnDisconnectedFromStream();
    }
}
