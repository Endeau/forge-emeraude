// Emeraude Sanctuaire — une statue à poser dans le village : aucun monstre n'apparaît dans son
// rayon, les monstres qui s'en approchent fuient, et les raids ne visent plus un joueur qui s'y trouve.
//
// La statue reprend le modèle de la pierre de garde (guard_stone), sans sa protection des constructions.
// Sanctuary.cs pose la zone anti-monstres, Raids.cs écarte les raids.
using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace EmeraudeSanctuaire
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    [SynchronizationMode(AdminOnlyStrictness.IfOnServer)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "emeraude.sanctuaire";
        public const string Name = "Emeraude Sanctuaire";
        public const string Version = "1.1.0";

        static readonly Color Emerald = new Color(0.25f, 0.9f, 0.55f);

        internal static ConfigEntry<float> Radius;
        internal static ConfigEntry<bool> RepelMonsters;
        internal static ConfigEntry<bool> BlockRaids;
        internal static ConfigEntry<string> Recipe;

        void Awake()
        {
            var adminOnly = new ConfigurationManagerAttributes { IsAdminOnly = true };
            Radius = Config.Bind("Sanctuary", "Radius", 32f,
                new ConfigDescription("Radius of a new statue in meters: no monster spawns inside. Its builder then changes it " +
                    "with Shift + E (16, 24, 32, 48, 64 m). / Rayon d'une nouvelle statue en mètres : aucun monstre n'y apparaît. " +
                    "Son constructeur le change ensuite avec Maj + E (16, 24, 32, 48, 64 m).",
                    new AcceptableValueRange<float>(8f, 64f), adminOnly));
            RepelMonsters = Config.Bind("Sanctuary", "RepelMonsters", true,
                new ConfigDescription("Monsters flee from the statue's area, like around the trader, and stop chasing a player " +
                    "inside it. Tamed creatures are not affected. / Les monstres fuient la zone de la statue, comme autour du " +
                    "marchand, et cessent de poursuivre un joueur qui s'y trouve. Les créatures apprivoisées ne sont pas concernées.",
                    null, adminOnly));
            BlockRaids = Config.Bind("Sanctuary", "BlockRaids", true,
                new ConfigDescription("No raid starts on a player standing inside a statue's area. / Aucun raid ne démarre " +
                    "sur un joueur qui se trouve dans la zone d'une statue.",
                    null, adminOnly));
            Recipe = Config.Bind("Sanctuary", "Recipe", "Stone:20,GreydwarfEye:10,SurtlingCore:2",
                new ConfigDescription("Cost of the statue (item:amount, comma separated), built with the hammer near a " +
                    "workbench. Requires a restart. / Coût de la statue (objet:quantité, séparés par des virgules), construite " +
                    "au marteau près d'un établi. Redémarrage requis.",
                    null, adminOnly));

            AddTranslations();
            PrefabManager.OnVanillaPrefabsAvailable += AddStatue;
            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo($"{Name} {Version} loaded");
        }

        void AddTranslations()
        {
            var localization = LocalizationManager.Instance.GetLocalization();
            localization.AddTranslation("English", new Dictionary<string, string>
            {
                ["emeraude_sanctuaire"] = "Emerald Statue",
                ["emeraude_sanctuaire_desc"] = "Monsters don't spawn around it and flee its area.",
                ["emeraude_sanctuaire_radius"] = "Radius: $1 m",
                ["emeraude_sanctuaire_change_radius"] = "Change the radius",
            });
            localization.AddTranslation("French", new Dictionary<string, string>
            {
                ["emeraude_sanctuaire"] = "Statue d'Émeraude",
                ["emeraude_sanctuaire_desc"] = "Aucun monstre n'apparaît autour d'elle et ils fuient sa zone.",
                ["emeraude_sanctuaire_radius"] = "Rayon : $1 m",
                ["emeraude_sanctuaire_change_radius"] = "Changer le rayon",
            });
        }

        void AddStatue()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= AddStatue;
            var config = new PieceConfig
            {
                Name = "$emeraude_sanctuaire",
                Description = "$emeraude_sanctuaire_desc",
                PieceTable = PieceTables.Hammer,
                CraftingStation = CraftingStations.Workbench,
                Category = PieceCategories.Misc,
            };
            foreach (var part in Recipe.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var itemAmount = part.Split(':');
                if (itemAmount.Length == 2 && int.TryParse(itemAmount[1].Trim(), out int amount))
                    config.AddRequirement(itemAmount[0].Trim(), amount);
                else
                    Logger.LogWarning($"Recipe : « {part} » ignoré, format attendu objet:quantité");
            }

            var piece = new CustomPiece(Sanctuary.PrefabName, "guard_stone", config);
            var prefab = piece.PiecePrefab;
            // La pierre de garde bloquait coffres et portes une fois activée : on n'en garde que le modèle,
            // la lueur et le cercle du rayon, repris par Sanctuary.
            var ward = prefab.GetComponent<PrivateArea>();
            var sanctuary = prefab.AddComponent<Sanctuary>();
            sanctuary.m_areaMarker = ward.m_areaMarker;
            sanctuary.m_model = ward.m_model;
            sanctuary.m_hoverOffset = ward.m_hoverOffset;
            if (ward.m_enabledEffect) ward.m_enabledEffect.SetActive(true);
            if (ward.m_inRangeEffect) ward.m_inRangeEffect.SetActive(false);
            DestroyImmediate(ward);
            Tint(prefab);
            // Lueur toujours allumée, comme une pierre de garde active.
            foreach (var material in sanctuary.m_model.sharedMaterials)
                if (material) material.EnableKeyword("_EMISSION");
            PieceManager.Instance.AddPiece(piece);
        }

        // Teinte la copie en vert pour la distinguer d'une pierre de garde. Les matériaux sont copiés :
        // ceux de la copie sont encore partagés avec la vraie pierre de garde.
        static void Tint(GameObject prefab)
        {
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (!materials[i]) continue;
                    var material = new Material(materials[i]);
                    if (material.HasProperty("_Color")) material.color *= Emerald;
                    // Même intensité de lueur que la pierre de garde, seule la teinte change.
                    if (material.HasProperty("_EmissionColor"))
                        material.SetColor("_EmissionColor", Emerald * material.GetColor("_EmissionColor").maxColorComponent);
                    materials[i] = material;
                }
                renderer.sharedMaterials = materials;
            }
            foreach (var light in prefab.GetComponentsInChildren<Light>(true))
                light.color = Emerald;
        }
    }
}
