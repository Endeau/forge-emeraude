using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace EmeraudeDeconstruction
{
    // Les prefabs des bateaux et des charrettes ont m_canBeRemoved = false : le marteau les ignore.
    [HarmonyPatch(typeof(Ship), "Awake")]
    static class ShipAwakePatch
    {
        static void Postfix(Ship __instance) => Removal.Allow(__instance);
    }

    [HarmonyPatch(typeof(Vagon), "Awake")]
    static class VagonAwakePatch
    {
        static void Postfix(Vagon __instance) => Removal.Allow(__instance);
    }

    // Piece.CanBeRemoved protège aussi des dégâts de l'environnement (cendres, lave) : nos
    // conditions ne s'appliquent que pendant un retrait au marteau.
    [HarmonyPatch(typeof(Player), "RemovePiece")]
    static class RemovePiecePatch
    {
        static void Prefix() => Removal.InProgress = true;

        // Après le « $msg_cantremovenow » du jeu, pour le remplacer par la vraie raison.
        static void Finalizer(Player __instance)
        {
            Removal.InProgress = false;
            if (Removal.Refusal != null) __instance.Message(MessageHud.MessageType.Center, Removal.Refusal);
            Removal.Refusal = null;
        }
    }

    // Sans ce patch, un bateau avec une cale ne vérifie que la cale privée, et une charrette rien.
    [HarmonyPatch(typeof(Piece), nameof(Piece.CanBeRemoved))]
    static class CanBeRemovedPatch
    {
        static void Postfix(Piece __instance, ref bool __result)
        {
            if (__result && Removal.InProgress) __result = Removal.IsFree(__instance);
        }
    }

    // Bateau détruit (par un monstre, l'eau, ou retiré au marteau) : le jeu rend ses matériaux en vrac,
    // les clous coulent et le bois dérive. Comme pour la cale, on les range dans une caisse flottante :
    // Piece.DropResources le fait déjà quand la pièce a un m_destroyedLootPrefab, que les bateaux n'ont pas.
    [HarmonyPatch(typeof(Piece), nameof(Piece.DropResources))]
    static class ShipLootPatch
    {
        const string CrateName = "CargoCrate";

        static void Prefix(Piece __instance)
        {
            if (__instance.m_destroyedLootPrefab || !__instance.GetComponent<Ship>()) return;
            // La caisse de la cale du bateau, sinon celle du jeu (le radeau n'a pas de cale).
            var crate = __instance.GetComponentsInChildren<Container>(true).Select(c => c.m_destroyedLootPrefab).FirstOrDefault(g => g)
                ?? ZNetScene.instance?.GetPrefab(CrateName);
            if (!crate || !crate.GetComponent<Container>()) return;
            __instance.m_destroyedLootPrefab = crate;
            Debug.Log($"[{Plugin.Name}] {Utils.GetPrefabName(__instance.gameObject)} destroyed: materials in {crate.name}");
        }
    }

    static class Removal
    {
        internal static bool InProgress;
        internal static string Refusal; // raison à afficher à la place du message du jeu

        internal static void Allow(Component vehicle)
        {
            var piece = vehicle.GetComponentInParent<Piece>();
            if (piece != null) piece.m_canBeRemoved = true;
        }

        internal static bool IsFree(Piece piece)
        {
            var ship = piece.GetComponentInChildren<Ship>();
            var cart = piece.GetComponentInChildren<Vagon>();
            if (ship == null && cart == null) return true;
            if (ship != null && ship.HasPlayerOnboard())
                return Refuse("Quelqu'un est à bord (toi compris, même contre la coque)", "Someone is aboard (you too, even against the hull)");
            if (cart != null && cart.InUse())
                return Refuse("Quelqu'un tire la charrette", "Someone is pulling the cart");

            // Toutes les cales : certains bateaux de mods en ont plusieurs.
            var cargos = piece.GetComponentsInChildren<Container>();
            if (cargos.Any(c => c.IsInUse()))
                return Refuse("Quelqu'un a la cale ouverte", "Someone has the cargo open");
            if (cargos.Any(c => c.GetInventory().NrOfItems() > 0))
                return Refuse("Vide d'abord la cale", "Empty the cargo first");

            // Avec Build On Ship, ce qui est construit sur le pont (coffres, lits, portails…) partirait avec le bateau.
            if (ship != null && HasBuildOnDeck(piece))
                return Refuse("Retire d'abord ce qui est construit sur le bateau", "Remove what is built on the ship first");
            return true;
        }

        static bool Refuse(string french, string english)
        {
            Refusal = French ? french : english;
            return false;
        }

        static bool French => Localization.instance != null && Localization.instance.GetSelectedLanguage() == "French";

        // Hauteur comptée au-dessus du point le plus haut de la coque (mâts, tentes, toits construits sur le pont).
        const float AboveHull = 12f;
        const float Margin = 0.5f;

        // Une construction est « sur le bateau » si sa position, vue depuis le bateau, tombe dans l'emprise de la
        // coque (longueur × largeur) et pas en dessous. On compare dans le repère du bateau, pas avec une boîte
        // alignée sur le monde : un ponton construit le long d'un bateau en biais ne compte pas.
        static bool HasBuildOnDeck(Piece shipPiece)
        {
            var root = shipPiece.transform;
            var view = shipPiece.GetComponent<ZNetView>();
            var hull = LocalBounds(root);
            if (hull.size == Vector3.zero) return false;
            var pieces = new List<Piece>();
            Piece.GetAllPiecesInRadius(root.TransformPoint(hull.center), hull.extents.magnitude + AboveHull, pieces);
            foreach (var other in pieces)
            {
                // Les parties du bateau lui-même n'ont pas leur propre ZNetView.
                var otherView = other.GetComponent<ZNetView>();
                if (other == shipPiece || otherView == null || otherView == view) continue;
                var p = root.InverseTransformPoint(other.transform.position);
                if (p.x >= hull.min.x - Margin && p.x <= hull.max.x + Margin
                    && p.z >= hull.min.z - Margin && p.z <= hull.max.z + Margin
                    && p.y >= hull.min.y - Margin && p.y <= hull.max.y + AboveHull)
                    return true;
            }
            return false;
        }

        // Emprise des collisions solides du bateau dans son propre repère.
        static Bounds LocalBounds(Transform root)
        {
            var bounds = new Bounds();
            bool first = true;
            foreach (var collider in root.GetComponentsInChildren<Collider>())
            {
                if (collider.isTrigger || !collider.enabled) continue;
                var world = collider.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = world.center + Vector3.Scale(world.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var local = root.InverseTransformPoint(corner);
                    if (first) { bounds = new Bounds(local, Vector3.zero); first = false; }
                    else bounds.Encapsulate(local);
                }
            }
            return bounds;
        }
    }
}
