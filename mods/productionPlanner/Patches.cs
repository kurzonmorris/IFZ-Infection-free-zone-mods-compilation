using System;
using System.Collections.Generic;
using System.Reflection;
using Gameplay.Units.Characters;
using Gameplay.Units.Player.Workers.WorkSystem;
using Gameplay.Units.Workers.WorkSystem;
using HarmonyLib;
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
            if (__result == null || !Crews.IsLocked(__result)) return;
            Character best = null;
            float bestDistance = float.MaxValue;
            foreach (var worker in __instance.Workers)
            {
                if (worker == null || Crews.IsLocked(worker)) continue;
                float distance = (worker.Position - position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = worker;
                }
            }
            if (best != null) __result = best;
        }
    }

    internal static class Patches
    {
        public static void Apply(Harmony harmony)
        {
            foreach (var type in new[] { typeof(PriorityGroupsPatch), typeof(SkipLockedCandidatePatch), typeof(PreferUnlockedWorkerPatch) })
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
