using System;
using System.Collections.Generic;
using System.Reflection;
using Gameplay.Units.Characters;
using Gameplay.Units.Virtual;
using Gameplay.Units.Player.Workers.WorkSystem;
using Gameplay.Units.Workers;
using Gameplay.Units.Workers.WorkSystem;
using HarmonyLib;
using Gameplay.Units;
using UnityEngine;

namespace IFZ.ProductionPlanner
{
    [HarmonyPatch(typeof(WorksPriorityManager), MethodType.Constructor, new[] { typeof(GameCustomization.GameCustomize) })]
    internal static class PriorityGroupsPatch
    {
        public const int GroupCount = 15;
        private static readonly FieldInfo GroupsField = AccessTools.Field(typeof(WorksPriorityManager), "_prioritiesWorks");

        private static void Postfix(WorksPriorityManager __instance)
        {
            var current = GroupsField.GetValue(__instance) as PriorityWorkGroup[];
            if (current == null || current.Length >= GroupCount) return;
            var groups = new PriorityWorkGroup[GroupCount];
            for (int i = 0; i < groups.Length; i++) groups[i] = i < current.Length ? current[i] : new PriorityWorkGroup();
            GroupsField.SetValue(__instance, groups);
        }
    }

    [HarmonyPatch(typeof(PriorityWorkGroup), nameof(PriorityWorkGroup.TryGetClosestWorker))]
    internal static class SkipLockedCandidatePatch
    {
        private static readonly FieldInfo WorksField = AccessTools.Field(typeof(PriorityWorkGroup), "_works");

