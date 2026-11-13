namespace TradeOnline.Protocol
{
    /// <summary>Protocol version. Bump when a change breaks older clients.</summary>
    public static class ProtocolInfo
    {
        public const int Version = 1;
        public const int DefaultPort = 7777;
    }

    public static class Limits
    {
        public const int MaxTypeLength = 32;
        public const int MaxLineLength = 16 * 1024;
        public const int MaxNameLength = 24;
        public const int PingIntervalSeconds = 10;
        public const int TimeoutSeconds = 30;
    }

    /// <summary>Message types. C = client sends, S = server sends.</summary>
    public static class MessageTypes
    {
        /// <summary>C: first message. Fields: name, version, city.</summary>
        public const string Hello = "HELLO";
        /// <summary>S: reply to HELLO. Fields: id, name (final, may get a suffix), motd.</summary>
        public const string Welcome = "WELCOME";
        /// <summary>S: a player is online. Fields: id, name, city.</summary>
        public const string PlayerJoined = "PLAYER_JOINED";
        /// <summary>S: a player went offline. Fields: id, name.</summary>
        public const string PlayerLeft = "PLAYER_LEFT";
        /// <summary>C: keep-alive. Fields: t (client time, echoed back).</summary>
        public const string Ping = "PING";
        /// <summary>S: reply to PING. Fields: t.</summary>
        public const string Pong = "PONG";
        /// <summary>C: send a payload to one player (to=id) or everyone (to=*).
        /// Fields: to, kind, plus any payload fields. Phase 2 trades use this.</summary>
        public const string Send = "SEND";
        /// <summary>S: a forwarded SEND. Same fields, plus from and fromName; "to" is removed.</summary>
        public const string Deliver = "DELIVER";
        /// <summary>C: polite disconnect.</summary>
        public const string Bye = "BYE";
        /// <summary>S: something went wrong. Fields: code, text.</summary>
        public const string Error = "ERROR";
    }

    public static class ErrorCodes
    {
        public const string VersionMismatch = "version_mismatch";
        public const string NotHelloFirst = "hello_required";
        public const string UnknownTarget = "unknown_target";
        public const string BadMessage = "bad_message";
        public const string ServerFull = "server_full";
    }
}
