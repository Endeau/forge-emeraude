// La décision : ce monstre ignore-t-il ce joueur ?
//
// L'armure se lit dans le ZDO du joueur (casque, plastron, jambières : le hachage du nom de l'objet,
// écrit par VisEquipment). Elle est donc connue sur toutes les machines, pas seulement chez le joueur.
//
// Un joueur qui frappe un monstre le provoque : le monstre le combat jusqu'à ForgetSeconds sans coup
// échangé. L'heure du dernier coup est écrite dans le ZDO du monstre, une clé par joueur : elle survit
// aux changements de propriétaire, fréquents avec NetworkPerformanceSystem. Les coups sont notés chez
// le propriétaire du monstre (là où tournent son IA et ses attaques), seul à pouvoir écrire son ZDO.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EmeraudePrestige
{
    static class Prestige
    {
        static readonly Dictionary<int, int> s_armorTiers = new Dictionary<int, int>();
        static readonly Dictionary<int, int> s_monsterTiers = new Dictionary<int, int>();

        internal static void Rebuild()
        {
            Fill(s_armorTiers, Plugin.ArmorTiers);
            Fill(s_monsterTiers, Plugin.MonsterTiers);
        }

        static void Fill(Dictionary<int, int> tiers, BepInEx.Configuration.ConfigEntry<string>[] entries)
        {
            tiers.Clear();
            for (int i = 0; i < entries.Length; i++)
                foreach (var name in Names(entries[i].Value))
                    tiers[name.GetStableHashCode()] = i + 1;
        }

        static IEnumerable<string> Names(string list) =>
            list.Split(',').Select(name => name.Trim()).Where(name => name.Length > 0);

        internal static bool Ignores(MonsterAI ai, Character monster, Character target)
        {
            if (!Plugin.Enabled.Value || !(target is Player player)) return false;
            if (monster.IsBoss() || monster.IsTamed()) return false;
            if (Plugin.StarredStayAggressive.Value && monster.GetLevel() > 1) return false;
            if (Plugin.RaidsStayAggressive.Value && ai.IsEventCreature()) return false;
            var monsterZdo = ZDOMan.instance.GetZDO(monster.GetZDOID());
            if (monsterZdo == null || !s_monsterTiers.TryGetValue(monsterZdo.GetPrefab(), out int monsterTier)) return false;
            if (IsProvoked(monsterZdo, player)) return false;
            return PlayerTier(player) >= monsterTier + Plugin.Gap.Value;
        }

        internal static int PlayerTier(Player player)
        {
            var zdo = ZDOMan.instance.GetZDO(player.GetZDOID());
            if (zdo == null) return 0;
            return Mathf.Min(ArmorTier(zdo.GetInt(ZDOVars.s_helmetItem)),
                Mathf.Min(ArmorTier(zdo.GetInt(ZDOVars.s_chestItem)), ArmorTier(zdo.GetInt(ZDOVars.s_legItem))));
        }

        static int ArmorTier(int itemHash) => s_armorTiers.TryGetValue(itemHash, out int tier) ? tier : 0;

        internal static void Provoke(Character monster, Character attacker)
        {
            if (!(attacker is Player) || !monster) return;
            var zdo = ZDOMan.instance.GetZDO(monster.GetZDOID());
            if (zdo != null && zdo.IsOwner()) zdo.Set(ProvokedKey(attacker), ZNet.instance.GetTime().Ticks);
        }

        static bool IsProvoked(ZDO monsterZdo, Player player)
        {
            long ticks = monsterZdo.GetLong(ProvokedKey(player), 0L);
            return ticks != 0L &&
                (ZNet.instance.GetTime() - new DateTime(ticks)).TotalSeconds < Plugin.ForgetSeconds.Value;
        }

        // UserID : l'identifiant réseau du joueur, le même pour toutes les machines pendant sa session.
        static int ProvokedKey(Character player) => ("emeraude_prestige_provoked_" + player.GetZDOID().UserID).GetStableHashCode();

        // Au chargement, signale dans le journal les noms des listes qui n'existent pas dans le jeu
        // (faute de frappe) et les armures du jeu qui n'ont pas de niveau.
        internal static void CheckArmorNames()
        {
            Jotunn.Managers.ItemManager.OnItemsRegistered -= CheckArmorNames;
            var known = new HashSet<string>(ObjectDB.instance.m_items.Where(item => item).Select(item => item.name));
            Warn("Armor", Plugin.ArmorTiers.SelectMany(entry => Names(entry.Value)).Where(name => !known.Contains(name)));
            var unranked = ObjectDB.instance.m_items
                .Select(item => item ? item.GetComponent<ItemDrop>() : null)
                .Where(drop => drop && IsArmor(drop.m_itemData.m_shared) && !s_armorTiers.ContainsKey(drop.name.GetStableHashCode()))
                .Select(drop => drop.name).ToList();
            if (unranked.Count > 0)
                Plugin.Log.LogInfo($"Pièces d'armure sans niveau (valent 0) : {string.Join(", ", unranked)}");
        }

        static bool IsArmor(ItemDrop.ItemData.SharedData shared) => shared.m_armor > 0f &&
            (shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet || shared.m_itemType == ItemDrop.ItemData.ItemType.Chest ||
             shared.m_itemType == ItemDrop.ItemData.ItemType.Legs);

        internal static void CheckMonsterNames()
        {
            Jotunn.Managers.PrefabManager.OnPrefabsRegistered -= CheckMonsterNames;
            var known = new HashSet<string>(ZNetScene.instance.m_prefabs.Where(prefab => prefab).Select(prefab => prefab.name));
            Warn("Monsters", Plugin.MonsterTiers.SelectMany(entry => Names(entry.Value)).Where(name => !known.Contains(name)));
            var unranked = ZNetScene.instance.m_prefabs
                .Where(prefab => prefab && prefab.GetComponent<MonsterAI>() && prefab.GetComponent<Character>() &&
                    !s_monsterTiers.ContainsKey(prefab.name.GetStableHashCode()))
                .Select(prefab => prefab.name).ToList();
            if (unranked.Count > 0)
                Plugin.Log.LogInfo($"Créatures sans niveau (attaquent toujours) : {string.Join(", ", unranked)}");
        }

        static void Warn(string section, IEnumerable<string> unknown)
        {
            var names = unknown.ToList();
            if (names.Count > 0)
                Plugin.Log.LogWarning($"[{section}] noms inconnus du jeu, ignorés : {string.Join(", ", names)}");
        }
    }
}
