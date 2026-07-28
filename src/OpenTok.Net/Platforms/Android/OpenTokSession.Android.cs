using Android.Content;
using Com.Opentok.Android;

// ImplicitUsings puts System.IO in scope, where Stream is the far more famous type — so the plain
// name is ambiguous. Aliased once here rather than fully qualified at each use.
using OpenTokNativeStream = Com.Opentok.Android.Stream;

namespace OpenTok.Net;

/// <summary>
/// The Android half of <see cref="OpenTokSession"/>, over <c>Com.Opentok.Android.Session</c> from
/// the <c>OpenTok.Net.Android</c> binding.
/// </summary>
/// <remarks>
/// <para>
/// Where the iOS side answers an <c>OTError</c> out parameter from every call, Android's equivalents
/// return void and report through listeners — so there is nothing synchronous to surface here, and
/// everything arrives on the <c>Error</c> event. That asymmetry is precisely what the façade exists
/// to absorb: a caller writes one error path.
/// </para>
/// <para>
/// The listener handlers are attached as events rather than by implementing
/// <c>Session.ISessionListener</c>. The binding generates both; the events avoid a
/// <c>Java.Lang.Object</c> subclass per listener, and their event args carry named properties
/// (<c>e.Stream</c>, <c>e.Error</c>) rather than <c>e.P0</c>/<c>e.P1</c> — see the parameter renames
/// in that repository's <c>Transforms/Metadata.xml</c>.
/// </para>
/// <para>
/// <b>Context.</b> Android's SDK needs one to construct a session. <c>Application.Context</c> is
/// used rather than the current activity deliberately: a session routinely outlives the activity
/// that created it (a rotation recreates the activity), and holding an activity reference in a
/// long-lived object is the classic Android leak.
/// </para>
/// </remarks>
public sealed partial class OpenTokSession
{
    private Session? _session;

    internal static Context AppContext => global::Android.App.Application.Context;

    private partial void CreateNative()
    {
        var session = new Session.Builder(AppContext, ApiKey, SessionId).Build();

        session.Connected += OnNativeConnected;
        session.Disconnected += OnNativeDisconnected;
        session.Error += OnNativeError;
        session.StreamReceived += OnNativeStreamReceived;
        session.StreamDropped += OnNativeStreamDropped;

        _session = session;
    }

    private partial void ConnectNative(string token) => _session!.Connect(token);

    private partial void DisconnectNative() => _session!.Disconnect();

    private partial void PublishNative(OpenTokPublisher publisher) =>
        _session!.Publish(publisher.NativePublisher);

    private partial void UnpublishNative(OpenTokPublisher publisher) =>
        _session!.Unpublish(publisher.NativePublisher);

    private partial void SubscribeNative(OpenTokSubscriber subscriber) =>
        _session!.Subscribe(subscriber.NativeSubscriber);

    private partial void UnsubscribeNative(OpenTokSubscriber subscriber) =>
        _session!.Unsubscribe(subscriber.NativeSubscriber);

    private partial void DisposeNative()
    {
        if (_session is null)
        {
            return;
        }

        // Detached explicitly. These are C# event subscriptions onto a Java peer, and leaving them
        // attached keeps this OpenTokSession reachable from the binding's listener implementor for
        // as long as the Java object lives.
        _session.Connected -= OnNativeConnected;
        _session.Disconnected -= OnNativeDisconnected;
        _session.Error -= OnNativeError;
        _session.StreamReceived -= OnNativeStreamReceived;
        _session.StreamDropped -= OnNativeStreamDropped;

        _session.Dispose();
        _session = null;
    }

    private void OnNativeConnected(object? sender, Session.ConnectedEventArgs e) => OnConnected();

    private void OnNativeDisconnected(object? sender, Session.DisconnectedEventArgs e) => OnDisconnected();

    private void OnNativeError(object? sender, Session.ErrorEventArgs e) => OnFailed(Convert(e.Error));

    private void OnNativeStreamReceived(object? sender, Session.StreamReceivedEventArgs e)
    {
        if (e.Stream is not null)
        {
            OnStreamReceived(Convert(e.Stream));
        }
    }

    private void OnNativeStreamDropped(object? sender, Session.StreamDroppedEventArgs e)
    {
        if (e.Stream is not null)
        {
            OnStreamDropped(Convert(e.Stream));
        }
    }

    /// <summary>Flattens an <c>OpentokError</c> onto the façade's error type.</summary>
    /// <remarks>
    /// <c>GetErrorCode()</c> returns a Java enum wrapper, not a number — <c>OpentokError.ErrorCode</c>
    /// is a nested <em>type</em>. Its <c>GetErrorCode()</c> is what yields the numeric value, hence
    /// the double hop.
    /// </remarks>
    internal static OpenTokError Convert(OpentokError? error) => error is null
        ? new OpenTokError(0, "unknown error")
        : new OpenTokError(error.GetErrorCode()?.GetErrorCode() ?? 0, error.Message ?? "unknown error");

    internal static OpenTokStream Convert(OpenTokNativeStream stream) =>
        new(stream.StreamId!, stream.Name, stream.HasAudio, stream.HasVideo, stream);
}
