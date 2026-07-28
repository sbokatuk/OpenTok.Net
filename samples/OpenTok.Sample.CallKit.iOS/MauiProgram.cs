using Microsoft.Extensions.Logging;
using OpenTok.Net.Maui;

namespace OpenTok.Sample.CallKit.iOS;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            // Registers the handler for OpenTokVideoView. Without this the view resolves no
            // handler and renders as an empty rectangle — no exception, nothing in the log.
            .UseOpenTok()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
