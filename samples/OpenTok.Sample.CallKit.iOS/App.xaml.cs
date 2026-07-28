namespace OpenTok.Sample.CallKit.iOS;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    // A plain NavigationPage rather than a Shell: this sample is one page, and a Shell would add a
    // routing model that has nothing to do with what is being demonstrated.
    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new NavigationPage(new MainPage()));
}
