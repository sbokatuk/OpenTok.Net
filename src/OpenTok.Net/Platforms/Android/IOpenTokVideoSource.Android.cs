namespace OpenTok.Net;

/// <summary>
/// Something that has a native video view to show — an <see cref="OpenTokPublisher"/> (the local
/// camera preview) or an <see cref="OpenTokSubscriber"/> (a remote participant).
/// </summary>
/// <remarks>
/// The Android declaration of the interface documented in
/// <c>Platforms/iOS/IOpenTokVideoSource.iOS.cs</c> — identical but for <see cref="NativeView"/>'s
/// type, which is what forces it to be declared per platform in the first place. Keep the two in
/// step.
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
    global::Android.Views.View? NativeView { get; }

    /// <summary>
    /// Raised when <see cref="NativeView"/> becomes available, for sources that do not have one
    /// from the start.
    /// </summary>
    event EventHandler? NativeViewAvailable;
}
