namespace OpenTok.Sample.Maui;

/// <summary>
/// Asks for camera and microphone access, where asking is a thing the platform does.
/// </summary>
/// <remarks>
/// <para>
/// The second and last place this sample branches, for the same reason as
/// <see cref="CaptureLifetime"/>: the platforms differ in whether the operation exists at all, not
/// merely in how it is spelled.
/// </para>
/// <para>
/// <b>iOS and Android</b> gate camera and microphone behind a runtime prompt, and an app that
/// starts publishing without asking gets a black frame and silence. <b>Windows</b> desktop has no
/// runtime prompt — access is a system-wide switch under Settings &gt; Privacy that the user owns
/// and the app cannot raise — so there is nothing to await and nothing to be refused.
/// </para>
/// <para>
/// A shim rather than a <c>#if</c> in <c>MainPage.xaml.cs</c>, which is deliberately free of them:
/// the point that file makes is that the OpenTok API needs no platform branching, and mixing in an
/// unrelated one would blunt it.
/// </para>
/// </remarks>
public static partial class CapturePermissions
{
    /// <summary>
    /// Returns whether capture may proceed. Never throws; a refusal is reported as
    /// <see langword="false"/>.
    /// </summary>
    public static Task<bool> RequestAsync() => RequestPlatformAsync();

    private static partial Task<bool> RequestPlatformAsync();
}
