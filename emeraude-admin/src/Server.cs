using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using UnityEngine;

namespace EmeraudeAdmin
{
    // Côté serveur : journal des joueurs et exécution des commandes admin.
    //
    // Fichiers dans BepInEx/config/journal/ :
    //   <Joueur>_<SteamID>.log        depuis la dernière sauvegarde de son personnage
    //   <Joueur>_<SteamID>.1.log …    périodes précédentes (Journal.KeptSaves)
    //   admin.log                     toutes les commandes admin, acceptées ou refusées
    static class Server
    {
        static readonly Encoding Utf8 = new UTF8Encoding(false);
        static readonly HashSet<long> online = new HashSet<long>();
        static readonly Dictionary<long, Dictionary<string, int>> inventories = new Dictionary<long, Dictionary<string, int>>();
        static float nextPosition;
        static string dir;

        static string Dir
        {
            get
            {
                if (dir == null)
                {
                    dir = Path.Combine(Paths.ConfigPath, "journal");
                    Directory.CreateDirectory(dir);
                }
                return dir;
            }
        }

        // ---------------------------------------------------------------- fichiers

        static string SteamId(ZNetPeer peer) => peer?.m_socket?.GetHostName() ?? "hote";

        static string Clean(string s) => new string((s ?? "").Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_').ToArray());

        static string FileOf(ZNetPeer peer, string name) => $"{Clean(name)}_{Clean(SteamId(peer))}";

        static void Write(string file, string line)
        {
            try
            {
                File.AppendAllText(Path.Combine(Dir, file), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | " + line + "\n", Utf8);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{Plugin.Name}] cannot write the journal: {e.Message}");
            }
        }

        static void Journal(ZNetPeer peer, string name, Vector3 pos, string text) =>
            Write(FileOf(peer, name) + ".log", $"{Net.Pos(pos)} | {text}");

        // <f>.log -> <f>.1.log -> <f>.2.log …, la plus ancienne est supprimée
        static void Rotate(string file)
        {
            try
            {
                int kept = Plugin.KeptSaves.Value;
                string PathOf(int i) => Path.Combine(Dir, i == 0 ? file + ".log" : $"{file}.{i}.log");
                File.Delete(PathOf(kept));
                for (int i = kept - 1; i >= 0; i--)
                    if (File.Exists(PathOf(i))) File.Move(PathOf(i), PathOf(i + 1));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{Plugin.Name}] cannot archive the journal: {e.Message}");
            }
        }

        static string Describe(Dictionary<string, int> inv) =>
            inv.Count == 0 ? L.T("empty", "vide") : string.Join(", ", inv.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key} ×{kv.Value}"));

        static string DescribeDelta(Dictionary<string, int> delta) =>
            string.Join(", ", delta.OrderBy(kv => kv.Key).Select(kv => (kv.Value > 0 ? "+" : "") + kv.Value + " " + kv.Key));

        // ---------------------------------------------------------------- journal

        static IEnumerable<ZNetPeer> InGame() =>
            ZNet.instance.GetPeers().Where(p => p.IsReady() && !p.m_characterID.IsNone() && !string.IsNullOrEmpty(p.m_playerName));

        public static void Tick()
        {
            foreach (var peer in InGame())
                if (online.Add(peer.m_uid))
                    Journal(peer, peer.m_playerName, peer.m_refPos, L.T("connected", "connexion"));

            if (Time.time < nextPosition) return;
            nextPosition = Time.time + Plugin.PositionEverySeconds.Value;
            foreach (var peer in InGame())
                Journal(peer, peer.m_playerName, peer.m_refPos, L.T("position", "position"));
        }

        public static void OnDisconnect(ZNetPeer peer)
        {
            if (peer == null || !online.Remove(peer.m_uid)) return;
            Journal(peer, peer.m_playerName, peer.m_refPos, L.T("disconnected", "déconnexion"));
            if (inventories.TryGetValue(peer.m_uid, out var inv))
                Journal(peer, peer.m_playerName, peer.m_refPos, L.T("inventory when leaving: ", "inventaire au départ : ") + Describe(inv));
            inventories.Remove(peer.m_uid);
        }

