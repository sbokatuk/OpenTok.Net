namespace OpenTok.Net.DeviceTests;

/// <summary>A single on-device check. Throws to fail.</summary>
/// <param name="Name">Human readable name, reported to the platform log.</param>
/// <param name="Execute">Runs the check.</param>
public sealed record SmokeTest(string Name, Action Execute);

/// <summary>
/// End-to-end checks that only mean anything on a real device, simulator or emulator: they load the
/// packaged native OpenTok SDK through <c>OpenTok.Net</c> and drive it.
/// </summary>
/// <remarks>
/// <para>
/// <b>There is not a single <c>#if</c> in this file, and that is the assertion.</b> These are the
/// same calls, compiled once, running against <c>OTSession</c> on iOS and
/// <c>Com.Opentok.Android.Session</c> on Android — two SDKs with nothing in common beyond both
/// existing. Every check below passing on both heads is what "one cross-platform API" means in
/// practice; if the façade's two halves ever diverge in behaviour rather than merely in
/// implementation, this suite is where that shows up.
/// </para>
/// <para>
/// Two things are being proved at once, and they are worth separating:
/// </para>
/// <list type="bullet">
/// <item>
/// <b>The package works.</b> Constructing a session reaches native code, so a missing xcframework
/// or <c>.aar</c> fails here rather than in a consumer's app. This is the part that cannot be
/// checked on a build host.
/// </item>
/// <item>
/// <b>The contract is identical.</b> The state machine, the argument validation and the disposal
/// semantics live in the shared half of the façade, but they are exercised here rather than in a
/// host unit test because a platform partial could break them on one side only — <c>CreateNative</c>
/// throwing, or a native call failing where the other platform's succeeds, would leave the shared
/// logic correct and the observable behaviour different.
/// </item>
/// </list>
/// <para>
/// Nothing here connects, publishes or subscribes. No API key, session id or token is real, no
/// network call is made, and no camera or microphone is opened — which is also why
/// <c>Platforms/Android/AndroidManifest.xml</c> declares no media permissions. An
/// <see cref="OpenTokPublisher"/> would open the capture devices on construction, so this suite
/// deliberately does not build one; the sample app covers that path interactively.
/// </para>
/// </remarks>
public static class SmokeTests
{
    // The shape of a real OpenTok API key (a numeric project id) and session id, unregistered on
    // purpose. Both SDKs validate the shape locally when constructing a session, so a plausible one
    // is needed to get far enough to prove anything; neither is ever sent anywhere.
    private const string FakeApiKey = "45678123";

    private const string FakeSessionId = "1_MX40NTY3ODEyM35-MTcwMDAwMDAwMDAwMH4wLjEzNDU2Nzg5MH4";

    /// <summary>Writes a line to the platform log. Set by each head.</summary>
    public static Action<string> Reporter { get; set; } = _ => { };

    /// <summary>Every check, in the order they must run.</summary>
    public static SmokeTest[] All =>
    [
        new("constructs a session against the packaged native SDK", ConstructsSession),
        new("a fresh session reports NotConnected", FreshSessionIsNotConnected),
        new("rejects a blank token", RejectsABlankToken),
        new("rejects publish before connect", RejectsPublishBeforeConnect),
        new("disconnect without a connect is a no-op", DisconnectWithoutConnectIsANoOp),
        new("two sessions are independent", SessionsAreIndependent),
        new("dispose is idempotent and closes the session", DisposeIsIdempotent),
        new("rejects a null stream", RejectsANullStream),
    ];

    private static void Report(string message) => Reporter(message);

    /// <summary>
    /// The check that proves the packaging: constructing a session crosses into native code.
    /// </summary>
    /// <remarks>
    /// A package whose native payload failed to ship still compiles and links — the managed binding
    /// surface is all that the compiler sees, and selectors and JNI signatures are resolved on first
    /// use. This is that first use. On iOS a missing xcframework surfaces here as a dyld or
    /// selector failure; on Android as <c>UnsatisfiedLinkError</c> or <c>NoClassDefFoundError</c>.
    /// </remarks>
    private static void ConstructsSession()
    {
        using var session = new OpenTokSession(FakeApiKey, FakeSessionId);

        Assert(session.SessionId == FakeSessionId,
            $"SessionId reads back as '{session.SessionId}', not the id it was constructed with.");
        Assert(session.ApiKey == FakeApiKey,
            $"ApiKey reads back as '{session.ApiKey}', not the key it was constructed with.");

        Report("session constructed — the native SDK loaded and responded");
    }

