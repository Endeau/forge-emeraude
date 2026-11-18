// Emeraude Prestige — les monstres ignorent un joueur dont l'armure a de l'avance sur leur biome.
//
// Chaque armure et chaque monstre a le niveau de son biome (1 Prairies … 8 Grand Nord). Un monstre
// ignore un joueur si le niveau de l'armure atteint celui du monstre + Gap. Le niveau du joueur est
// celui de sa pièce la plus faible parmi casque, plastron et jambières : une pièce absente ou hors
// des listes (chapeau, serre-tête) vaut 0. Un monstre absent des listes attaque toujours.
//
// Prestige.cs décide, Patches.cs branche la décision sur l'IA des monstres.
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;

namespace EmeraudePrestige
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    [SynchronizationMode(AdminOnlyStrictness.IfOnServer)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "emeraude.prestige";
        public const string Name = "Emeraude Prestige";
        public const string Version = "1.0.1";

        internal const int TierCount = 8;
        static readonly string[] Biomes =
        {
            "Meadows / Prairies", "Black Forest / Forêt Noire", "Swamp / Marais", "Mountains / Montagnes",
            "Plains / Plaines", "Mistlands / Brumes", "Ashlands / Terres cendrées", "Deep North / Grand Nord",
        };

        static readonly string[] DefaultArmor =
        {
            "ArmorRagsChest,ArmorRagsLegs,HelmetLeather,ArmorLeatherChest,ArmorLeatherLegs",
            "HelmetTrollLeather,ArmorTrollLeatherChest,ArmorTrollLeatherLegs,HelmetBronze,ArmorBronzeChest,ArmorBronzeLegs," +
                "HelmetBerserkerHood,ArmorBerserkerChest,ArmorBerserkerLegs",
            "HelmetIron,ArmorIronChest,ArmorIronLegs,HelmetRoot,ArmorRootChest,ArmorRootLegs",
            "HelmetDrake,ArmorWolfChest,ArmorWolfLegs,HelmetFenring,ArmorFenringChest,ArmorFenringLegs",
            "HelmetPadded,ArmorPaddedCuirass,ArmorPaddedGreaves,HelmetLox,ArmorLoxChest,ArmorLoxLegs," +
                "HelmetBerserkerUndead,ArmorBerserkerUndeadChest,ArmorBerserkerUndeadLegs",
            "HelmetCarapace,ArmorCarapaceChest,ArmorCarapaceLegs,HelmetMage,ArmorMageChest,ArmorMageLegs",
            "HelmetFlametal,ArmorFlametalChest,ArmorFlametalLegs,HelmetAshlandsMediumHood,ArmorAshlandsMediumChest," +
                "ArmorAshlandsMediumlegs,HelmetMage_Ashlands,ArmorMageChest_Ashlands,ArmorMageLegs_Ashlands",
            "HelmetDNHeavy,ArmorDeepNorthHeavyChest,ArmorDeepNorthHeavylegs,HelmetDNMediumHood,ArmorDeepNorthMediumChest," +
                "ArmorDeepNorthMediumlegs,HelmetDNMage,ArmorDeepNorthMageChest,ArmorDeepNorthMagelegs",
        };

        // Les variantes « _sleeping » (monstres endormis posés dans les lieux) ont le niveau de leur espèce.
        // Les Nains gris (Greydwarf) sont classés Prairies à la demande des joueurs : ils ignorent dès le
        // Bronze. Leur Brute (Greydwarf_Elite) et leur Chaman restent en Forêt Noire.
        static readonly string[] DefaultMonsters =
        {
            "Greyling,Neck,Greydwarf",
            "Greydwarf_Elite,Greydwarf_Shaman,Troll,Troll_sleeping,Ghost,Ghost_sleeping,Bjorn,Bjorn_sleeping,Skeleton,Skeleton_NoArcher,Skeleton_Poison,Skeleton_Meadows," +
                "Skeleton_Meadows_noarcher,Skeleton_Swamps,Skeleton_Swamps_noarcher,Skeleton_Mountains,Skeleton_Mountains_noarcher",
            "Draugr,Draugr_sleeping,Draugr_Elite,Draugr_Elite_sleeping,Draugr_Ranged,Draugr_Ranged_sleeping,Blob,BlobElite,Leech,Surtling,Wraith,Abomination,Bat_Swamp",
            "Wolf,Hatchling,StoneGolem,Fenring,Fenring_Cultist,Ulv,Bat",
            "Deathsquito,Goblin,GoblinArcher,GoblinBrute,GoblinShaman,BlobTar,Unbjorn,Lox",
            "Seeker,SeekerBrute,SeekerBrood,Gjall,Tick",
            "Charred_Melee,Charred_Archer,Charred_Mage,Charred_Twitcher,Morgen,Morgen_NonSleeping,FallenValkyrie,Asksvin,Volture,BlobLava",
            "",
        };

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<int> Gap;
        internal static ConfigEntry<float> ForgetSeconds;
        internal static ConfigEntry<bool> StarredStayAggressive;
        internal static ConfigEntry<bool> RaidsStayAggressive;
        internal static readonly ConfigEntry<string>[] ArmorTiers = new ConfigEntry<string>[TierCount];
        internal static readonly ConfigEntry<string>[] MonsterTiers = new ConfigEntry<string>[TierCount];

        internal static BepInEx.Logging.ManualLogSource Log;

        void Awake()
        {
            Log = Logger;
            var adminOnly = new ConfigurationManagerAttributes { IsAdminOnly = true };
            Enabled = Config.Bind("Prestige", "Enabled", true,
                new ConfigDescription("Monsters ignore players whose armor is ahead of their biome. / Les monstres ignorent " +
                    "les joueurs dont l'armure a de l'avance sur leur biome.", null, adminOnly));
            Gap = Config.Bind("Prestige", "Gap", 1,
                new ConfigDescription("Biomes of advance needed: a monster ignores a player whose armor tier is at least " +
                    "the monster's tier + Gap. With 1, Iron armor (Swamp) makes Black Forest monsters ignore you. / Biomes " +
                    "d'avance nécessaires : un monstre ignore un joueur dont l'armure atteint le niveau du monstre + Gap. " +
                    "Avec 1, l'armure de Fer (Marais) fait ignorer les monstres de la Forêt Noire.",
                    new AcceptableValueRange<int>(0, TierCount), adminOnly));
            ForgetSeconds = Config.Bind("Prestige", "ForgetSeconds", 30f,
                new ConfigDescription("A monster hit by a player fights back, and forgets them after this many seconds " +
                    "without a blow either way. / Un monstre frappé par un joueur riposte, puis l'oublie après ce nombre de " +
                    "secondes sans coup échangé.", new AcceptableValueRange<float>(5f, 600f), adminOnly));
            StarredStayAggressive = Config.Bind("Prestige", "StarredStayAggressive", true,
                new ConfigDescription("Starred monsters always attack. / Les monstres étoilés attaquent toujours.",
                    null, adminOnly));
            RaidsStayAggressive = Config.Bind("Prestige", "RaidsStayAggressive", true,
                new ConfigDescription("Raid monsters always attack. / Les monstres des raids attaquent toujours.",
                    null, adminOnly));

            for (int i = 0; i < TierCount; i++)
            {
                ArmorTiers[i] = Config.Bind("Armor", $"Tier{i + 1}", DefaultArmor[i],
                    new ConfigDescription($"Armor pieces of tier {i + 1}, {Biomes[i]} (item names, comma separated). / " +
                        $"Pièces d'armure de niveau {i + 1} (noms d'objets, séparés par des virgules).", null, adminOnly));
                MonsterTiers[i] = Config.Bind("Monsters", $"Tier{i + 1}", DefaultMonsters[i],
                    new ConfigDescription($"Monsters of tier {i + 1}, {Biomes[i]} (prefab names, comma separated). Unlisted " +
                        $"monsters always attack. / Monstres de niveau {i + 1} (noms de prefab, séparés par des virgules). " +
                        "Un monstre absent des listes attaque toujours.", null, adminOnly));
                ArmorTiers[i].SettingChanged += (_, __) => Prestige.Rebuild();
                MonsterTiers[i].SettingChanged += (_, __) => Prestige.Rebuild();
            }
            Prestige.Rebuild();

            ItemManager.OnItemsRegistered += Prestige.CheckArmorNames;
            PrefabManager.OnPrefabsRegistered += Prestige.CheckMonsterNames;
            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo($"{Name} {Version} loaded");
        }
    }
}
