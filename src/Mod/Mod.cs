using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using TradeOnline.Network;
using TradeOnline.Systems;

namespace TradeOnline
{
    /// <summary>Entry point the game calls when the mod is loaded and unloaded.</summary>
    public class Mod : IMod
    {
        public const string Id = "TradeOnline";

        // Log file: %USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\TradeOnline.log
        public static readonly ILog Log = LogManager.GetLogger(Id).SetShowsErrorsInUI(false);

        public static Setting Settings { get; private set; }
        public static TradeClient Client { get; private set; }

        public void OnLoad(UpdateSystem updateSystem)
        {
            Log.Info("Carregando Cities 2 Trade Online...");
            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                Log.Info("Mod em: " + asset.path);

            Client = new TradeClient(text => Log.Info(text));

            Settings = new Setting(this);
            Settings.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(Settings));
            GameManager.instance.localizationManager.AddSource("pt-BR", new LocalePTBR(Settings));
            AssetDatabase.global.LoadSettings(Id, Settings, new Setting(this));

            // MainLoop runs in menus too, so connection status stays fresh everywhere.
            updateSystem.UpdateAt<TradeNetworkSystem>(SystemUpdatePhase.MainLoop);
            Log.Info("Pronto.");
        }

        public void OnDispose()
        {
            Log.Info("Descarregando.");
            if (Client != null) { Client.Dispose(); Client = null; }
            if (Settings != null) { Settings.UnregisterInOptionsUI(); Settings = null; }
        }
    }
}
