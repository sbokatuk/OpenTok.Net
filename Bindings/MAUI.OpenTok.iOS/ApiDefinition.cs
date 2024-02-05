using System;
using AVFoundation;
using CoreFoundation;
using CoreGraphics;
using CoreMedia;
using CoreVideo;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace OpenTok.Net.iOS
{
    // @interface OTSessionICEConfig : NSObject
    [BaseType(typeof(NSObject))]
    interface OTSessionICEConfig
    {
        // @property (assign, nonatomic) enum OTSessionICEIncludeServers includeServers;
        [Export("includeServers", ArgumentSemantic.Assign)]
        OTSessionICEIncludeServers IncludeServers { get; set; }

        // @property (assign, nonatomic) enum OTSessionICETransportPolicy transportPolicy;
        [Export("transportPolicy", ArgumentSemantic.Assign)]
        OTSessionICETransportPolicy TransportPolicy { get; set; }

        // @property (readonly) NSArray * _Nullable customIceServers;
        [NullAllowed, Export("customIceServers")]
        NSObject[] CustomIceServers { get; }

        // +(NSInteger)maxTURNServersLimit;
        [Static]
        [Export("maxTURNServersLimit")]
        nint MaxTURNServersLimit { get; }

        // -(void)addICEServerWithURL:(NSString * _Nonnull)turn_url userName:(NSString * _Nonnull)username credential:(NSString * _Nonnull)credential error:(NSError * _Nonnull * _Nullable)errorPtr;
        [Export("addICEServerWithURL:userName:credential:error:")]
        void AddICEServerWithURL(string turn_url, string username, string credential, out NSError errorPtr);
    }

    // @interface OTSessionSettings : NSObject
    [BaseType(typeof(NSObject))]
    interface OTSessionSettings
    {
        // @property (assign, nonatomic) BOOL connectionEventsSuppressed;
        [Export("connectionEventsSuppressed")]
        bool ConnectionEventsSuppressed { get; set; }

        // @property (retain, nonatomic) OTSessionICEConfig * _Nullable iceConfig;
        [NullAllowed, Export("iceConfig", ArgumentSemantic.Strong)]
        OTSessionICEConfig IceConfig { get; set; }

        // @property (nonatomic, strong) NSURL * _Nullable apiURL;
        [NullAllowed, Export("apiURL", ArgumentSemantic.Strong)]
        NSUrl ApiURL { get; set; }

        // @property (assign, nonatomic) BOOL ipWhitelist;
        [Export("ipWhitelist")]
        bool IpWhitelist { get; set; }

        // @property (nonatomic, strong) NSString * _Nullable proxyURL;
        [NullAllowed, Export("proxyURL", ArgumentSemantic.Strong)]
        string ProxyURL { get; set; }
    }

    // @interface OTSession : NSObject
    [BaseType(typeof(NSObject), Delegates = new string[] { "WeakDelegate" }, Events = new Type[] { typeof(OTSessionDelegate) })]
    interface OTSession
    {
        // @property (readonly) OTSessionConnectionStatus sessionConnectionStatus;
        [Export("sessionConnectionStatus")]
        OTSessionConnectionStatus SessionConnectionStatus { get; }

        // @property (readonly) NSString * _Nonnull sessionId;
        [Export("sessionId")]
        string SessionId { get; }

        // @property (readonly) NSDictionary<NSString *,OTStream *> * _Nonnull streams;
        [Export("streams")]
        NSDictionary Streams { get; }

        // @property (readonly) OTConnection * _Nullable connection;
        [NullAllowed, Export("connection")]
        OTConnection Connection { get; }

        [Wrap("WeakDelegate")]
        [NullAllowed]
        IOTSessionDelegate Delegate { get; set; }

        // @property (assign, nonatomic) id<OTSessionDelegate> _Nullable delegate;
        [NullAllowed, Export("delegate", ArgumentSemantic.Assign)]
        NSObject WeakDelegate { get; set; }

        // @property (assign, nonatomic) dispatch_queue_t _Nonnull apiQueue;
        [Export("apiQueue", ArgumentSemantic.Assign)]
        DispatchQueue ApiQueue { get; set; }

        // @property (readonly) OTSessionCapabilities * _Nullable capabilities;
        [NullAllowed, Export("capabilities")]
        OTSessionCapabilities Capabilities { get; }

        // -(id _Nullable)initWithApiKey:(NSString * _Nonnull)apiKey sessionId:(NSString * _Nonnull)sessionId delegate:(id<OTSessionDelegate> _Nullable)delegate;
        [Export("initWithApiKey:sessionId:delegate:")]
        IntPtr Constructor(string apiKey, string sessionId, [NullAllowed] IOTSessionDelegate @delegate);

        // -(id _Nullable)initWithApiKey:(NSString * _Nonnull)apiKey sessionId:(NSString * _Nonnull)sessionId delegate:(id<OTSessionDelegate> _Nullable)delegate settings:(OTSessionSettings * _Nullable)settings;
        [Export("initWithApiKey:sessionId:delegate:settings:")]
        IntPtr Constructor(string apiKey, string sessionId, [NullAllowed] IOTSessionDelegate @delegate, [NullAllowed] OTSessionSettings settings);

        // -(void)connectWithToken:(NSString * _Nonnull)token error:(OTError * _Nullable * _Nullable)error;
        [Export("connectWithToken:error:")]
        void ConnectWithToken(string token, [NullAllowed] out OTError error);

        // -(void)disconnect:(OTError * _Nullable * _Nullable)error;
        [Export("disconnect:")]
        void Disconnect([NullAllowed] out OTError error);

        // -(void)disconnect __attribute__((deprecated("use disconnect: instead")));
        [Export("disconnect")]
        void Disconnect();

        // -(void)publish:(OTPublisherKit * _Nonnull)publisher error:(OTError * _Nullable * _Nullable)error;
        [Export("publish:error:")]
        void Publish(OTPublisherKit publisher, [NullAllowed] out OTError error);

        // -(void)publish:(OTPublisherKit * _Nonnull)publisher __attribute__((deprecated("use publish:error: instead")));
        [Export("publish:")]
        void Publish(OTPublisherKit publisher);

        // -(void)unpublish:(OTPublisherKit * _Nonnull)publisher error:(OTError * _Nullable * _Nullable)error;
        [Export("unpublish:error:")]
        void Unpublish(OTPublisherKit publisher, [NullAllowed] out OTError error);

        // -(void)unpublish:(OTPublisherKit * _Nonnull)publisher __attribute__((deprecated("use unpublish:error: instead")));
        [Export("unpublish:")]
        void Unpublish(OTPublisherKit publisher);

        // -(void)subscribe:(OTSubscriberKit * _Nonnull)subscriber error:(OTError * _Nullable * _Nullable)error;
        [Export("subscribe:error:")]
        void Subscribe(OTSubscriber subscriber, [NullAllowed] out OTError error);

        // -(void)subscribe:(OTSubscriberKit * _Nonnull)subscriber __attribute__((deprecated("use subscribe:error: instead")));
        [Export("subscribe:")]
        void Subscribe(OTSubscriber subscriber);

        // -(void)unsubscribe:(OTSubscriberKit * _Nonnull)subscriber error:(OTError * _Nullable * _Nullable)error;
        [Export("unsubscribe:error:")]
        void Unsubscribe(OTSubscriber subscriber, [NullAllowed] out OTError error);

        // -(void)unsubscribe:(OTSubscriberKit * _Nonnull)subscriber __attribute__((deprecated("use unsubscribe:error: instead")));
        [Export("unsubscribe:")]
        void Unsubscribe(OTSubscriber subscriber);

        // -(void)signalWithType:(NSString * _Nullable)type string:(NSString * _Nullable)string connection:(OTConnection * _Nullable)connection error:(OTError * _Nullable * _Nullable)error;
        [Export("signalWithType:string:connection:error:")]
        void SignalWithType([NullAllowed] string type, [NullAllowed] string @string, [NullAllowed] OTConnection connection, [NullAllowed] out OTError error);

        // -(void)signalWithType:(NSString * _Nullable)type string:(NSString * _Nullable)string connection:(OTConnection * _Nullable)connection retryAfterReconnect:(BOOL)retryAfterReconnect error:(OTError * _Nullable * _Nullable)error;
        [Export("signalWithType:string:connection:retryAfterReconnect:error:")]
        void SignalWithType([NullAllowed] string type, [NullAllowed] string @string, [NullAllowed] OTConnection connection, bool retryAfterReconnect, [NullAllowed] out OTError error);

        // -(void)forceMuteAll:(NSArray<OTStream *> * _Nullable)excludedStreams error:(OTError * _Nullable * _Nullable)error;
        [Export("forceMuteAll:error:")]
        void ForceMuteAll([NullAllowed] OTStream[] excludedStreams, [NullAllowed] out OTError error);

        // -(void)disableForceMute:(OTError * _Nullable * _Nullable)error;
        [Export("disableForceMute:")]
        void DisableForceMute([NullAllowed] out OTError error);

        // -(void)forceMuteStream:(OTStream * _Nonnull)stream error:(OTError * _Nullable * _Nullable)error;
        [Export("forceMuteStream:error:")]
        void ForceMuteStream(OTStream stream, [NullAllowed] out OTError error);

        // -(void)reportIssue:(NSString * _Nullable * _Nullable)issueId;
        [Export("reportIssue:")]
        void ReportIssue([NullAllowed] out string issueId);

        // -(void)setEncryptionSecret:(NSString * _Nonnull)secret error:(OTError * _Nullable * _Nullable)error;
        [Export("setEncryptionSecret:error:")]
        void SetEncryptionSecret(string secret, [NullAllowed] out OTError error);
    }

    interface IOTSessionDelegate
    {
    }

    [Protocol, Model, BaseType(typeof(NSObject))]
    partial interface OTSessionDelegate
    {
        // @required -(void)sessionDidConnect:(OTSession * _Nonnull)session;
        [Export("sessionDidConnect:")]
        void DidConnect(OTSession session);

        // @required -(void)sessionDidDisconnect:(OTSession * _Nonnull)session;
        [Export("sessionDidDisconnect:")]
        void DidDisconnect(OTSession session);

        // @required -(void)session:(OTSession * _Nonnull)session didFailWithError:(OTError * _Nonnull)error;
        [Export("session:didFailWithError:"), EventArgs("OTSessionDelegateError")]
        void DidFailWithError(OTSession session, OTError error);

        // @required -(void)session:(OTSession * _Nonnull)session streamCreated:(OTStream * _Nonnull)stream;
        [Export("session:streamCreated:"), EventArgs("OTSessionDelegateStream")]
        void StreamCreated(OTSession session, OTStream stream);

        // @required -(void)session:(OTSession * _Nonnull)session streamDestroyed:(OTStream * _Nonnull)stream;
        [Export("session:streamDestroyed:"), EventArgs("OTSessionDelegateStream")]
        void StreamDestroyed(OTSession session, OTStream stream);

        // @optional -(void)session:(OTSession * _Nonnull)session connectionCreated:(OTConnection * _Nonnull)connection;
        [Export("session:connectionCreated:"), EventArgs("OTSessionDelegateConnection")]
        void ConnectionCreated(OTSession session, OTConnection connection);

        // @optional -(void)session:(OTSession * _Nonnull)session connectionDestroyed:(OTConnection * _Nonnull)connection;
        [Export("session:connectionDestroyed:"), EventArgs("OTSessionDelegateConnection")]
        void ConnectionDestroyed(OTSession session, OTConnection connection);

        // @optional -(void)session:(OTSession * _Nonnull)session receivedSignalType:(NSString * _Nullable)type fromConnection:(OTConnection * _Nullable)connection withString:(NSString * _Nullable)string;
        [Export("session:receivedSignalType:fromConnection:withString:"), EventArgs("OTSessionDelegateSignal")]
        void ReceivedSignalType(OTSession session, [NullAllowed] string type, [NullAllowed] OTConnection connection, [NullAllowed] string stringData);

        // @optional -(void)session:(OTSession * _Nonnull)session archiveStartedWithId:(NSString * _Nonnull)archiveId name:(NSString * _Nullable)name;
        [Export("session:archiveStartedWithId:name:"), EventArgs("OTSessionDelegateArchiveStarted")]
        void ArchiveStartedWithId(OTSession session, string archiveId, [NullAllowed] string name);

        // @optional -(void)session:(OTSession * _Nonnull)session archiveStoppedWithId:(NSString * _Nonnull)archiveId;
        [Export("session:archiveStoppedWithId:"), EventArgs("OTSessionDelegateArchiveStopped")]
        void ArchiveStoppedWithId(OTSession session, string archiveId);

        // @optional -(void)sessionDidBeginReconnecting:(OTSession * _Nonnull)session;
        [Export("sessionDidBeginReconnecting:"), EventArgs("OTSessionDelegateDidBeginReconnecting")]
        void DidBeginReconnecting(OTSession session);

        // @optional -(void)sessionDidReconnect:(OTSession * _Nonnull)session;
        [Export("sessionDidReconnect:"), EventArgs("OTSessionDelegateDidReconnect")]
        void DidReconnect(OTSession session);

        // @optional -(void)session:(OTSession * _Nonnull)session muteForced:(OTMuteForcedInfo * _Nonnull)muteForcedInfo;
        [Export("session:muteForced:"), EventArgs("OTSessionDelegateMuteForced")]
        void MuteForced(OTSession session, OTMuteForcedInfo muteForcedInfo);
    }

    // @interface OTMuteForcedInfo : NSObject
    [BaseType(typeof(NSObject))]
    interface OTMuteForcedInfo
    {
        // @property (assign, nonatomic) BOOL active;
        [Export("active")]
        bool Active { get; set; }
    }

    // @interface OTSessionCapabilities : NSObject
    [BaseType(typeof(NSObject))]
    interface OTSessionCapabilities
    {
        // @property (readonly) BOOL canPublish;
        [Export("canPublish")]
        bool CanPublish { get; }

        // @property (readonly) BOOL canSubscribe;
        [Export("canSubscribe")]
        bool CanSubscribe { get; }

        // @property (readonly) BOOL canForceMute;
        [Export("canForceMute")]
        bool CanForceMute { get; }

        // -(instancetype _Nonnull)initWithCanPublish:(BOOL)publish canSubscribe:(BOOL)subscribe;
        [Export("initWithCanPublish:canSubscribe:")]
        IntPtr Constructor(bool publish, bool subscribe);
    }

    // @interface OTPublisherKitSettings : NSObject
    [BaseType(typeof(NSObject))]
    interface OTPublisherKitSettings
    {
        // @property (copy, nonatomic) NSString * _Nullable name;
        [NullAllowed, Export("name")]
        string Name { get; set; }

        // @property (nonatomic) BOOL audioTrack;
        [Export("audioTrack")]
        bool AudioTrack { get; set; }

        // @property (nonatomic) BOOL videoTrack;
        [Export("videoTrack")]
        bool VideoTrack { get; set; }

        // @property (nonatomic) int audioBitrate;
        [Export("audioBitrate")]
        int AudioBitrate { get; set; }

        // @property (nonatomic) BOOL enableOpusDtx;
        [Export("enableOpusDtx")]
        bool EnableOpusDtx { get; set; }

        // @property (nonatomic) BOOL scalableScreenshare;
        [Export("scalableScreenshare")]
        bool ScalableScreenshare { get; set; }

        // @property (assign, nonatomic) BOOL subscriberAudioFallbackEnabled;
        [Export("subscriberAudioFallbackEnabled")]
        bool SubscriberAudioFallbackEnabled { get; set; }

        // @property (assign, nonatomic) BOOL publisherAudioFallbackEnabled;
        [Export("publisherAudioFallbackEnabled")]
        bool PublisherAudioFallbackEnabled { get; set; }
    }

    // @interface OTPublisherKit : NSObject
    [BaseType(typeof(NSObject), Delegates = new string[] { "WeakDelegate", "AudioLevelDelegate", "NetworkStatsDelegate", "RtcStatsReportDelegate" }, Events = new Type[] { typeof(OTPublisherKitDelegate), typeof(OTPublisherKitAudioLevelDelegate), typeof(OTPublisherKitNetworkStatsDelegate), typeof(OTPublisherKitRtcStatsReportDelegate) })]
    interface OTPublisherKit
    {
        // -(instancetype _Nullable)initWithDelegate:(id<OTPublisherKitDelegate> _Nullable)delegate;
        [Export("initWithDelegate:")]
        IntPtr Constructor([NullAllowed] OTPublisherKitDelegate @delegate);

        // -(instancetype _Nullable)initWithDelegate:(id<OTPublisherKitDelegate> _Nullable)delegate settings:(OTPublisherKitSettings * _Nonnull)settings;
        [Export("initWithDelegate:settings:")]
        IntPtr Constructor([NullAllowed] OTPublisherKitDelegate @delegate, OTPublisherKitSettings settings);

        // -(instancetype _Nullable)initWithDelegate:(id<OTPublisherKitDelegate> _Nullable)delegate name:(NSString * _Nullable)name __attribute__((deprecated("Use initWithDelegate: settings: instead")));
        [Export("initWithDelegate:name:")]
        IntPtr Constructor([NullAllowed] OTPublisherKitDelegate @delegate, [NullAllowed] string name);

        // -(instancetype _Nullable)initWithDelegate:(id<OTPublisherKitDelegate> _Nullable)delegate name:(NSString * _Nullable)name audioTrack:(BOOL)audioTrack videoTrack:(BOOL)videoTrack __attribute__((deprecated("Use initWithDelegate: settings: instead")));
        [Export("initWithDelegate:name:audioTrack:videoTrack:")]
        IntPtr Constructor([NullAllowed] OTPublisherKitDelegate @delegate, [NullAllowed] string name, bool audioTrack, bool videoTrack);

        [Wrap("WeakDelegate")]
        [NullAllowed]
        IOTPublisherKitDelegate Delegate { get; set; }

        // @property (assign, nonatomic) id<OTPublisherKitDelegate> _Nullable delegate;
        [NullAllowed, Export("delegate", ArgumentSemantic.Assign)]
        NSObject WeakDelegate { get; set; }

        [Wrap("WeakAudioLevelDelegate")]
        [NullAllowed]
        IOTPublisherKitAudioLevelDelegate AudioLevelDelegate { get; set; }

        // @property (assign, nonatomic) id<OTPublisherKitAudioLevelDelegate> _Nullable audioLevelDelegate;
        [NullAllowed, Export("audioLevelDelegate", ArgumentSemantic.Assign)]
        NSObject WeakAudioLevelDelegate { get; set; }

        [Wrap("WeakNetworkStatsDelegate")]
        [NullAllowed]
        IOTPublisherKitNetworkStatsDelegate NetworkStatsDelegate { get; set; }

        // @property (assign, nonatomic) id<OTPublisherKitNetworkStatsDelegate> _Nullable networkStatsDelegate;
        [NullAllowed, Export("networkStatsDelegate", ArgumentSemantic.Assign)]
        NSObject WeakNetworkStatsDelegate { get; set; }

        [Wrap("WeakRtcStatsReportDelegate")]
        [NullAllowed]
        IOTPublisherKitRtcStatsReportDelegate RtcStatsReportDelegate { get; set; }

        // @property (nonatomic, weak) id<OTPublisherKitRtcStatsReportDelegate> _Nullable rtcStatsReportDelegate;
        [NullAllowed, Export("rtcStatsReportDelegate", ArgumentSemantic.Weak)]
        NSObject WeakRtcStatsReportDelegate { get; set; }

        // @property (readonly) OTSession * _Nullable session;
        [NullAllowed, Export("session")]
        OTSession Session { get; }

        // @property (readonly) OTStream * _Nullable stream;
        [NullAllowed, Export("stream")]
        OTStream Stream { get; }

        // @property (readonly) NSString * _Nullable name;
        [NullAllowed, Export("name")]
        string Name { get; }

        // -(void)getRtcStatsReport;
        [Export("getRtcStatsReport")]
        void GetRtcStatsReport();

        // @property (nonatomic) BOOL publishAudio;
        [Export("publishAudio")]
        bool PublishAudio { get; set; }

        // @property (nonatomic) BOOL publishVideo;
        [Export("publishVideo")]
        bool PublishVideo { get; set; }

        // @property (nonatomic) BOOL publishCaptions;
        [Export("publishCaptions")]
        bool PublishCaptions { get; set; }

        // @property (retain, nonatomic) id<OTVideoCapture> _Nullable videoCapture;
        [NullAllowed, Export("videoCapture", ArgumentSemantic.Strong)]
        IOTVideoCapture VideoCapture { get; set; }

        // @property (assign, nonatomic) OTPublisherKitVideoType videoType;
        [Export("videoType", ArgumentSemantic.Assign)]
        OTPublisherKitVideoType VideoType { get; set; }

        // @property (retain, nonatomic) id<OTVideoRender> _Nullable videoRender;
        [NullAllowed, Export("videoRender", ArgumentSemantic.Strong)]
        IOTVideoRender VideoRender { get; set; }

        // @property (assign, nonatomic) BOOL audioFallbackEnabled;
        [Export("audioFallbackEnabled")]
        bool AudioFallbackEnabled { get; set; }

        // @property (nonatomic, strong) NSArray * _Nonnull videoTransformers;
        [Export("videoTransformers", ArgumentSemantic.Strong)]
        NSObject[] VideoTransformers { get; set; }

        // @property (nonatomic, strong) NSArray * _Nonnull audioTransformers;
        [Export("audioTransformers", ArgumentSemantic.Strong)]
        NSObject[] AudioTransformers { get; set; }
    }

    interface IOTPublisherKitDelegate
    {
    }

    // @protocol OTPublisherKitDelegate <NSObject>
    [Protocol(Name = "OTPublisherKitDelegate"), Model]
    //[Protocol(Name = "OTPublisherDelegate")]
    [BaseType(typeof(NSObject))]
    partial interface OTPublisherKitDelegate
    {
        [Export("publisher:didFailWithError:"), EventArgs("OTPublisherDelegateError")]
        void DidFailWithError(OTPublisher publisher, OTError error);

        [Export("muteForced:")]
        void MuteForced(OTPublisher publisher);

        [Export("publisher:streamCreated:"), EventArgs("OTPublisherDelegateStream")]
        void StreamCreated(OTPublisher publisher, OTStream stream);

        [Export("publisher:streamDestroyed:"), EventArgs("OTPublisherDelegateStream")]
        void StreamDestroyed(OTPublisher publisher, OTStream stream);

        [Export("publisherVideoDisabled:reason:"), EventArgs("OTPublisherDelegateEventReason")]
        void PublisherVideoDisabled(OTPublisher publisher, OTPublisherVideoEventReason reason);

        [Export("publisherVideoEnabled:reason:"), EventArgs("OTPublisherDelegateEventReason")]
        void PublisherVideoEnabled(OTPublisher publisher, OTPublisherVideoEventReason reason);

        [Export("publisherVideoDisableWarning:")]
        void PublisherVideoDisableWarning(OTPublisher publisher);

        [Export("publisherVideoDisableWarningLifted:")]
        void PublisherVideoDisableWarningLifted(OTPublisher publisher);

        [Export("publisher:didChangeCameraPosition:"), EventArgs("OTPublisherDelegateEventCameraPosition")]
        void DidChangeCameraPosition(OTPublisher publisher, AVCaptureDevicePosition position);
    }

    interface IOTPublisherKitAudioLevelDelegate
    {
    }

    // @protocol OTPublisherKitAudioLevelDelegate <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    partial interface OTPublisherKitAudioLevelDelegate
    {
        [Export("publisher:audioLevelUpdated:"), EventArgs("OTPublisherKitAudioLevelDelegateAudioLevelUpdated")]
        void AudioLevelUpdated(OTPublisher publisher, float audioLevel);
    }

    interface IOTPublisherKitNetworkStatsDelegate
    {
    }

    // @protocol OTPublisherKitNetworkStatsDelegate <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTPublisherKitNetworkStatsDelegate
    {
        // @optional -(void)publisher:(OTPublisherKit * _Nonnull)publisher videoNetworkStatsUpdated:(NSArray<OTPublisherKitVideoNetworkStats *> * _Nonnull)stats;
        [Export("publisher:videoNetworkStatsUpdated:"), EventArgs("OTPublisherKitNetworkStatsDelegateAudioVideoNetworkStatsUpdated")]
        void VideoNetworkStatsUpdated(OTPublisherKit publisher, OTPublisherKitVideoNetworkStats[] stats);

        // @optional -(void)publisher:(OTPublisherKit * _Nonnull)publisher audioNetworkStatsUpdated:(NSArray<OTPublisherKitAudioNetworkStats *> * _Nonnull)stats;
        [Export("publisher:audioNetworkStatsUpdated:"), EventArgs("OTPublisherKitNetworkStatsDelegateAudioNetworkStatsUpdated")]
        void AudioNetworkStatsUpdated(OTPublisherKit publisher, OTPublisherKitAudioNetworkStats[] stats);
    }

    // @interface OTPublisherRtcStats : NSObject
    [BaseType(typeof(NSObject))]
    interface OTPublisherRtcStats
    {
        // @property (strong) NSString * _Nonnull connectionId;
        [Export("connectionId", ArgumentSemantic.Strong)]
        string ConnectionId { get; set; }

        // @property (strong) NSString * _Nonnull jsonArrayOfReports;
        [Export("jsonArrayOfReports", ArgumentSemantic.Strong)]
        string JsonArrayOfReports { get; set; }
    }

    interface IOTPublisherKitRtcStatsReportDelegate
    {
    }

    // @protocol OTPublisherKitRtcStatsReportDelegate <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTPublisherKitRtcStatsReportDelegate
    {
        // @optional -(void)publisher:(OTPublisherKit * _Nonnull)publisher rtcStatsReport:(NSArray<OTPublisherRtcStats *> * _Nonnull)stats;
        [Export("publisher:rtcStatsReport:"), EventArgs("OTPublisherKitRtcStatsReportDelegateRtcStats")]
        void RtcStatsReport(OTPublisherKit publisher, OTPublisherRtcStats[] stats);
    }

    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTCustomVideoTransformer
    {
        // @required -(void)transform:(OTVideoFrame * _Nonnull)frame;
        [Abstract]
        [Export("transform:")]
        void Transform(OTVideoFrame frame);
    }

    interface IOTCustomVideoTransformer
    {
    }

    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTCustomAudioTransformer
    {
        // @required -(void)transform:(OTAudioData * _Nonnull)audioData;
        [Abstract]
        [Export("transform:")]
        void Transform(OTAudioData audioData);
    }

    interface IOTCustomAudioTransformer
    {
    }

    // @interface OTVideoTransformer : NSObject
    [BaseType(typeof(NSObject))]
    interface OTVideoTransformer
    {
        // -(id _Nullable)initWithName:(NSString * _Nonnull)name properties:(NSString * _Nonnull)properties;
        [Export("initWithName:properties:")]
        IntPtr Constructor(string name, string properties);

        // -(id _Nullable)initWithName:(NSString * _Nonnull)name transformer:(id<OTCustomVideoTransformer> _Nonnull)transformer;
        [Export("initWithName:transformer:")]
        IntPtr Constructor(string name, IOTCustomVideoTransformer transformer);
    }

    // @interface OTAudioTransformer : NSObject
    [BaseType(typeof(NSObject))]
    interface OTAudioTransformer
    {
        // -(id _Nullable)initWithName:(NSString * _Nonnull)name transformer:(id<OTCustomAudioTransformer> _Nonnull)transformer;
        [Export("initWithName:transformer:")]
        IntPtr Constructor(string name, IOTCustomAudioTransformer transformer);
    }

    // @interface OTSubscriberKit : NSObject
    [BaseType(typeof(NSObject), Delegates = new string[] { "Delegate", "AudioLevelDelegate", "CaptionsDelegate", "NetworkStatsDelegate", "RtcStatsReportDelegate" }, Events = new Type[] { typeof(OTSubscriberKitDelegate), typeof(OTSubscriberKitAudioLevelDelegate), typeof(OTSubscriberKitCaptionsDelegate), typeof(OTSubscriberKitNetworkStatsDelegate), typeof(OTSubscriberKitRtcStatsReportDelegate) })]
    interface OTSubscriber
    {
        // @property (readonly) OTSession * _Nonnull session;
        [Export("session")]
        OTSession Session { get; }

        // @property (readonly) OTStream * _Nullable stream;
        [NullAllowed, Export("stream")]
        OTStream Stream { get; }

        [Wrap("WeakDelegate")]
        [NullAllowed]
        IOTSubscriberKitDelegate Delegate { get; set; }

        // @property (assign, nonatomic) id<OTSubscriberKitDelegate> _Nullable delegate;
        [NullAllowed, Export("delegate", ArgumentSemantic.Assign)]
        NSObject WeakDelegate { get; set; }

        [Wrap("WeakAudioLevelDelegate")]
        [NullAllowed]
        IOTSubscriberKitAudioLevelDelegate AudioLevelDelegate { get; set; }

        // @property (assign, nonatomic) id<OTSubscriberKitAudioLevelDelegate> _Nullable audioLevelDelegate;
        [NullAllowed, Export("audioLevelDelegate", ArgumentSemantic.Assign)]
        NSObject WeakAudioLevelDelegate { get; set; }

        [Wrap("WeakCaptionsDelegate")]
        [NullAllowed]
        IOTSubscriberKitCaptionsDelegate CaptionsDelegate { get; set; }

        // @property (nonatomic, weak) id<OTSubscriberKitCaptionsDelegate> _Nullable captionsDelegate;
        [NullAllowed, Export("captionsDelegate", ArgumentSemantic.Weak)]
        NSObject WeakCaptionsDelegate { get; set; }

        // @property (nonatomic) BOOL subscribeToAudio;
        [Export("subscribeToAudio")]
        bool SubscribeToAudio { get; set; }

        // @property (nonatomic) BOOL subscribeToVideo;
        [Export("subscribeToVideo")]
        bool SubscribeToVideo { get; set; }

        // @property (nonatomic) BOOL subscribeToCaptions;
        [Export("subscribeToCaptions")]
        bool SubscribeToCaptions { get; set; }

        // @property (retain, nonatomic) id<OTVideoRender> _Nullable videoRender;
        [NullAllowed, Export("videoRender", ArgumentSemantic.Strong)]
        IOTVideoRender VideoRender { get; set; }

        // @property (nonatomic) CGSize preferredResolution;
        [Export("preferredResolution", ArgumentSemantic.Assign)]
        CGSize PreferredResolution { get; set; }

        // @property (nonatomic) float preferredFrameRate;
        [Export("preferredFrameRate")]
        float PreferredFrameRate { get; set; }

        // @property (nonatomic) double audioVolume;
        [Export("audioVolume")]
        double AudioVolume { get; set; }

        [Wrap("WeakNetworkStatsDelegate")]
        [NullAllowed]
        IOTSubscriberKitNetworkStatsDelegate NetworkStatsDelegate { get; set; }

        // @property (assign, nonatomic) id<OTSubscriberKitNetworkStatsDelegate> _Nullable networkStatsDelegate;
        [NullAllowed, Export("networkStatsDelegate", ArgumentSemantic.Assign)]
        NSObject WeakNetworkStatsDelegate { get; set; }

        [Wrap("WeakRtcStatsReportDelegate")]
        [NullAllowed]
        IOTSubscriberKitRtcStatsReportDelegate RtcStatsReportDelegate { get; set; }

        // @property (nonatomic, weak) id<OTSubscriberKitRtcStatsReportDelegate> _Nullable rtcStatsReportDelegate;
        [NullAllowed, Export("rtcStatsReportDelegate", ArgumentSemantic.Weak)]
        NSObject WeakRtcStatsReportDelegate { get; set; }

        // -(id _Nullable)initWithStream:(OTStream * _Nonnull)stream delegate:(id<OTSubscriberKitDelegate> _Nullable)delegate;
        [Export("initWithStream:delegate:")]
        IntPtr Constructor(OTStream stream, [NullAllowed] IOTSubscriberKitDelegate @delegate);

        // -(void)getRtcStatsReport;
        [Export("getRtcStatsReport")]
        void GetRtcStatsReport();

        // @property (readonly) UIView * _Nullable view;
        [NullAllowed, Export("view")]
        UIView View { get; }

        // @property (nonatomic) OTVideoViewScaleBehavior viewScaleBehavior;
        [Export("viewScaleBehavior", ArgumentSemantic.Assign)]
        OTVideoViewScaleBehavior ViewScaleBehavior { get; set; }
    }

    interface IOTSubscriberKitDelegate
    {
    }

    // @protocol OTSubscriberKitDelegate <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    partial interface OTSubscriberKitDelegate
    {
        [Export("subscriberDidConnectToStream:")]
        void DidConnectToStream(OTSubscriber subscriber);

        [Export("subscriber:didFailWithError:"), EventArgs("OTSubscriberKitDelegateError")]
        void DidFailWithError(OTSubscriber subscriber, OTError error);

        [Export("subscriberVideoDisabled:reason:"), EventArgs("OTSubscriberKitDelegateVideoEventReason")]
        void VideoDisabled(OTSubscriber subscriber, OTSubscriberVideoEventReason reason);

        [Export("subscriberVideoEnabled:reason:"), EventArgs("OTSubscriberKitDelegateVideoEventReason")]
        void VideoEnabled(OTSubscriber subscriber, OTSubscriberVideoEventReason reason);

        [Export("subscriberVideoDisableWarning:")]
        void VideoDisableWarning(OTSubscriber subscriber);

        [Export("subscriberVideoDisableWarningLifted:")]
        void VideoDisableWarningLifted(OTSubscriber subscriber);

        [Export("subscriberDidDisconnectFromStream:")]
        void DidDisconnectFromStream(OTSubscriber subscriber);

        [Export("subscriberDidReconnectToStream:")]
        void DidReconnectToStream(OTSubscriber subscriber);

        [Export("subscriberVideoDataReceived:")]
        void VideoDataReceived(OTSubscriber subscriber);
    }

    interface IOTSubscriberKitAudioLevelDelegate
    {
    }

    // @protocol OTSubscriberKitAudioLevelDelegate <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTSubscriberKitAudioLevelDelegate
    {
        // @required -(void)subscriber:(OTSubscriberKit * _Nonnull)subscriber audioLevelUpdated:(float)audioLevel;
        [Export("subscriber:audioLevelUpdated:"), EventArgs("OTSubscriberKitAudioLevelDelegateAudioLevelUpdated")]
        void AudioLevelUpdated(OTSubscriber subscriber, float audioLevel);
    }

    interface IOTSubscriberKitCaptionsDelegate
    {
    }

    // @protocol OTSubscriberKitCaptionsDelegate <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTSubscriberKitCaptionsDelegate
    {
        // @optional -(void)subscriber:(OTSubscriberKit * _Nonnull)subscriber caption:(NSString * _Nonnull)text isFinal:(BOOL)isFinal;
        [Export("subscriber:caption:isFinal:"), EventArgs("OTSubscriberKitCaptionsDelegateCaption")]
        void Caption(OTSubscriber subscriber, string text, bool isFinal);
    }

    interface IOTSubscriberKitNetworkStatsDelegate
    {
    }

    // @protocol OTSubscriberKitNetworkStatsDelegate <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTSubscriberKitNetworkStatsDelegate
    {
        // @optional -(void)subscriber:(OTSubscriber *)subscriber videoNetworkStatsUpdated:(OTSubscriberVideoNetworkStats *)stats;
        [Export("subscriber:videoNetworkStatsUpdated:"), EventArgs("OTSubscriberKitNetworkStatsDelegateVideoNetworkStatsUpdated")]
        void VideoNetworkStatsUpdated(OTSubscriber subscriber, OTSubscriberKitVideoNetworkStats stats);

        // @optional -(void)subscriber:(OTSubscriber *)subscriber audioNetworkStatsUpdated:(OTSubscriberAudioNetworkStats *)stats;
        [Export("subscriber:audioNetworkStatsUpdated:"), EventArgs("OTSubscriberKitNetworkStatsDelegateAudioNetworkStatsUpdated")]
        void AudioNetworkStatsUpdated(OTSubscriber subscriber, OTSubscriberKitAudioNetworkStats stats);
    }

    interface IOTSubscriberKitRtcStatsReportDelegate
    {
    }

    // @protocol OTSubscriberKitRtcStatsReportDelegate <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTSubscriberKitRtcStatsReportDelegate
    {
        // @optional -(void)subscriber:(OTSubscriberKit * _Nonnull)subscriber rtcStatsReport:(NSString * _Nonnull)jsonArrayOfReports;
        [Export("subscriber:rtcStatsReport:"), EventArgs("OTSubscriberKitRtcStatsReportDelegateJson")]
        void RtcStatsReport(OTSubscriber subscriber, string jsonArrayOfReports);
    }

    // @interface OTStream : NSObjec
    [BaseType(typeof(NSObject))]
    interface OTStream
    {
        // @property (readonly) OTConnection * _Nonnull connection;
        [Export("connection")]
        OTConnection Connection { get; }

        // @property (readonly) OTSession * _Nonnull session;
        [Export("session")]
        OTSession Session { get; }

        // @property (readonly) NSString * _Nonnull streamId;
        [Export("streamId")]
        string StreamId { get; }

        // @property (readonly) NSDate * _Nonnull creationTime;
        [Export("creationTime")]
        NSDate CreationTime { get; }

        // @property (readonly) NSString * _Nullable name;
        [NullAllowed, Export("name")]
        string Name { get; }

        // @property (readonly) BOOL hasAudio;
        [Export("hasAudio")]
        bool HasAudio { get; }

        // @property (readonly) BOOL hasVideo;
        [Export("hasVideo")]
        bool HasVideo { get; }

        // @property (readonly) BOOL hasCaptions;
        [Export("hasCaptions")]
        bool HasCaptions { get; }

        // @property (readonly) CGSize videoDimensions;
        [Export("videoDimensions")]
        CGSize VideoDimensions { get; }

        // @property (readonly) OTStreamVideoType videoType;
        [Export("videoType")]
        OTStreamVideoType VideoType { get; }
    }

    // @interface OTConnection : NSObject
    [BaseType(typeof(NSObject))]
    interface OTConnection
    {
        // @property (readonly) NSString * _Nonnull connectionId;
        [Export("connectionId")]
        string ConnectionId { get; }

        // @property (readonly) NSDate * _Nonnull creationTime;
        [Export("creationTime")]
        NSDate CreationTime { get; }

        // @property (readonly) NSString * _Nullable data;
        [NullAllowed, Export("data")]
        string Data { get; }
    }

    // @interface OTError : NSError
    [BaseType(typeof(NSError))]
    interface OTError
    {
    }

    // @interface OTVideoFormat : NSObject
    [BaseType(typeof(NSObject))]
    interface OTVideoFormat
    {
        // @property (copy, nonatomic) NSString * _Nonnull name;
        [Export("name")]
        string Name { get; set; }

        // @property (assign, nonatomic) OTPixelFormat pixelFormat;
        [Export("pixelFormat", ArgumentSemantic.Assign)]
        OTPixelFormat PixelFormat { get; set; }

        // @property (retain, nonatomic) NSMutableArray * _Nonnull bytesPerRow;
        [Export("bytesPerRow", ArgumentSemantic.Retain)]
        NSMutableArray BytesPerRow { get; set; }

        // @property (assign, nonatomic) uint32_t imageWidth;
        [Export("imageWidth")]
        uint ImageWidth { get; set; }

        // @property (assign, nonatomic) uint32_t imageHeight;
        [Export("imageHeight")]
        uint ImageHeight { get; set; }

        // @property (assign, nonatomic) double estimatedFramesPerSecond;
        [Export("estimatedFramesPerSecond")]
        double EstimatedFramesPerSecond { get; set; }

        // @property (assign, nonatomic) double estimatedCaptureDelay;
        [Export("estimatedCaptureDelay")]
        double EstimatedCaptureDelay { get; set; }

        // +(OTVideoFormat * _Nonnull)videoFormatI420WithWidth:(uint32_t)width height:(uint32_t)height;
        [Static]
        [Export("videoFormatI420WithWidth:height:")]
        OTVideoFormat VideoFormatI420WithWidth(uint width, uint height);

        // +(OTVideoFormat * _Nonnull)videoFormatNV12WithWidth:(uint32_t)width height:(uint32_t)height;
        [Static]
        [Export("videoFormatNV12WithWidth:height:")]
        OTVideoFormat VideoFormatNV12WithWidth(uint width, uint height);

        // +(OTVideoFormat * _Nonnull)videoFormatARGBWithWidth:(uint32_t)width height:(uint32_t)height;
        [Static]
        [Export("videoFormatARGBWithWidth:height:")]
        OTVideoFormat VideoFormatARGBWithWidth(uint width, uint height);
    }

    // @interface OTVideoFrame : NSObject
    [BaseType(typeof(NSObject))]
    interface OTVideoFrame
    {
        //// @property (retain, nonatomic) NSPointerArray * _Nullable planes;
        //[NullAllowed, Export("planes", ArgumentSemantic.Strong)]
        //NSPointerArray Planes { get; set; }

        // @property (assign, nonatomic) CMTime timestamp;
        [Export("timestamp", ArgumentSemantic.Assign)]
        CMTime Timestamp { get; set; }

        // @property (assign, nonatomic) OTVideoOrientation orientation;
        [Export("orientation", ArgumentSemantic.Assign)]
        OTVideoOrientation Orientation { get; set; }

        // @property (retain, nonatomic) OTVideoFormat * _Nullable format;
        [NullAllowed, Export("format", ArgumentSemantic.Strong)]
        OTVideoFormat Format { get; set; }

        // @property (readonly, nonatomic) NSData * _Nullable metadata;
        [NullAllowed, Export("metadata")]
        NSData Metadata { get; }

        // -(id _Nonnull)initWithFormat:(OTVideoFormat * _Nonnull)videoFormat;
        [Export("initWithFormat:")]
        IntPtr Constructor(OTVideoFormat videoFormat);

        // -(void)setPlanesWithPointers:(uint8_t * _Nonnull *)planes numPlanes:(int)numPlanes;
        [Export("setPlanesWithPointers:numPlanes:")]
        unsafe void SetPlanesWithPointers(NSObject planes, int numPlanes);

        // -(void)clearPlanes;
        [Export("clearPlanes")]
        void ClearPlanes();

        // -(int)getPlaneStride:(int)plane;
        [Export("getPlaneStride:")]
        int GetPlaneStride(int plane);

        // -(void)setMetadata:(NSData * _Nonnull)data error:(OTError * _Nullable *)error;
        [Export("setMetadata:error:")]
        void SetMetadata(NSData data, [NullAllowed] out OTError error);
    }

    // @protocol OTVideoCaptureConsumer <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTVideoCaptureConsumer
    {
        // @required -(void)consumeFrame:(OTVideoFrame * _Nonnull)frame;
        [Abstract]
        [Export("consumeFrame:")]
        void ConsumeFrame(OTVideoFrame frame);

        // @required -(BOOL)consumeImageBuffer:(CVImageBufferRef _Nonnull)frame orientation:(OTVideoOrientation)orientation timestamp:(CMTime)ts metadata:(NSData * _Nullable)metadata;
        [Abstract]
        [Export("consumeImageBuffer:orientation:timestamp:metadata:")]
        unsafe bool ConsumeImageBuffer(IntPtr frame, OTVideoOrientation orientation, CMTime ts, [NullAllowed] NSData metadata);
    }

    interface IOTVideoCapture
    {
    }

    interface IOTVideoCaptureConsumer
    {
    }

    // @protocol OTVideoCapture <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTVideoCapture
    {
        //// @required @property (assign, atomic) id<OTVideoCaptureConsumer> _Nullable videoCaptureConsumer;
        //[Abstract]
        //[NullAllowed, Export("videoCaptureConsumer", ArgumentSemantic.Weak)]
        //IOTVideoCaptureConsumer VideoCaptureConsumer { get; set; }

        // @required @property (readwrite, nonatomic) OTVideoContentHint videoContentHint;
        [Abstract]
        [Export("videoContentHint", ArgumentSemantic.Assign)]
        OTVideoContentHint VideoContentHint { get; set; }

        // @required -(void)initCapture;
        [Abstract]
        [Export("initCapture")]
        void InitCapture();

        // @required -(void)releaseCapture;
        [Abstract]
        [Export("releaseCapture")]
        void ReleaseCapture();

        // @required -(int32_t)startCapture;
        [Abstract]
        [Export("startCapture")]
        int StartCapture();

        // @required -(int32_t)stopCapture;
        [Abstract]
        [Export("stopCapture")]
        int StopCapture();

        // @required -(BOOL)isCaptureStarted;
        [Abstract]
        [Export("isCaptureStarted")]
        bool IsCaptureStarted { get; }

        // @required -(int32_t)captureSettings:(OTVideoFormat * _Nonnull)videoFormat;
        [Abstract]
        [Export("captureSettings:")]
        int CaptureSettings(OTVideoFormat videoFormat);
    }

    interface IOTVideoRender
    {
    }

    // @protocol OTVideoRender <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTVideoRender
    {
        // @required -(void)renderVideoFrame:(OTVideoFrame * _Nonnull)frame;
        [Abstract]
        [Export("renderVideoFrame:")]
        void RenderVideoFrame(OTVideoFrame frame);
    }

    // @interface OTAudioDeviceManager : NSObject
    [BaseType(typeof(NSObject))]
    interface OTAudioDeviceManager
    {
        // +(void)setAudioDevice:(id<OTAudioDevice> _Nullable)device;
        [Static]
        [Export("setAudioDevice:")]
        void SetAudioDevice([NullAllowed] OTAudioDevice device);

        // +(id<OTAudioDevice> _Nullable)currentAudioDevice;
        [Static]
        [NullAllowed, Export("currentAudioDevice")]
        IOTAudioDevice CurrentAudioDevice { get; }
    }

    // @interface OTAudioFormat : NSObject
    [BaseType(typeof(NSObject))]
    interface OTAudioFormat
    {
        // @property (assign, nonatomic) uint16_t sampleRate;
        [Export("sampleRate")]
        ushort SampleRate { get; set; }

        // @property (assign, nonatomic) uint8_t numChannels;
        [Export("numChannels")]
        byte NumChannels { get; set; }
    }

    interface IOTAudioBus
    {
    }

    // @protocol OTAudioBus <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTAudioBus
    {
        // @required -(void)writeCaptureData:(void * _Nonnull)data numberOfSamples:(uint32_t)count;
        [Abstract]
        [Export("writeCaptureData:numberOfSamples:")]
        unsafe void WriteCaptureData(IntPtr data, uint count);

        // @required -(uint32_t)readRenderData:(void * _Nonnull)data numberOfSamples:(uint32_t)count;
        [Abstract]
        [Export("readRenderData:numberOfSamples:")]
        unsafe int ReadRenderData(IntPtr data, uint count);
    }

    interface IOTAudioDevice
    {
    }

    // @protocol OTAudioDevice <NSObject>
    [Protocol, Model]
    [BaseType(typeof(NSObject))]
    interface OTAudioDevice
    {
        // @required -(BOOL)setAudioBus:(id<OTAudioBus> _Nullable)audioBus;
        [Abstract]
        [Export("setAudioBus:")]
        bool SetAudioBus([NullAllowed] IOTAudioBus audioBus);

        // @required -(OTAudioFormat * _Nonnull)captureFormat;
        [Abstract]
        [Export("captureFormat")]
        OTAudioFormat CaptureFormat { get; }

        // @required -(OTAudioFormat * _Nonnull)renderFormat;
        [Abstract]
        [Export("renderFormat")]
        OTAudioFormat RenderFormat { get; }

        // @required -(BOOL)renderingIsAvailable;
        [Abstract]
        [Export("renderingIsAvailable")]
        bool RenderingIsAvailable { get; }

        // @required -(BOOL)initializeRendering;
        [Abstract]
        [Export("initializeRendering")]
        bool InitializeRendering();

        // @required -(BOOL)renderingIsInitialized;
        [Abstract]
        [Export("renderingIsInitialized")]
        bool RenderingIsInitialized { get; }

        // @required -(BOOL)startRendering;
        [Abstract]
        [Export("startRendering")]
        bool StartRendering();

        // @required -(BOOL)stopRendering;
        [Abstract]
        [Export("stopRendering")]
        bool StopRendering();

        // @required -(BOOL)isRendering;
        [Abstract]
        [Export("isRendering")]
        bool IsRendering { get; }

        // @required -(uint16_t)estimatedRenderDelay;
        [Abstract]
        [Export("estimatedRenderDelay")]
        ushort EstimatedRenderDelay { get; }

        // @required -(BOOL)captureIsAvailable;
        [Abstract]
        [Export("captureIsAvailable")]
        bool CaptureIsAvailable { get; }

        // @required -(BOOL)initializeCapture;
        [Abstract]
        [Export("initializeCapture")]
        bool InitializeCapture();

        // @required -(BOOL)captureIsInitialized;
        [Abstract]
        [Export("captureIsInitialized")]
        bool CaptureIsInitialized { get; }

        // @required -(BOOL)startCapture;
        [Abstract]
        [Export("startCapture")]
        bool StartCapture();

        // @required -(BOOL)stopCapture;
        [Abstract]
        [Export("stopCapture")]
        bool StopCapture();

        // @required -(BOOL)isCapturing;
        [Abstract]
        [Export("isCapturing")]
        bool IsCapturing { get; }

        // @required -(uint16_t)estimatedCaptureDelay;
        [Abstract]
        [Export("estimatedCaptureDelay")]
        ushort EstimatedCaptureDelay { get; }
    }

    // @interface OTAudioData : NSObject
    [BaseType(typeof(NSObject))]
    interface OTAudioData
    {
        //// @property (assign, nonatomic) int16_t * _Nullable sampleBuffer;
        //[NullAllowed, Export("sampleBuffer", ArgumentSemantic.Assign)]
        //unsafe short* SampleBuffer { get; set; }

        // @property (assign, nonatomic) uint32_t bitsPerSample;
        [Export("bitsPerSample")]
        uint BitsPerSample { get; set; }

        // @property (assign, nonatomic) uint32_t sampleRate;
        [Export("sampleRate")]
        uint SampleRate { get; set; }

        // @property (assign, nonatomic) uint32_t numberOfChannels;
        [Export("numberOfChannels")]
        uint NumberOfChannels { get; set; }

        // @property (assign, nonatomic) uint32_t numberOfSamples;
        [Export("numberOfSamples")]
        uint NumberOfSamples { get; set; }
    }

    // @interface OTSubscriberKitVideoNetworkStats : NSObject
    [BaseType(typeof(NSObject))]
    interface OTSubscriberKitVideoNetworkStats
    {
        // @property (readonly) uint64_t videoPacketsLost;
        [Export("videoPacketsLost")]
        ulong VideoPacketsLost { get; }

        // @property (readonly) uint64_t videoPacketsReceived;
        [Export("videoPacketsReceived")]
        ulong VideoPacketsReceived { get; }

        // @property (readonly) uint64_t videoBytesReceived;
        [Export("videoBytesReceived")]
        ulong VideoBytesReceived { get; }

        // @property (readonly) double timestamp;
        [Export("timestamp")]
        double Timestamp { get; }

        // -(instancetype _Nonnull)initWithPacketsLost:(uint64_t)packetsLost packetsReceived:(uint64_t)packetsReceived bytesReceived:(uint64_t)bytesReceived timestamp:(double)timestamp;
        [Export("initWithPacketsLost:packetsReceived:bytesReceived:timestamp:")]
        IntPtr Constructor(ulong packetsLost, ulong packetsReceived, ulong bytesReceived, double timestamp);
    }

    // @interface OTSubscriberKitAudioNetworkStats : NSObject
    [BaseType(typeof(NSObject))]
    interface OTSubscriberKitAudioNetworkStats
    {
        // @property (readonly) uint64_t audioPacketsLost;
        [Export("audioPacketsLost")]
        ulong AudioPacketsLost { get; }

        // @property (readonly) uint64_t audioPacketsReceived;
        [Export("audioPacketsReceived")]
        ulong AudioPacketsReceived { get; }

        // @property (readonly) uint64_t audioBytesReceived;
        [Export("audioBytesReceived")]
        ulong AudioBytesReceived { get; }

        // @property (readonly) double timestamp;
        [Export("timestamp")]
        double Timestamp { get; }

        // -(instancetype _Nonnull)initWithPacketsLost:(uint64_t)packetsLost packetsReceived:(uint64_t)packetsReceived bytesReceived:(uint64_t)bytesReceived timestamp:(double)timestamp;
        [Export("initWithPacketsLost:packetsReceived:bytesReceived:timestamp:")]
        IntPtr Constructor(ulong packetsLost, ulong packetsReceived, ulong bytesReceived, double timestamp);
    }

    [BaseType(typeof(NSObject))]
    interface OTPublisherKitVideoNetworkStats
    {
        // @property (readonly) NSString * _Nonnull connectionId;
        [Export("connectionId")]
        string ConnectionId { get; }

        // @property (readonly) NSString * _Nonnull subscriberId;
        [Export("subscriberId")]
        string SubscriberId { get; }

        // @property (readonly) int64_t videoPacketsLost;
        [Export("videoPacketsLost")]
        long VideoPacketsLost { get; }

        // @property (readonly) int64_t videoPacketsSent;
        [Export("videoPacketsSent")]
        long VideoPacketsSent { get; }

        // @property (readonly) int64_t videoBytesSent;
        [Export("videoBytesSent")]
        long VideoBytesSent { get; }

        // @property (readonly) double timestamp;
        [Export("timestamp")]
        double Timestamp { get; }

        // @property (readonly) double startTime;
        [Export("startTime")]
        double StartTime { get; }

        // -(instancetype _Nonnull)initWithConnectionId:(NSString * _Nonnull)connectionId subscriberId:(NSString * _Nonnull)subscriberId packetsLost:(int64_t)packetsLost packetsSent:(int64_t)packetsSent bytesSent:(int64_t)bytesSent timestamp:(double)timestamp startTime:(double)startTime;
        [Export("initWithConnectionId:subscriberId:packetsLost:packetsSent:bytesSent:timestamp:startTime:")]
        IntPtr Constructor(string connectionId, string subscriberId, long packetsLost, long packetsSent, long bytesSent, double timestamp, double startTime);
    }

    // @interface OTPublisherKitAudioNetworkStats : NSObject
    [BaseType(typeof(NSObject))]
    interface OTPublisherKitAudioNetworkStats
    {
        // @property (readonly) NSString * _Nonnull connectionId;
        [Export("connectionId")]
        string ConnectionId { get; }

        // @property (readonly) NSString * _Nonnull subscriberId;
        [Export("subscriberId")]
        string SubscriberId { get; }

        // @property (readonly) int64_t audioPacketsLost;
        [Export("audioPacketsLost")]
        long AudioPacketsLost { get; }

        // @property (readonly) int64_t audioPacketsSent;
        [Export("audioPacketsSent")]
        long AudioPacketsSent { get; }

        // @property (readonly) int64_t audioBytesSent;
        [Export("audioBytesSent")]
        long AudioBytesSent { get; }

        // @property (readonly) double timestamp;
        [Export("timestamp")]
        double Timestamp { get; }

        // @property (readonly) double startTime;
        [Export("startTime")]
        double StartTime { get; }

        // -(instancetype _Nonnull)initWithConnectionId:(NSString * _Nonnull)connectionId subscriberId:(NSString * _Nonnull)subscriberId packetsLost:(int64_t)packetsLost packetsSent:(int64_t)packetsSent bytesSent:(int64_t)bytesSent timestamp:(double)timestamp startTime:(double)startTime;
        [Export("initWithConnectionId:subscriberId:packetsLost:packetsSent:bytesSent:timestamp:startTime:")]
        IntPtr Constructor(string connectionId, string subscriberId, long packetsLost, long packetsSent, long bytesSent, double timestamp, double startTime);
    }

    // @interface OTPublisherSettings : OTPublisherKitSettings
    [BaseType(typeof(OTPublisherKitSettings))]
    interface OTPublisherSettings
    {
        // @property (retain, nonatomic) id<OTVideoCapture> _Nullable videoCapture;
        [NullAllowed, Export("videoCapture", ArgumentSemantic.Retain)]
        IOTVideoCapture VideoCapture { get; set; }

        // @property (nonatomic) OTCameraCaptureResolution cameraResolution;
        [Export("cameraResolution", ArgumentSemantic.Assign)]
        OTCameraCaptureResolution CameraResolution { get; set; }

        // @property (nonatomic) OTCameraCaptureFrameRate cameraFrameRate;
        [Export("cameraFrameRate", ArgumentSemantic.Assign)]
        OTCameraCaptureFrameRate CameraFrameRate { get; set; }
    }

    // @interface OTPublisher : OTPublisherKit
    [BaseType(typeof(OTPublisherKit))]
    interface OTPublisher
    {
        // -(instancetype _Nullable)initWithDelegate:(id<OTPublisherKitDelegate> _Nullable)delegate settings:(OTPublisherSettings * _Nonnull)settings;
        [Export("initWithDelegate:settings:")]
        IntPtr Constructor([NullAllowed] OTPublisherKitDelegate @delegate, OTPublisherSettings settings);

        // -(instancetype _Nullable)initWithDelegate:(id<OTPublisherKitDelegate> _Nullable)delegate name:(NSString * _Nullable)name cameraResolution:(OTCameraCaptureResolution)cameraResolution cameraFrameRate:(OTCameraCaptureFrameRate)cameraFrameRate __attribute__((deprecated("Use initWithDelegate: settings: instead")));
        [Export("initWithDelegate:name:cameraResolution:cameraFrameRate:")]
        IntPtr Constructor([NullAllowed] OTPublisherKitDelegate @delegate, [NullAllowed] string name, OTCameraCaptureResolution cameraResolution, OTCameraCaptureFrameRate cameraFrameRate);

        // @property (readonly) UIView * _Nullable view;
        [NullAllowed, Export("view")]
        UIView View { get; }

        // @property (nonatomic) OTVideoViewScaleBehavior viewScaleBehavior;
        [Export("viewScaleBehavior", ArgumentSemantic.Assign)]
        OTVideoViewScaleBehavior ViewScaleBehavior { get; set; }

        // @property (nonatomic) AVCaptureDevicePosition cameraPosition;
        [Export("cameraPosition", ArgumentSemantic.Assign)]
        AVCaptureDevicePosition CameraPosition { get; set; }
    }
}