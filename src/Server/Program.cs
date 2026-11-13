using System;
using System.Threading;
using System.Threading.Tasks;
using TradeOnline.Protocol;

namespace TradeOnline.Server
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            int port = ProtocolInfo.DefaultPort;
            int maxPlayers = 8;
            string motd = "Bem-vindo ao Cities 2 Trade Online!";

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--port" when i + 1 < args.Length: port = int.Parse(args[++i]); break;
                    case "--max-players" when i + 1 < args.Length: maxPlayers = int.Parse(args[++i]); break;
                    case "--motd" when i + 1 < args.Length: motd = args[++i]; break;
                    case "-h":
                    case "--help":
                        Console.WriteLine("TradeServer [--port 7777] [--max-players 8] [--motd \"texto\"]");
                        return 0;
                }
            }

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            var server = new RelayServer(port, maxPlayers, motd);
            try
            {
                await server.RunAsync(cts.Token);
            }
            catch (OperationCanceledException) { }
            Log.Info("Servidor encerrado.");
            return 0;
        }
    }

    internal static class Log
    {
        private static readonly object s_Lock = new object();
        public static void Info(string text) => Write("INFO", text);
        public static void Warn(string text) => Write("WARN", text);
        private static void Write(string level, string text)
        {
            lock (s_Lock) Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {level} {text}");
        }
    }
}
