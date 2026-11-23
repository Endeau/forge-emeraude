using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace EmeraudeCoffres
{
    // Le bouton « Fabriquer » : le jeu ne compte que l'inventaire du joueur, Craft From Containers
    // exige tous les ingrédients. On accepte la recette si l'un d'eux suffit en comptant les coffres.
    [HarmonyPatch(typeof(Player), "HaveRequirementItems", typeof(Recipe), typeof(bool), typeof(int), typeof(int))]
    static class HaveRequirementItemsPatch
    {
        [HarmonyPriority(Priority.Last)]
        static void Postfix(Player __instance, Recipe piece, bool discover, int qualityLevel, int amount, ref bool __result)
        {
            if (__result || discover || !piece.m_requireOnlyOneIngredient || !Chests.Active()) return;
            __result = Chests.FindIngredient(__instance, piece, qualityLevel, amount) != null;
        }
    }

    // La fabrication : le jeu retire l'ingrédient choisi de l'inventaire du joueur et abandonne s'il
    // n'y est pas. On le rapatrie des coffres juste avant.
    [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
    static class DoCraftingPatch
    {
        static void Prefix(Player player, Recipe ___m_craftRecipe, ItemDrop.ItemData ___m_craftUpgradeItem,
            bool ___m_multiCrafting, int ___m_multiCraftAmount)
        {
            var recipe = ___m_craftRecipe;
            if (recipe == null || !recipe.m_requireOnlyOneIngredient || ___m_craftUpgradeItem != null || !Chests.Active()) return;
            int multiplier = ___m_multiCrafting ? ___m_multiCraftAmount : 1;
            if (player.GetFirstRequiredItem(player.GetInventory(), recipe, 1, out _, out _, multiplier) != null) return;
            var found = Chests.FindIngredient(player, recipe, 1, multiplier);
            if (found != null) Chests.Pull(player, found);
        }
    }

    class Ingredient
    {
        internal string Name;
        internal int Quality;
        internal int Missing;
        internal List<Container> Containers;
    }

    static class Chests
    {
        static ConfigEntry<bool> modEnabled;
        static Func<bool> allowByKey;
        static Func<Vector3, List<Container>> nearbyContainers;

        internal static bool Bind()
        {
            var plugin = AccessTools.TypeByName("CraftFromContainers.BepInExPlugin");
            if (plugin == null) return false;
            modEnabled = (ConfigEntry<bool>)AccessTools.Field(plugin, "modEnabled").GetValue(null);
            allowByKey = AccessTools.MethodDelegate<Func<bool>>(AccessTools.Method(plugin, "AllowByKey"));
            nearbyContainers = AccessTools.MethodDelegate<Func<Vector3, List<Container>>>(AccessTools.Method(plugin, "GetNearbyContainers"));
            return true;
        }

        // Mêmes interrupteurs que Craft From Containers : mod activé, touche d'inhibition non tenue.
        internal static bool Active() => modEnabled.Value && allowByKey();

        // Premier ingrédient de la recette disponible en assez grande quantité, d'une seule qualité,
        // entre l'inventaire du joueur et les coffres à portée. Même filtre que Player.HaveRequirementItems.
        internal static Ingredient FindIngredient(Player player, Recipe recipe, int qualityLevel, int amount)
        {
            var station = player.GetCurrentCraftingStation();
            var containers = nearbyContainers(player.transform.position);
            foreach (var req in recipe.m_resources)
            {
                if (!req.m_resItem) continue;
                if (station != null ? station.m_upgrader != req.m_upgraderResource : req.m_upgraderResource) continue;
                int needed = req.GetAmount(qualityLevel) * amount;
                if (needed <= 0) continue;
                var shared = req.m_resItem.m_itemData.m_shared;
                for (int quality = 1; quality <= shared.m_maxQuality; quality++)
                {
                    int carried = player.GetInventory().CountItems(shared.m_name, quality, true);
                    int total = carried + containers.Sum(c => c.GetInventory().CountItems(shared.m_name, quality, true));
                    if (total >= needed)
                        return new Ingredient { Name = shared.m_name, Quality = quality, Missing = needed - carried, Containers = containers };
                }
            }
            return null;
        }

        // Déplace ce qui manque des coffres vers l'inventaire du joueur.
        internal static void Pull(Player player, Ingredient ingredient)
        {
            var inventory = player.GetInventory();
            int missing = ingredient.Missing;
            foreach (var container in ingredient.Containers)
            {
                var stacks = container.GetInventory().GetAllItems()
                    .Where(i => i.m_shared.m_name == ingredient.Name && i.m_quality == ingredient.Quality && i.m_worldLevel >= Game.m_worldLevel)
                    .ToList();
                foreach (var stack in stacks)
                {
                    int take = Math.Min(stack.m_stack, missing);
                    if (!inventory.CanAddItem(stack, take))
                    {
                        player.Message(MessageHud.MessageType.Center, L.T("Inventory full", "Inventaire plein"));
                        return;
                    }
                    container.GetComponent<ZNetView>()?.ClaimOwnership();
                    var moved = stack.Clone();
                    moved.m_stack = take;
                    inventory.AddItem(moved);
                    container.GetInventory().RemoveItem(stack, take);
                    missing -= take;
                    if (missing <= 0) return;
                }
            }
        }
    }
}
