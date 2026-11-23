using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace EmeraudeMinerais
{
    // Barres de métal : ce qu'on compte en sortie de four et à la fabrication.
    static class Bars
    {
        static readonly HashSet<string> prefabs = new HashSet<string>
        {
            "Copper", "Tin", "Bronze", "Iron", "Silver", "BlackMetal", "Flametal", "FlametalNew",
        };

        public static bool IsBar(string prefab) => prefabs.Contains(prefab);

        // Minerais et ferrailles qu'un four transforme en barre.
        static readonly HashSet<string> ores = new HashSet<string>
        {
            "CopperOre", "CopperScrap", "TinOre", "BronzeScrap", "IronOre", "IronScrap", "SilverOre",
            "BlackMetalScrap", "FlametalOre", "FlametalOreNew",
        };

        public static bool IsOre(string prefab) => ores.Contains(prefab);

        // Barre produite par ce four à partir de ce minerai, ou null (four à charbon, rouet, moulin…).
        public static string FromOre(Smelter smelter, string ore)
        {
            var conversion = smelter.m_conversion.FirstOrDefault(c => c.m_from && c.m_from.gameObject.name == ore);
            string bar = conversion != null && conversion.m_to ? conversion.m_to.gameObject.name : null;
            return bar != null && IsBar(bar) ? bar : null;
        }
    }

    // Fours, chez le propriétaire du four (le seul qui modifie sa file d'attente).
    // À côté de chaque minerai en attente "item<i>", le ZDO garde "emeraude_by<i>" = celui qui l'a déposé,
    // décalé comme la file à chaque barre produite.
    static class SmelterPatches
    {
        const string ByKey = "emeraude_by";
        static readonly AccessTools.FieldRef<Smelter, ZNetView> View = AccessTools.FieldRefAccess<Smelter, ZNetView>("m_nview");

        static string adder; // joueur dont on traite le dépôt (RPC_AddOre)
        static string popped; // déposant du minerai qui vient de sortir de la file (RemoveOneOre)

        [HarmonyPatch(typeof(Smelter), "RPC_AddOre")]
        static class AddOrePatch
        {
            static void Prefix(Smelter __instance, long sender, bool cheated)
            {
                adder = null;
                if (cheated || !View(__instance).IsOwner()) return;
                // Le personnage d'un joueur appartient toujours à sa machine : son propriétaire est l'expéditeur.
                var player = Player.GetAllPlayers().FirstOrDefault(p => p.GetComponent<ZNetView>()?.GetZDO()?.GetOwner() == sender);
                if (player) adder = Miner.Of(player).ToString();
            }

            static void Finalizer() => adder = null;
        }

        [HarmonyPatch(typeof(Smelter), "QueueOre")]
        static class QueueOrePatch
        {
            static void Prefix(Smelter __instance, string name)
            {
                var zdo = View(__instance).GetZDO();
                string by = adder != null && Bars.FromOre(__instance, name) != null ? adder : "";
                zdo.Set(ByKey + zdo.GetInt(ZDOVars.s_queued), by);
                if (Miner.TryParse(by, out var miner)) Stats.Report(miner, Stat.Ore, name, 1);
            }
        }

        [HarmonyPatch(typeof(Smelter), "RemoveOneOre")]
        static class RemoveOneOrePatch
        {
            static void Prefix(Smelter __instance)
            {
                var zdo = View(__instance).GetZDO();
                int size = zdo.GetInt(ZDOVars.s_queued);
                popped = size > 0 ? zdo.GetString(ByKey + 0) : null;
                for (int i = 0; i < size; i++)
                    zdo.Set(ByKey + i, zdo.GetString(ByKey + (i + 1)));
            }
        }

        // Toujours appelé juste après RemoveOneOre, quand le four a fini une barre.
        [HarmonyPatch(typeof(Smelter), "QueueProcessed")]
        static class QueueProcessedPatch
        {
            static void Prefix(Smelter __instance, string ore)
            {
                string bar = Bars.FromOre(__instance, ore);
                if (bar != null && Miner.TryParse(popped, out var miner)) Stats.Report(miner, Stat.Produced, bar, 1);
                popped = null;
            }
        }
    }

    // Barres dépensées par le joueur local, pour fabriquer, améliorer ou construire (y compris celles que
    // Craft From Containers prend dans les coffres). Le jeu n'appelle pas ConsumeResources en mode sans coût.
    [HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
    static class ConsumeResourcesPatch
    {
        [HarmonyPriority(Priority.First)]
        static void Prefix(Player __instance, Piece.Requirement[] requirements, int qualityLevel, int multiplier)
        {
            if (__instance != Player.m_localPlayer || requirements == null) return;
            // Mêmes ressources que celles que le jeu retire (cf. Player.ConsumeResources).
            var station = __instance.GetCurrentCraftingStation();
            var used = new Dictionary<string, int>();
            foreach (var requirement in requirements)
            {
                if (!requirement.m_resItem || !Bars.IsBar(requirement.m_resItem.gameObject.name)) continue;
                if (station ? station.m_upgrader != requirement.m_upgraderResource : requirement.m_upgraderResource) continue;
                string bar = requirement.m_resItem.gameObject.name;
                used.TryGetValue(bar, out int total);
                used[bar] = total + requirement.GetAmount(qualityLevel) * multiplier;
            }
            var miner = Miner.Of(__instance);
            foreach (var kv in used) Stats.Report(miner, Stat.Used, kv.Key, kv.Value);
        }
    }

    // Minerai récolté : ce que le joueur local ramasse au sol. Le minage fait apparaître le minerai par
    // DropTable (Instantiate) ; ItemDrop.DropItem sert aux objets lâchés par un joueur et aux coffres ou
    // bateaux détruits : ceux-là sont marqués dans leur ZDO et ne comptent pas.
    static class Harvest
    {
        internal static readonly int DroppedKey = "emeraude_dropped".GetStableHashCode();

        [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.DropItem))]
        static class DropItemPatch
        {
            static void Postfix(ItemDrop __result)
            {
                try
                {
                    var zdo = __result ? __result.GetComponent<ZNetView>()?.GetZDO() : null;
                    if (zdo != null && Bars.IsOre(Utils.GetPrefabName(__result.gameObject))) zdo.Set(DroppedKey, true);
                }
                catch (System.Exception e) { Debug.LogWarning($"[{Plugin.Name}] DropItem: {e}"); }
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Pickup))]
        static class PickupPatch
        {
            // Lu avant : une fois ramassé, l'objet est détruit et sa pile a pu être fusionnée dans l'inventaire.
            static void Prefix(Humanoid __instance, GameObject go, out (string prefab, int stack) __state)
            {
                __state = (null, 0);
                // Ne doit jamais empêcher le ramassage : toute erreur est seulement journalisée.
                try
                {
                    if (__instance != Player.m_localPlayer || go == null) return;
                    string prefab = Utils.GetPrefabName(go);
                    var drop = go.GetComponent<ItemDrop>();
                    var zdo = go.GetComponent<ZNetView>()?.GetZDO();
                    if (!Bars.IsOre(prefab) || drop == null || zdo == null || zdo.GetBool(DroppedKey)) return;
                    __state = (prefab, zdo.GetInt(ZDOVars.s_stack, drop.m_itemData.m_stack));
                }
                catch (System.Exception e) { Debug.LogWarning($"[{Plugin.Name}] Pickup: {e}"); }
            }

            static void Postfix(Humanoid __instance, bool __result, (string prefab, int stack) __state)
            {
                try
                {
                    if (__result && __state.prefab != null) Stats.Report(Miner.Of((Player)__instance), Stat.Mined, __state.prefab, __state.stack);
                }
                catch (System.Exception e) { Debug.LogWarning($"[{Plugin.Name}] Pickup: {e}"); }
            }
        }
    }
}
