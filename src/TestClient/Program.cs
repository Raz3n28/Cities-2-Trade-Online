using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TradeOnline.Protocol;

namespace TradeOnline.TestClient
{
    /// <summary>
    /// A tiny console client that speaks the same protocol as the mod, so the
    /// server can be tested without opening the game.
    /// </summary>
    public static class Program
    {
        private static readonly Dictionary<string, string> s_Players = new();
        private static readonly object s_Lock = new();
        private static volatile bool s_ManualPing;

        public static async Task<int> Main(string[] args)
        {
            string host = "127.0.0.1", name = "Teste", city = "Cidade Teste";
            int port = ProtocolInfo.DefaultPort;
            for (int i = 0; i + 1 < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--host": host = args[++i]; break;
                    case "--port": port = int.Parse(args[++i]); break;
                    case "--name": name = args[++i]; break;
                    case "--city": city = args[++i]; break;
                }
            }

            using var tcp = new TcpClient { NoDelay = true };
            try { await tcp.ConnectAsync(host, port); }
            catch (SocketException e)
            {
                Print($"Não consegui conectar em {host}:{port}: {e.Message}");
                return 1;
            }

            var stream = tcp.GetStream();
            var writer = new StreamWriter(stream, new UTF8Encoding(false)) { NewLine = "\n", AutoFlush = true };
            var reader = new StreamReader(stream, new UTF8Encoding(false));

            await writer.WriteLineAsync(new Message(MessageTypes.Hello)
                .With("name", name).With("version", ProtocolInfo.Version).With("city", city).Encode());
            Print($"Conectado em {host}:{port}. Comandos: /lista, /enviar <id|*> <texto>, /ping, /sair");

            var readTask = Task.Run(() => ReadLoop(reader));
            var pingTask = Task.Run(async () =>
            {
                while (tcp.Connected)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Limits.PingIntervalSeconds));
                    try { lock (writer) writer.WriteLine(new Message(MessageTypes.Ping).With("t", Environment.TickCount64).Encode()); }
                    catch { return; }
                }
            });

            string line;
            while ((line = Console.ReadLine()) != null && !readTask.IsCompleted)
            {
                line = line.Trim();
                if (line.Length == 0) continue;
                Message msg = null;
                if (line == "/sair") break;
                else if (line == "/lista")
                {
                    lock (s_Lock)
                    {
                        Print(s_Players.Count == 0 ? "Ninguém mais online." : "Online:");
                        foreach (var kv in s_Players) Print($"  {kv.Key}  {kv.Value}");
                    }
                }
                else if (line == "/ping") { s_ManualPing = true; msg = new Message(MessageTypes.Ping).With("t", Environment.TickCount64); }
                else if (line.StartsWith("/enviar "))
                {
                    var parts = line.Split(' ', 3);
                    if (parts.Length < 3) { Print("Uso: /enviar <id|*> <texto>"); continue; }
                    msg = new Message(MessageTypes.Send).With("to", parts[1]).With("kind", "chat").With("text", parts[2]);
                }
                else if (line.StartsWith("/espera "))
                {
                    // Used by automated tests: wait N milliseconds.
                    await Task.Delay(int.Parse(line.Substring(8)));
                }
                else Print("Comando desconhecido.");

                if (msg != null) lock (writer) writer.WriteLine(msg.Encode());
            }

            try { lock (writer) writer.WriteLine(new Message(MessageTypes.Bye).Encode()); } catch { }
            await Task.Delay(200);
            return 0;
        }

        private static void ReadLoop(StreamReader reader)
        {
            try
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var msg = Message.Decode(line);
                    if (msg == null) continue;
                    switch (msg.Type)
                    {
                        case MessageTypes.Welcome:
                            Print($"Bem-vindo, {msg.Get("name")} (seu id: {msg.Get("id")}). {msg.Get("motd")}");
                            break;
                        case MessageTypes.PlayerJoined:
                            lock (s_Lock) s_Players[msg.Get("id")] = msg.Get("name");
                            Print($"+ {msg.Get("name")} ({msg.Get("id")}) entrou, cidade \"{msg.Get("city")}\"");
                            break;
                        case MessageTypes.PlayerLeft:
                            lock (s_Lock) s_Players.Remove(msg.Get("id"));
                            Print($"- {msg.Get("name")} ({msg.Get("id")}) saiu");
                            break;
                        case MessageTypes.Deliver:
                            Print($"[{msg.Get("fromName")}] {msg.Get("kind")}: {msg.Get("text")}");
                            break;
                        case MessageTypes.Pong:
                            if (!s_ManualPing) break; // automatic keep-alive, stay quiet
                            s_ManualPing = false;
                            long sent = msg.GetLong("t");
                            Print($"pong ({Environment.TickCount64 - sent} ms)");
                            break;
                        case MessageTypes.Error:
                            Print($"ERRO {msg.Get("code")}: {msg.Get("text")}");
                            break;
                        default:
                            Print(line);
                            break;
                    }
                }
            }
            catch (IOException) { }
            Print("Conexão encerrada pelo servidor.");
        }

        private static void Print(string text) => Console.WriteLine(text);
    }
}
