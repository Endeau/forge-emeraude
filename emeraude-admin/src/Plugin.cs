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
        public const string Version = "1.2.0";

        internal static ConfigEntry<int> PositionEverySeconds;
        internal static ConfigEntry<int> SnapshotEveryMinutes;
        internal static ConfigEntry<int> KeptSaves;

        void Awake()
        {
            PositionEverySeconds = Config.Bind("Journal", "PositionEverySeconds", 60,
                new ConfigDescription("Serveur : intervalle entre deux positions notées pour chaque joueur, en secondes.",
                    new AcceptableValueRange<int>(10, 3600)));
            SnapshotEveryMinutes = Config.Bind("Journal", "SnapshotEveryMinutes", 5,
                new ConfigDescription("Joueur : intervalle entre deux inventaires complets envoyés au serveur (seulement s'il a changé), en minutes.",
                    new AcceptableValueRange<int>(1, 60)));
            KeptSaves = Config.Bind("Journal", "KeptSaves", 3,
                new ConfigDescription("Serveur : nombre de périodes de sauvegarde archivées par joueur (<joueur>.1.log, .2.log…).",
                    new AcceptableValueRange<int>(1, 20)));

            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            // Si la console a déjà été initialisée avant le chargement du mod
            if (Traverse.Create(typeof(Terminal)).Field<bool>("m_terminalInitialized").Value) Commands.Register();
            Logger.LogInfo($"{Name} {Version} chargé");
        }

        void Update()
        {
            if (ZNet.instance == null || ZRoutedRpc.instance == null) return;
            if (ZNet.instance.IsServer()) Server.Tick();
            Client.Tick();
        }
    }
}
