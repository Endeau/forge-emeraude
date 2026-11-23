using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EmeraudeAdmin
{
    // RPC entre le serveur et les joueurs, et format texte des inventaires.
    static class Net
    {
        // joueur -> serveur
        public const string Log = "EA_Log";       // (type, position, inventaire) : apparition, delta, inventaire, sauvegarde, mort
        public const string Admin = "EA_Admin";   // (ligne de commande) envoyée par un admin
        // serveur -> joueur
        public const string Reply = "EA_Reply";       // (texte) réponse à une commande admin
        public const string Teleport = "EA_Teleport"; // (destination, position exacte ?)
        public const string Give = "EA_Give";         // (prefab, quantité, qualité)
        public const string Do = "EA_Do";             // (action, argument) : take, skill, heal, repair, god, ghost, fly

        public static void Register()
        {
            var rpc = ZRoutedRpc.instance;
            rpc.Register<string, Vector3, string>(Log, Server.OnLog);
            rpc.Register<string>(Admin, Server.OnAdmin);
            rpc.Register<string>(Reply, Client.OnReply);
            rpc.Register<Vector3, bool>(Teleport, Client.OnTeleport);
            rpc.Register<string, int, int>(Give, Client.OnGive);
            rpc.Register<string, string>(Do, Client.OnDo);
        }

        // Les ordres tp/give/réponses ne sont acceptés que s'ils viennent du serveur.
        public static bool FromServer(long sender)
        {
            if (ZNet.instance.IsServer()) return sender == ZNet.GetUID();
            var server = ZNet.instance.GetServerPeer();
            return server != null && server.m_uid == sender;
        }

        // Inventaire <-> texte "Prefab:quantité;Prefab*qualité:quantité"
        public static string Encode(Dictionary<string, int> items) =>
            string.Join(";", items.Where(kv => kv.Value != 0).Select(kv => kv.Key + ":" + kv.Value));

        public static Dictionary<string, int> Decode(string text)
        {
            var items = new Dictionary<string, int>();
            foreach (var part in (text ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int i = part.LastIndexOf(':');
                if (i > 0 && int.TryParse(part.Substring(i + 1), out int n)) items[part.Substring(0, i)] = n;
            }
            return items;
        }

        public static string Pos(Vector3 p) => $"({p.x:0}, {p.y:0}, {p.z:0})";
    }
}
