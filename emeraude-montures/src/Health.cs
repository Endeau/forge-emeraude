using HarmonyLib;
using UnityEngine;

namespace EmeraudeMontures
{
    // Le cavalier devient propriétaire de la monture quand il la prend : c'est donc lui qui écrit.
    [HarmonyPatch(typeof(Sadle), "FixedUpdate")]
    static class SadleFixedUpdatePatch
    {
        static void Postfix(Character ___m_character, ZNetView ___m_nview)
        {
            var rider = Player.m_localPlayer;
            if (rider == null || !___m_nview.IsValid() || !___m_nview.IsOwner()) return;
            if (___m_nview.GetZDO().GetLong(ZDOVars.s_user) != rider.GetZDOID().UserID) return;
            if (Health.IsMount(___m_character)) Health.MatchRider(___m_character, rider);
        }
    }

    // Au chargement d'une bête en pleine santé, ou à un changement de niveau, le jeu remet ses PV max
    // d'origine.
    [HarmonyPatch(typeof(Character), "SetupMaxHealth")]
    static class SetupMaxHealthPatch
    {
        static void Postfix(Character __instance)
        {
            if (!Health.IsMount(__instance)) return;
            float stored = __instance.GetComponent<ZNetView>().GetZDO().GetFloat(Health.MaxHealthKey);
            if (stored > __instance.GetMaxHealth()) __instance.SetMaxHealth(stored);
        }
    }

    static class Health
    {
        internal static readonly int MaxHealthKey = "emeraude_mount_maxhealth".GetStableHashCode();

        internal static bool IsMount(Character character) =>
            character != null && character.IsTamed() && Mounts.SharesRiderHealth(character.gameObject);

        internal static void MatchRider(Character mount, Player rider)
        {
            float own = mount.GetMaxHealthBase() * mount.GetLevel();
            float target = Mathf.Max(own, rider.GetMaxHealth());
            float current = mount.GetMaxHealth();
            if (Mathf.Abs(current - target) < 1f) return;

            float ratio = mount.GetHealth() / current;
            mount.SetMaxHealth(target);
            mount.SetHealth(target * ratio);
            mount.GetComponent<ZNetView>().GetZDO().Set(MaxHealthKey, target);
        }
    }
}
