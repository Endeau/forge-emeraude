using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EmeraudeAdmin
{
    // Côté joueur : suit l'inventaire et l'envoie au serveur, applique tp et give.
    static class Client
    {
        const float CheckEvery = 1f;
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
            p.Message(MessageHud.MessageType.Center, $"Reçu : {amount} {prefab}");
        }
    }
}
