// Emeraude Montures — réglages des montures de Mount Up Restored.
//
// Montures : le loup, le sanglier et le lox se montent, pas l'asksvin (que le jeu de base permet).
// PV : un loup ou un sanglier apprivoisé (pas le lox) prend les PV max de son cavalier (jamais moins que les
//      siens), en gardant son pourcentage de vie. La valeur reste enregistrée sur la bête.
// Caméra : en selle, elle suit le corps de la monture, lissé, au lieu de l'os du dos auquel Mount Up
//          attache le joueur, et elle ne penche plus.
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace EmeraudeMontures
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "emeraude.montures";
        public const string Name = "Emeraude Montures";
        public const string Version = "1.1.0";

        internal static ConfigEntry<float> CameraSmoothing;

        void Awake()
        {
            CameraSmoothing = Config.Bind("Camera", "CameraSmoothing", 1f,
                new ConfigDescription("While riding, how long the camera takes to follow the rider's bobbing on the mount, in seconds. " +
                    "Higher is steadier.", new AcceptableValueRange<float>(0.1f, 5f)));

            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo($"{Name} {Version} loaded");
        }
    }
}
