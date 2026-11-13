using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TradeOnline.Protocol;

namespace TradeOnline.Server
{
    /// <summary>One connected client: reads lines, writes from an outgoing queue.</summary>
    internal sealed class ClientSession
    {
        private readonly RelayServer m_Server;
        private readonly TcpClient m_Tcp;
        private readonly Channel<string> m_Outgoing = Channel.CreateBounded<string>(
            new BoundedChannelOptions(1024) { FullMode = BoundedChannelFullMode.DropWrite });
        private readonly CancellationTokenSource m_Cts = new();
        private bool m_Joined;

        public string Id { get; }
        public string Name { get; set; } = "?";
        public string City { get; private set; } = "";

        public ClientSession(RelayServer server, TcpClient tcp)
        {
            m_Server = server;
            m_Tcp = tcp;
            m_Tcp.NoDelay = true;
            Id = server.NextId();
        }

        public void Send(Message msg) => m_Outgoing.Writer.TryWrite(msg.Encode());

        public void Close() => m_Cts.Cancel();

        public async Task RunAsync(CancellationToken serverCt)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(serverCt, m_Cts.Token);
            var ct = linked.Token;
            string endpoint = m_Tcp.Client.RemoteEndPoint?.ToString() ?? "?";
            try
            {
                using var stream = m_Tcp.GetStream();
                var writer = WriteLoopAsync(stream, ct);
                await ReadLoopAsync(stream, ct);
                m_Outgoing.Writer.TryComplete();
                await writer;
            }
            catch (OperationCanceledException) { }
            catch (IOException) { }
            catch (SocketException) { }
            catch (Exception e) { Log.Warn($"Erro com {endpoint}: {e.Message}"); }
            finally
            {
                if (m_Joined) m_Server.Leave(this);
                m_Tcp.Dispose();
            }
        }

        private async Task ReadLoopAsync(NetworkStream stream, CancellationToken ct)
        {
            using var reader = new StreamReader(stream, new UTF8Encoding(false), false, 4096, leaveOpen: true);
            while (!ct.IsCancellationRequested)
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(TimeSpan.FromSeconds(Limits.TimeoutSeconds));
                string line;
                try { line = await reader.ReadLineAsync(timeout.Token); }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    Log.Warn($"{Name} ({Id}) sem resposta há {Limits.TimeoutSeconds}s, desconectando.");
                    return;
                }
                if (line == null) return; // client closed
                if (line.Length > Limits.MaxLineLength) { Error(ErrorCodes.BadMessage, "Mensagem grande demais."); return; }

                var msg = Message.Decode(line);
                if (msg == null) { Error(ErrorCodes.BadMessage, "Mensagem inválida."); continue; }
                if (!Handle(msg)) return;
            }
        }

        /// <summary>Returns false when the connection should close.</summary>
        private bool Handle(Message msg)
        {
            if (!m_Joined)
            {
                if (msg.Type != MessageTypes.Hello) { Error(ErrorCodes.NotHelloFirst, "Envie HELLO primeiro."); return false; }
                if (msg.GetLong("version") != ProtocolInfo.Version)
                {
                    Error(ErrorCodes.VersionMismatch, $"Servidor usa protocolo v{ProtocolInfo.Version}; atualize o mod.");
                    return false;
                }
                Name = Clean(msg.Get("name"), "Jogador");
                City = Clean(msg.Get("city"), "");
                string err = m_Server.TryJoin(this);
                if (err != null) { Error(err, "Servidor cheio."); return false; }
                m_Joined = true;
                return true;
            }

            switch (msg.Type)
            {
                case MessageTypes.Ping:
                    Send(new Message(MessageTypes.Pong).With("t", msg.Get("t")));
                    return true;
                case MessageTypes.Send:
                    m_Server.Route(this, msg);
                    return true;
                case MessageTypes.Bye:
                    return false;
                default:
                    Error(ErrorCodes.BadMessage, $"Tipo desconhecido: {msg.Type}");
                    return true;
            }
        }

        private void Error(string code, string text) =>
            Send(new Message(MessageTypes.Error).With("code", code).With("text", text));

        private async Task WriteLoopAsync(NetworkStream stream, CancellationToken ct)
        {
            using var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, leaveOpen: true) { NewLine = "\n" };
            await foreach (var line in m_Outgoing.Reader.ReadAllAsync(ct))
            {
                await writer.WriteLineAsync(line);
                if (m_Outgoing.Reader.Count == 0) await writer.FlushAsync(ct);
            }
            await writer.FlushAsync(ct);
        }

        private static string Clean(string value, string fallback)
        {
            value = (value ?? "").Trim();
            if (value.Length == 0) return fallback;
            return value.Length > Limits.MaxNameLength ? value.Substring(0, Limits.MaxNameLength) : value;
        }
    }
}
