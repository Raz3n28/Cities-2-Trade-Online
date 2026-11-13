// Shared by the mod (net472) and the server (net8). Keep this file free of
// external dependencies and of newer C# features so both can compile it.
using System;
using System.Collections.Generic;
using System.Text;

namespace TradeOnline.Protocol
{
    /// <summary>
    /// One protocol message. On the wire it is a single UTF-8 line:
    /// <c>TYPE\tkey=value\tkey=value\n</c>. Values are escaped so they never
    /// contain tabs or newlines. See docs/PLANO.md for the full list of types.
    /// </summary>
    public sealed class Message
    {
        public string Type { get; private set; }
        private readonly Dictionary<string, string> m_Fields = new Dictionary<string, string>();

        public Message(string type)
        {
            if (string.IsNullOrEmpty(type) || type.IndexOfAny(new[] { '\t', '\n', '\r', '=' }) >= 0)
                throw new ArgumentException("Invalid message type", nameof(type));
            Type = type;
        }

        public IEnumerable<KeyValuePair<string, string>> Fields { get { return m_Fields; } }

        public Message With(string key, string value)
        {
            if (string.IsNullOrEmpty(key) || key.IndexOfAny(new[] { '\t', '\n', '\r', '=' }) >= 0)
                throw new ArgumentException("Invalid field key", nameof(key));
            m_Fields[key] = value ?? string.Empty;
            return this;
        }

        public Message With(string key, long value)
        {
            return With(key, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        public string Get(string key, string fallback = "")
        {
            string value;
            return m_Fields.TryGetValue(key, out value) ? value : fallback;
        }

        public long GetLong(string key, long fallback = 0)
        {
            long value;
            return long.TryParse(Get(key), System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out value) ? value : fallback;
        }

        public bool Has(string key) { return m_Fields.ContainsKey(key); }

        /// <summary>Serializes to one line, without the trailing newline.</summary>
        public string Encode()
        {
            var sb = new StringBuilder(Type);
            foreach (var kv in m_Fields)
            {
                sb.Append('\t').Append(kv.Key).Append('=');
                Escape(kv.Value, sb);
            }
            return sb.ToString();
        }

        /// <summary>Parses one line. Returns null if the line is empty or malformed.</summary>
        public static Message Decode(string line)
        {
            if (string.IsNullOrEmpty(line)) return null;
            line = line.TrimEnd('\r', '\n');
            var parts = line.Split('\t');
            if (parts[0].Length == 0 || parts[0].Length > Limits.MaxTypeLength) return null;

            Message msg;
            try { msg = new Message(parts[0]); }
            catch (ArgumentException) { return null; }

            for (int i = 1; i < parts.Length; i++)
            {
                int eq = parts[i].IndexOf('=');
                if (eq <= 0) return null;
                msg.m_Fields[parts[i].Substring(0, eq)] = Unescape(parts[i].Substring(eq + 1));
            }
            return msg;
        }

        private static void Escape(string value, StringBuilder sb)
        {
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    default: sb.Append(c); break;
                }
            }
        }

        private static string Unescape(string value)
        {
            if (value.IndexOf('\\') < 0) return value;
            var sb = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c != '\\' || i == value.Length - 1) { sb.Append(c); continue; }
                char n = value[++i];
                switch (n)
                {
                    case 't': sb.Append('\t'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    default: sb.Append(n); break;
                }
            }
            return sb.ToString();
        }

        public override string ToString() { return Encode(); }
    }
}