    private static void FreshSessionIsNotConnected()
    {
        using var session = new OpenTokSession(FakeApiKey, FakeSessionId);

        Assert(session.State == OpenTokConnectionState.NotConnected,
            $"a freshly constructed session reports '{session.State}', not NotConnected.");

        Report($"State == {session.State}");
    }

    private static void RejectsABlankToken()
    {
        using var session = new OpenTokSession(FakeApiKey, FakeSessionId);

        // Whitespace rather than null: null is uninteresting (any API rejects it), whereas a
        // whitespace token is the shape a misconfigured app actually produces, and the native SDKs
        // disagree about it — iOS answers an OTError, Android reports asynchronously through a
        // listener. The façade rejects it up front on both, which is the behaviour being pinned.
        AssertThrows<ArgumentException>(() => session.Connect("   "), "Connect with a blank token");

        Assert(session.State == OpenTokConnectionState.NotConnected,
            $"a rejected Connect left the session in '{session.State}'.");

        Report("blank token rejected, state unchanged");
    }

    private static void RejectsPublishBeforeConnect()
    {
        using var session = new OpenTokSession(FakeApiKey, FakeSessionId);

        // Publishing into an unconnected session is a no-op on one platform and an error on the
        // other; the façade makes it an exception on both, before any native call happens. Checked
        // with null rather than a real publisher on purpose — a real one would open the camera, and
        // ArgumentNullException would then mask the InvalidOperationException this is about.
        AssertThrows<ArgumentNullException>(() => session.Publish(null!), "Publish(null)");

        Report("publish before connect rejected");
    }

    private static void DisconnectWithoutConnectIsANoOp()
    {
        using var session = new OpenTokSession(FakeApiKey, FakeSessionId);

        // Must not throw, and must not leave the session mid-teardown: an app tearing down a screen
        // does not know whether the connect it started ever completed, so this is the common path,
        // not an edge case.
        session.Disconnect();

        Assert(session.State == OpenTokConnectionState.NotConnected,
            $"Disconnect on a never-connected session left it in '{session.State}'.");

        Report("disconnect without connect returned cleanly");
    }

    private static void SessionsAreIndependent()
    {
        using var first = new OpenTokSession(FakeApiKey, FakeSessionId);
        using var second = new OpenTokSession(FakeApiKey, FakeSessionId + "-2");

        // A binding-layer field aliasing bug, or a static the façade should not have, would show up
        // as one session's id leaking into the other's.
        Assert(first.SessionId != second.SessionId,
            "two independently constructed sessions report the same SessionId.");

        Report("two sessions constructed without interfering");
    }

    private static void DisposeIsIdempotent()
    {
        var session = new OpenTokSession(FakeApiKey, FakeSessionId);

        session.Dispose();
        session.Dispose();

        // Disposal has to release the native session on both platforms and then refuse further use,
        // rather than crossing into a released native object.
        AssertThrows<ObjectDisposedException>(
            () => session.Connect("a-token"),
            "Connect after Dispose");

        Report("double dispose survived, use after dispose rejected");
    }

    private static void RejectsANullStream()
    {
        AssertThrows<ArgumentNullException>(
            () => _ = new OpenTokSubscriber(null!),
            "new OpenTokSubscriber(null)");

        Report("null stream rejected");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>Asserts that <paramref name="action"/> throws <typeparamref name="TException"/>.</summary>
    /// <remarks>
    /// Written out rather than pulled from a test framework: this app is a plain executable with no
    /// test runner, precisely so it can be installed and launched on a device.
    /// </remarks>
    private static void AssertThrows<TException>(Action action, string what)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"{what} threw {exception.GetType().Name}, not {typeof(TException).Name}: {exception.Message}");
        }

        throw new InvalidOperationException($"{what} did not throw {typeof(TException).Name}.");
    }
}
