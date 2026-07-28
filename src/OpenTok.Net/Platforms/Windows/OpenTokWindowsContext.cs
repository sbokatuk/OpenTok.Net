using Microsoft.UI.Dispatching;
using OpenTok.Net.Win;

namespace OpenTok.Net;

/// <summary>
/// The one <c>OpenTok.Context</c> every façade object on Windows shares.
/// </summary>
/// <remarks>
/// <para>
/// Windows is the only platform of the three where the SDK has an explicit context object.
/// <c>Session</c>, <c>Publisher</c> and <c>Subscriber</c> are all built from one, they must be built
/// from the <em>same</em> one to interoperate, and it owns the native resources they sit on. iOS and
/// Android have no equivalent — their objects are constructed directly — so nothing in the façade's
/// shared API can carry it and it has to live here.
/// </para>
/// <para>
/// Created with an <see cref="OpenTokDispatcher"/> rather than through <c>Context.Instance</c>, and
/// that is the whole reason this type exists rather than a one-line singleton. The default context
/// raises every event on the SDK's own threads; the façade's contract is that events are raised on
/// the platform's callback thread and a UI app must marshal — but on Windows "must marshal" is not
/// a caution, it is <c>RPC_E_WRONG_THREAD</c> the first time a handler touches XAML. Binding the
/// context to the UI thread's dispatcher queue makes the Windows head behave the way an app
/// written against the iOS and Android heads already expects.
/// </para>
/// <para>
/// Consequently the first façade object must be constructed on a UI thread. That matches what
/// <see cref="OpenTokSession"/> already documents — drive one session from one thread, in a UI app
/// the UI thread — and failing loudly here is far kinder than the alternative, which is video that
/// works until the first event handler runs.
/// </para>
/// </remarks>
internal static class OpenTokWindowsContext
{
    private static readonly object Gate = new();
    private static Context? _context;
    private static int _references;

    /// <summary>
    /// The shared context, creating it if this is the first façade object. Each call must be paired
    /// with a <see cref="Release"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Called off a UI thread before any context exists.</exception>
    internal static Context Acquire()
    {
        lock (Gate)
        {
            if (_context is null)
            {
                var dispatcherQueue = DispatcherQueue.GetForCurrentThread()
                    ?? throw new InvalidOperationException(
                        "The first OpenTok object on Windows must be created on a UI thread. The " +
                        "SDK's context is bound to that thread's dispatcher queue so that session, " +
                        "publisher and subscriber events arrive somewhere they can safely touch the " +
                        "UI; there is no dispatcher queue on this thread to bind to.");

                _context = new Context(new OpenTokDispatcher(dispatcherQueue));
            }

            _references++;
            return _context;
        }
    }

    /// <summary>
    /// Drops one reference, disposing the context when the last façade object goes.
    /// </summary>
    /// <remarks>
    /// Reference counted rather than disposed with the session, because a publisher can outlive the
    /// session it was published to — republishing an existing publisher into a new session is a
    /// perfectly ordinary thing to do, and disposing the context underneath it would take its native
    /// resources with it.
    /// </remarks>
    internal static void Release()
    {
        lock (Gate)
        {
            if (_references == 0)
            {
                return;
            }

            _references--;

            if (_references > 0)
            {
                return;
            }

            _context?.Dispose();
            _context = null;
        }
    }
}
