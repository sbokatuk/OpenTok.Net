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

/// <summary>
/// Another client's connection to the session — one remote participant, whether or not they are
/// publishing anything.
/// </summary>
/// <remarks>
/// A snapshot, for the same reason <see cref="OpenTokStream"/> is: the native objects are owned by
/// their SDK and invalidated when the connection ends.
/// </remarks>
public sealed class OpenTokConnection
{
    internal OpenTokConnection(string connectionId, string? data, object nativeConnection)
    {
        ConnectionId = connectionId;
        Data = data;
        NativeConnection = nativeConnection;
    }

    /// <summary>The session-unique identifier of this connection.</summary>
    public string ConnectionId { get; }

    /// <summary>Whatever connection metadata the participant's token was minted with.</summary>
    public string? Data { get; }

    /// <summary>The platform object this was read from. Held so signals can be addressed to it.</summary>
    internal object NativeConnection { get; }

    /// <inheritdoc />
    public override string ToString() => ConnectionId;
}

/// <summary>Carries the connection a connection-related event is about.</summary>
public sealed class OpenTokConnectionEventArgs(OpenTokConnection connection) : EventArgs
{
    /// <summary>The connection the event concerns.</summary>
    public OpenTokConnection Connection { get; } = connection;
}

/// <summary>A signal received from another client in the session.</summary>
/// <remarks>
/// Signalling is the SDKs' general-purpose message channel — text chat, custom app state, "raise
/// hand", anything that is not media. Both platforms carry it and it costs nothing beyond the
/// session already being connected.
/// </remarks>
public sealed class OpenTokSignalEventArgs(string? type, string? data, OpenTokConnection? from, bool fromSelf)
    : EventArgs
{
    /// <summary>The signal's type, as chosen by the sender. Null when the sender omitted one.</summary>
    public string? Type { get; } = type;

    /// <summary>The signal's payload.</summary>
    public string? Data { get; } = data;

    /// <summary>
    /// Who sent it, or <see langword="null"/> if the SDK did not report a connection.
    /// </summary>
    public OpenTokConnection? From { get; } = from;

    /// <summary>
    /// Whether this client sent it.
    /// </summary>
    /// <remarks>
    /// Both SDKs deliver a signal back to its own sender, which surprises people writing chat: the
    /// message appears twice unless the handler checks. Computed here rather than left to the app
    /// comparing connection ids, because that comparison is the part that gets forgotten.
    /// </remarks>
    public bool FromSelf { get; } = fromSelf;
}

/// <summary>Carries the archive an archiving event is about.</summary>
public sealed class OpenTokArchiveEventArgs(string archiveId, string? archiveName) : EventArgs
{
    /// <summary>The archive's unique identifier.</summary>
    public string ArchiveId { get; } = archiveId;

    /// <summary>The archive's name, when one was given at creation. Always null on stop.</summary>
    public string? ArchiveName { get; } = archiveName;
}

/// <summary>
/// What the token this client connected with is allowed to do.
/// </summary>
/// <remarks>
/// Only meaningful once connected — the session has to be told the role before it can report it.
/// A publisher-role token has <see cref="CanPublish"/> but not <see cref="CanForceMute"/>; a
/// subscriber-role token has neither.
/// </remarks>
public sealed class OpenTokCapabilities
{
    internal OpenTokCapabilities(bool canPublish, bool canSubscribe, bool canForceMute, bool canForceDisconnect)
    {
        CanPublish = canPublish;
        CanSubscribe = canSubscribe;
        CanForceMute = canForceMute;
        CanForceDisconnect = canForceDisconnect;
    }

    /// <summary>Whether this client may publish a stream.</summary>
    public bool CanPublish { get; }

    /// <summary>Whether this client may subscribe to remote streams.</summary>
    public bool CanSubscribe { get; }

    /// <summary>Whether this client may force-mute others — a moderator token.</summary>
    public bool CanForceMute { get; }

    /// <summary>Whether this client may force-disconnect others — a moderator token.</summary>
    public bool CanForceDisconnect { get; }

    /// <inheritdoc />
    public override string ToString() =>
        $"publish={CanPublish} subscribe={CanSubscribe} forceMute={CanForceMute} forceDisconnect={CanForceDisconnect}";
}

/// <summary>Which camera a publisher is capturing from.</summary>
public enum OpenTokCameraPosition
{
    /// <summary>The user-facing camera. The default for a video call.</summary>
    Front,

    /// <summary>The rear-facing camera.</summary>
    Back,
}

/// <summary>Carries a caption line delivered by a subscriber.</summary>
public sealed class OpenTokCaptionEventArgs(string text, bool isFinal) : EventArgs
{
    /// <summary>The caption text.</summary>
    public string Text { get; } = text;