        public static void OnLog(long sender, string kind, Vector3 pos, string data)
        {
            if (!ZNet.instance.IsServer()) return;
            var peer = ZNet.instance.GetPeer(sender);
            string name = peer?.m_playerName ?? Game.instance?.GetPlayerProfile()?.GetName() ?? "?";
            var items = Net.Decode(data);
            if (!inventories.TryGetValue(sender, out var inv)) inventories[sender] = inv = new Dictionary<string, int>();

            switch (kind)
            {
                case "delta":
                    foreach (var kv in items)
                    {
                        inv.TryGetValue(kv.Key, out int n);
                        if (n + kv.Value == 0) inv.Remove(kv.Key); else inv[kv.Key] = n + kv.Value;
                    }
                    Journal(peer, name, pos, DescribeDelta(items));
                    break;
                case "sauvegarde":
                    inventories[sender] = items;
                    Journal(peer, name, pos, L.T("character saved, inventory: ", "personnage sauvegardé, inventaire : ") + Describe(items));
                    Rotate(FileOf(peer, name));
                    break;
                case "mort":
                    Journal(peer, name, pos, L.T("DIED, inventory left in the tombstone: ", "MORT, inventaire laissé dans la tombe : ") + Describe(items));
                    break;
                default: // apparition, inventaire
                    inventories[sender] = items;
                    string label = kind == "apparition" ? L.T("spawned with", "apparition") : L.T("inventory", "inventaire");
                    Journal(peer, name, pos, label + L.T(": ", " : ") + Describe(items));
                    break;
            }
        }

        // ---------------------------------------------------------------- commandes admin

        static void Reply(long to, string text) => ZRoutedRpc.instance.InvokeRoutedRPC(to, Net.Reply, text);

        static ZNetPeer Find(string name, out string error)
        {
            var peers = InGame().ToList();
            var exact = peers.Where(p => p.m_playerName.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
            var found = exact.Count > 0 ? exact : peers.Where(p => p.m_playerName.StartsWith(name, StringComparison.OrdinalIgnoreCase)).ToList();
            error = found.Count == 1 ? null
                : found.Count == 0 ? L.T($"Player \"{name}\" not found. Online: ", $"Joueur « {name} » introuvable. En ligne : ") + string.Join(", ", peers.Select(p => p.m_playerName))
                : L.T($"\"{name}\" is ambiguous: ", $"« {name} » est ambigu : ") + string.Join(", ", found.Select(p => p.m_playerName));
            return found.Count == 1 ? found[0] : null;
        }

        static GameObject FindItem(string name) =>
            ObjectDB.instance?.m_items.FirstOrDefault(i => i != null && i.name.Equals(name, StringComparison.OrdinalIgnoreCase) && i.GetComponent<ItemDrop>() != null);

        public static void OnAdmin(long sender, string line)
        {
            if (!ZNet.instance.IsServer()) return;
            var peer = ZNet.instance.GetPeer(sender);
            bool local = sender == ZNet.GetUID();
            string admin = local ? "hôte" : peer?.m_playerName ?? sender.ToString();
            if (!local && (peer == null || !ZNet.instance.IsAdmin(SteamId(peer))))
            {
                Write("admin.log", L.T("REFUSED ", "REFUSÉ ") + $"{admin} ({SteamId(peer)}) : {line}");
                Reply(sender, L.T("You are not an admin of this server (adminlist.txt).", "Tu n'es pas admin du serveur (adminlist.txt)."));
                return;
            }
            Write("admin.log", $"{admin} : {line}");
            Reply(sender, Run(line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), admin));
        }

