namespace OpenTok.Net;

/// <summary>
/// Hands audio control to the operating system's calling stack — CallKit on iOS,
/// <c>android.telecom</c> on Android.
/// </summary>
/// <remarks>
/// <para>
/// By default the SDK manages audio itself: it configures and activates <c>AVAudioSession</c> on
/// iOS, and requests audio focus on Android. That is right for an in-app call and wrong for a
/// system call, because the OS wants to own both. Left alone, the two fight — audio that does not
/// start when the user answers, or that stops when a second call arrives.
/// </para>
/// <para>
/// This is the whole SDK-side handshake for both platforms, and it really is symmetric even though
/// the underlying APIs are not:
/// </para>
/// <list type="table">
/// <listheader><term>Here</term><description>iOS / Android</description></listheader>
/// <item>
/// <term><see cref="EnableCallingServicesMode"/></term>
/// <description>
/// <c>OTAudioSessionManager.enableCallingServicesMode</c> / <c>AudioFocusManager.setRequestAudioFocus(false)</c>
/// </description>
/// </item>
/// <item>
/// <term><see cref="PrepareForCall"/></term>
/// <description><c>preconfigureAudioSessionForCallWithMode:</c> / nothing — Android has no equivalent</description>
/// </item>
/// <item>
/// <term><see cref="NotifyActivated"/></term>
/// <description><c>audioSessionDidActivate:</c> / <c>audioFocusActivated()</c></description>
/// </item>
/// <item>
/// <term><see cref="NotifyDeactivated"/></term>
/// <description><c>audioSessionDidDeactivate:</c> / <c>audioFocusDeactivated()</c></description>
/// </item>
/// </list>
/// <para>
/// What is <em>not</em> here is the calling integration itself. <c>CXProvider</c> and
/// <c>ConnectionService</c> have nothing in common beyond their purpose — different lifecycles,
/// different registration, different threading — so no honest cross-platform shape exists for them,
/// and pretending otherwise would hide the part an app genuinely has to write per platform. See
/// <c>samples/OpenTok.Sample.CallKit.iOS</c> and <c>samples/OpenTok.Sample.Telecom.Android</c>.
/// </para>
/// <para>
/// Every method here is safe to call on either platform and safe to call when the SDK has not
/// created an audio device yet; each is a no-op in that case rather than a throw, because the
/// callbacks that drive them arrive on the OS's schedule and not the app's.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // once, at launch — before any call
/// OpenTokAudioSession.EnableCallingServicesMode();
///
/// // answering: configure, but let the OS activate
/// OpenTokAudioSession.PrepareForCall();
///
/// // from CXProviderDelegate.DidActivateAudioSession, or the Android focus callback
/// OpenTokAudioSession.NotifyActivated();
/// OpenTokAudioSession.NotifyDeactivated();
/// </code>
/// </example>
public static partial class OpenTokAudioSession
{
    /// <summary>
    /// Tells the SDK to stop managing audio itself, because a calling service will.
    /// </summary>
    /// <remarks>
    /// Call this once, early — at launch, or at least before the first call. Until it is called the
    /// SDK owns the audio session and the other three methods here do nothing.
    /// </remarks>
    public static void EnableCallingServicesMode() => EnableCallingServicesModeNative();

    /// <summary>
    /// Configures audio for a call that is about to start, without activating it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call it in response to the system starting or answering a call — <c>CXAnswerCallAction</c> /
    /// <c>CXStartCallAction</c> on iOS, <c>onAnswer</c> / <c>onShowIncomingCallUi</c> on Android.
    /// Deliberately does not activate: the OS does that, and doing it here is what produces the
    /// two-owners problem this class exists to avoid.
    /// </para>
    /// <para>
    /// A no-op on Android, which has nothing to preconfigure — audio focus is requested and granted
    /// in one step. Kept on the cross-platform surface anyway so lifecycle code reads the same on
    /// both, rather than an <c>#if IOS</c> around one line.
    /// </para>
    /// </remarks>
    public static void PrepareForCall() => PrepareForCallNative();

    /// <summary>
    /// Tells the SDK the system has activated audio and media can flow.
    /// </summary>
    /// <remarks>
    /// Forward <c>CXProviderDelegate.DidActivateAudioSession</c> here on iOS, and the
    /// <c>AUDIOFOCUS_GAIN</c> branch of an <c>OnAudioFocusChangeListener</c> on Android.
    /// </remarks>
    public static void NotifyActivated() => NotifyActivatedNative();

    /// <summary>
    /// Tells the SDK the system has deactivated audio, so it can release its resources.
    /// </summary>
    /// <remarks>
    /// Forward <c>CXProviderDelegate.DidDeactivateAudioSession</c> here on iOS, and
    /// <c>AUDIOFOCUS_LOSS</c> on Android.
    /// </remarks>
    public static void NotifyDeactivated() => NotifyDeactivatedNative();

    private static partial void EnableCallingServicesModeNative();
    private static partial void PrepareForCallNative();
    private static partial void NotifyActivatedNative();
    private static partial void NotifyDeactivatedNative();
}
