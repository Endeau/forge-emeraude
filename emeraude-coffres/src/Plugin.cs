// Emeraude Coffres — complète Craft From Containers (aedenthorn) pour les recettes « un seul
// ingrédient au choix », comme celles qui acceptent n'importe quel poisson.
//
// Le jeu de base marque ces recettes m_requireOnlyOneIngredient : il suffit d'avoir l'un des
// ingrédients, en quantité suffisante et d'une même qualité (étoiles du poisson). Craft From
// Containers ignore ce drapeau : il exige tous les ingrédients, et le jeu ne cherche l'ingrédient
// choisi que dans l'inventaire du joueur. Résultat : le poisson rangé dans un coffre n'est jamais vu.
//
// Le serveur impose aussi la portée des coffres de Craft From Containers (Range.cs).
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace EmeraudeCoffres
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(CraftFromContainersGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "emeraude.coffres";
        public const string Name = "Emeraude Coffres";
        public const string Version = "1.4.0";
        const string CraftFromContainersGuid = "aedenthorn.CraftFromContainers";

        internal static ConfigEntry<Language> Lang;
        internal static BepInEx.Logging.ManualLogSource Log;

        void Awake()
        {
            Log = Logger;
            Lang = Config.Bind("General", "Language", Language.English,
                "Language of the in-game messages. / Langue des messages en jeu.");
            if (!Chests.Bind())
            {
                Logger.LogWarning($"{Name} : Craft From Containers introuvable, rien à compléter");
                return;
            }
            Range.Bind(Config);
            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo($"{Name} {Version} loaded");
        }
    }
}
