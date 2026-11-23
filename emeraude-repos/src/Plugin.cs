// Emeraude Repos — le buff « Reposé » dure plus longtemps.
//
// Le jeu calcule sa durée chez chaque joueur : m_baseTTL (5 min) + (confort - 1) × m_TTLPerComfortLevel (1 min).
// Chaque joueur reçoit sa propre copie du buff (SEMan.AddStatusEffect le clone) : on la multiplie une fois,
// dans Setup, avant le premier calcul de durée.
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace EmeraudeRepos
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "emeraude.repos";
        public const string Name = "Emeraude Repos";
        public const string Version = "1.0.0";

        internal static ConfigEntry<float> DurationMultiplier;

        void Awake()
        {
            DurationMultiplier = Config.Bind("Rested", "DurationMultiplier", 1.5f,
                new ConfigDescription("Multiplies the duration of the Rested buff (base time and time per comfort level).",
                    new AcceptableValueRange<float>(0.1f, 10f)));
            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo($"{Name} {Version} loaded (Rested x{DurationMultiplier.Value})");
        }
    }

    [HarmonyPatch(typeof(SE_Rested), nameof(SE_Rested.Setup))]
    static class RestedSetupPatch
    {
        static void Prefix(SE_Rested __instance)
        {
            __instance.m_baseTTL *= Plugin.DurationMultiplier.Value;
            __instance.m_TTLPerComfortLevel *= Plugin.DurationMultiplier.Value;
        }
    }
}
