using UIKit;

namespace OpenTok.Net;

/// <summary>
/// Something that has a native video view to show — an <see cref="OpenTokPublisher"/> (the local
/// camera preview) or an <see cref="OpenTokSubscriber"/> (a remote participant).
/// </summary>
/// <remarks>
/// <para>
/// The one deliberately platform-typed member of this package's API. <see cref="NativeView"/> is a
/// <c>UIView</c> here and an <c>Android.Views.View</c> on Android, so the declaration lives under
/// <c>Platforms/</c> and is compiled once per platform rather than once. It cannot be unified
/// without inventing a wrapper type, and the only consumer that needs it —
/// <c>OpenTok.Net.Maui</c>'s handler — is itself per-platform, so a unified type would be
/// ceremony for nobody's benefit.
/// </para>
/// <para>
/// Public rather than internal so that an app rendering video its own way (a custom handler, a
/// plain .NET for iOS app with no MAUI at all) can reach the view without this package having to
/// grant it friend access.
/// </para>
/// </remarks>
public interface IOpenTokVideoSource
{
    /// <summary>
    /// The SDK-created view, or <see langword="null"/> when there is nothing to show yet.
    /// </summary>
    /// <remarks>
    /// Null-until-ready is the subscriber's case: it has no decoded video until it connects to its
    /// stream. Watch <see cref="NativeViewAvailable"/> rather than polling.
    /// </remarks>
    UIView? NativeView { get; }

    /// <summary>
    /// Raised when <see cref="NativeView"/> becomes available, for sources that do not have one
    /// from the start.
    /// </summary>
    event EventHandler? NativeViewAvailable;
}
