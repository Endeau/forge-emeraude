using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace EmeraudeAdmin
{
    // Côté joueur : suit l'inventaire et l'envoie au serveur, applique les ordres des admins (tp, give, take…).
    static class Client
    {
        const float CheckEvery = 1f;
        const string KeepKey = "emeraude_admin.";

        // Multiplicateur de dégâts du joueur local (adm:damage), jusqu'à la fermeture du jeu.
        internal static float DamageFactor = 1f;
        static Player tracked;
        static Dictionary<string, int> last;
        static string lastSnapshot;
        static float nextCheck, nextSnapshot;

        // Clé d'un objet : nom du prefab, suivi de *qualité au-delà de 1 (ex. Bow*3)
        static string Key(ItemDrop.ItemData item)
        {
            string name = item.m_dropPrefab != null ? item.m_dropPrefab.name : item.m_shared.m_name;
            return item.m_quality > 1 ? $"{name}*{item.m_quality}" : name;
        }

        static Dictionary<string, int> Count(Player p)
        {
            var items = new Dictionary<string, int>();
            foreach (var item in p.GetInventory().GetAllItems())
            {
                string k = Key(item);
                items.TryGetValue(k, out int n);
                items[k] = n + item.m_stack;
            }
            return items;
        }

        // À l'apparition du joueur local : remet les modes dieu et fantôme activés par un admin.
        internal static void Restore(Player p)
        {
            var on = new List<string>();
            if (p.m_customData.ContainsKey(KeepKey + "god")) { p.SetGodMode(true); on.Add(L.T("God mode", "Mode dieu")); }
            if (p.m_customData.ContainsKey(KeepKey + "ghost")) { p.SetGhostMode(true); on.Add(L.T("Ghost mode", "Mode fantôme")); }
            if (on.Count > 0) p.Message(MessageHud.MessageType.Center, string.Join(", ", on) + L.T(": still on", " : toujours activé"));
        }

        static void Send(string kind, Player p, string data) =>
            ZRoutedRpc.instance.InvokeRoutedRPC(Net.Log, kind, p.transform.position, data);

        static float SnapshotEvery => Plugin.SnapshotEveryMinutes.Value * 60f;

        public static void Tick()
        {
            var p = Player.m_localPlayer;
            if (p == null) { tracked = null; return; }
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + CheckEvery;

            var now = Count(p);
            if (p != tracked)
            {
                // Nouveau personnage en jeu (connexion ou réapparition après une mort)
                tracked = p;
                last = now;
                lastSnapshot = Net.Encode(now);
                nextSnapshot = Time.time + SnapshotEvery;
                Send("apparition", p, lastSnapshot);
                return;
            }

            var delta = new Dictionary<string, int>();
            foreach (var k in now.Keys.Union(last.Keys))
            {
                now.TryGetValue(k, out int a);
                last.TryGetValue(k, out int b);
                if (a != b) delta[k] = a - b;
            }
            if (delta.Count > 0) Send("delta", p, Net.Encode(delta));
            last = now;

            if (Time.time >= nextSnapshot)
            {
                nextSnapshot = Time.time + SnapshotEvery;
                string snap = Net.Encode(now);
                if (snap != lastSnapshot) Send("inventaire", p, snap);
                lastSnapshot = snap;
            }
        }

        public static void OnSaved(Player p)
        {
            if (ZRoutedRpc.instance == null || ZNet.instance == null || ZNet.instance.IsDedicated()) return;
            var now = Count(p);
            last = now;
            lastSnapshot = Net.Encode(now);
            Send("sauvegarde", p, lastSnapshot);
        }

        public static void OnDeath(Player p)
        {
            if (ZRoutedRpc.instance != null) Send("mort", p, Net.Encode(Count(p)));
        }

        public static void OnReply(long sender, string text)
        {
            if (!Net.FromServer(sender)) return;
            Console.instance?.Print($"[{Plugin.Name}] {text}");
        }

        public static void OnTeleport(long sender, Vector3 pos, bool exact)
        {
            var p = Player.m_localPlayer;
            if (!Net.FromServer(sender) || p == null) return;
            p.TeleportTo(pos, p.transform.rotation, distantTeleport: true);
        }

        public static void OnGive(long sender, string prefab, int amount, int quality)
        {
            var p = Player.m_localPlayer;
            if (!Net.FromServer(sender) || p == null) return;
            var go = ObjectDB.instance.GetItemPrefab(prefab);
            var drop = go != null ? go.GetComponent<ItemDrop>() : null;
            if (drop == null) return;
            int max = Math.Max(1, drop.m_itemData.m_shared.m_maxStackSize);
            var inv = p.GetInventory();
            for (int left = amount; left > 0; left -= max)
            {
                int n = Math.Min(left, max);
                // Objet normal (pas marqué « triche ») ; tombe aux pieds si l'inventaire est plein
                if (inv.AddItem(prefab, n, quality, 0, 0L, "", false) == null)
                {
                    var data = drop.m_itemData.Clone();
                    data.m_quality = quality;
                    data.m_dropPrefab = go;
                    ItemDrop.DropItem(data, n, p.transform.position + p.transform.forward + Vector3.up, Quaternion.identity);
                }
            }
            p.Message(MessageHud.MessageType.Center, L.T($"Received: {amount} {prefab}", $"Reçu : {amount} {prefab}"));
        }

        // Effets négatifs retirés par adm:heal
        static readonly string[] BadEffects = { "Burning", "Poison", "Frost", "Lightning", "Wet", "Cold", "Freezing", "Smoked", "Tared" };

        // Objets portés : avec Equipment and Quick Slots, ils ne sont plus dans l'inventaire principal.
        static readonly string[] Worn = { "m_rightItem", "m_leftItem", "m_chestItem", "m_legItem", "m_ammoItem", "m_helmetItem", "m_shoulderItem", "m_utilityItem", "m_trinketItem" };

        public static void OnDo(long sender, string action, string arg)
        {
            var p = Player.m_localPlayer;
            if (!Net.FromServer(sender) || p == null) return;
            var args = (arg ?? "").Split(' ');
            string message;
            switch (action)
            {
                case "take":
                {
                    var go = ObjectDB.instance.GetItemPrefab(args[0]);
                    var drop = go != null ? go.GetComponent<ItemDrop>() : null;
                    if (drop == null || args.Length < 2 || !int.TryParse(args[1], out int amount)) return;
                    var inv = p.GetInventory();
                    string name = drop.m_itemData.m_shared.m_name;
                    int taken = Math.Min(amount, inv.CountItems(name));
                    if (taken > 0) inv.RemoveItem(name, taken);
                    message = L.T($"An admin took: {taken} {args[0]}", $"Retiré par un admin : {taken} {args[0]}");
                    break;
                }
                case "skill":
                {
                    if (args.Length < 2 || !float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float level)) return;
                    var skills = p.GetSkills();
                    skills.CheatResetSkill(args[0]);
                    if (level > 0f) skills.CheatRaiseSkill(args[0], level, false);
                    message = L.T($"Skill {args[0]}: {level}", $"Compétence {args[0]} : {level}");
                    break;
                }
                case "heal":
                    p.Heal(p.GetMaxHealth(), false);
                    p.AddStamina(p.GetMaxStamina());
                    p.AddEitr(p.GetMaxEitr());
                    foreach (var effect in BadEffects) p.GetSEMan().RemoveStatusEffect(effect.GetStableHashCode(), true);
                    message = L.T("Healed by an admin", "Soigné par un admin");
                    break;
                case "repair":
                {
                    var items = p.GetInventory().GetAllItems().ToList();
                    var t = Traverse.Create(p);
                    items.AddRange(Worn.Select(f => t.Field(f).GetValue<ItemDrop.ItemData>()).Where(i => i != null));
                    foreach (var item in items.Distinct().Where(i => i.m_shared.m_useDurability))
                        item.m_durability = item.GetMaxDurability();
                    message = L.T("Items repaired by an admin", "Objets réparés par un admin");
                    break;
                }
                case "damage":
                    if (!float.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out float factor) || factor <= 0f) return;
                    DamageFactor = factor;
                    message = L.T($"Damage x{arg}", $"Dégâts x{arg}");
                    break;
                case "god":
                case "ghost":
                case "fly":
                {
                    bool now = action == "god" ? p.InGodMode() : action == "ghost" ? p.InGhostMode() : p.IsDebugFlying();
                    bool want = arg == "on" || (arg != "off" && !now);
                    if (action == "god") p.SetGodMode(want);
                    else if (action == "ghost") p.SetGhostMode(want);
                    else if (want != now) p.ToggleDebugFly();
                    // Dieu et fantôme : gardés dans le personnage, remis à chaque apparition (mort, reconnexion).
                    if (action != "fly")
                    {
                        if (want) p.m_customData[KeepKey + action] = "on";
                        else p.m_customData.Remove(KeepKey + action);
                    }
                    string mode = action == "god" ? L.T("God mode", "Mode dieu") : action == "ghost" ? L.T("Ghost mode", "Mode fantôme") : L.T("Fly mode", "Mode vol");
                    message = mode + (want ? L.T(": on", " : activé") : L.T(": off", " : désactivé"));
                    break;
                }
                default:
                    return;
            }
            p.Message(MessageHud.MessageType.Center, message);
        }
    }
}
