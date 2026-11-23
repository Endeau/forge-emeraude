// Emeraude Admin — journal des joueurs et commandes d'administration pour serveur dédié Valheim.
//
// Serveur : écrit un journal par joueur (connexions, positions, objets gagnés/perdus, morts,
//           sauvegardes) et exécute les commandes adm:* des admins (adminlist.txt).
// Joueurs : envoient au serveur les changements de leur inventaire, appliquent tp et give.
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace EmeraudeAdmin
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "emeraude.admin";
        public const string Name = "Emeraude Admin";
        public const string Version = "1.4.2";

        internal static ConfigEntry<Language> Lang;
        internal static ConfigEntry<int> PositionEverySeconds;
        internal static ConfigEntry<int> SnapshotEveryMinutes;
        internal static ConfigEntry<int> KeptSaves;

        void Awake()
        {
            Lang = Config.Bind("General", "Language", Language.English,
                "Language of the in-game messages, of the replies to admins and of the journal (server side). / " +
                "Langue des messages en jeu, des réponses aux admins et du journal (côté serveur).");
            PositionEverySeconds = Config.Bind("Journal", "PositionEverySeconds", 60,
                new ConfigDescription("Server: interval between two logged positions for each player, in seconds.",
                    new AcceptableValueRange<int>(10, 3600)));
            SnapshotEveryMinutes = Config.Bind("Journal", "SnapshotEveryMinutes", 5,
                new ConfigDescription("Player: interval between two full inventories sent to the server (only if it changed), in minutes.",
                    new AcceptableValueRange<int>(1, 60)));
            KeptSaves = Config.Bind("Journal", "KeptSaves", 3,
                new ConfigDescription("Server: number of archived save periods kept per player (<player>.1.log, .2.log…).",
                    new AcceptableValueRange<int>(1, 20)));

            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            // Si la console a déjà été initialisée avant le chargement du mod
            if (Traverse.Create(typeof(Terminal)).Field<bool>("m_terminalInitialized").Value) Commands.Register();
            Logger.LogInfo($"{Name} {Version} loaded ({Lang.Value})");
        }

        void Update()
        {
            if (ZNet.instance == null || ZRoutedRpc.instance == null) return;
            if (ZNet.instance.IsServer()) Server.Tick();
            Client.Tick();
        }
    }
}
