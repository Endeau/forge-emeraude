using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace EmeraudeCoffres
{
    // Portées imposées par le serveur. Craft From Containers lit sa portée (General.ContainerRange) chez
    // chaque joueur et ne la synchronise pas ; la portée de construction de l'établi en pierre est celle du
    // prefab, calculée chez chaque joueur. Le joueur demande les valeurs du serveur à son apparition ; le
    // serveur répond avec Server.ContainerRange et Server.StonecutterRangeMultiplier, que le joueur applique
    // en mémoire, sans toucher à son fichier, puis remet les siennes en quittant le monde.
    [HarmonyPatch(typeof(Game), "Start")]
    static class GameStartPatch
    {
        static void Postfix() => Range.Register();
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    static class OnSpawnedPatch
    {
        static void Postfix(Player __instance)
        {
            if (__instance == Player.m_localPlayer) Range.Ask();
        }
    }

    [HarmonyPatch(typeof(ZNet), "OnDestroy")]
    static class ZNetDestroyPatch
    {
        static void Postfix() => Range.Restore();
    }

    static class Range
    {
        const string AskRpc = "EmeraudeCoffres_AskRange";   // joueur -> serveur
        const string SetRpc = "EmeraudeCoffres_SetRange";   // serveur -> joueur (portée des coffres, multiplicateur de l'établi en pierre)
        const string Stonecutter = "piece_stonecutter";

        internal static ConfigEntry<float> ServerRange;
        internal static ConfigEntry<float> StonecutterMultiplier;
        static ConfigEntry<float> containerRange;
        static AccessTools.FieldRef<Vector3> lastPosition;
        static float? playerRange;
        static CraftingStation stonecutterPrefab;   // gardé : ZNetScene n'existe plus quand on quitte le monde
        static float? stonecutterRange;   // portée de construction du jeu de base, lue sur le prefab
        static bool stonecutterChanged;

        internal static void Bind(ConfigFile config)
        {
            ServerRange = config.Bind("Server", "ContainerRange", 15f,
                new ConfigDescription("Server only: chest range in meters sent to every player, replacing Craft From Containers' " +
                    "ContainerRange (10 by default) while they play here. 0 = each player keeps their own. / Serveur seulement : " +
                    "portée des coffres en mètres envoyée à chaque joueur, à la place du ContainerRange de Craft From Containers " +
                    "(10 par défaut) tant qu'il joue ici. 0 = chacun garde la sienne.",
                    new AcceptableValueRange<float>(0f, 100f)));
            StonecutterMultiplier = config.Bind("Server", "StonecutterRangeMultiplier", 1.5f,
                new ConfigDescription("Server only: multiplies the stonecutter's build range (and the no-spawn area around it) " +
                    "for every player. 1 = base game. / Serveur seulement : multiplie la portée de construction de l'établi en " +
                    "pierre (et la zone sans apparition de monstres autour) pour chaque joueur. 1 = jeu de base.",
                    new AcceptableValueRange<float>(1f, 5f)));
            var plugin = AccessTools.TypeByName("CraftFromContainers.BepInExPlugin");
            containerRange = (ConfigEntry<float>)AccessTools.Field(plugin, "maxRange").GetValue(null);
            lastPosition = AccessTools.StaticFieldRefAccess<Vector3>(AccessTools.Field(plugin, "lastPosition"));
        }

        internal static void Register()
        {
            ZRoutedRpc.instance.Register(AskRpc, OnAsk);
            ZRoutedRpc.instance.Register<float, float>(SetRpc, OnSet);
        }

        internal static void Ask() => ZRoutedRpc.instance?.InvokeRoutedRPC(AskRpc);

        static void OnAsk(long sender)
        {
            if (!ZNet.instance.IsServer()) return;
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, SetRpc, ServerRange.Value, StonecutterMultiplier.Value);
        }

        static void OnSet(long sender, float range, float stonecutterMultiplier)
        {
            if (!FromServer(sender)) return;
            if (range > 0f)
            {
                if (playerRange == null) playerRange = containerRange.Value;
                Apply(range);
                Plugin.Log.LogInfo($"Chest range set by the server: {range} m (own setting: {playerRange} m)");
            }
            if (stonecutterMultiplier > 1f && SetStonecutterRange(stonecutterMultiplier))
            {
                stonecutterChanged = true;
                Plugin.Log.LogInfo($"Stonecutter build range set by the server: {stonecutterRange * stonecutterMultiplier} m (base {stonecutterRange} m)");
            }
        }

        internal static void Restore()
        {
            if (playerRange != null) Apply(playerRange.Value);
            playerRange = null;
            if (stonecutterChanged) SetStonecutterRange(1f);
            stonecutterChanged = false;
        }

        // Le prefab, pour les établis qui apparaîtront, et ceux déjà chargés. Le jeu recalcule leur portée
        // (cercle affiché et zone sans monstres compris) à partir de m_rangeBuild toutes les 2 s.
        static bool SetStonecutterRange(float multiplier)
        {
            if (stonecutterPrefab == null) stonecutterPrefab = ZNetScene.instance?.GetPrefab(Stonecutter)?.GetComponent<CraftingStation>();
            var prefab = stonecutterPrefab;
            if (prefab == null) return false;
            if (stonecutterRange == null) stonecutterRange = prefab.m_rangeBuild;
            float range = stonecutterRange.Value * multiplier;
            prefab.m_rangeBuild = range;
            foreach (var station in Object.FindObjectsOfType<CraftingStation>())
                if (station.m_name == prefab.m_name) station.m_rangeBuild = range;
            return true;
        }

        // Change la valeur sans réécrire le fichier du joueur, et vide le cache de coffres de Craft From
        // Containers, qui ne se recalcule que lorsque le joueur bouge d'un demi-mètre.
        static void Apply(float range)
        {
            var file = containerRange.ConfigFile;
            bool save = file.SaveOnConfigSet;
            file.SaveOnConfigSet = false;
            containerRange.Value = range;
            file.SaveOnConfigSet = save;
            lastPosition() = new Vector3(1e9f, 1e9f, 1e9f);
        }

        static bool FromServer(long sender)
        {
            if (ZNet.instance.IsServer()) return sender == ZNet.GetUID();
            var server = ZNet.instance.GetServerPeer();
            return server != null && server.m_uid == sender;
        }
    }
}
