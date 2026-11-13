using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using TradeOnline.Network;

namespace TradeOnline
{
    /// <summary>Options page: Options > Cities 2 Trade Online.</summary>
    [FileLocation(Mod.Id)]
    [SettingsUIGroupOrder(kConnectionGroup, kActionsGroup)]
    [SettingsUIShowGroupName(kConnectionGroup, kActionsGroup)]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";
        public const string kConnectionGroup = "Connection";
        public const string kActionsGroup = "Actions";

        public Setting(IMod mod) : base(mod) { }

        /// <summary>Server address as host:port, e.g. 127.0.0.1:7777 or 203.0.113.5:7777.</summary>
        [SettingsUISection(kSection, kConnectionGroup)]
        [SettingsUITextInput]
        public string ServerAddress { get; set; }

        [SettingsUISection(kSection, kConnectionGroup)]
        [SettingsUITextInput]
        public string PlayerName { get; set; }

        /// <summary>Connect automatically after a city finishes loading.</summary>
        [SettingsUISection(kSection, kConnectionGroup)]
        public bool AutoConnect { get; set; }

        [SettingsUISection(kSection, kActionsGroup)]
        public bool Connect { set { TradeNetworkSystem.ConnectNow(); } }

        [SettingsUISection(kSection, kActionsGroup)]
        public bool Disconnect { set { if (Mod.Client != null) Mod.Client.Disconnect(); } }

        /// <summary>Read-only line that shows the connection state.</summary>
        [SettingsUISection(kSection, kActionsGroup)]
        public string Status { get { return TradeNetworkSystem.StatusText; } }

        public override void SetDefaults()
        {
            ServerAddress = "127.0.0.1:" + Protocol.ProtocolInfo.DefaultPort;
            PlayerName = "Prefeito";
            AutoConnect = false;
        }

        /// <summary>Splits ServerAddress into host and port, falling back to the default port.</summary>
        public void GetEndpoint(out string host, out int port)
        {
            string value = (ServerAddress ?? "").Trim();
            port = Protocol.ProtocolInfo.DefaultPort;
            int colon = value.LastIndexOf(':');
            int parsed;
            if (colon > 0 && int.TryParse(value.Substring(colon + 1), out parsed) && parsed > 0 && parsed < 65536)
            {
                port = parsed;
                value = value.Substring(0, colon);
            }
            host = value.Length > 0 ? value : "127.0.0.1";
        }
    }
}
