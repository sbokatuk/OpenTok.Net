using OpenTok.Net.iOS;

namespace OpenTok.Net;

/// <summary>
/// The iOS half of <see cref="OpenTokSession"/>, over <c>OTSession</c> from the
/// <c>OpenTok.Net.iOS</c> binding.
/// </summary>
/// <remarks>
/// Two things shape this file, both of them iOS's rather than choices made here.
///
/// <para>
/// <b>Errors are out parameters, not exceptions.</b> Every <c>OTSession</c> call answers an
/// <c>OTError</c> that is null on success. They are surfaced through <see cref="OpenTokSession.Failed"/>
/// rather than thrown, so that a caller writes the same error handling on both platforms — Android
/// has no synchronous failure to catch at all, because its equivalents return void and report
/// through a listener.
/// </para>
/// <para>
/// <b>The delegate must be kept alive.</b> <c>OTSession.delegate</c> is a <em>weak</em> reference on
/// the Objective-C side, so nothing but this field keeps the delegate object reachable; letting it
/// be collected silently stops every callback, which presents as a session that connects and then
/// appears to do nothing.
/// </para>
/// </remarks>
public sealed partial class OpenTokSession
{
    private OTSession? _session;
    private SessionDelegate? _delegate;

    private partial void CreateNative()
    {
        _delegate = new SessionDelegate(this);
        _session = new OTSession(ApiKey, SessionId, _delegate);
    }

    private partial void ConnectNative(string token)
    {
        _session!.ConnectWithToken(token, out var error);
        ReportIfFailed(error);
    }

    private partial void DisconnectNative()
    {
        _session!.Disconnect(out var error);
        ReportIfFailed(error);
    }

    private partial void PublishNative(OpenTokPublisher publisher)
    {
        _session!.Publish(publisher.NativePublisher, out var error);
        ReportIfFailed(error);
    }

    private partial void UnpublishNative(OpenTokPublisher publisher) =>
        _session!.Unpublish(publisher.NativePublisher, out _);

    private partial void SubscribeNative(OpenTokSubscriber subscriber)
    {
        _session!.Subscribe(subscriber.NativeSubscriber, out var error);
        ReportIfFailed(error);
    }

    private partial void UnsubscribeNative(OpenTokSubscriber subscriber) =>
        _session!.Unsubscribe(subscriber.NativeSubscriber, out _);

    private partial void SignalNative(string? type, string? data, OpenTokConnection? to)
    {
        _session!.SignalWithType(type, data, (OTConnection?)to?.NativeConnection, out var error);
        ReportIfFailed(error);
    }

    private partial void ForceMuteAllNative(OpenTokStream[] except)
    {
        _session!.ForceMuteAll([.. except.Select(s => (OTStream)s.NativeStream)], out var error);
        ReportIfFailed(error);
    }

    private partial void DisableForceMuteNative()
    {
        _session!.DisableForceMute(out var error);
        ReportIfFailed(error);
    }

    private partial void ForceMuteStreamNative(OpenTokStream stream)
    {
        _session!.ForceMuteStream((OTStream)stream.NativeStream, out var error);
        ReportIfFailed(error);
    }

    private partial void ForceDisconnectNative(OpenTokConnection connection)
    {
        _session!.ForceDisconnect((OTConnection)connection.NativeConnection, out var error);
        ReportIfFailed(error);
    }

    private partial void SetEncryptionSecretNative(string secret)
    {
        _session!.SetEncryptionSecret(secret, out var error);
        ReportIfFailed(error);
    }

    private partial OpenTokCapabilities? GetCapabilitiesNative() =>
        _session?.Capabilities is { } c
            ? new OpenTokCapabilities(c.CanPublish, c.CanSubscribe, c.CanForceMute, c.CanForceDisconnect)
            : null;

    private partial string? OwnConnectionIdNative() => _session?.Connection?.ConnectionId;

