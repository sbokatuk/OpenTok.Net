#if IOS
using ObjCRuntime;
using OpenTok.Net.iOS;
#endif
#if ANDROID
using Com.Opentok.Android;
#endif

namespace OpenTok.Net.DeviceTests;

/// <summary>A single on-device check. Throws to fail.</summary>
/// <param name="Name">Human readable name, reported to the platform log.</param>
/// <param name="Execute">Runs the check.</param>
public sealed record SmokeTest(string Name, Action Execute);

/// <summary>
/// End-to-end checks that only mean anything on a real device or simulator: they load the native
/// OpenTok framework out of the packaged binding and construct real native objects through it.
/// </summary>
/// <remarks>
/// Unlike a façade repository, OpenTok.Net has no shared cross-platform API - it projects the two
/// native SDKs as-is, and <c>Com.Opentok.Android.Session</c> has nothing in common with
/// <c>OTSession</c> beyond both existing. So this file cannot be "one set of calls that happens to
/// compile on both heads"; each check is one native object graph per platform, guarded by
/// <c>#if ANDROID</c> / <c>#if IOS</c>, with the same intent on both sides: prove the packaged
/// native library actually loaded and the binding's selectors/JNI signatures still match it.
/// <para>
/// Nothing here calls <c>Connect</c>/<c>connect</c>. These construct objects and read properties
/// only - deliberately, so the suite never depends on network access or a real OpenTok API key and
/// never contacts Vonage's servers from CI.
/// </para>
/// </remarks>
public static class SmokeTests
{
    private const string FakeApiKey = "00000000";

    private const string FakeSessionId = "1_MX4wMH4-e2e-fake-session-id-for-device-tests";

    /// <summary>Writes a line to the platform log. Set by each head.</summary>
    public static Action<string> Reporter { get; set; } = _ => { };

    /// <summary>Every check, in the order they must run.</summary>
    public static SmokeTest[] All =>
    [
        new("the native OpenTok library is loaded", NativeLibraryIsLoaded),
        new("constructs a session against the native SDK", ConstructsSession),
        new("constructs and configures session settings", ConstructsSessionSettings),
    ];

    private static void Report(string message) => Reporter(message);

    private static void NativeLibraryIsLoaded()
    {
#if IOS
        // Not dlopen-by-path: OTXCFramework ships OpenTok.framework as a *framework of static
        // libraries* (see the MT7091 warning this package produces at build time), so its object
        // code is linked directly into this app's own executable rather than copied into
        // Frameworks/ as a separate Mach-O image - there is no "OpenTok.framework/OpenTok" file at
        // @rpath for dlopen to find, whether or not the xcframework linked correctly. That made
        // this check fail unconditionally (confirmed on a build where ConstructsSession, which
        // actually drives the SDK, passed). objc_getClass, not dlopen, is what actually resolves a
        // class either way - a dynamically linked framework's classes are visible through the exact
        // same runtime lookup - and gives the same "clearer failure than a bare dyld error" this
        // check was written for.
        var handle = Class.GetHandle("OTSession");
        Assert(handle != IntPtr.Zero, "objc_getClass(\"OTSession\") returned NULL - the xcframework " +
            "did not link into this app.");
#elif ANDROID
        // There is no Android equivalent of dlopen-by-name here: the .aar's native libraries load
        // implicitly the first time a JNI-backed type is touched, which ConstructsSession does. A
        // missing .so surfaces there as UnsatisfiedLinkError, not here.
        Report("Android has no separate native-load probe; see ConstructsSession.");
#else
        throw new PlatformNotSupportedException("OpenTok.Net.DeviceTests only runs on iOS or Android.");
#endif
    }

    private static void ConstructsSession()
    {
#if IOS
        var session = new OTSession(FakeApiKey, FakeSessionId, null);

        Assert(session.SessionId == FakeSessionId,
            $"OTSession.SessionId reads back as '{session.SessionId}', not the id it was constructed with.");

        Report($"OTSession constructed, sessionConnectionStatus={session.SessionConnectionStatus}");
#elif ANDROID
        var context = global::Android.App.Application.Context;

        var session = new Session.Builder(context, FakeApiKey, FakeSessionId).Build();
        Assert(session is not null, "Session.Builder.Build() returned null.");

        Assert(session!.SessionId == FakeSessionId,
            $"Session.SessionId reads back as '{session.SessionId}', not the id it was constructed with.");

        Report($"Session constructed, sessionId={session.SessionId}");
#else
        throw new PlatformNotSupportedException("OpenTok.Net.DeviceTests only runs on iOS or Android.");
#endif
    }

    private static void ConstructsSessionSettings()
    {
#if IOS
        var settings = new OTSessionSettings { IpWhitelist = false };

        Assert(!settings.IpWhitelist, "OTSessionSettings.IpWhitelist did not read back as set.");
#elif ANDROID
        // The Android SDK folds session-level settings into Session.Builder itself rather than a
        // separate settings object, so there is no direct analogue of OTSessionSettings to
        // construct here. What is worth re-checking on this side is that building two independent
        // Session instances from the same Builder shape does not share state between them - a
        // binding-layer field aliasing bug would otherwise show up as one session's id leaking into
        // the other's.
        var context = global::Android.App.Application.Context;

        var first = new Session.Builder(context, FakeApiKey, FakeSessionId).Build();
        var second = new Session.Builder(context, FakeApiKey, FakeSessionId + "-2").Build();
        Assert(first is not null && second is not null, "Session.Builder.Build() returned null.");

        Assert(first!.SessionId != second!.SessionId,
            "Two independently constructed Session objects report the same SessionId.");
#else
        throw new PlatformNotSupportedException("OpenTok.Net.DeviceTests only runs on iOS or Android.");
#endif
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
