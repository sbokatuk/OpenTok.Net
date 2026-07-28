namespace OpenTok.Net.Maui;

/// <summary>Wires this package's views into a MAUI app.</summary>
public static class AppBuilderExtensions
{
    /// <summary>
    /// Registers the handler for <see cref="OpenTokVideoView"/>.
    /// </summary>
    /// <remarks>
    /// Call it from <c>MauiProgram.CreateMauiApp</c>:
    /// <code>
    /// builder.UseMauiApp&lt;App&gt;().UseOpenTok();
    /// </code>
    /// Without it an <see cref="OpenTokVideoView"/> resolves no handler and renders as an empty
    /// rectangle — no exception, no warning, just nothing on screen. Registering the handler in the
    /// package rather than leaving it to the app is the whole reason this one-line extension
    /// exists.
    /// </remarks>
    public static MauiAppBuilder UseOpenTok(this MauiAppBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureMauiHandlers(handlers =>
            handlers.AddHandler<OpenTokVideoView, OpenTokVideoViewHandler>());

        return builder;
    }
}
