using Android.Views;
using Android.Widget;
using Microsoft.Maui.Handlers;

namespace OpenTok.Net.Maui;

/// <summary>
/// Hosts an OpenTok video source in a plain <see cref="FrameLayout"/> container.
/// </summary>
/// <remarks>
/// The Android counterpart of the iOS handler, and the same shape for the same reason — see
/// <see cref="OpenTokVideoView"/>.
/// </remarks>
public partial class OpenTokVideoViewHandler : ViewHandler<OpenTokVideoView, FrameLayout>
{
    /// <summary>The view has no properties of its own; the mapper exists because a handler needs one.</summary>
    public static readonly IPropertyMapper<OpenTokVideoView, OpenTokVideoViewHandler> VideoMapper =
        new PropertyMapper<OpenTokVideoView, OpenTokVideoViewHandler>(ViewMapper);

    /// <summary>Creates the handler. Registered by <c>UseOpenTok()</c>.</summary>
    public OpenTokVideoViewHandler() : base(VideoMapper)
    {
    }

    /// <inheritdoc />
    protected override FrameLayout CreatePlatformView() => new(Context);

    /// <inheritdoc />
    protected override void ConnectHandler(FrameLayout platformView)
    {
        base.ConnectHandler(platformView);

        VirtualView.SourceChanged += OnSourceChanged;
        Attach(VirtualView.Source);
    }

    /// <inheritdoc />
    protected override void DisconnectHandler(FrameLayout platformView)
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
        // The SDK raises this on its own thread; touching the view hierarchy off the UI thread
        // throws CalledFromWrongThreadException on Android.
        var view = (sender as IOpenTokVideoSource)?.NativeView;
        MainThread.BeginInvokeOnMainThread(() => Show(view));
    }

    private void Show(global::Android.Views.View? nativeView)
    {
        if (nativeView is null || PlatformView is null)
        {
            return;
        }

        Clear();

        // The SDK's view may still be attached to a container this source was previously shown in;
        // Android throws "the specified child already has a parent" rather than re-parenting.
        (nativeView.Parent as ViewGroup)?.RemoveView(nativeView);

        PlatformView.AddView(nativeView, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
    }

    /// <summary>
    /// Empties the container without disposing what it held — the SDK owns the render view and
    /// reuses it.
    /// </summary>
    private void Clear() => PlatformView?.RemoveAllViews();
}
