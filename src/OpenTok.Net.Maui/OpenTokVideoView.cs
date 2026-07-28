namespace OpenTok.Net.Maui;

/// <summary>
/// A MAUI view that shows an OpenTok video source — a local camera preview
/// (<see cref="OpenTokPublisher"/>) or a remote participant (<see cref="OpenTokSubscriber"/>).
/// </summary>
/// <remarks>
/// <para>
/// Set <see cref="Source"/> and the view shows it; set it to <see langword="null"/> and the view
/// empties. Nothing else is required — no handler registration beyond
/// <c>UseOpenTok()</c> in <c>MauiProgram</c>, and no per-platform code in the app.
/// </para>
/// <para>
/// <b>Why this exists.</b> OpenTok's SDKs hand a ready-made native view <em>out</em> of a publisher
/// or subscriber, rather than taking an app-supplied view in the way Agora's do. That inverts the
/// usual MAUI arrangement: the platform view a handler creates cannot be the video surface, because
/// the SDK has not made one yet at handler-creation time. So the platform view here is a plain
/// container, and the SDK's view is attached into it once it exists — which is also why
/// <see cref="IOpenTokVideoSource.NativeViewAvailable"/> exists. Every app consuming the raw
/// bindings has to write this; here it is written once.
/// </para>
/// </remarks>
/// <example>
/// <code language="xml">
/// &lt;opentok:OpenTokVideoView Source="{Binding LocalPublisher}" /&gt;
/// </code>
/// </example>
public class OpenTokVideoView : View
{
    /// <summary>Backing store for <see cref="Source"/>.</summary>
    public static readonly BindableProperty SourceProperty = BindableProperty.Create(
        nameof(Source),
        typeof(IOpenTokVideoSource),
        typeof(OpenTokVideoView),
        defaultValue: null,
        propertyChanged: OnSourceChanged);

    /// <summary>
    /// The publisher or subscriber to show, or <see langword="null"/> to show nothing.
    /// </summary>
    /// <remarks>
    /// Assigning a source whose video is not ready yet is fine and expected — a subscriber has none
    /// until it connects. The view attaches it when it arrives.
    /// </remarks>
    public IOpenTokVideoSource? Source
    {
        get => (IOpenTokVideoSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>
    /// Raised when <see cref="Source"/> changes, so the handler can re-attach.
    /// </summary>
    /// <remarks>
    /// An event rather than a property-mapper entry because the handler also has to
    /// subscribe and unsubscribe from the outgoing and incoming source's
    /// <see cref="IOpenTokVideoSource.NativeViewAvailable"/>, which needs both values — and a
    /// mapper is only handed the view.
    /// </remarks>
    internal event EventHandler<OpenTokVideoSourceChangedEventArgs>? SourceChanged;

    private static void OnSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (OpenTokVideoView)bindable;
        view.SourceChanged?.Invoke(
            view,
            new OpenTokVideoSourceChangedEventArgs(
                oldValue as IOpenTokVideoSource,
                newValue as IOpenTokVideoSource));
    }
}

/// <summary>Carries the outgoing and incoming sources of an <see cref="OpenTokVideoView"/>.</summary>
internal sealed class OpenTokVideoSourceChangedEventArgs(
    IOpenTokVideoSource? oldSource,
    IOpenTokVideoSource? newSource) : EventArgs
{
    /// <summary>The source being replaced, if any.</summary>
    public IOpenTokVideoSource? OldSource { get; } = oldSource;

    /// <summary>The source now being shown, if any.</summary>
    public IOpenTokVideoSource? NewSource { get; } = newSource;
}
