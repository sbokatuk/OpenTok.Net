using Foundation;
using UIKit;

namespace OpenTok.Net.DeviceTests;

/// <summary>
/// Host for the on-simulator checks. Runs every one on launch, reports the outcome to stdout -
/// which <c>simctl launch --console-pty</c> streams straight back to CI - and exits with a verdict
/// line the runner script greps for.
/// </summary>
public static class Program
{
    private static void Main(string[] args) => UIApplication.Main(args, null, typeof(AppDelegate));
}

[Register(nameof(AppDelegate))]
public sealed class AppDelegate : UIApplicationDelegate
{
    public override UIWindow? Window { get; set; }

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        // A window is not strictly needed for a headless run, but iOS terminates an app that never
        // presents one, which would look like a crash rather than a failing check.
        var root = new UIViewController();
        root.View!.BackgroundColor = UIColor.White;

#pragma warning disable CA1422
        Window = new UIWindow(UIScreen.MainScreen.Bounds) { RootViewController = root };
#pragma warning restore CA1422
        Window.MakeKeyAndVisible();

        TestRunner.RunAndReport(
            Console.WriteLine,
            exitCode =>
            {
                // Drain before exiting, and this is not belt-and-braces — it is the fix for an
                // observed failure. simctl streams stdout over a pty, and Environment.Exit closes
                // that pty the instant it is called. A run on net10.0-ios26.0 passed all eight
                // checks and still failed the job: the last two PASS lines and the
                // OPENTOK_E2E_DONE marker were still in the buffer when the process died, so the
                // runner script grepped a truncated log. The os_log dump it captured on the way out
                // showed every line present, which is how the race was identified.
                //
                // TestRunner's own doc comment already notes this hazard for Android, where the
                // runner deliberately does not kill the app. iOS has to terminate — otherwise
                // simctl launch --console-pty never returns — so it flushes and waits instead.
                Console.Out.Flush();
                Thread.Sleep(TimeSpan.FromSeconds(1));

                Environment.Exit(exitCode);
            });

        return true;
    }
}
