using Microsoft.UI.Xaml;

namespace OpenTok.Net;

/// <summary>
/// Something that has a native video view to show — an <see cref="OpenTokPublisher"/> (the local
/// camera preview) or an <see cref="OpenTokSubscriber"/> (a remote participant).
/// </summary>
/// <remarks>
/// <para>
/// The one deliberately platform-typed member of this package's API. <see cref="NativeView"/> is a
/// <c>FrameworkElement</c> here, a <c>UIView</c> on iOS and an <c>Android.Views.View</c> on Android,
/// so the declaration lives under <c>Platforms/</c> and is compiled once per platform.
/// </para>
/// <para>
/// Windows differs from the other two in where the view comes from. On iOS and Android the SDK
/// creates it and the façade hands it over; here the SDK creates nothing — it renders into an
/// <c>IVideoRenderer</c> that the caller supplies. So the view is an <c>OpenTokVideoView</c> from
/// <c>OpenTok.Net.Win</c>, constructed by the façade, and it exists from the moment the
/// publisher or subscriber does rather than appearing later.
/// </para>
/// </remarks>
public interface IOpenTokVideoSource
{
    /// <summary>
    /// The view to display, or <see langword="null"/> once disposed.
    /// </summary>
    /// <remarks>
    /// Unlike iOS and Android this is non-null from construction, because on Windows the façade owns
    /// the view rather than waiting for the SDK to produce one. <see cref="NativeViewAvailable"/> is
    /// still raised once, immediately, so that a consumer written against the other two platforms
    /// works unchanged.
    /// </remarks>
    FrameworkElement? NativeView { get; }

    /// <summary>
    /// Raised when <see cref="NativeView"/> becomes available, for sources that do not have one
    /// from the start.
    /// </summary>
    event EventHandler? NativeViewAvailable;
}
