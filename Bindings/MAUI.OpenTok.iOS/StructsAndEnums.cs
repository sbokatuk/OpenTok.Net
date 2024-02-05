using ObjCRuntime;

namespace OpenTok.Net.iOS
{
    public enum OTSessionConnectionStatus
    {
        NotConnected,
        Connected,
        Connecting,
        Reconnecting,
        Disconnecting,
        Failed
    }

    public enum OTSessionICEIncludeServers
    {
        All,
        Custom
    }

    public enum OTSessionICETransportPolicy
    {
        All,
        Relay
    }

    public enum OTPublisherKitVideoType
    {
        Camera = 1,
        Screen = 2
    }

    public enum OTSubscriberVideoEventReason
    {
        PublisherPropertyChanged = 1,
        SubscriberPropertyChanged = 2,
        QualityChanged = 3,
        CodecNotSupported = 4
    }

    public enum OTStreamVideoType
    {
        Camera = 1,
        Screen = 2,
        Custom = 3
    }

    public enum OTSessionErrorCode
    {
        SessionSuccess = 0,
        AuthorizationFailure = 1004,
        ErrorInvalidSession = 1005,
        ConnectionFailed = 1006,
        NullOrInvalidParameter = 1011,
        NotConnected = 1010,
        SessionIllegalState = 1015,
        NoMessagingServer = 1503,
        ConnectionRefused = 1023,
        SessionStateFailed = 1020,
        P2PSessionMaxParticipants = 1403,
        SessionConnectionTimeout = 1021,
        SessionInternalError = 2000,
        SessionInvalidSignalType = 1461,
        SessionSignalDataTooLong = 1413,
        SessionUnableToForceMute = 1540,
        ConnectionDropped = 1022,
        SessionSubscriberNotFound = 1112,
        SessionPublisherNotFound = 1113,
        SessionBlockedCountry = 1026,
        SessionConnectionLimitExceeded = 1027,
        SessionEncryptionSecretMissing = 6000,
        SessionInvalidEncryptionSecret = 6004
    }

    public enum OTPublisherErrorCode
    {
        PublisherSuccess = 0,
        SessionDisconnected = 1010,
        PublisherInternalError = 2000,
        PublisherWebRTCError = 1610,
        PublisherEncryptionInternalError = 6001
    }

    public enum OTSubscriberErrorCode
    {
        OTSubscriberSuccess = 0,
        OTConnectionTimedOut = 1542,
        OTSubscriberSessionDisconnected = 1010,
        OTSubscriberWebRTCError = 1600,
        OTSubscriberServerCannotFindStream = 1604,
        OTSubscriberStreamLimitExceeded = 1605,
        OTSubscriberInternalError = 2000,
        OTSubscriberDecryptionInternalError = 6002,
        EncryptionSecretMismatch = 6003
    }

    public enum OTVideoOrientation
    {
        Up = 1,
        Down = 2,
        Left = 3,
        Right = 4
    }

    public enum OTPixelFormat
    {
        I420 = 1228157488,
        Argb = 1095911234,
        Nv12 = 1314271538
    }

    [Native]
    public enum OTVideoViewScaleBehavior : long
    {
        t,
        ll
    }

    [Native]
    public enum OTVideoContentHint : long
    {
        None,
        Motion,
        Detail,
        Text
    }

    public enum OTPublisherVideoEventReason
    {
        PublisherPropertyChanged = 1,
        QualityChanged = 3
    }

    [Native]
    public enum OTCameraCaptureResolution : long
    {
        Low,
        Medium,
        High,
        High1080p
    }

    [Native]
    public enum OTCameraCaptureFrameRate : long
    {
        OTCameraCaptureFrameRate30FPS = 30,
        OTCameraCaptureFrameRate15FPS = 15,
        OTCameraCaptureFrameRate7FPS = 7,
        OTCameraCaptureFrameRate1FPS = 1
    }

}


