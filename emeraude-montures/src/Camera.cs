using HarmonyLib;
using UnityEngine;

namespace EmeraudeMontures
{
    // En selle, le jeu place la caméra sur la position brute du joueur, que Mount Up colle chaque
    // image à un os du dos de la monture : elle suit chaque foulée. On la place sur le corps de la
    // monture, plus la position du cavalier par rapport à lui, lissée.
    [HarmonyPatch(typeof(GameCamera), "GetOffsetedEyePos")]
    static class EyePosPatch
    {
        static void Postfix(ref Vector3 __result)
        {
            var player = Player.m_localPlayer;
            if (Riding.TryGetMount(player, out var mount)) __result += Riding.Seat(player, mount) - player.transform.position;
        }
    }

    // La caméra penche avec le cavalier, donc avec l'os du dos : on la garde droite.
    [HarmonyPatch(typeof(GameCamera), "ApplyCameraTilt")]
    static class CameraTiltPatch
    {
        static bool Prefix(Player player, ref Quaternion rot, ref Vector3 ___m_smoothedCameraUp)
        {
            if (!Riding.TryGetMount(player, out _)) return true;
            ___m_smoothedCameraUp = Vector3.up;
            rot = Quaternion.LookRotation(player.m_eye.forward, Vector3.up);
            return false;
        }
    }

    static class Riding
    {
        static Character s_mount;
        static Vector3 s_seat;
        static Vector3 s_seatVelocity;

        internal static bool TryGetMount(Player player, out Character mount)
        {
            mount = player != null && player.IsAttached() && player.GetDoodadController() is Sadle sadle ? sadle.GetCharacter() : null;
            return mount != null;
        }

        // Position du cavalier dans le repère de la monture, pour que ses virages n'ajoutent pas de retard
        internal static Vector3 Seat(Player player, Character mount)
        {
            var seat = mount.transform.InverseTransformPoint(player.transform.position);
            if (mount != s_mount)
            {
                s_mount = mount;
                s_seat = seat;
                s_seatVelocity = Vector3.zero;
            }
            s_seat = Vector3.SmoothDamp(s_seat, seat, ref s_seatVelocity, Plugin.CameraSmoothing.Value, 999f, Time.deltaTime);
            return mount.transform.TransformPoint(s_seat);
        }
    }
}
