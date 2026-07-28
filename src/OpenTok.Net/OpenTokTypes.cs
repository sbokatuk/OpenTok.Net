namespace OpenTok.Net;

/// <summary>Where a <see cref="OpenTokSession"/> is in its connection lifecycle.</summary>
/// <remarks>
/// A deliberately smaller set than either native SDK exposes. iOS reports six states through
/// <c>OTSessionConnectionStatus</c>; Android reports none at all, only callbacks. This enum is the
/// intersection that both platforms can actually answer for, tracked by this façade rather than
/// asked of the SDK, so it means the same thing on both.
/// </remarks>
public enum OpenTokConnectionState
{
    /// <summary>Never connected, or fully disconnected.</summary>
    NotConnected,

    /// <summary><see cref="OpenTokSession.Connect"/> has been called and no outcome has arrived.</summary>
    Connecting,

    /// <summary>Connected. Publishing and subscribing are possible.</summary>
    Connected,

    /// <summary><see cref="OpenTokSession.Disconnect"/> has been called and has not completed.</summary>
    Disconnecting,
}

/// <summary>
/// A remote media stream in a session — one other participant's audio and/or video.
/// </summary>
/// <remarks>
/// A snapshot of the native stream's identifying fields, not a live view of it. The native objects
/// (iOS <c>OTStream</c>, Android <c>Com.Opentok.Android.Stream</c>) are owned by their SDK and are
/// invalidated when the stream ends; copying the fields out at the point the stream is reported
/// means a handler can hold on to this safely, which is what application code invariably wants.
/// Pass it to <see cref="OpenTokSubscriber"/> to receive the stream.
/// </remarks>
public sealed class OpenTokStream
{
    internal OpenTokStream(string streamId, string? name, bool hasAudio, bool hasVideo, object nativeStream)
    {
        StreamId = streamId;
        Name = name;
        HasAudio = hasAudio;
        HasVideo = hasVideo;
        NativeStream = nativeStream;
    }

    /// <summary>The session-unique identifier of this stream.</summary>
    public string StreamId { get; }

    /// <summary>The name the publisher gave the stream, if any.</summary>
    public string? Name { get; }

    /// <summary>Whether the stream carried audio when it was reported.</summary>
    public bool HasAudio { get; }

    /// <summary>Whether the stream carried video when it was reported.</summary>
    public bool HasVideo { get; }

    /// <summary>
    /// The platform object this was read from — <c>OTStream</c> on iOS,
    /// <c>Com.Opentok.Android.Stream</c> on Android.
    /// </summary>
    /// <remarks>
    /// Internal, and held only so <see cref="OpenTokSubscriber"/> can hand it back to the SDK when
    /// subscribing. Not exposed: it is a different type on each platform, so a public
    /// <c>object</c>-typed escape hatch would be untypeable at the call site anyway — an app that
    /// needs the native object should use the platform binding package directly.
    /// </remarks>
    internal object NativeStream { get; }

    /// <inheritdoc />
    public override string ToString() =>
        $"{StreamId}{(Name is null ? "" : $" ({Name})")} audio={HasAudio} video={HasVideo}";
}

/// <summary>An error reported by the native SDK.</summary>
/// <remarks>
/// <see cref="Code"/> is the platform's own numeric code and is <em>not</em> unified: OpenTok's iOS
/// and Android SDKs number their errors differently, and mapping them onto a shared enum would mean
/// inventing a correspondence that Vonage does not document. Treat <see cref="Code"/> as
/// diagnostic — log it, do not branch on it across platforms.
/// </remarks>
public sealed class OpenTokError
{
    internal OpenTokError(int code, string message)
    {
        Code = code;
        Message = message;
    }

    /// <summary>The platform-specific numeric error code.</summary>
    public int Code { get; }

    /// <summary>The SDK's own description of the failure.</summary>
    public string Message { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Code}: {Message}";
}

/// <summary>Carries the stream a stream-related event is about.</summary>
public sealed class OpenTokStreamEventArgs(OpenTokStream stream) : EventArgs
{
    /// <summary>The stream the event concerns.</summary>
    public OpenTokStream Stream { get; } = stream;
}

/// <summary>Carries the error a failure event is about.</summary>
public sealed class OpenTokErrorEventArgs(OpenTokError error) : EventArgs
{
    /// <summary>What went wrong.</summary>
    public OpenTokError Error { get; } = error;
}