        private static void Postfix(PriorityWorkGroup __instance, WorkBase relatedWork, ref Character candidate, ref bool __result)
        {
            if (!__result || candidate == null || !Crews.IsLocked(candidate)) return;
            var works = WorksField.GetValue(__instance) as List<WorkBase>;
            Vector3 position = relatedWork.GetPosition();
            Character best = null;
            float bestDistance = float.MaxValue;
            if (works != null)
            {
                foreach (var work in works)
                {
                    if (work == null || work.IsSuspended || work.IsPaused) continue;
                    foreach (var worker in work.Workers)
                    {
                        if (worker == null || Crews.IsLocked(worker)) continue;
                        float distance = work.EmploymentType() == ScriptableObjectScripts.UI.Employment.prof_guard
                            ? 999999f
                            : (position - worker.Position).sqrMagnitude;
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            best = worker;
                        }
                    }
                }
            }
            candidate = best;
            __result = best != null;
        }
    }

    [HarmonyPatch(typeof(WorkBase), nameof(WorkBase.GetClosestWorker))]
    internal static class PreferUnlockedWorkerPatch
    {
        private static void Postfix(WorkBase __instance, Vector3 position, ref Character __result)
        {
            if (__result == null) return;
            string job = Experience.JobId(__instance.GetRelatedStructure());
            Character best = null;
            var bestLevel = Level.Expert;
            float bestDistance = float.MaxValue;
            foreach (var worker in __instance.Workers)
            {
                if (worker == null || Crews.IsLocked(worker)) continue;
                var level = job != null ? Experience.SelectionLevel(worker, job) : Level.None;
                float distance = (worker.Position - position).sqrMagnitude;
                if (best == null || level < bestLevel || (level == bestLevel && distance < bestDistance))
                {
                    best = worker;
                    bestLevel = level;
                    bestDistance = distance;
                }
            }
            if (best != null) __result = best;
        }
    }

    [HarmonyPatch(typeof(WorkModule), nameof(WorkModule.ExecuteWork), new[] { typeof(float) })]
    internal static class ExperienceBoostPatch
    {
        private static readonly FieldInfo CharacterField = AccessTools.Field(typeof(WorkModule), "_character");

        private static void Prefix(WorkModule __instance, ref float timeSinceLastTick)
        {
            if (!Plugin.ExperienceEnabled) return;
            var character = CharacterField.GetValue(__instance) as Character;
            if (character == null) return;
            float factor = (1f + Experience.BoostFor(character)) * (1f + Experience.ForemanBoost(character.WorkModule.CurrentWork));
            if (factor > 1f) timeSinceLastTick *= factor;
        }
    }

    [HarmonyPatch(typeof(Gameplay.Rebuilding.Structure), nameof(Gameplay.Rebuilding.Structure.HasSpaceForCitizen))]
    internal static class HouseOffPatch
    {
        private static void Postfix(Gameplay.Rebuilding.Structure __instance, ref bool __result)
        {
            if (__result && Houses.IsOff(__instance)) __result = false;
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.FindBestHouse))]
    internal static class LockedHousePatch
    {
        private static bool Prefix(Character __instance)
        {
            var house = Houses.LockedHouse(__instance);
            if (house == null || Houses.IsOff(house)) return true;
            if (__instance.HouseData.House as Gameplay.Rebuilding.Structure != house)
                __instance.HouseData.House = house as Gameplay.Core.EnterableSystem.IEnterable;
            return false;
        }
    }

    [HarmonyPatch(typeof(Gameplay.Units.Skills.SkillsHandler), nameof(Gameplay.Units.Skills.SkillsHandler.GetSkillBonusValue))]
    internal static class SkillLevelPatch
    {
        private static void Postfix(Gameplay.Units.Skills.SkillsHandler __instance, Gameplay.Units.Skills.SkillId id, ref float __result)
        {
            if (__result != 0f && Plugin.CombatEnabled) __result *= Combat.SkillMultiplier(__instance, id);
        }
    }

    [HarmonyPatch(typeof(CharacterFightHandler), "GetDamage")]
    internal static class CombatDamagePatch
    {
        private static readonly FieldInfo CharacterField = AccessTools.Field(typeof(CharacterFightHandler), "_character");

        private static void Postfix(CharacterFightHandler __instance, ref float __result)
        {
            if (!Plugin.CombatEnabled) return;
            if (CharacterField.GetValue(__instance) is ICharacter c && Combat.TryGetBonus(c, out var bonus)) __result *= 1f + bonus.Damage;
        }
    }

    [HarmonyPatch(typeof(CharacterFightHandler), nameof(CharacterFightHandler.GetWeaponAttackReach))]
    internal static class CombatRangePatch
    {
        private static readonly FieldInfo CharacterField = AccessTools.Field(typeof(CharacterFightHandler), "_character");

        private static void Postfix(CharacterFightHandler __instance, ref float __result)
        {
            if (!Plugin.CombatEnabled) return;
            if (CharacterField.GetValue(__instance) is ICharacter c && Combat.TryGetBonus(c, out var bonus)) __result *= 1f + bonus.Range;
        }
    }

    [HarmonyPatch(typeof(CharacterFightHandler), "ResetAttackCooldown")]
    internal static class CombatFireRatePatch
    {
        private static readonly FieldInfo CharacterField = AccessTools.Field(typeof(CharacterFightHandler), "_character");
        private static readonly FieldInfo NextField = AccessTools.Field(typeof(CharacterFightHandler), "_nextAttackTime");
        private static readonly FieldInfo LastField = AccessTools.Field(typeof(CharacterFightHandler), "_lastTimeCooldownRefreshed");

        private static void Postfix(CharacterFightHandler __instance, Gameplay.Units.Equipment.Weapon weapon)
        {
            if (!Plugin.CombatEnabled || !(CharacterField.GetValue(__instance) is ICharacter c)) return;
            if (__instance.HaveEnemy()) Combat.OnShot(c, weapon != null && weapon.Stats != null ? weapon.Stats.AttackCooldownGts : 1f);
            if (!Combat.TryGetBonus(c, out var bonus) || bonus.FireRate <= 0f) return;
            float last = (float)LastField.GetValue(__instance);
            float next = (float)NextField.GetValue(__instance);
            NextField.SetValue(__instance, last + (next - last) / (1f + bonus.FireRate));
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.Kill))]
    internal static class SickDeathPatch
    {
        private static void Prefix(Character __instance, SubtractionHpReason reason) => Guards.OnCitizenKilled(__instance, reason);
    }

    [HarmonyPatch(typeof(Controllers.Sickness.SicknessController), "KillAndTurn")]
    internal static class SickTurnStartPatch
    {
        private static void Prefix() => Guards.BeginSicknessDeaths();
    }

    [HarmonyPatch(typeof(Controllers.Sickness.SicknessController), "SetCriticalSick")]
    internal static class SickCriticalStartPatch
    {
        private static void Prefix() => Guards.BeginSicknessDeaths();
    }

    [HarmonyPatch(typeof(Controllers.Sickness.SicknessController), "SpawnHorde")]
    internal static class GuardedTurnPatch
    {
        private static bool Prefix(ref int count)
        {
            count = Guards.AdjustHorde(count);
            return count > 0;
        }
    }

    internal static class Patches
    {
        public static void Apply(Harmony harmony)
        {
            foreach (var type in new[] { typeof(PriorityGroupsPatch), typeof(SkipLockedCandidatePatch), typeof(PreferUnlockedWorkerPatch), typeof(ExperienceBoostPatch), typeof(HouseOffPatch), typeof(LockedHousePatch), typeof(SkillLevelPatch), typeof(CombatDamagePatch), typeof(CombatRangePatch), typeof(CombatFireRatePatch), typeof(SickDeathPatch), typeof(SickTurnStartPatch), typeof(SickCriticalStartPatch), typeof(GuardedTurnPatch) })
            {
                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Patch {type.Name} failed: {e}");
                }
            }
        }
    }
}
