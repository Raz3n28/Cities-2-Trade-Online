using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TradeOnline.Protocol;

namespace TradeOnline.Network
{
    public enum ConnectionState { Disconnected, Connecting, Connected }

    /// <summary>
    /// Talks to the relay server on a background thread so the game never waits
    /// on the network. Incoming messages are queued; the game drains them on the
    /// main thread (see TradeNetworkSystem). Reconnects automatically.
    /// No Unity or game types in here, so it can be reasoned about on its own.
    /// </summary>
    public sealed class TradeClient : IDisposable
    {
        private const int ReconnectDelayMs = 5000;

        private readonly ConcurrentQueue<Message> m_Incoming = new ConcurrentQueue<Message>();
        private readonly object m_WriteLock = new object();
        private readonly Action<string> m_Log;

        private Thread m_Thread;
        private volatile bool m_Wanted;
        private volatile ConnectionState m_State = ConnectionState.Disconnected;
        private TcpClient m_Tcp;
        private StreamWriter m_Writer;

        private string m_Host;
        private int m_Port;
        private string m_Name;
        private string m_City;

        public TradeClient(Action<string> log) { m_Log = log ?? (s => { }); }

        public ConnectionState State { get { return m_State; } }
        public string MyId { get; private set; }
        public string LastError { get; private set; }

        public void Connect(string host, int port, string playerName, string cityName)
        {
            Disconnect();
            m_Host = host; m_Port = port; m_Name = playerName; m_City = cityName;
            m_Wanted = true;
            LastError = null;
            m_Thread = new Thread(Run) { IsBackground = true, Name = "TradeOnline.Network" };
            m_Thread.Start();
        }

        public void Disconnect()
        {
            m_Wanted = false;
            if (m_State == ConnectionState.Connected) TrySend(new Message(MessageTypes.Bye));
            CloseSocket();
            if (m_Thread != null && m_Thread != Thread.CurrentThread) m_Thread.Join(2000);
            m_Thread = null;
            m_State = ConnectionState.Disconnected;
        }

        /// <summary>Queue-safe send from any thread. Returns false if not connected.</summary>
        public bool TrySend(Message msg)
        {
            lock (m_WriteLock)
            {
                if (m_Writer == null) return false;
                try { m_Writer.WriteLine(msg.Encode()); m_Writer.Flush(); return true; }
                catch (Exception) { return false; }
            }
        }

        public bool TryDequeue(out Message msg) { return m_Incoming.TryDequeue(out msg); }

        public void Dispose() { Disconnect(); }

        private void Run()
        {
            while (m_Wanted)
            {
                try { Session(); }
                catch (Exception e) when (e is IOException || e is SocketException || e is ObjectDisposedException)
                {
                    if (m_Wanted) { LastError = e.Message; m_Log("Conexão perdida: " + e.Message); }
                }
                catch (Exception e)
                {
                    LastError = e.Message;
                    m_Log("Erro inesperado na rede: " + e);
                }
                CloseSocket();
                m_State = ConnectionState.Disconnected;
                if (!m_Wanted) break;
                m_Log("Tentando reconectar em " + (ReconnectDelayMs / 1000) + "s...");
                for (int waited = 0; waited < ReconnectDelayMs && m_Wanted; waited += 100) Thread.Sleep(100);
            }
        }

        private void Session()
        {
            m_State = ConnectionState.Connecting;
            m_Log("Conectando em " + m_Host + ":" + m_Port + "...");
            var tcp = new TcpClient { NoDelay = true };
            tcp.Connect(m_Host, m_Port);
            var stream = tcp.GetStream();
            var reader = new StreamReader(stream, new UTF8Encoding(false));
            lock (m_WriteLock)
            {
                m_Tcp = tcp;
                m_Writer = new StreamWriter(stream, new UTF8Encoding(false)) { NewLine = "\n" };
            }

            TrySend(new Message(MessageTypes.Hello)
                .With("name", m_Name).With("version", ProtocolInfo.Version).With("city", m_City));

            // Keep-alive on a timer so the read below can simply block.
            using (new Timer(_ => TrySend(new Message(MessageTypes.Ping).With("t", DateTime.UtcNow.Ticks)),
                null, Limits.PingIntervalSeconds * 1000, Limits.PingIntervalSeconds * 1000))
            {
                while (m_Wanted)
                {
                    string line = reader.ReadLine(); // throws when Disconnect() closes the socket
                    if (line == null) throw new IOException("Servidor fechou a conexão.");
                    var msg = Message.Decode(line);
                    if (msg != null) Handle(msg);
                }
            }
        }

        private void Handle(Message msg)
        {
            if (msg.Type == MessageTypes.Welcome)
            {
                MyId = msg.Get("id");
                m_State = ConnectionState.Connected;
            }
            else if (msg.Type == MessageTypes.Error)
            {
                LastError = msg.Get("text");
                string code = msg.Get("code");
                if (code == ErrorCodes.VersionMismatch || code == ErrorCodes.ServerFull)
                    m_Wanted = false; // retrying will not help
            }
            if (msg.Type != MessageTypes.Pong) m_Incoming.Enqueue(msg);
        }

        private void CloseSocket()
        {
            lock (m_WriteLock)
            {
                try { if (m_Tcp != null) m_Tcp.Close(); } catch (Exception) { }
                m_Tcp = null;
                m_Writer = null;
            }
        }
    }
}
