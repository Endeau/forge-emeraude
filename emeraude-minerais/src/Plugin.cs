// Emeraude Minerais — compte, par joueur, le minerai récolté, le minerai mis au four, les barres produites et les barres utilisées.
//
// Joueurs : le propriétaire d'un four (celui qui fait tourner sa zone) note qui a déposé chaque minerai et
//           signale le dépôt puis la barre produite ; chacun signale les barres qu'il dépense en fabriquant
//           ou en construisant.
// Serveur : additionne tout dans BepInEx/config/emeraude-minerais/classement.tsv, lu par le bot Discord.
using BepInEx;
using HarmonyLib;

namespace EmeraudeMinerais
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "emeraude.minerais";
        public const string Name = "Emeraude Minerais";
        public const string Version = "1.1.0";

        void Awake()
        {
            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo($"{Name} {Version} loaded");
        }

        void Update()
        {
            if (ZNet.instance != null && ZNet.instance.IsServer()) Stats.Tick();
        }
    }
}
