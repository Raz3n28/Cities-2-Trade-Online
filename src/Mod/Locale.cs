using System.Collections.Generic;
using Colossal;

namespace TradeOnline
{
    /// <summary>Base for option-page texts; each language only supplies the strings.</summary>
    public abstract class LocaleBase : IDictionarySource
    {
        private readonly Setting m_Setting;
        protected LocaleBase(Setting setting) { m_Setting = setting; }

        protected abstract string[] Texts { get; }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            var t = Texts;
            var s = m_Setting;
            return new Dictionary<string, string>
            {
                { s.GetSettingsLocaleID(), t[0] },
                { s.GetOptionTabLocaleID(Setting.kSection), t[1] },
                { s.GetOptionGroupLocaleID(Setting.kConnectionGroup), t[2] },
                { s.GetOptionGroupLocaleID(Setting.kActionsGroup), t[3] },
                { s.GetOptionLabelLocaleID(nameof(Setting.ServerAddress)), t[4] },
                { s.GetOptionDescLocaleID(nameof(Setting.ServerAddress)), t[5] },
                { s.GetOptionLabelLocaleID(nameof(Setting.PlayerName)), t[6] },
                { s.GetOptionDescLocaleID(nameof(Setting.PlayerName)), t[7] },
                { s.GetOptionLabelLocaleID(nameof(Setting.AutoConnect)), t[8] },
                { s.GetOptionDescLocaleID(nameof(Setting.AutoConnect)), t[9] },
                { s.GetOptionLabelLocaleID(nameof(Setting.Connect)), t[10] },
                { s.GetOptionDescLocaleID(nameof(Setting.Connect)), t[11] },
                { s.GetOptionLabelLocaleID(nameof(Setting.Disconnect)), t[12] },
                { s.GetOptionDescLocaleID(nameof(Setting.Disconnect)), t[13] },
                { s.GetOptionLabelLocaleID(nameof(Setting.Status)), t[14] },
                { s.GetOptionDescLocaleID(nameof(Setting.Status)), t[15] },
            };
        }

        public void Unload() { }
    }

    public class LocaleEN : LocaleBase
    {
        public LocaleEN(Setting s) : base(s) { }
        protected override string[] Texts { get { return new[] {
            "Cities 2 Trade Online", "Main", "Connection", "Actions",
            "Server address", "Relay server as host:port, for example 127.0.0.1:7777.",
            "Player name", "Name other players see.",
            "Connect automatically", "Connect as soon as a city finishes loading.",
            "Connect", "Connect to the server now.",
            "Disconnect", "Leave the server.",
            "Status", "Current connection state.",
        }; } }
    }

    public class LocalePTBR : LocaleBase
    {
        public LocalePTBR(Setting s) : base(s) { }
        protected override string[] Texts { get { return new[] {
            "Cities 2 Trade Online", "Principal", "Conexão", "Ações",
            "Endereço do servidor", "Servidor no formato ip:porta, por exemplo 127.0.0.1:7777.",
            "Nome do jogador", "Nome que os outros jogadores veem.",
            "Conectar automaticamente", "Conecta assim que a cidade termina de carregar.",
            "Conectar", "Conecta ao servidor agora.",
            "Desconectar", "Sai do servidor.",
            "Status", "Estado atual da conexão.",
        }; } }
    }
}
