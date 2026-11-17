// Les raids partent de la position d'un joueur (RandEventSystem.GetValidEventPoints, côté serveur).
// On retire les joueurs qui se trouvent dans la zone d'une statue. ZenRaids patche CheckBase et
// UpdateRandomEvent, pas cette méthode : les deux se cumulent.
//
// Le serveur dédié ne crée pas forcément les objets autour des joueurs : on cherche les statues
// dans les ZDO, une fois par image au plus (la méthode est appelée une fois par type de raid).
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EmeraudeSanctuaire
{
    [HarmonyPatch(typeof(RandEventSystem), "GetValidEventPoints")]
    static class Raids
    {
        static readonly List<KeyValuePair<Vector3, float>> s_statues = new List<KeyValuePair<Vector3, float>>();
        static readonly List<ZDO> s_zdos = new List<ZDO>();
        static int s_frame = -1;

        static void Postfix(List<Vector3> __result)
        {
            if (!Plugin.BlockRaids.Value || __result.Count == 0) return;
            var statues = Statues();
            __result.RemoveAll(point => statues.Exists(statue => Utils.DistanceXZ(point, statue.Key) < statue.Value));
        }

        static List<KeyValuePair<Vector3, float>> Statues()
        {
            if (s_frame == Time.frameCount) return s_statues;
            s_frame = Time.frameCount;
            s_statues.Clear();
            s_zdos.Clear();
            int index = 0;
            while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(Sanctuary.PrefabName, s_zdos, ref index)) { }
            foreach (var zdo in s_zdos) s_statues.Add(new KeyValuePair<Vector3, float>(zdo.GetPosition(), Sanctuary.RadiusOf(zdo)));
            return s_statues;
        }
    }
}
