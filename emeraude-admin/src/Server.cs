using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using BepInEx;
using HarmonyLib;
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

        static IEnumerable<ZNetPeer> InGame()
        {
            var peers = ZNet.instance.GetPeers().Where(p => p.IsReady() && !p.m_characterID.IsNone() && !string.IsNullOrEmpty(p.m_playerName));
            var host = Host();
            return host != null ? peers.Concat(new[] { host }) : peers;
        }

        // L'hôte d'une partie lancée depuis le jeu n'est pas un pair réseau : on lui fabrique un pair
        // (sans connexion) pour qu'il soit trouvé comme les autres joueurs. Les RPC envoyés à son
        // identifiant sont traités en local.
        static ZNetPeer host;

        static ZNetPeer Host()
        {
            var player = Player.m_localPlayer;
            if (ZNet.instance.IsDedicated() || player == null) return null;
            if (host == null) host = (ZNetPeer)FormatterServices.GetUninitializedObject(typeof(ZNetPeer));
            host.m_uid = ZNet.GetUID();
            host.m_playerName = player.GetPlayerName();
            host.m_refPos = player.transform.position;
            host.m_characterID = player.GetZDOID();
            return host;
        }

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
            Reply(sender, Run(line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), admin, sender));
        }

        static string Run(string[] a, string admin, long sender)
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
                case "portal":
                    return Portal(a, admin);
                case "take":
                {
                    if (a.Length < 3) return L.T("Usage: adm:take <player> <item> [amount]", "Usage : adm:take <joueur> <objet> [quantité]");
                    var who = Find(a[1], out error);
                    if (who == null) return error;
                    var item = FindItem(a[2]);
                    if (item == null) return L.T($"Unknown item \"{a[2]}\" (English prefab name).", $"Objet « {a[2]} » inconnu (nom anglais du prefab).");
                    int amount = 1;
                    if (a.Length > 3 && (!int.TryParse(a[3], out amount) || amount < 1 || amount > 9999)) return L.T("Invalid amount (1 to 9999).", "Quantité invalide (1 à 9999).");
                    Do(who.m_uid, "take", $"{item.name} {amount}");
                    Journal(who, who.m_playerName, who.m_refPos, L.T($"{amount} {item.name} taken by {admin}", $"{amount} {item.name} retiré(s) par {admin}"));
                    return L.T($"{amount} {item.name} taken from {who.m_playerName} (fewer if they had less).", $"{amount} {item.name} retiré(s) à {who.m_playerName} (moins s'il en avait moins).");
                }
                case "skill":
                {
                    if (a.Length < 4) return L.T("Usage: adm:skill <player> <skill> <level>  (skills: ", "Usage : adm:skill <joueur> <compétence> <niveau>  (compétences : ") + SkillNames + ")";
                    var who = Find(a[1], out error);
                    if (who == null) return error;
                    if (!Enum.TryParse(a[2], true, out Skills.SkillType skill) || skill == Skills.SkillType.None || skill == Skills.SkillType.All || !Enum.IsDefined(typeof(Skills.SkillType), skill))
                        return L.T($"Unknown skill \"{a[2]}\". Skills: ", $"Compétence « {a[2]} » inconnue. Compétences : ") + SkillNames;
                    if (!float.TryParse(a[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float level) || level < 0f || level > 100f) return L.T("Invalid level (0 to 100).", "Niveau invalide (0 à 100).");
                    Do(who.m_uid, "skill", skill + " " + level.ToString(CultureInfo.InvariantCulture));
                    Journal(who, who.m_playerName, who.m_refPos, L.T($"skill {skill} set to {level} by {admin}", $"compétence {skill} mise à {level} par {admin}"));
                    return L.T($"{skill} of {who.m_playerName} set to {level}.", $"{skill} de {who.m_playerName} mis à {level}.");
                }
                case "heal":
                case "repair":
                {
                    string cmd = Commands.Canonical(a[0]);
                    if (a.Length < 2) return L.T($"Usage: adm:{cmd} <player>", $"Usage : adm:{cmd} <joueur>");
                    var who = Find(a[1], out error);
                    if (who == null) return error;
                    Do(who.m_uid, cmd, "");
                    Journal(who, who.m_playerName, who.m_refPos, cmd == "heal" ? L.T($"healed by {admin}", $"soigné par {admin}") : L.T($"items repaired by {admin}", $"objets réparés par {admin}"));
                    return cmd == "heal" ? L.T($"{who.m_playerName} healed.", $"{who.m_playerName} soigné.") : L.T($"Items of {who.m_playerName} repaired.", $"Objets de {who.m_playerName} réparés.");
                }
                case "god":
                case "ghost":
                case "fly":
                {
                    string cmd = Commands.Canonical(a[0]);
                    // [joueur] [on|off] : sans joueur, c'est l'admin lui-même
                    bool IsState(string s) => s.Equals("on", StringComparison.OrdinalIgnoreCase) || s.Equals("off", StringComparison.OrdinalIgnoreCase);
                    int next = 1;
                    long uid = sender;
                    string name = admin;
                    ZNetPeer peer = ZNet.instance.GetPeer(sender);
                    if (a.Length > 1 && !IsState(a[1]))
                    {
                        peer = Find(a[1], out error);
                        if (peer == null) return error;
                        uid = peer.m_uid;
                        name = peer.m_playerName;
                        next = 2;
                    }
                    string state = a.Length > next && IsState(a[next]) ? a[next].ToLowerInvariant() : "toggle";
                    Do(uid, cmd, state);
                    Journal(peer, name, peer?.m_refPos ?? Vector3.zero, L.T($"{cmd} mode {state} by {admin}", $"mode {cmd} {state} par {admin}"));
                    return L.T($"{cmd} mode ({state}) sent to {name}: the message on their screen confirms it.", $"Mode {cmd} ({state}) envoyé à {name} : le message sur son écran le confirme.");
                }
                case "tomb":
                    return Tomb(a, admin);
                case "journal":
                    return JournalTail(a);
                case "ores":
                    return Ores(a);
                case "say":
                {
                    if (a.Length < 2) return L.T("Usage: adm:say <message>", "Usage : adm:say <message>");
                    string text = string.Join(" ", a.Skip(1));
                    ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ShowMessage", (int)MessageHud.MessageType.Center, text);
                    return L.T("Message shown to everyone: ", "Message affiché à tous : ") + text;
                }
                case "day":
                case "night":
                    return SkipTime(Commands.Canonical(a[0]) == "night");
                case "raid":
                    return Raid(a, admin);
                case "kick":
                {
                    if (a.Length < 2) return L.T("Usage: adm:kick <player> [reason]", "Usage : adm:kick <joueur> [raison]");
                    var who = Find(a[1], out error);
                    if (who == null) return error;
                    if (who == host) return L.T("The host can't be kicked.", "On ne peut pas expulser l'hôte.");
                    string reason = string.Join(" ", a.Skip(2));
                    if (reason.Length > 0)
                        ZRoutedRpc.instance.InvokeRoutedRPC(who.m_uid, "ShowMessage", (int)MessageHud.MessageType.Center, L.T("Kicked: ", "Expulsé : ") + reason);
                    Journal(who, who.m_playerName, who.m_refPos, L.T($"kicked by {admin}", $"expulsé par {admin}") + (reason.Length > 0 ? " : " + reason : ""));
                    KickPeer(ZNet.instance, who);
                    return L.T($"{who.m_playerName} kicked.", $"{who.m_playerName} expulsé.");
                }
                default:
                    return Commands.Help;
            }
        }

        // ---------------------------------------------------------------- aides des commandes

        // ZNet.Kick(string) cherche le joueur par identifiant de plateforme puis par nom : on lui donne directement le bon.
        static readonly Action<ZNet, ZNetPeer> KickPeer = AccessTools.MethodDelegate<Action<ZNet, ZNetPeer>>(
            AccessTools.Method(typeof(ZNet), "InternalKick", new[] { typeof(ZNetPeer) }));

        static void Do(long uid, string action, string arg) => ZRoutedRpc.instance.InvokeRoutedRPC(uid, Net.Do, action, arg);

        static string SkillNames => string.Join(", ", Enum.GetValues(typeof(Skills.SkillType)).Cast<Skills.SkillType>()
            .Where(s => s != Skills.SkillType.None && s != Skills.SkillType.All).Select(s => s.ToString()).Distinct());

        static List<ZDO> AllZdos(string prefab)
        {
            var list = new List<ZDO>();
            int index = 0;
            while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(prefab, list, ref index)) { }
            return list;
        }

        // Sans argument : liste. Sinon le portail dont le nom correspond ; de deux portails liés, le plus loin du joueur.
        static string Portal(string[] a, string admin)
        {
            var portals = ZDOMan.instance.GetPortalList().Where(z => z != null && z.IsValid()).ToList();
            string Tag(ZDO z) => z.GetString(ZDOVars.s_tag);
            if (a.Length < 3)
            {
                if (portals.Count == 0) return L.T("No portal.", "Aucun portail.");
                return L.T("Usage: adm:portal <player> <portal name>. Portals:\n", "Usage : adm:portal <joueur> <nom du portail>. Portails :\n") +
                    string.Join("\n", portals.OrderBy(Tag).Select(z => $"  {(Tag(z).Length > 0 ? Tag(z) : L.T("(no name)", "(sans nom)"))} {Net.Pos(z.GetPosition())}"));
            }
            var who = Find(a[1], out string error);
            if (who == null) return error;
            string name = string.Join(" ", a.Skip(2));
            var found = portals.Where(z => Tag(z).Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
            if (found.Count == 0) found = portals.Where(z => Tag(z).IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            var tags = found.Select(Tag).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (tags.Count == 0) return L.T($"No portal named \"{name}\". adm:portal lists them.", $"Aucun portail « {name} ». adm:portail les liste.");
            if (tags.Count > 1) return L.T($"\"{name}\" is ambiguous: ", $"« {name} » est ambigu : ") + string.Join(", ", tags);
            var portal = found.OrderByDescending(z => Vector3.Distance(z.GetPosition(), who.m_refPos)).First();
            // Devant le portail, pas dedans
            var dest = portal.GetPosition() + portal.GetRotation() * Vector3.forward * 2.5f + Vector3.up * 0.5f;
            ZRoutedRpc.instance.InvokeRoutedRPC(who.m_uid, Net.Teleport, dest, true);
            string where = L.T($"portal {tags[0]} {Net.Pos(portal.GetPosition())}", $"portail {tags[0]} {Net.Pos(portal.GetPosition())}");
            Journal(who, who.m_playerName, who.m_refPos, L.T($"teleported by {admin} to {where}", $"téléporté par {admin} vers {where}"));
            return who.m_playerName + L.T($" teleported to {where}.", $" téléporté vers {where}.");
        }

        // Tombes d'un joueur (connecté ou non), numérotées ; avec un numéro, le joueur est téléporté dessus.
        static string Tomb(string[] a, string admin)
        {
            if (a.Length < 2) return L.T("Usage: adm:tomb <player> [number]", "Usage : adm:tomb <joueur> [numéro]");
            string Owner(ZDO z) => z.GetString(ZDOVars.s_ownerName);
            var all = AllZdos("Player_tombstone");
            var tombs = all.Where(z => Owner(z).Equals(a[1], StringComparison.OrdinalIgnoreCase)).ToList();
            if (tombs.Count == 0) tombs = all.Where(z => Owner(z).StartsWith(a[1], StringComparison.OrdinalIgnoreCase)).ToList();
            tombs = tombs.OrderBy(Owner).ThenBy(z => z.m_uid.ID).ToList();
            if (tombs.Count == 0) return L.T($"No tombstone for \"{a[1]}\".", $"Aucune tombe pour « {a[1]} ».");
            if (a.Length < 3)
                return string.Join("\n", tombs.Select((z, i) => $"  {i + 1}. {Owner(z)} {Net.Pos(z.GetPosition())}")) +
                    L.T($"\nadm:tomb {a[1]} <number> teleports the player there.", $"\nadm:tombe {a[1]} <numéro> l'y téléporte.");
            if (!int.TryParse(a[2], out int n) || n < 1 || n > tombs.Count) return L.T($"Invalid number (1 to {tombs.Count}).", $"Numéro invalide (1 à {tombs.Count}).");
            var who = Find(a[1], out string error);
            if (who == null) return error;
            var tomb = tombs[n - 1];
            ZRoutedRpc.instance.InvokeRoutedRPC(who.m_uid, Net.Teleport, tomb.GetPosition() + new Vector3(1.5f, 0.5f, 0f), true);
            string where = L.T("tombstone ", "tombe ") + Net.Pos(tomb.GetPosition());
            Journal(who, who.m_playerName, who.m_refPos, L.T($"teleported by {admin} to {where}", $"téléporté par {admin} vers {where}"));
            return who.m_playerName + L.T($" teleported to {where}.", $" téléporté vers {where}.");
        }

        // Fin du journal courant d'un joueur, connecté ou non (fichier <Joueur>_<SteamID>.log le plus récent).
        static string JournalTail(string[] a)
        {
            if (a.Length < 2) return L.T("Usage: adm:journal <player> [lines]", "Usage : adm:journal <joueur> [lignes]");
            int count = 20;
            if (a.Length > 2 && (!int.TryParse(a[2], out count) || count < 1 || count > 100)) return L.T("Invalid number of lines (1 to 100).", "Nombre de lignes invalide (1 à 100).");
            var files = Directory.GetFiles(Dir, "*.log")
                .Select(f => Path.GetFileName(f))
                .Where(f => f != "admin.log" && f.Count(c => c == '.') == 1 && f.LastIndexOf('_') > 0)
                .ToList();
            string Player(string f) => f.Substring(0, f.LastIndexOf('_'));
            string target = Clean(a[1]);
            var found = files.Where(f => Player(f).Equals(target, StringComparison.OrdinalIgnoreCase)).ToList();
            if (found.Count == 0) found = files.Where(f => Player(f).StartsWith(target, StringComparison.OrdinalIgnoreCase)).ToList();
            if (found.Count == 0) return L.T($"No journal for \"{a[1]}\".", $"Aucun journal pour « {a[1]} ».");
            string file = found.OrderByDescending(f => File.GetLastWriteTime(Path.Combine(Dir, f))).First();
            var lines = File.ReadAllLines(Path.Combine(Dir, file), Utf8);
            return file + " :\n" + string.Join("\n", lines.Skip(Math.Max(0, lines.Length - count)));
        }

        // Fichier écrit par le mod Emeraude Minerais sur ce serveur.
        static string Ores(string[] a)
        {
            string path = Path.Combine(Paths.ConfigPath, "emeraude-minerais", "classement.tsv");
            if (!File.Exists(path)) return L.T("No ore count yet (Emeraude Minerais not installed, or nothing smelted).", "Aucun compte de minerais (Emeraude Minerais absent, ou rien fondu).");
            var rows = File.ReadAllLines(path, Utf8).Skip(1).Select(l => l.Split('\t')).Where(c => c.Length == 5 && long.TryParse(c[4], out _)).ToList();
            var stats = new[] { ("ore", L.T("Ore smelted", "Minerai mis au four")), ("produced", L.T("Bars produced", "Barres produites")), ("used", L.T("Bars used", "Barres utilisées")) };
            var sb = new StringBuilder();
            if (a.Length < 2)
            {
                foreach (var (stat, title) in stats)
                {
                    sb.Append(title).Append(" :");
                    var top = rows.Where(c => c[2] == stat).GroupBy(c => c[0]).Select(g => (name: g.Last()[1], total: g.Sum(c => long.Parse(c[4]))))
                        .OrderByDescending(t => t.total).Take(5).ToList();
                    sb.Append(top.Count == 0 ? L.T(" nobody", " personne") : "").Append("\n");
                    for (int i = 0; i < top.Count; i++) sb.Append($"  {i + 1}. {top[i].name} {top[i].total}\n");
                }
                return sb.ToString().TrimEnd();
            }
            var mine = rows.Where(c => c[1].Equals(a[1], StringComparison.OrdinalIgnoreCase)).ToList();
            if (mine.Count == 0) mine = rows.Where(c => c[1].StartsWith(a[1], StringComparison.OrdinalIgnoreCase)).ToList();
            if (mine.Count == 0) return L.T($"Nothing counted for \"{a[1]}\".", $"Rien de compté pour « {a[1]} ».");
            foreach (var group in mine.GroupBy(c => c[0]))
            {
                sb.Append(group.Last()[1]).Append("\n");
                foreach (var (stat, title) in stats)
                {
                    var items = group.Where(c => c[2] == stat).ToList();
                    if (items.Count > 0) sb.Append($"  {title} : {items.Sum(c => long.Parse(c[4]))} ({string.Join(", ", items.Select(c => $"{c[3]} {c[4]}"))})\n");
                }
            }
            return sb.ToString().TrimEnd();
        }

        // Comme le sommeil : le temps défile vite pendant une douzaine de secondes. Le matin commence à 15 % de la
        // journée, le soir à 85 % (EnvMan.RescaleDayFraction).
        static string SkipTime(bool night)
        {
            var env = EnvMan.instance;
            if (env == null) return L.T("No environment on this server.", "Pas d'environnement sur ce serveur.");
            if (env.IsTimeSkipping()) return L.T("Time is already skipping.", "Le temps défile déjà.");
            double now = ZNet.instance.GetTimeSeconds();
            double length = env.m_dayLengthSec;
            double start = Math.Floor(now / length) * length;
            double fraction = (now - start) / length;
            bool isDay = fraction >= 0.15 && fraction < 0.85;
            if (night == !isDay) return night ? L.T("It is already night.", "Il fait déjà nuit.") : L.T("It is already day.", "Il fait déjà jour.");
            if (!night)
            {
                env.SkipToMorning();
                return L.T("Skipping to the morning.", "Passage au matin.");
            }
            double target = start + length * 0.86;
            var t = Traverse.Create(env);
            t.Field("m_skipToTime").SetValue(target);
            t.Field("m_timeSkipSpeed").SetValue((target - now) / 12.0);
            t.Field("m_skipTime").SetValue(true);
            return L.T("Skipping to the evening.", "Passage au soir.");
        }

        static string Raid(string[] a, string admin)
        {
            var events = RandEventSystem.instance;
            if (events == null) return L.T("No event system on this server.", "Pas de système d'événements sur ce serveur.");
            if (a.Length >= 2 && a[1].Equals("stop", StringComparison.OrdinalIgnoreCase))
            {
                var active = events.GetActiveEvent();
                events.ResetRandomEvent();
                return active != null ? L.T($"Raid {active.m_name} stopped.", $"Raid {active.m_name} arrêté.") : L.T("No raid in progress.", "Aucun raid en cours.");
            }
            string names = string.Join(", ", events.m_events.Select(e => e.m_name).Where(n => !string.IsNullOrEmpty(n)).Distinct().OrderBy(n => n));
            if (a.Length < 3) return L.T("Usage: adm:raid <player> <raid>  or  adm:raid stop. Raids: ", "Usage : adm:raid <joueur> <raid>  ou  adm:raid stop. Raids : ") + names;
            var who = Find(a[1], out string error);
            if (who == null) return error;
            var raid = events.m_events.FirstOrDefault(e => e.m_name.Equals(a[2], StringComparison.OrdinalIgnoreCase));
            if (raid == null) return L.T($"Unknown raid \"{a[2]}\". Raids: ", $"Raid « {a[2]} » inconnu. Raids : ") + names;
            events.SetRandomEventByName(raid.m_name, who.m_refPos);
            Journal(who, who.m_playerName, who.m_refPos, L.T($"raid {raid.m_name} started on them by {admin}", $"raid {raid.m_name} lancé sur lui par {admin}"));
            return L.T($"Raid {raid.m_name} started near {who.m_playerName}.", $"Raid {raid.m_name} lancé près de {who.m_playerName}.");
        }
    }
}
