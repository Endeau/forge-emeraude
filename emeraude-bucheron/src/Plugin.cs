// Emeraude Bûcheron — un grand arbre abattu donne toujours de la résine (1 à 3) et ses graines.
//
// En vanilla, l'arbre qui tombe (TreeBase.RPC_Damage, chez le propriétaire de l'arbre) tire son butin dans
// m_dropWhenDestroyed : 50 % de chance de 1 à 3 objets parmi graines, plumes et résine. Un bouleau donne
// ainsi moins d'une graine en moyenne. On complète la liste tirée pour cet arbre-là : au moins 1 à 3 résines
// si l'arbre peut en donner, et ses graines à leur quantité vanilla si le tirage n'en a pas sorti.
// Les petits arbres et les troncs (Destructible, TreeLog) ne sont pas touchés.
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using HarmonyLib;
using Jotunn.Utils;
using UnityEngine;

namespace EmeraudeBucheron
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "emeraude.bucheron";
        public const string Name = "Emeraude Bucheron";
        public const string Version = "1.0.0";

        internal static BepInEx.Logging.ManualLogSource Log;

        void Awake()
        {
            Log = Logger;
            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo($"{Name} {Version} loaded");
        }
    }

    static class TreeDrops
    {
        const string ResinPrefab = "Resin";
        const int ResinMin = 1;
        const int ResinMax = 3;
        static readonly HashSet<string> SeedPrefabs = new HashSet<string> { "BeechSeeds", "BirchSeeds", "Acorn", "FirCone", "PineCone" };

        // L'arbre en train de traiter un coup, pour reconnaître son tirage dans DropTable.GetDropList.
        internal static TreeBase Felling;

        internal static void Complete(DropTable table, List<GameObject> drops)
        {
            var resin = table.m_drops.FirstOrDefault(d => IsResin(d.m_item));
            if (resin.m_item)
            {
                int missing = Random.Range(ResinMin, ResinMax + 1) - drops.Count(IsResin);
                for (int i = 0; i < missing; i++) drops.Add(resin.m_item);
            }
            foreach (var seed in table.m_drops.Where(d => IsSeed(d.m_item)))
            {
                if (drops.Any(d => d.name == seed.m_item.name)) continue;
                int count = Random.Range(Mathf.Max(1, seed.m_stackMin), Mathf.Max(1, seed.m_stackMax) + 1);
                for (int i = 0; i < count; i++) drops.Add(seed.m_item);
            }
        }

        internal static bool IsResin(GameObject item) => item && item.name == ResinPrefab;
        internal static bool IsSeed(GameObject item) => item && SeedPrefabs.Contains(item.name);
    }

    [HarmonyPatch(typeof(TreeBase), "RPC_Damage")]
    static class TreeBaseDamagePatch
    {
        static void Prefix(TreeBase __instance) => TreeDrops.Felling = __instance;

        static System.Exception Finalizer(System.Exception __exception)
        {
            TreeDrops.Felling = null;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(DropTable), nameof(DropTable.GetDropList), new System.Type[0])]
    static class DropListPatch
    {
        static void Postfix(DropTable __instance, List<GameObject> __result)
        {
            var tree = TreeDrops.Felling;
            if (!tree || __instance != tree.m_dropWhenDestroyed) return;
            TreeDrops.Complete(__instance, __result);
        }
    }

    // Au démarrage, liste dans le journal les arbres concernés, pour vérifier les noms des graines.
    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    static class TreeListPatch
    {
        static void Postfix(ZNetScene __instance)
        {
            var lines = __instance.m_prefabs
                .Select(p => p ? p.GetComponent<TreeBase>() : null)
                .Where(t => t)
                .Select(t => $"{t.name} [{string.Join(", ", t.m_dropWhenDestroyed.m_drops.Select(d => d.m_item ? d.m_item.name : "?"))}]"
                    + (t.m_dropWhenDestroyed.m_drops.Any(d => TreeDrops.IsResin(d.m_item)) ? " résine" : "")
                    + (t.m_dropWhenDestroyed.m_drops.Any(d => TreeDrops.IsSeed(d.m_item)) ? " graines" : ""));
            Plugin.Log.LogInfo("Arbres : " + string.Join(" ; ", lines));
        }
    }
}
