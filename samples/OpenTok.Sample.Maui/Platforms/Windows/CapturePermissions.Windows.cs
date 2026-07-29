namespace OpenTok.Sample.Maui;

/// <summary>The Windows half: nothing to ask.</summary>
/// <remarks>
/// <para>
/// A Windows desktop app has no runtime camera or microphone prompt. Access is governed by a
/// system-wide switch under Settings &gt; Privacy &amp; security that the user owns; an app cannot
/// raise it and is not told about it. If capture is blocked there, the SDK simply produces no
/// frames.
/// </para>
/// <para>
/// So this returns true rather than calling MAUI's <c>Permissions</c> API. That API's behaviour on
/// an unpackaged Windows app is version-dependent — it either reports <c>Granted</c> immediately or
/// declines to answer — and neither outcome means the user refused anything. Treating a
/// non-committal answer as a refusal would leave the sample unable to publish on the one platform
/// where nothing was ever denied.
/// </para>
/// </remarks>
public static partial class CapturePermissions
{
    private static partial Task<bool> RequestPlatformAsync() => Task.FromResult(true);
}
