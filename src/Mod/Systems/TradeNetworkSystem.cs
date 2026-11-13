using System;
using System.Collections.Generic;
using Colossal.Serialization.Entities;
using Game;
using Game.City;
using TradeOnline.Network;
using TradeOnline.Protocol;

namespace TradeOnline.Systems
{
    /// <summary>
    /// Runs every frame on the main thread: takes messages the network thread
    /// received and applies them to the game. In Phase 1 that means logging and
    /// keeping the player list and status text up to date.
    /// </summary>
    public partial class TradeNetworkSystem : GameSystemBase
    {
        private static TradeNetworkSystem s_Instance;
        private const int MaxMessagesPerFrame = 64;

        /// <summary>Players currently online (id → name), not counting us.</summary>
        public static readonly Dictionary<string, string> Players = new Dictionary<string, string>();

        public static string StatusText { get; private set; } = "Desconectado";

        private CityConfigurationSystem m_CityConfig;

        protected override void OnCreate()
        {
            base.OnCreate();
            s_Instance = this;
            m_CityConfig = World.GetOrCreateSystemManaged<CityConfigurationSystem>();
        }

        protected override void OnDestroy()
        {
            if (s_Instance == this) s_Instance = null;
            base.OnDestroy();
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (mode == GameMode.Game && Mod.Settings != null && Mod.Settings.AutoConnect)
                ConnectNow();
        }

        /// <summary>Connects using the current settings. Safe to call from the options button.</summary>
        public static void ConnectNow()
        {
            if (Mod.Client == null || Mod.Settings == null) return;
            string host; int port;
            Mod.Settings.GetEndpoint(out host, out port);
            string city = s_Instance != null ? s_Instance.CurrentCityName() : "";
            lock (Players) Players.Clear();
            Mod.Client.Connect(host, port, Mod.Settings.PlayerName, city);
        }

        private string CurrentCityName()
        {
            try { return m_CityConfig != null ? m_CityConfig.cityName ?? "" : ""; }
            catch (Exception) { return ""; }
        }

        protected override void OnUpdate()
        {
            var client = Mod.Client;
            if (client == null) return;

            Message msg;
            for (int i = 0; i < MaxMessagesPerFrame && client.TryDequeue(out msg); i++)
                Apply(msg);

            UpdateStatus(client);
        }

        private static void Apply(Message msg)
        {
            switch (msg.Type)
            {
                case MessageTypes.Welcome:
                    Mod.Log.Info("Conectado como " + msg.Get("name") + " (id " + msg.Get("id") + "). " + msg.Get("motd"));
                    break;
                case MessageTypes.PlayerJoined:
                    lock (Players) Players[msg.Get("id")] = msg.Get("name");
                    Mod.Log.Info("Jogador online: " + msg.Get("name") + " (cidade \"" + msg.Get("city") + "\")");
                    break;
                case MessageTypes.PlayerLeft:
                    lock (Players) Players.Remove(msg.Get("id"));
                    Mod.Log.Info("Jogador saiu: " + msg.Get("name"));
                    break;
                case MessageTypes.Deliver:
                    // Phase 2 will turn "trade" deliveries into resources here.
                    Mod.Log.Info("Recebido de " + msg.Get("fromName") + ": " + msg.Get("kind") + " " + msg.Encode());
                    break;
                case MessageTypes.Error:
                    Mod.Log.Warn("Servidor: " + msg.Get("code") + " - " + msg.Get("text"));
                    break;
            }
        }

        private static void UpdateStatus(TradeClient client)
        {
            switch (client.State)
            {
                case ConnectionState.Connected:
                    int count; lock (Players) count = Players.Count;
                    StatusText = "Conectado (" + count + " outro(s) online)";
                    break;
                case ConnectionState.Connecting:
                    StatusText = "Conectando...";
                    break;
                default:
                    StatusText = client.LastError != null ? "Desconectado: " + client.LastError : "Desconectado";
                    break;
            }
        }
    }
}
