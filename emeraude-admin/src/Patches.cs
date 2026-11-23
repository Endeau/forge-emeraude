using HarmonyLib;

namespace EmeraudeAdmin
{
    [HarmonyPatch(typeof(Game), "Start")]
    static class GameStartPatch
    {
        static void Postfix() => Net.Register();
    }

    [HarmonyPatch(typeof(Terminal), "InitTerminal")]
    static class TerminalPatch
    {
        static void Postfix() => Commands.Register();
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect))]
    static class DisconnectPatch
    {
        static void Prefix(ZNetPeer peer)
        {
            if (ZNet.instance != null && ZNet.instance.IsServer()) Server.OnDisconnect(peer);
        }
    }

    // Appelé à chaque sauvegarde du personnage : sauvegarde du monde (le serveur la demande
    // à tous les joueurs), déconnexion, ou minuterie du jeu.
    [HarmonyPatch(typeof(Game), nameof(Game.SavePlayerProfile))]
    static class SaveProfilePatch
    {
        // m_saveTimer n'est remis à zéro que si la sauvegarde a vraiment eu lieu
        static void Postfix(Game __instance)
        {
            if (__instance.m_saveTimer == 0f && Player.m_localPlayer != null) Client.OnSaved(Player.m_localPlayer);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
    static class DeathPatch
    {
        static void Prefix(Player __instance)
        {
            if (__instance == Player.m_localPlayer) Client.OnDeath(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    static class SpawnedPatch
    {
        static void Postfix(Player __instance)
        {
            if (__instance == Player.m_localPlayer) Client.Restore(__instance);
        }
    }

    // adm:damage : chaque coup du joueur local (mêlée, zone, projectile) passe par SEMan.ModifyAttack.
    [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyAttack))]
    static class DamagePatch
    {
        static readonly AccessTools.FieldRef<SEMan, Character> Owner = AccessTools.FieldRefAccess<SEMan, Character>("m_character");

        static void Postfix(SEMan __instance, ref HitData hitData)
        {
            if (Client.DamageFactor != 1f && Owner(__instance) == Player.m_localPlayer) hitData.ApplyModifier(Client.DamageFactor);
        }
    }
}
