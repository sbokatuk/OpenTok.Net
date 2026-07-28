using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;

namespace OpenTok.Sample.Maui;

/// <summary>
/// A foreground service that keeps the camera and microphone usable while the app is in the
/// background.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is not optional, and it is not about being polite.</b> Since Android 14 an app in the
/// background is refused camera and microphone access unless a foreground service with the matching
/// <c>foregroundServiceType</c> is running. There is no exception and no error: the app keeps
/// running, the OpenTok session stays connected, and it simply captures nothing — which the *other*
/// participant experiences as a frozen frame and silence. Any OpenTok Android app that can be
/// backgrounded mid-call needs this, telecom or not.
/// </para>
/// <para>
/// <b>The 10-second rule.</b> <c>StartForeground</c> must be called within a few seconds of
/// <c>StartForegroundService</c>, or the system kills the process with a
/// <c>ForegroundServiceDidNotStartInTimeException</c>. So the notification is built and posted in
/// <see cref="OnStartCommand"/>, before anything else — never after work that might block.
/// </para>
/// <para>
/// It carries no OpenTok state. The session lives in the app; this service exists purely to hold
/// the process in the foreground, which is why it is <c>StartCommandResult.Sticky</c>-free and does
/// nothing on rebind.
/// </para>
/// </remarks>
[Service(
    Exported = false,
    ForegroundServiceType = ForegroundService.TypeCamera | ForegroundService.TypeMicrophone,
    Name = "com.sbokatuk.opentok.sample.OpenTokCaptureService")]
public sealed class OpenTokCaptureService : Service
{
    private const string ChannelId = "opentok_call";
    private const int NotificationId = 1;

    /// <summary>Starts the service, so capture survives backgrounding.</summary>
    public static void Start(Context context)
    {
        var intent = new Intent(context, typeof(OpenTokCaptureService));

        // StartForegroundService, not StartService: from the background the latter throws
        // IllegalStateException on anything since Android 8.
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            context.StartForegroundService(intent);
        }
        else
        {
            context.StartService(intent);
        }
    }

    /// <summary>Stops the service. Call it when the call ends, not when the app backgrounds.</summary>
    public static void Stop(Context context) =>
        context.StopService(new Intent(context, typeof(OpenTokCaptureService)));

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        CreateNotificationChannel();

        // Every NotificationCompat.Builder setter is bound as returning a nullable Builder — the
        // Java API is annotation-free here — so the chain is written out rather than fluent, and
        // the one nullable result that matters is checked once at the end.
        var builder = new NotificationCompat.Builder(this, ChannelId);
        builder.SetContentTitle("Call in progress");
        builder.SetContentText("OpenTok is using the camera and microphone");
        builder.SetSmallIcon(global::Android.Resource.Drawable.PresenceVideoOnline);
        builder.SetOngoing(true);
        builder.SetCategory(NotificationCompat.CategoryCall);

        var notification = builder.Build();
        if (notification is null)
        {
            // Cannot happen in practice, but StartForeground must still be called within seconds
            // or the process is killed — so failing loudly here beats being killed opaquely.
            throw new InvalidOperationException("the call notification could not be built.");
        }

        // The three-argument overload from Android 10 on: the type has to be restated here as well
        // as in the manifest, and the runtime rejects a type the manifest did not declare.
        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            StartForeground(
                NotificationId,
                notification,
                ForegroundService.TypeCamera | ForegroundService.TypeMicrophone);
        }
        else
        {
            StartForeground(NotificationId, notification);
        }

        // NotSticky: if the system kills this, the call is already gone — restarting a capture
        // service with no session behind it would hold the camera for nothing.
        return StartCommandResult.NotSticky;
    }

    private void CreateNotificationChannel()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            return;
        }

        var channel = new NotificationChannel(
            ChannelId,
            "Calls",
            NotificationImportance.Low)
        {
            Description = "Shown while a call is using the camera and microphone",
        };

        var manager = (NotificationManager?)GetSystemService(NotificationService);
        manager?.CreateNotificationChannel(channel);
    }
}
