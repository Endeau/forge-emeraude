using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmeraudeCoffres
{
    // Deux boutons à gauche de « Fabriquer » : « x10 » et « Tout (N) ». Maj + Fabriquer garde les 5 du jeu.
    //
    // Le jeu sait déjà fabriquer m_multiCraftAmount exemplaires d'un coup : OnCraftPressed lance une
    // fabrication multiple si Maj est tenue ou si m_touchMultiCrafting est levé (bouton tactile), et
    // DoCrafting relit m_multiCraftAmount à la fin de la barre de progression. Un bouton lève
    // m_touchMultiCrafting et note son nombre ; le prefix d'OnCraftPressed le pose dans
    // m_multiCraftAmount, ou remet celui du jeu pour un clic normal.
    [HarmonyPatch(typeof(InventoryGui), "Awake")]
    static class AwakePatch
    {
        static void Postfix(InventoryGui __instance) => MultiCraft.CreateButtons(__instance);
    }

    [HarmonyPatch(typeof(InventoryGui), "UpdateRecipe")]
    static class UpdateRecipePatch
    {
        static void Postfix(InventoryGui __instance, Player player) => MultiCraft.UpdateButtons(__instance, player);
    }

    [HarmonyPatch(typeof(InventoryGui), "OnCraftPressed")]
    static class OnCraftPressedPatch
    {
        static void Prefix(InventoryGui __instance) => MultiCraft.ApplyPending(__instance);
    }

    static class MultiCraft
    {
        const int Batch = 10;
        const int Limit = 999;
        // UpdateRecipe tourne à chaque image : le calcul « autant que possible » est gardé un instant.
        const float CacheSeconds = 0.5f;
        static Recipe cachedRecipe;
        static float cachedTime;
        static int cachedMax;

        static int vanillaAmount = 5;
        static int pendingAmount;
        static Button batchButton, allButton;

        // m_selectedRecipe est d'un type privé du jeu (RecipeDataPair) : lu par réflexion.
        static Traverse SelectedPair(InventoryGui gui) => Traverse.Create(gui).Field("m_selectedRecipe");
        static Recipe Selected(InventoryGui gui) => SelectedPair(gui).Property("Recipe").GetValue<Recipe>();

        // Sur la ligne de « Fabriquer » : x10 et Tout se partagent la moitié gauche, « Fabriquer » garde la
        // droite. Au-dessus, ils cachaient la liste des ingrédients.
        const float Gap = 4f;
        const float BatchShare = 0.22f, AllShare = 0.28f;
        static Vector2 craftCenter;
        static float craftWidth;

        internal static void CreateButtons(InventoryGui gui)
        {
            vanillaAmount = gui.m_multiCraftAmount;
            var craft = (RectTransform)gui.m_craftButton.transform;
            craftWidth = craft.rect.width;
            craftCenter = (Vector2)craft.localPosition + Vector2.Scale(Half - craft.pivot, craft.rect.size);
            float left = craftCenter.x - craftWidth / 2f;
            batchButton = Clone(craft, "EmeraudeCraftBatch", () => Craft(gui, Batch));
            Place((RectTransform)batchButton.transform, left, craftWidth * BatchShare - Gap);
            allButton = Clone(craft, "EmeraudeCraftAll", () => Craft(gui, Max(Player.m_localPlayer, Selected(gui))));
            Place((RectTransform)allButton.transform, left + craftWidth * BatchShare, craftWidth * AllShare - Gap);
            AutoSize(gui.m_craftButton);
        }

        static readonly Vector2 Half = new Vector2(0.5f, 0.5f);

        // Copie du bouton « Fabriquer » : même style, sans son raccourci manette ni ses actions.
        static Button Clone(RectTransform craft, string name, UnityEngine.Events.UnityAction onClick)
        {
            var copy = Object.Instantiate(craft.gameObject, craft.parent);
            copy.name = name;
            foreach (var pad in copy.GetComponentsInChildren<UIGamePad>(true)) Object.Destroy(pad);
            var tooltip = copy.GetComponent<UITooltip>();
            if (tooltip) tooltip.m_text = "";
            var button = copy.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(onClick);
            AutoSize(button);
            copy.SetActive(false);
            return button;
        }

        // Le texte rapetisse s'il ne tient pas dans un bouton plus étroit (« Fabriquer x 5 »).
        static void AutoSize(Button button)
        {
            var text = button.GetComponentInChildren<TMP_Text>(true);
            if (!text || text.enableAutoSizing) return;
            text.fontSizeMax = text.fontSize;
            text.fontSizeMin = text.fontSize * 0.5f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true;
        }

        // Pose le bouton à partir du bord gauche left, à la hauteur de « Fabriquer », quel que soit le pivot.
        static void Place(RectTransform rect, float left, float width)
        {
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            var center = new Vector2(left + width / 2f, craftCenter.y);
            rect.localPosition = center - Vector2.Scale(Half - rect.pivot, rect.rect.size);
        }

        internal static void UpdateButtons(InventoryGui gui, Player player)
        {
            if (!batchButton || !allButton) return;
            var pair = SelectedPair(gui);
            var recipe = pair.Property("Recipe").GetValue<Recipe>();
            // Ni pendant une fabrication (bouton « Fabriquer » caché), ni pour une amélioration.
            bool show = recipe && gui.m_craftButton.gameObject.activeSelf
                && pair.Property("ItemData").GetValue<ItemDrop.ItemData>() == null;
            batchButton.gameObject.SetActive(show);
            allButton.gameObject.SetActive(show);
            // « Fabriquer » reprend toute la ligne quand les boutons sont cachés (améliorations).
            float shared = craftWidth * (BatchShare + AllShare);
            if (show) Place((RectTransform)gui.m_craftButton.transform, craftCenter.x - craftWidth / 2f + shared, craftWidth - shared);
            else Place((RectTransform)gui.m_craftButton.transform, craftCenter.x - craftWidth / 2f, craftWidth);
            if (!show) return;

            // Le bouton du jeu tient compte de la station et du niveau de qualité ; on ajoute la quantité.
            int max = Max(player, recipe);
            bool usable = gui.m_craftButton.interactable;
            batchButton.interactable = usable && max >= Batch;
            allButton.interactable = usable && max >= 1;
            batchButton.GetComponentInChildren<TMP_Text>().text = "x" + Batch;
            allButton.GetComponentInChildren<TMP_Text>().text = $"{L.T("All", "Tout")} ({max})";
        }

        static void Craft(InventoryGui gui, int amount)
        {
            if (amount < 1) return;
            pendingAmount = amount;
            Traverse.Create(gui).Field("m_touchMultiCrafting").SetValue(true);
            Traverse.Create(gui).Method("OnCraftPressed").GetValue();
        }

        internal static void ApplyPending(InventoryGui gui)
        {
            gui.m_multiCraftAmount = pendingAmount > 0 ? pendingAmount : vanillaAmount;
            pendingAmount = 0;
        }

        // Plus grand nombre de fabrications permis par les ingrédients (coffres compris, via Craft From
        // Containers et Emeraude Coffres) et la place dans l'inventaire ; 0 si aucune.
        static int Max(Player player, Recipe recipe)
        {
            if (player == null || recipe == null) return 0;
            if (recipe == cachedRecipe && Time.time - cachedTime < CacheSeconds) return cachedMax;
            cachedRecipe = recipe;
            cachedTime = Time.time;
            cachedMax = Search(player, recipe);
            return cachedMax;
        }

        // Doublement puis dichotomie, jusqu'à Limit.
        static int Search(Player player, Recipe recipe)
        {
            if (!Possible(player, recipe, 1)) return 0;
            int ok = 1, ko = 2;
            while (ko <= Limit && Possible(player, recipe, ko)) { ok = ko; ko *= 2; }
            if (ko > Limit) { if (Possible(player, recipe, Limit)) return Limit; ko = Limit; }
            while (ko - ok > 1)
            {
                int mid = (ok + ko) / 2;
                if (Possible(player, recipe, mid)) ok = mid; else ko = mid;
            }
            return ok;
        }

        static bool Possible(Player player, Recipe recipe, int amount) =>
            player.HaveRequirements(recipe, false, 1, amount)
            && player.GetInventory().CanAddItem(recipe.m_item.gameObject, recipe.m_amount * amount);
    }
}
