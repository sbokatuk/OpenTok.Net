using Microsoft.UI.Xaml;
using OpenTok.Net.Win.Rendering;
using WindowsSubscriber = OpenTok.Subscriber;

namespace OpenTok.Net;

/// <summary>
/// The Windows half of <see cref="OpenTokSubscriber"/>, over <c>OpenTok.Subscriber</c>.
/// </summary>
/// <remarks>
/// The view is available immediately here, unlike iOS and Android where the SDK produces one only
/// once video is decoding. On Windows the SDK renders into an <c>IVideoRenderer</c> the caller
/// supplies, so the façade owns the view from the start — see <see cref="IOpenTokVideoSource"/>.
/// </remarks>
public sealed partial class OpenTokSubscriber : IOpenTokVideoSource
{
    private WindowsSubscriber? _subscriber;
    private OpenTokVideoView? _view;
    private Context? _context;

    // The language that was *asked for*, which on Windows is not the same claim the other two heads
    // make. OpenTok.Client's CaptionsTranslationLanguage is set-only: there is no getter.
    //
    // OpenTokSubscriber's setter reads the value back from the SDK on purpose — "the SDK silently
    // declines a language it does not support, and reporting the requested value would be a lie".
    // Windows cannot honour that, because there is nothing to read. So this reports the request, and
    // if the SDK declined it the property will disagree with reality until Vonage adds a getter.
    //
    // Better than the alternatives: returning null would break the round-trip on every platform for
    // the sake of one, and throwing would break the shared API.
    private string? _requestedCaptionsTranslationLanguage;

    /// <inheritdoc />
    public FrameworkElement? NativeView => _view;

    /// <inheritdoc />
    public event EventHandler? NativeViewAvailable;

    internal WindowsSubscriber NativeSubscriber =>
        _subscriber ?? throw new ObjectDisposedException(nameof(OpenTokSubscriber));

    private partial void CreateNative(OpenTokStream stream)
    {
        _context = OpenTokWindowsContext.Acquire();

        // Uniform, not UniformToFill: a remote participant is the thing being watched, and cropping
        // a face out of frame to fill a tile is worse than letterboxing it.
        _view = new OpenTokVideoView();

        _subscriber = new WindowsSubscriber.Builder(_context, (Stream)stream.NativeStream)
        {
            Renderer = _view.Renderer,
        }.Build();

        _subscriber.Connected += (_, _) => OnConnected();
        _subscriber.StreamDisconnected += (_, _) => OnDisconnectedFromStream();
        _subscriber.Error += (_, e) =>
            OnFailed(new OpenTokError((int)e.ErrorCode, e.ErrorDescription ?? "unknown error"));

        _subscriber.CaptionText += (_, e) => OnCaption(e.Text, e.IsFinal);
        _subscriber.AudioLevel += (_, e) => OnAudioLevel(e.AudioLevel);

        // Raised immediately, for the reason given on IOpenTokVideoSource: a consumer written
        // against iOS or Android waits for this before showing the tile.
        NativeViewAvailable?.Invoke(this, EventArgs.Empty);
    }

    private partial void SetSubscribeToAudioNative(bool value) => _subscriber!.SubscribeToAudio = value;

    private partial void SetSubscribeToVideoNative(bool value) => _subscriber!.SubscribeToVideo = value;

    private partial void SetSubscribeToCaptionsNative(bool value) => _subscriber!.SubscribeToCaptions = value;

    private partial void SetCaptionsTranslationLanguageNative(string? value)
    {
        _subscriber!.CaptionsTranslationLanguage = value;
        _requestedCaptionsTranslationLanguage = value;
    }

    private partial string? GetCaptionsTranslationLanguageNative() => _requestedCaptionsTranslationLanguage;

    private partial void SetAudioVolumeNative(double value) => _subscriber!.AudioVolume = value;

    private partial void DisposeNative()
    {
        _subscriber?.Dispose();
        _subscriber = null;

        _view?.Dispose();
        _view = null;

        if (_context is not null)
        {
            _context = null;
            OpenTokWindowsContext.Release();
        }
    }
}
