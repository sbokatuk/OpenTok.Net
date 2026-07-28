namespace OpenTok.Sample.Maui;

/// <summary>The Android half: MAUI's own permission prompts, which map to the runtime permissions.</summary>
/// <remarks>
/// Identical to the iOS half rather than shared with it, because the shared file is the one place a
/// Windows implementation has to differ — and two five-line copies read better than a three-way
/// conditional over a thing that is only conditional once.
/// </remarks>
public static partial class CapturePermissions
{
    private static async partial Task<bool> RequestPlatformAsync()
    {
        var camera = await Permissions.RequestAsync<Permissions.Camera>();
        var microphone = await Permissions.RequestAsync<Permissions.Microphone>();

        return camera == PermissionStatus.Granted && microphone == PermissionStatus.Granted;
    }
}
