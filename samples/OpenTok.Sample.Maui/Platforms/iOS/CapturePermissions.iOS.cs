namespace OpenTok.Sample.Maui;

/// <summary>The iOS half: MAUI's own permission prompts, which map to AVFoundation's.</summary>
public static partial class CapturePermissions
{
    private static async partial Task<bool> RequestPlatformAsync()
    {
        var camera = await Permissions.RequestAsync<Permissions.Camera>();
        var microphone = await Permissions.RequestAsync<Permissions.Microphone>();

        return camera == PermissionStatus.Granted && microphone == PermissionStatus.Granted;
    }
}
