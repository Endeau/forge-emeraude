using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace EmeraudeMinerais
{
    // Les quatre compteurs, tels qu'écrits dans le fichier et lus par le bot Discord.
    static class Stat
    {
        public const string Mined = "mined";       // minerai ramassé au sol après minage (pas celui lâché par un joueur)
        public const string Ore = "ore";           // minerai mis au four
        public const string Produced = "produced"; // barre sortie du four, pour celui qui a déposé le minerai
        public const string Used = "used";         // barre dépensée pour fabriquer ou construire
    }

    // Joueur compté : identifiant du personnage (stable) et dernier nom connu.
    struct Miner
    {
        public long Id;
        public string Name;

        public static Miner Of(Player player) => new Miner { Id = player.GetPlayerID(), Name = player.GetPlayerName() };

        // Forme rangée dans le ZDO d'un four, à côté de chaque minerai en attente : "<id>|<nom>".
        public override string ToString() => Id.ToString(CultureInfo.InvariantCulture) + "|" + Name;

        public static bool TryParse(string text, out Miner miner)
        {
            miner = default;
            int bar = (text ?? "").IndexOf('|');
            if (bar <= 0 || !long.TryParse(text.Substring(0, bar), NumberStyles.Integer, CultureInfo.InvariantCulture, out miner.Id))
                return false;
            miner.Name = text.Substring(bar + 1);
            return true;
        }
    }

    // Côté serveur : les totaux en mémoire, enregistrés dans BepInEx/config/emeraude-minerais/classement.tsv
    // (une ligne par joueur, compteur et objet : id, nom, compteur, prefab, quantité).
    static class Stats
    {
        const string Rpc = "EM_Count";
        const float SaveEverySeconds = 30f;
        static readonly Encoding Utf8 = new UTF8Encoding(false);

        // (id, compteur, prefab) -> quantité
        static readonly Dictionary<(long, string, string), long> counts = new Dictionary<(long, string, string), long>();
        static readonly Dictionary<long, string> names = new Dictionary<long, string>();
        static bool loaded, dirty;
        static float nextSave;

        static string FilePath => Path.Combine(Paths.ConfigPath, "emeraude-minerais", "classement.tsv");

        [HarmonyPatch(typeof(Game), "Start")]
        static class GameStartPatch
        {
            static void Postfix() => ZRoutedRpc.instance.Register<ZPackage>(Rpc, OnCount);
        }

        [HarmonyPatch(typeof(ZNet), "OnDestroy")]
        static class ZNetDestroyPatch
        {
            static void Prefix()
            {
                if (ZNet.instance != null && ZNet.instance.IsServer()) Save();
            }
        }

        // Appelé chez n'importe quel joueur (ou le serveur) : le serveur fait le compte.
        public static void Report(Miner miner, string stat, string prefab, int amount)
        {
            if (amount <= 0 || ZRoutedRpc.instance == null) return;
            var package = new ZPackage();
            package.Write(miner.Id);
            package.Write(miner.Name);
            package.Write(stat);
            package.Write(prefab);
            package.Write(amount);
            ZRoutedRpc.instance.InvokeRoutedRPC(Rpc, package);
        }

        static void OnCount(long sender, ZPackage package)
        {
            if (!ZNet.instance.IsServer()) return;
            Load();
            long id = package.ReadLong();
            string name = Clean(package.ReadString());
            string stat = package.ReadString();
            string prefab = Clean(package.ReadString());
            int amount = package.ReadInt();
            if (amount <= 0 || (stat != Stat.Mined && stat != Stat.Ore && stat != Stat.Produced && stat != Stat.Used)) return;
            if (name.Length > 0) names[id] = name;
            counts.TryGetValue((id, stat, prefab), out long total);
            counts[(id, stat, prefab)] = total + amount;
            dirty = true;
        }

        public static void Tick()
        {
            if (!dirty || Time.time < nextSave) return;
            nextSave = Time.time + SaveEverySeconds;
            Save();
        }

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            if (!File.Exists(FilePath)) return;
            try
            {
                foreach (var line in File.ReadAllLines(FilePath, Utf8).Skip(1))
                {
                    var cells = line.Split('\t');
                    if (cells.Length != 5
                        || !long.TryParse(cells[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out long id)
                        || !long.TryParse(cells[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out long amount))
                        continue;
                    names[id] = cells[1];
                    counts[(id, cells[2], cells[3])] = amount;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{Plugin.Name}] cannot read {FilePath}: {e.Message}");
            }
        }

        // Écrit dans un fichier à côté puis le met à la place : le bot ne lit jamais un fichier à moitié écrit.
        static void Save()
        {
            if (!dirty) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                var lines = new List<string> { "id\tname\tstat\tprefab\tamount" };
                lines.AddRange(counts.OrderBy(kv => kv.Key.Item1).ThenBy(kv => kv.Key.Item2).ThenBy(kv => kv.Key.Item3)
                    .Select(kv => string.Join("\t", kv.Key.Item1.ToString(CultureInfo.InvariantCulture),
                        names.TryGetValue(kv.Key.Item1, out var name) ? name : "?", kv.Key.Item2, kv.Key.Item3,
                        kv.Value.ToString(CultureInfo.InvariantCulture))));
                string temp = FilePath + ".tmp";
                File.WriteAllLines(temp, lines, Utf8);
                if (File.Exists(FilePath)) File.Replace(temp, FilePath, null);
                else File.Move(temp, FilePath);
                dirty = false;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{Plugin.Name}] cannot write {FilePath}: {e.Message}");
            }
        }

        // Pas de tabulation ni de retour à la ligne dans le fichier.
        static string Clean(string text) => new string((text ?? "").Where(c => !char.IsControl(c)).ToArray()).Trim();
    }
}
