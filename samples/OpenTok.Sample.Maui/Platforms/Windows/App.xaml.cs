using Microsoft.UI.Xaml;

namespace OpenTok.Sample.Maui.WinUI;

/// <summary>The Windows entry point. Everything real is in <see cref="MauiProgram"/>.</summary>
public partial class App : MauiWinUIApplication
{
    public App() => InitializeComponent();

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
