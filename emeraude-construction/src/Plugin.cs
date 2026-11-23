// Emeraude Construction — une touche remet l'accroche de la pièce en mode automatique.
//
// En mode construction, Q et E font défiler les points d'accroche de la pièce à poser (Player.m_manualSnapPoint,
// -1 = automatique). Le jeu garde ce choix tant qu'on pose la même pièce : pour revenir à l'automatique, il faut
// refaire tout le tour. La touche de ce mod le remet à -1 d'un coup, juste avant que le jeu place la pièce fantôme.
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace EmeraudeConstruction
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "emeraude.construction";
        public const string Name = "Emeraude Construction";
        public const string Version = "1.0.0";

        internal static ConfigEntry<KeyboardShortcut> ResetSnapKey;

        void Awake()
        {
            ResetSnapKey = Config.Bind("Snapping", "ResetSnapKey", new KeyboardShortcut(KeyCode.Backspace),
                "While placing a piece, puts snapping back to automatic (undoes the snap point chosen with Q/E).");
            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo($"{Name} {Version} loaded (reset snapping: {ResetSnapKey.Value})");
        }
    }

    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    static class ResetSnapPatch
    {
        const int AutomaticSnap = -1;

        static void Prefix(Player __instance, GameObject ___m_placementGhost, ref int ___m_manualSnapPoint)
        {
            if (__instance != Player.m_localPlayer || ___m_placementGhost == null) return;
            if (___m_manualSnapPoint == AutomaticSnap || KeysUsedElsewhere()) return;
            if (!Plugin.ResetSnapKey.Value.IsDown()) return;

            ___m_manualSnapPoint = AutomaticSnap;
            __instance.Message(MessageHud.MessageType.Center, "$msg_snapping $msg_snapping_auto");
        }

        // Mêmes garde-fous que le jeu pour Q/E, plus la saisie d’un texte (panneau, nom de portail).
        static bool KeysUsedElsewhere() =>
            Hud.IsPieceSelectionVisible() || Minimap.instance.m_mode == Minimap.MapMode.Large ||
            Console.IsVisible() || Chat.instance.HasFocus() || TextInput.IsVisible();
    }
}
