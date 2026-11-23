// Emeraude Deconstruction — déconstruire les bateaux et les charrettes au marteau.
//
// Le jeu sait déjà retirer un bateau ou une charrette au marteau (matériaux rendus, cale vidée
// comme quand on les détruit), mais leurs prefabs ont m_canBeRemoved = false. Ce mod le réactive,
// sauf si quelqu'un est à bord, tire la charrette, a la cale ouverte ou si la cale n'est pas vide.
// Les règles habituelles restent : établi à portée, zone protégée.
// Un bateau détruit laisse ses matériaux dans une caisse flottante, comme sa cale.
using BepInEx;
using HarmonyLib;

namespace EmeraudeDeconstruction
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "emeraude.deconstruction";
        public const string Name = "Emeraude Deconstruction";
        public const string Version = "1.2.0";

        void Awake()
        {
            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo($"{Name} {Version} loaded");
        }
    }
}
