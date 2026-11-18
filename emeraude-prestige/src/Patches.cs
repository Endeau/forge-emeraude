// Branchement sur l'IA des monstres.
//
// BaseAI.IsEnemy sert à la fois au choix des cibles et à décider si un coup blesse (Attack, Projectile,
// Aoe) : on ne le fausse que pendant MonsterAI.UpdateTarget, sinon le joueur ne pourrait plus toucher
// un monstre qui l'ignore. Dans UpdateTarget, IsEnemy décide de la nouvelle cible (FindEnemy) et
// lâche la cible courante devenue « amie » ; un monstre qui lâche ainsi un joueur cesse d'être alerté,
// sinon il irait encore le chercher à sa dernière position.
//
// Les coups échangés (MonsterAI.OnDamaged, Character.Damage) provoquent le monstre : voir Prestige.
using System;
using HarmonyLib;

namespace EmeraudePrestige
{
    [HarmonyPatch(typeof(MonsterAI), "UpdateTarget")]
    static class UpdateTargetPatch
    {
        static readonly AccessTools.FieldRef<MonsterAI, Character> TargetCreature =
            AccessTools.FieldRefAccess<MonsterAI, Character>("m_targetCreature");
        static readonly AccessTools.FieldRef<BaseAI, Character> OwnCharacter =
            AccessTools.FieldRefAccess<BaseAI, Character>("m_character");
        static readonly Action<BaseAI, bool> SetAlerted =
            AccessTools.MethodDelegate<Action<BaseAI, bool>>(AccessTools.Method(typeof(BaseAI), "SetAlerted"));

        internal static MonsterAI s_ai;
        internal static Character s_monster;

        static void Prefix(MonsterAI __instance, out Character __state)
        {
            s_ai = __instance;
            s_monster = OwnCharacter(__instance);
            __state = TargetCreature(__instance);
        }

        static void Postfix(MonsterAI __instance, Character __state)
        {
            if (__state is Player && TargetCreature(__instance) == null && Prestige.Ignores(__instance, s_monster, __state))
                SetAlerted(__instance, false);
        }

        static void Finalizer()
        {
            s_ai = null;
            s_monster = null;
        }
    }

    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.IsEnemy), typeof(Character), typeof(Character))]
    static class IsEnemyPatch
    {
        static void Postfix(Character a, Character b, ref bool __result)
        {
            if (__result && UpdateTargetPatch.s_ai && a == UpdateTargetPatch.s_monster &&
                Prestige.Ignores(UpdateTargetPatch.s_ai, a, b))
                __result = false;
        }
    }

    // Le joueur frappe le monstre (chez le propriétaire du monstre).
    [HarmonyPatch(typeof(MonsterAI), "OnDamaged")]
    static class OnDamagedPatch
    {
        static void Prefix(MonsterAI __instance, Character attacker) =>
            Prestige.Provoke(__instance.GetComponent<Character>(), attacker);
    }

    // Le monstre frappe le joueur : appelé chez l'attaquant, donc chez le propriétaire du monstre.
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    static class DamagePatch
    {
        static void Prefix(Character __instance, HitData hit)
        {
            if (__instance is Player && hit.GetAttacker() is Character attacker && attacker.GetBaseAI() is MonsterAI)
                Prestige.Provoke(attacker, __instance);
        }
    }
}
