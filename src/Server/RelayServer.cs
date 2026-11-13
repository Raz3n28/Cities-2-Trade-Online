using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TradeOnline.Protocol;

namespace TradeOnline.Server
{
    /// <summary>
    /// Accepts TCP clients, tracks who is online and forwards SEND messages.
    /// The server holds no game state: each city stays authoritative for itself.
    /// </summary>
    public sealed class RelayServer
    {
        private readonly int m_Port;
        private readonly int m_MaxPlayers;
        private readonly string m_Motd;
        private readonly ConcurrentDictionary<string, ClientSession> m_Players = new();
        private int m_NextId;

        public RelayServer(int port, int maxPlayers, string motd)
        {
            m_Port = port;
            m_MaxPlayers = maxPlayers;
            m_Motd = motd;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            var listener = new TcpListener(IPAddress.Any, m_Port);
            listener.Start();
            Log.Info($"Servidor ouvindo na porta {m_Port} (protocolo v{ProtocolInfo.Version}, max {m_MaxPlayers} jogadores). Ctrl+C para sair.");
            using var reg = ct.Register(listener.Stop);

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    TcpClient tcp = await listener.AcceptTcpClientAsync(ct);
                    var session = new ClientSession(this, tcp);
                    _ = session.RunAsync(ct);
                }
            }
            finally
            {
                foreach (var p in m_Players.Values) p.Close();
            }
        }

        internal string NextId() => "p" + Interlocked.Increment(ref m_NextId);

        /// <summary>Called by a session after a valid HELLO. Returns an error code or null.</summary>
        internal string TryJoin(ClientSession session)
        {
            if (m_Players.Count >= m_MaxPlayers) return ErrorCodes.ServerFull;

            session.Name = UniqueName(session.Name);
            m_Players[session.Id] = session;

            session.Send(new Message(MessageTypes.Welcome)
                .With("id", session.Id).With("name", session.Name).With("motd", m_Motd));

            foreach (var other in m_Players.Values.Where(p => p != session))
                session.Send(JoinedMessage(other));
            Broadcast(JoinedMessage(session), except: session);

            Log.Info($"{session.Name} ({session.Id}, cidade \"{session.City}\") entrou. Online: {m_Players.Count}");
            return null;
        }

        internal void Leave(ClientSession session)
        {
            if (!m_Players.TryRemove(session.Id, out _)) return;
            Broadcast(new Message(MessageTypes.PlayerLeft).With("id", session.Id).With("name", session.Name), except: null);
            Log.Info($"{session.Name} ({session.Id}) saiu. Online: {m_Players.Count}");
        }

        internal void Route(ClientSession from, Message send)
        {
            string to = send.Get("to");
            var deliver = new Message(MessageTypes.Deliver)
                .With("from", from.Id).With("fromName", from.Name);
            foreach (var kv in send.Fields)
                if (kv.Key != "to" && kv.Key != "from" && kv.Key != "fromName")
                    deliver.With(kv.Key, kv.Value);

            if (to == "*")
            {
                Broadcast(deliver, except: from);
            }
            else if (m_Players.TryGetValue(to, out var target))
            {
                target.Send(deliver);
            }
            else
            {
                from.Send(new Message(MessageTypes.Error)
                    .With("code", ErrorCodes.UnknownTarget).With("text", $"Jogador '{to}' não está online."));
                return;
            }
            Log.Info($"{from.Name} -> {(to == "*" ? "todos" : to)}: {send.Get("kind", "?")}");
        }

        private void Broadcast(Message msg, ClientSession except)
        {
            foreach (var p in m_Players.Values)
                if (p != except) p.Send(msg);
        }

        private static Message JoinedMessage(ClientSession s) =>
            new Message(MessageTypes.PlayerJoined).With("id", s.Id).With("name", s.Name).With("city", s.City);

        private string UniqueName(string name)
        {
            string candidate = name;
            for (int n = 2; m_Players.Values.Any(p => string.Equals(p.Name, candidate, StringComparison.OrdinalIgnoreCase)); n++)
                candidate = $"{name} ({n})";
            return candidate;
        }
    }
}
