#if ANDROID
using Android.Views;
using Android.Widget;
#elif IOS
using UIKit;
#endif
using Microsoft.Maui.Handlers;

namespace OpenTok.Maui.Sample;

/// <summary>
/// A MAUI view that hosts whatever native view OpenTok hands back for a local preview or a
/// remote subscriber stream.
///
/// On both platforms, OpenTok's own <c>PublisherKit.View</c> / <c>SubscriberKit.View</c>
/// (Android) and <c>OTPublisher.View</c> / <c>OTSubscriber.View</c> (iOS) are read-only: the SDK
/// creates the render view itself once a publisher/subscriber exists, and the app's job is to
/// attach that view into its own layout — see <see cref="MainPage"/>. So the platform view here
/// is a plain container created up front (as any MAUI handler's platform view must be), with the
/// SDK-owned view attached into it later, once one exists. There is no shared C# type behind the
/// two platform views — see <c>docs/native-surface.md</c> — so the handler itself is split
/// <c>#if ANDROID</c> / <c>#if IOS</c>, the same way the platform-specific samples in
/// OpenTok.Net.Android and OpenTok.Net.iOS are.
/// </summary>
public class OpenTokVideoView : Microsoft.Maui.Controls.View
{
#if IOS
    /// <summary>The native container this view renders as, once the handler has been created.</summary>
    public UIView? PlatformNativeView => Handler?.PlatformView as UIView;
#endif
}

#if ANDROID
/// <summary>Binds <see cref="OpenTokVideoView"/> to a native <see cref="FrameLayout"/> container.</summary>
public class OpenTokVideoViewHandler : ViewHandler<OpenTokVideoView, FrameLayout>
{
    public static readonly IPropertyMapper<OpenTokVideoView, OpenTokVideoViewHandler> PreviewMapper =
        new PropertyMapper<OpenTokVideoView, OpenTokVideoViewHandler>(ViewMapper);

    public OpenTokVideoViewHandler() : base(PreviewMapper)
    {
    }

    protected override FrameLayout CreatePlatformView() => new(Context);

    /// <summary>Replaces whatever this container currently shows with <paramref name="child"/>.</summary>
    public void SetChild(global::Android.Views.View child)
    {
        PlatformView.RemoveAllViews();
        PlatformView.AddView(child, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
    }

    /// <summary>Empties the container — called before releasing the publisher/subscriber that owned the view.</summary>
    public void Clear() => PlatformView.RemoveAllViews();
}
#elif IOS
/// <summary>Maps <see cref="OpenTokVideoView"/> to a bare black <c>UIView</c>.</summary>
public class OpenTokVideoViewHandler : ViewHandler<OpenTokVideoView, UIView>
{
    public static readonly IPropertyMapper<OpenTokVideoView, OpenTokVideoViewHandler> PreviewMapper =
        new PropertyMapper<OpenTokVideoView, OpenTokVideoViewHandler>(ViewMapper);

    public OpenTokVideoViewHandler() : base(PreviewMapper)
    {
    }

    protected override UIView CreatePlatformView() => new() { BackgroundColor = UIColor.Black, ClipsToBounds = true };
}
#endif
