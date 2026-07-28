using OpenTok.Net.iOS;
using UIKit;

namespace OpenTok.Net;

/// <summary>The iOS half of <see cref="OpenTokSubscriber"/>, over <c>OTSubscriber</c>.</summary>
public sealed partial class OpenTokSubscriber : IOpenTokVideoSource
{
    private OTSubscriber? _subscriber;
    private SubscriberDelegate? _delegate;

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
    }

    private partial void SetSubscribeToAudioNative(bool value) => _subscriber!.SubscribeToAudio = value;

    private partial void SetSubscribeToVideoNative(bool value) => _subscriber!.SubscribeToVideo = value;

    private partial void DisposeNative()
    {
        _subscriber?.View?.RemoveFromSuperview();

        _subscriber?.Dispose();
        _subscriber = null;
        _delegate = null;
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
