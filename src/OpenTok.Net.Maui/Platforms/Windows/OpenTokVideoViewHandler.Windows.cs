using Microsoft.Maui.Handlers;

// Aliased rather than imported wholesale, because .NET MAUI and WinUI name the same concepts
// identically and a handler is the one place both are in scope at once: Grid, SolidColorBrush,
// HorizontalAlignment and VerticalAlignment all exist in Microsoft.Maui.* and Microsoft.UI.Xaml.*,
// and plain `using` directives for both make every one of them ambiguous (CS0104).
//
// A `W` prefix for the Windows side, so which framework a type belongs to is visible at the use
// site rather than inferred from the using block.
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WGrid = Microsoft.UI.Xaml.Controls.Grid;
using WHorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment;
using WPanel = Microsoft.UI.Xaml.Controls.Panel;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WVerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment;

namespace OpenTok.Net.Maui;

/// <summary>
/// Hosts an OpenTok video source in a WinUI Grid container.
/// </summary>
/// <remarks>
/// <para>
/// Same shape as the iOS and Android handlers — a container created up front, the source's own view
/// added to it once available — but the timing underneath differs. On Windows the source's view
/// exists from construction, because the façade creates it rather than waiting for the SDK to
/// produce one. <c>NativeViewAvailable</c> is still raised, immediately, so this handler needs no
/// special case for that.
/// </para>
/// <para>
/// The other difference is what a "native view" can be attached to. A WinUI element has a single
/// parent and will throw if added to a second one, so <see cref="Clear"/> detaches before attaching
/// rather than after — moving a source between two <see cref="OpenTokVideoView"/> instances is
/// otherwise an exception rather than a moved tile.
/// </para>
/// </remarks>
public partial class OpenTokVideoViewHandler : ViewHandler<OpenTokVideoView, WPanel>
{
    /// <summary>The view has no properties of its own; the mapper exists because a handler needs one.</summary>
    public static readonly IPropertyMapper<OpenTokVideoView, OpenTokVideoViewHandler> VideoMapper =
        new PropertyMapper<OpenTokVideoView, OpenTokVideoViewHandler>(ViewMapper);

    /// <summary>Creates the handler. Registered by <c>UseOpenTok()</c>.</summary>
    public OpenTokVideoViewHandler() : base(VideoMapper)
    {
    }

    /// <inheritdoc />
    protected override WPanel CreatePlatformView() =>
        new WGrid
        {
            Background = new WSolidColorBrush(Microsoft.UI.Colors.Black),

            // The video element is sized by the grid, and anything overflowing is the letterbox
            // case — clipped so a UniformToFill source cannot paint over neighbouring tiles.
            Clip = null,
        };

    /// <inheritdoc />
    protected override void ConnectHandler(WPanel platformView)
    {
        base.ConnectHandler(platformView);

        VirtualView.SourceChanged += OnSourceChanged;
        Attach(VirtualView.Source);
    }

    /// <inheritdoc />
    protected override void DisconnectHandler(WPanel platformView)
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
        // Marshalled for the same reason as the other two handlers. It is very likely already the
        // UI thread here — the façade's Windows head binds the SDK's context to the UI dispatcher
        // queue — but this handler cannot verify that from where it stands, and BeginInvoke on the
        // thread you are already on is cheap.
        var view = (sender as IOpenTokVideoSource)?.NativeView;
        MainThread.BeginInvokeOnMainThread(() => Show(view));
    }

    private void Show(WFrameworkElement? nativeView)
    {
        if (nativeView is null || PlatformView is null)
        {
            return;
        }

        Clear();

        // Removed from a previous parent first. Unlike UIView.AddSubview, which reparents silently,
        // adding a WinUI element that still has a parent throws — and the case that hits it is
        // ordinary: a subscriber tile moved between two views during a layout change.
        if (nativeView.Parent is WPanel previous)
        {
            previous.Children.Remove(nativeView);
        }

        nativeView.HorizontalAlignment = WHorizontalAlignment.Stretch;
        nativeView.VerticalAlignment = WVerticalAlignment.Stretch;

        PlatformView.Children.Add(nativeView);
    }

    /// <summary>
    /// Empties the container without disposing what it held.
    /// </summary>
    /// <remarks>
    /// The façade owns the video view and reuses it — removing it from a parent is detaching, not
    /// destroying. Disposing it here would break a source moved between two
    /// <see cref="OpenTokVideoView"/> instances, and would also dispose the renderer the SDK is
    /// still holding a reference to.
    /// </remarks>
    private void Clear() => PlatformView?.Children.Clear();
}
