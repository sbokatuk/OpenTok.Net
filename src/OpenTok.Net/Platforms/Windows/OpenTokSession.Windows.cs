using WindowsSession = OpenTok.Session;

namespace OpenTok.Net;

/// <summary>
/// The Windows half of <see cref="OpenTokSession"/>, over <c>OpenTok.Session</c> from Vonage's
/// <c>OpenTok.Client</c> package.
/// </summary>
/// <remarks>
/// <para>
/// The least like the other two heads, because the SDK underneath is not a binding — it is
/// hand-written managed .NET, with .NET events and a builder instead of an Objective-C delegate or
/// a Java listener. That makes this file the short one: there is no delegate object to keep alive
/// against a weak native reference, and no listener class to translate.
/// </para>
/// <para>
/// <b>Errors arrive only as events.</b> Unlike iOS, no call here returns an error to inspect;
/// unlike Android, there is no listener interface to implement. Everything goes through
/// <c>Session.Error</c>, which is forwarded to <see cref="OpenTokSession.Failed"/>.
/// </para>
/// <para>
/// <b>Some of the façade has no Windows equivalent.</b> Encryption secrets and pause/resume are
/// documented no-ops below rather than throwing, because the façade's contract is that an app
/// written once runs on all three platforms — a method that throws on one of them is not a façade.
/// </para>
/// </remarks>
public sealed partial class OpenTokSession
{
    private WindowsSession? _session;
    private Context? _context;

    private partial void CreateNative()
    {
        _context = OpenTokWindowsContext.Acquire();

        _session = new WindowsSession.Builder(_context, ApiKey, SessionId).Build();

        _session.Connected += (_, _) => OnConnected();
        _session.Disconnected += (_, _) => OnDisconnected();
        _session.Error += (_, e) => OnFailed(new OpenTokError((int)e.ErrorCode, e.ErrorDescription ?? "unknown error"));

        _session.StreamReceived += (_, e) => OnStreamReceived(Convert(e.Stream));
        _session.StreamDropped += (_, e) => OnStreamDropped(Convert(e.Stream));

        _session.ConnectionCreated += (_, e) => OnConnectionCreated(Convert(e.Connection));
        _session.ConnectionDropped += (_, e) => OnConnectionDestroyed(Convert(e.Connection));

        _session.Signal += (_, e) =>
            OnSignalReceived(e.Type, e.Data, e.Connection is null ? null : Convert(e.Connection));

        _session.ArchiveStarted += (_, e) => OnArchiveStarted(e.ArchiveId, e.ArchiveName);
        _session.ArchiveStopped += (_, e) => OnArchiveStopped(e.ArchiveId);

        // Named for the transition rather than the state, unlike iOS's DidBeginReconnecting /
        // DidReconnect — the same two moments under different names.
        _session.ReconnectionStart += (_, _) => OnReconnecting();
        _session.ReconnectionSuccess += (_, _) => OnReconnected();

        _session.MuteForced += (_, e) => OnMuteForced(e.IsActive);
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

    // The trailing false is retryAfterReconnect. Left off to match iOS and Android, where a signal
    // sent while reconnecting is simply lost rather than queued — the façade cannot offer a
    // guarantee that only one of its three platforms can keep.
    private partial void SignalNative(string? type, string? data, OpenTokConnection? to) =>
        _session!.SendSignal(type, data, (Connection?)to?.NativeConnection, false);

    private partial void ForceMuteAllNative(OpenTokStream[] except) =>
        _session!.ForceMuteAll([.. except.Select(s => (Stream)s.NativeStream)]);

    private partial void DisableForceMuteNative() => _session!.DisableForceMute();

    private partial void ForceMuteStreamNative(OpenTokStream stream) =>
        _session!.ForceMuteStream((Stream)stream.NativeStream);

    private partial void ForceDisconnectNative(OpenTokConnection connection) =>
        _session!.ForceDisconnect((Connection)connection.NativeConnection);

    private partial void SetEncryptionSecretNative(string secret)
    {
        // No Windows equivalent. End-to-end encryption is exposed on iOS (setEncryptionSecret) and
        // Android, but OpenTok.Client 2.34.1 has no corresponding member on Session — checked
        // against its own XML documentation, not inferred.
        //
        // Silent rather than throwing: an app that calls this and runs on all three platforms would
        // otherwise crash only on Windows. Failing here would also be misleading, since the media
        // itself is still transport-encrypted.
        _ = secret;
    }

    private partial void PauseNative()
    {
        // Nothing to do, for the same reason as iOS: Windows has no application lifecycle event
        // that stops the camera, so there is nothing to hand back. Android is the outlier.
    }

    private partial void ResumeNative()
    {
    }

    private partial OpenTokCapabilities? GetCapabilitiesNative() =>
        _session?.Capabilities is { } c
            ? new OpenTokCapabilities(c.CanPublish, c.CanSubscribe, c.CanForceMute, c.CanForceDisconnect)
            : null;

    private partial string? OwnConnectionIdNative() => _session?.Connection?.Id;

    private partial void DisposeNative()
    {
        _session?.Dispose();
        _session = null;

        if (_context is not null)
        {
            _context = null;
            OpenTokWindowsContext.Release();
        }
    }

    internal static OpenTokStream Convert(Stream stream) =>
        new(stream.Id, stream.Name, stream.HasAudio, stream.HasVideo, stream);

    internal static OpenTokConnection Convert(Connection connection) =>
        new(connection.Id, connection.Data, connection);
}