    /// <summary>
    /// Whether this line is final, or an interim result that a later event will replace.
    /// </summary>
    /// <remarks>
    /// Interim lines arrive continuously while someone is speaking. A UI that appends every line
    /// rather than replacing the interim one produces a wall of half-sentences.
    /// </remarks>
    public bool IsFinal { get; } = isFinal;
}

/// <summary>Carries an audio level sample.</summary>
public sealed class OpenTokAudioLevelEventArgs(float level) : EventArgs
{
    /// <summary>
    /// The level, from 0.0 to 1.0, sampled about 20 times a second.
    /// </summary>
    /// <remarks>
    /// Raw and linear, as the SDKs report it. A level meter usually wants this on a logarithmic
    /// scale with some smoothing; both SDKs' own samples do that conversion in the app rather than
    /// in the SDK, and so does this façade.
    /// </remarks>
    public float Level { get; } = level;
}

/// <summary>
/// A media transformation applied to a publisher's outgoing video or audio.
/// </summary>
/// <remarks>
/// <para>
/// <b>Requires a transformers package.</b> The implementations live in a separate native library —
/// <c>OpenTok.Net.Transformers.iOS</c> / <c>OpenTok.Net.Transformers.Android</c> — which is not a
/// dependency of this one, because it costs around 70 MB and most apps do not want it. Without it
/// the SDK reports that the transformers library is not loaded, at runtime, from a call that
/// compiled and linked cleanly.
/// </para>
/// <para>
/// Use the factory methods rather than the constructor for the built-in transformers; the name and
/// the JSON shape of the properties are Vonage's, and getting either wrong is another runtime-only
/// failure.
/// </para>
/// </remarks>
public sealed class OpenTokTransformer
{
    /// <summary>Creates a transformer by name, with a raw JSON properties string.</summary>
    /// <remarks>
    /// The escape hatch, for a transformer Vonage adds after this package was built. Prefer
    /// <see cref="BackgroundBlur"/>, <see cref="BackgroundReplacement"/> or
    /// <see cref="NoiseSuppression"/>.
    /// </remarks>
    public OpenTokTransformer(string name, string properties = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Properties = properties;
    }

    /// <summary>The transformer's name, as the SDK knows it.</summary>
    public string Name { get; }

    /// <summary>The transformer's configuration, as a JSON string. Empty when it takes none.</summary>
    public string Properties { get; }

    /// <summary>Blurs the background behind the person in frame.</summary>
    /// <param name="radius">
    /// How strongly to blur. <see cref="OpenTokBlurRadius.Custom"/> requires
    /// <paramref name="customRadius"/>.
    /// </param>
    /// <param name="customRadius">
    /// The blur radius, when <paramref name="radius"/> is <see cref="OpenTokBlurRadius.Custom"/>.
    /// </param>
    public static OpenTokTransformer BackgroundBlur(
        OpenTokBlurRadius radius = OpenTokBlurRadius.High,
        int? customRadius = null)
    {
        if (radius is OpenTokBlurRadius.Custom && customRadius is null)
        {
            throw new ArgumentNullException(
                nameof(customRadius),
                "A custom blur radius is required when radius is OpenTokBlurRadius.Custom.");
        }

        // Built by hand rather than through a serializer: the shape is three fixed keys, and taking
        // a JSON dependency into a package whose whole job is to be small is a poor trade.
        var properties = radius is OpenTokBlurRadius.Custom
            ? $$"""{"radius":"Custom","custom_radius":"{{customRadius}}"}"""
            : $$"""{"radius":"{{radius}}"}""";

        return new OpenTokTransformer("BackgroundBlur", properties);
    }

    /// <summary>Replaces the background behind the person in frame with an image.</summary>
    /// <param name="imagePath">
    /// A path to the image <em>on the device's filesystem</em> — not a resource name and not a
    /// bundled asset. An app shipping a background image has to copy it out to a real file first.
    /// </param>
    public static OpenTokTransformer BackgroundReplacement(string imagePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);

        return new OpenTokTransformer(
            "BackgroundReplacement",
            $$"""{"image_file_path":"{{imagePath}}"}""");
    }

    /// <summary>Suppresses background noise in the outgoing audio.</summary>
    /// <remarks>An audio transformer — assign it to the publisher's audio transformers, not video.</remarks>
    public static OpenTokTransformer NoiseSuppression() => new("NoiseSuppression");

    /// <inheritdoc />
    public override string ToString() =>
        Properties.Length == 0 ? Name : $"{Name} {Properties}";
}

/// <summary>How strongly <see cref="OpenTokTransformer.BackgroundBlur"/> blurs.</summary>
public enum OpenTokBlurRadius
{
    /// <summary>No blur. Effectively off.</summary>
    None,

    /// <summary>A light blur — the background stays recognisable.</summary>
    Low,

    /// <summary>A strong blur. The usual choice.</summary>
    High,

    /// <summary>A blur radius given as a number; see the <c>customRadius</c> parameter.</summary>
    Custom,
}
