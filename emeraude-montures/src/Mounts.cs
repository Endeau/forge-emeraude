using HarmonyLib;
using UnityEngine;

namespace EmeraudeMontures
{
    // Le jeu de base permet aussi de monter l'asksvin : on refuse de lui poser une selle et de monter
    // dessus. Retirer une selle déjà posée reste possible.
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.UseItem))]
    static class UseSaddlePatch
    {
        static bool Prefix(Tameable __instance, Humanoid user, ItemDrop.ItemData item, ref bool __result)
        {
            var saddle = __instance.m_saddleItem;
            if (saddle == null || item.m_shared.m_name != saddle.m_itemData.m_shared.m_name) return true;
            if (Mounts.IsRideable(__instance.gameObject)) return true;
            user.Message(MessageHud.MessageType.Center, Mounts.Refused);
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(Sadle), nameof(Sadle.Interact))]
    static class RidePatch
    {
        static bool Prefix(Sadle __instance, Humanoid character, bool repeat, bool alt, ref bool __result)
        {
            if (repeat || alt || Mounts.IsRideable(__instance.GetCharacter().gameObject)) return true;
            character.Message(MessageHud.MessageType.Center, Mounts.Refused);
            __result = false;
            return false;
        }
    }

    static class Mounts
    {
        internal const string Refused = "Seuls le loup, le sanglier et le lox peuvent être montés";

        internal static bool IsRideable(GameObject creature)
        {
            string prefab = Utils.GetPrefabName(creature);
            return prefab == "Wolf" || prefab == "Boar" || prefab == "Lox";
        }

        // Montures qui prennent les PV de leur cavalier : pas le lox, assez solide tout seul.
        internal static bool SharesRiderHealth(GameObject creature)
        {
            string prefab = Utils.GetPrefabName(creature);
            return prefab == "Wolf" || prefab == "Boar";
        }
    }
}