    // Nothing to do. The iOS SDK observes UIApplication's own notifications and AVAudioSession
    // interruptions itself, so there is no OTSession call to make here — Android's is the outlier,
    // not this. See the doc comment on OpenTokSession.Pause for why the method exists anyway.
    private partial void PauseNative()
    {
    }

    private partial void ResumeNative()
    {
    }

    private partial void DisposeNative()
    {
        _session?.Dispose();
        _session = null;

        // Released only after the session is gone. The delegate is weakly held natively, but the
        // session can still call into it while it is being torn down.
        _delegate = null;
    }

    /// <summary>
    /// Forwards a synchronous <c>OTError</c> to <see cref="OpenTokSession.Failed"/>.
    /// </summary>
    /// <remarks>
    /// <c>error.Code</c> is <c>NSError</c>'s <c>nint</c>. Narrowed to <c>int</c> because every code
    /// in <c>OTSessionErrorCode</c> is a small positive number (the largest is 6004) — the cast
    /// cannot lose information for any value the SDK actually produces.
    /// </remarks>
    private void ReportIfFailed(OTError? error)
    {
        if (error is not null)
        {
            OnFailed(Convert(error));
        }
    }

    internal static OpenTokError Convert(OTError error) =>
        new((int)error.Code, error.LocalizedDescription ?? "unknown error");

    internal static OpenTokStream Convert(OTStream stream) =>
        new(stream.StreamId, stream.Name, stream.HasAudio, stream.HasVideo, stream);

    internal static OpenTokConnection Convert(OTConnection connection) =>
        new(connection.ConnectionId, connection.Data, connection);

    /// <summary>
    /// Translates <c>OTSessionDelegate</c>'s callbacks onto the façade's events.
    /// </summary>
    /// <remarks>
    /// A subclass of the bound <c>[Model]</c> class rather than the event-based API the binding also
    /// generates: <c>OTSessionDelegate</c>'s first five callbacks are <c>@required</c>, and the
    /// generated events set the delegate property behind the scenes — mixing the two would mean two
    /// objects competing for one weak delegate slot.
    /// </remarks>
    private sealed class SessionDelegate(OpenTokSession owner) : OTSessionDelegate
    {
        public override void DidConnect(OTSession session) => owner.OnConnected();

        public override void DidDisconnect(OTSession session) => owner.OnDisconnected();

        public override void DidFailWithError(OTSession session, OTError error) =>
            owner.OnFailed(Convert(error));

        public override void StreamCreated(OTSession session, OTStream stream) =>
            owner.OnStreamReceived(Convert(stream));

        public override void StreamDestroyed(OTSession session, OTStream stream) =>
            owner.OnStreamDropped(Convert(stream));

        // Everything below is @optional in the protocol. Overriding an optional method on a [Model]
        // subclass is what registers it as implemented, so these are only delivered because they
        // are here.

        public override void ConnectionCreated(OTSession session, OTConnection connection) =>
            owner.OnConnectionCreated(Convert(connection));

        public override void ConnectionDestroyed(OTSession session, OTConnection connection) =>
            owner.OnConnectionDestroyed(Convert(connection));

        public override void ReceivedSignalType(
            OTSession session,
            string? type,
            OTConnection? connection,
            string? stringData) =>
            owner.OnSignalReceived(type, stringData, connection is null ? null : Convert(connection));

        public override void ArchiveStartedWithId(OTSession session, string archiveId, string? name) =>
            owner.OnArchiveStarted(archiveId, name);

        public override void ArchiveStoppedWithId(OTSession session, string archiveId) =>
            owner.OnArchiveStopped(archiveId);

        public override void DidBeginReconnecting(OTSession session) => owner.OnReconnecting();

        public override void DidReconnect(OTSession session) => owner.OnReconnected();

        public override void MuteForced(OTSession session, OTMuteForcedInfo muteForcedInfo) =>
            owner.OnMuteForced(muteForcedInfo.Active);
    }
}