        static string Run(string[] a, string admin)
        {
            string error;
            switch (Commands.Canonical(a.Length > 0 ? a[0] : ""))
            {
                case "players":
                {
                    var peers = InGame().ToList();
                    return peers.Count == 0 ? L.T("Nobody in game.", "Personne en jeu.")
                        : string.Join("\n", peers.Select(p => $"{p.m_playerName} {Net.Pos(p.m_refPos)}"));
                }
                case "pos":
                {
                    if (a.Length < 2) return L.T("Usage: adm:pos <player>", "Usage : adm:pos <joueur>");
                    var p = Find(a[1], out error);
                    return p == null ? error : p.m_playerName + L.T(" is at ", " est en ") + Net.Pos(p.m_refPos);
                }
                case "inv":
                {
                    if (a.Length < 2) return L.T("Usage: adm:inv <player>", "Usage : adm:inv <joueur>");
                    var p = Find(a[1], out error);
                    if (p == null) return error;
                    return inventories.TryGetValue(p.m_uid, out var inv)
                        ? p.m_playerName + L.T(": ", " : ") + Describe(inv)
                        : p.m_playerName + L.T(": inventory unknown (Emeraude Admin not installed on their game?)", " : inventaire inconnu (Emeraude Admin absent chez lui ?)");
                }
                case "tp":
                {
                    if (a.Length != 3 && a.Length != 4) return L.T("Usage: adm:tp <player> <player>  or  adm:tp <player> <x> <z>", "Usage : adm:tp <joueur> <joueur>  ou  adm:tp <joueur> <x> <z>");
                    var who = Find(a[1], out error);
                    if (who == null) return error;
                    Vector3 dest;
                    bool exact;
                    string where;
                    if (a.Length == 4)
                    {
                        if (!float.TryParse(a[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
                            !float.TryParse(a[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
                            return L.T("Invalid coordinates: adm:tp <player> <x> <z>", "Coordonnées invalides : adm:tp <joueur> <x> <z>");
                        // Comme la commande goto du jeu : hauteur arbitraire, le jeu pose le joueur au sol
                        dest = new Vector3(x, 30f, z);
                        exact = false;
                        where = $"({x:0}, {z:0})";
                    }
                    else
                    {
                        var to = Find(a[2], out error);
                        if (to == null) return error;
                        dest = to.m_refPos + new Vector3(1.5f, 0.5f, 0f);
                        exact = true;
                        where = to.m_playerName + " " + Net.Pos(to.m_refPos);
                    }
                    ZRoutedRpc.instance.InvokeRoutedRPC(who.m_uid, Net.Teleport, dest, exact);
                    Journal(who, who.m_playerName, who.m_refPos, L.T($"teleported by {admin} to {where}", $"téléporté par {admin} vers {where}"));
                    return who.m_playerName + L.T($" teleported to {where}.", $" téléporté vers {where}.");
                }
                case "give":
                {
                    if (a.Length < 3) return L.T("Usage: adm:give <player> <item> [amount] [quality]", "Usage : adm:give <joueur> <objet> [quantité] [qualité]");
                    var who = Find(a[1], out error);
                    if (who == null) return error;
                    var item = FindItem(a[2]);
                    if (item == null) return L.T($"Unknown item \"{a[2]}\" (English prefab name, e.g. Copper, Tin, Bronze, Wood).", $"Objet « {a[2]} » inconnu (nom anglais du prefab, ex. Copper, Tin, Bronze, Wood).");
                    int amount = 1, quality = 1;
                    if (a.Length > 3 && (!int.TryParse(a[3], out amount) || amount < 1 || amount > 9999)) return L.T("Invalid amount (1 to 9999).", "Quantité invalide (1 à 9999).");
                    if (a.Length > 4 && (!int.TryParse(a[4], out quality) || quality < 1 || quality > 10)) return L.T("Invalid quality (1 to 10).", "Qualité invalide (1 à 10).");
                    ZRoutedRpc.instance.InvokeRoutedRPC(who.m_uid, Net.Give, item.name, amount, quality);
                    Journal(who, who.m_playerName, who.m_refPos, L.T($"receives from {admin}: {amount} {item.name}", $"reçoit de {admin} : {amount} {item.name}") + (quality > 1 ? L.T($" (quality {quality})", $" (qualité {quality})") : ""));
                    return L.T($"{amount} {item.name} given to {who.m_playerName}.", $"{amount} {item.name} donné(s) à {who.m_playerName}.");
                }
                default:
                    return Commands.Help;
            }
        }
    }
}
