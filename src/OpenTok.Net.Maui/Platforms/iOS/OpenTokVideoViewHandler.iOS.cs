using Microsoft.Maui.Handlers;
using UIKit;

namespace OpenTok.Net.Maui;

/// <summary>
/// Hosts an OpenTok video source in a plain <see cref="UIView"/> container.
/// </summary>
/// <remarks>
/// The container is created up front, as every MAUI handler's platform view must be; the SDK's own
/// render view is added as a subview once it exists. See <see cref="OpenTokVideoView"/> for why it
/// is that way round.
/// </remarks>
public partial class OpenTokVideoViewHandler : ViewHandler<OpenTokVideoView, UIView>
{
    /// <summary>The view has no properties of its own; the mapper exists because a handler needs one.</summary>
    public static readonly IPropertyMapper<OpenTokVideoView, OpenTokVideoViewHandler> VideoMapper =
        new PropertyMapper<OpenTokVideoView, OpenTokVideoViewHandler>(ViewMapper);

    /// <summary>Creates the handler. Registered by <c>UseOpenTok()</c>.</summary>
    public OpenTokVideoViewHandler() : base(VideoMapper)
    {
    }

    /// <inheritdoc />
    protected override UIView CreatePlatformView() =>
        new() { BackgroundColor = UIColor.Black, ClipsToBounds = true };

    /// <inheritdoc />
    protected override void ConnectHandler(UIView platformView)
    {
        base.ConnectHandler(platformView);

        VirtualView.SourceChanged += OnSourceChanged;
        Attach(VirtualView.Source);
    }

    /// <inheritdoc />
    protected override void DisconnectHandler(UIView platformView)
    {
        VirtualView.SourceChanged -= OnSourceChanged;
        Detach(VirtualView.Source);
        Clear();

        base.DisconnectHandler(platformView);
    }

    private void OnSourceChanged(object? sender, OpenTokVideoSourceChangedEventArgs e)
    {
        Detach(e.OldSource);
        Clear();
        Attach(e.NewSource);
    }

    private void Attach(IOpenTokVideoSource? source)
    {
        if (source is null)
        {
            return;
        }

        // A subscriber has no view until it connects; the event is how the wait ends.
        source.NativeViewAvailable += OnNativeViewAvailable;
        Show(source.NativeView);
    }

    private void Detach(IOpenTokVideoSource? source)
    {
        if (source is not null)
        {
            source.NativeViewAvailable -= OnNativeViewAvailable;
        }
    }

    private void OnNativeViewAvailable(object? sender, EventArgs e)
    {
        // The SDK raises this on its own callback thread, which is not the UI thread — adding a
        // subview from there is undefined behaviour on iOS, and shows up as a view that never
        // appears rather than as a crash.
        var view = (sender as IOpenTokVideoSource)?.NativeView;
        MainThread.BeginInvokeOnMainThread(() => Show(view));
    }

    private void Show(UIView? nativeView)
    {
        if (nativeView is null || PlatformView is null)
        {
            return;
        }

        Clear();

        nativeView.Frame = PlatformView.Bounds;
        nativeView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
        PlatformView.AddSubview(nativeView);
    }

    /// <summary>
    /// Empties the container without disposing what it held.
    /// </summary>
    /// <remarks>
    /// The SDK owns the render view and reuses it — removing it from a superview is detaching, not
    /// destroying. Disposing it here would break a source moved between two
    /// <see cref="OpenTokVideoView"/> instances.
    /// </remarks>
    private void Clear()
    {
        if (PlatformView is null)
        {
            return;
        }

        foreach (var subview in PlatformView.Subviews)
        {
            subview.RemoveFromSuperview();
        }
    }
}
