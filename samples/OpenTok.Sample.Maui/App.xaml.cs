namespace OpenTok.Sample.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    /// <summary>The page, so the window's activation events can reach the session.</summary>
    private MainPage? _page;

    // A plain NavigationPage rather than a Shell: this sample is one page, and a Shell would add a
    // routing model that has nothing to do with what is being demonstrated.
    protected override Window CreateWindow(IActivationState? activationState)
    {
        _page = new MainPage();

        var window = new Window(new NavigationPage(_page));

        // The app-lifecycle half of backgrounding. The foreground service (see CaptureLifetime)
        // makes capture *legal* in the background; this tells the SDK whether it should still be
        // capturing at all.
        //
        // Deactivated/Activated rather than Stopped/Resumed: on Android the session should stand
        // down as soon as the activity is no longer in front, which is what Deactivated maps to.
        // Both are no-ops on iOS, whose SDK observes UIApplication itself — so this reads the same
        // on both platforms even though only one of them acts on it.
        window.Deactivated += (_, _) => _page?.PauseSession();
        window.Activated += (_, _) => _page?.ResumeSession();

        return window;
    }
}
