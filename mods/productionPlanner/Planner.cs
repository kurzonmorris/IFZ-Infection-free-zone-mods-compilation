using System;
using System.Collections.Generic;
using System.Reflection;
using Controllers;
using Controllers.CharacterLogic;
using Controllers.Time;
using Data.Resorces;
using Game;
using Gameplay.GameResources;
using Gameplay.Laws;
using Gameplay.Production;
using Gameplay.Rebuilding;
using Gameplay.Units.Characters;
using Gameplay.Units.Workers;
using UnityEngine;
using Zenject;

namespace IFZ.ProductionPlanner
{
    internal sealed class InputLine
    {
        public string Name;
        public float PerDay;
        public float InStock;
    }

    internal sealed class PlanResult
    {
        public string OutputName;
        public float OutputPerCycle;
        public float CycleHours;
        public float WorkHoursPerDay;
        public float MoodFactor;
        public bool MoodFromWorkers;
        public float WeatherFactor;
        public float ExperienceFactor;
        public float HaulingFactor;
        public float PerWorkerPerDay;
        public int RequiredWorkers;
        public int SlotLimit;
        public int CurrentMax;
        public int MaxDayProduction;
        public float ReachablePerDay;
        public float VolumeForWorkers;
        public float VolumeForDailyCap;
        public float BuildingVolume;
        public int ProducedToday;
        public bool ProducedFullDay;
        public readonly List<InputLine> Inputs = new List<InputLine>();
    }

    internal static class Planner
    {
        private static readonly FieldInfo LawsField = typeof(Work).GetField("_lawsController", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo WorkersConfigField = typeof(Work).GetField("_workersConfig", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo StockroomsField = typeof(Work).GetField("_stockroomsController", BindingFlags.Instance | BindingFlags.NonPublic);

        private static InfoPanelController _infoPanels;
        private sealed class DayCount
        {
            public int Day;
            public int Start;
            public bool FullDay;
        }

        private static readonly Dictionary<ProductionWork, DayCount> DayCounts = new Dictionary<ProductionWork, DayCount>();

        public static void TrackDay(ProductionWork work)
        {
            int day = TimeController.Day;
            int now = work.DailyProducedAmount;
            if (!DayCounts.TryGetValue(work, out var count))
            {
                DayCounts[work] = new DayCount { Day = day, Start = now, FullDay = false };
                return;
            }
            if (count.Day != day)
            {
                count.Day = day;
                count.Start = now;
                count.FullDay = true;
            }
            else if (now < count.Start)
            {
                count.Start = now;
            }
        }

        public static int ProducedToday(ProductionWork work, out bool fullDay)
        {
            TrackDay(work);
            var count = DayCounts[work];
            fullDay = count.FullDay;
            return work.DailyProducedAmount - count.Start;
        }

        public static Structure SelectedStructure()
        {
            if (_infoPanels == null) _infoPanels = UnityEngine.Object.FindAnyObjectByType<InfoPanelController>();
            if (_infoPanels == null) return null;
            var panel = _infoPanels.CurrentDisplayedPanel;
            return panel != null ? panel.Structure : null;
        }

        private static SceneContext _context;

        public static T Resolve<T>() where T : class
        {
            if (_context == null) _context = UnityEngine.Object.FindAnyObjectByType<SceneContext>();
            if (_context == null) return null;
            return _context.Container?.TryResolve<T>();
        }

        public static float WorkHoursPerDay(Work work)
        {
            var config = WorkersConfigField?.GetValue(work) as WorkersConfig;
            var laws = LawsField?.GetValue(work) as LawsController;
            if (config == null) return 24f;
            float start = TimeController.SunriseHour + config.WorkStartHourAfterSunrise + (laws?.GetWorkStartingHourModifier() ?? 0f);
            float end = TimeController.SunsetHour - config.WorkEndHourBeforeSunset + (laws?.GetWorkEndingHourModifier() ?? 0f);
            float hours = end - start;
            if (hours < 0f) hours += 24f;
            return Mathf.Clamp(hours, 0.1f, 24f);
        }

        public static float MoodFactor(ProductionWork work, out bool fromWorkers)
        {
            fromWorkers = false;
            var workers = work.Workers;
            if (workers == null || workers.Count == 0) return 1f;
            float sum = 0f;
            int count = 0;
            foreach (Character worker in workers)
            {
                if (worker == null) continue;
                sum += 1f / (2f - worker.WorkerEfficiencyModifier);
                count++;
            }
            if (count == 0) return 1f;
            fromWorkers = true;
            return sum / count;
        }

        public static float ExperienceFactor(ProductionWork work)
        {
            var workers = work.Workers;
            if (workers == null || workers.Count == 0) return 1f;
            float sum = 0f;
            int count = 0;
            foreach (Character worker in workers)
            {
                if (worker == null) continue;
                sum += 1f + Experience.BoostFor(worker);
                count++;
            }
            return (count == 0 ? 1f : sum / count) * (1f + Experience.ForemanBoost(work));
        }

        public static PlanResult Calculate(ProductionWork work, float goalPerDay, Settings settings)
        {
            var data = work.ProductionData;
            if (data == null) return null;
            var profits = data.GetProfitPairs();
            if (profits.Count == 0) return null;

            var result = new PlanResult();
            result.OutputName = SafeName(profits[0].Key);
            result.OutputPerCycle = profits[0].Value;
            float cycleGts = Mathf.Max(0.001f, data.GetProductionTime());
            float hourGts = Mathf.Max(1, TimeController.HourLengthInGts);
            result.CycleHours = cycleGts / hourGts;
            result.WorkHoursPerDay = settings.UseWorkHours ? WorkHoursPerDay(work) : 24f;
            result.MoodFactor = settings.UseMood ? MoodFactor(work, out result.MoodFromWorkers) : 1f;
            result.WeatherFactor = settings.UseWeather ? Mathf.Max(0f, work.GetCurrentTemperatureEfficiency()) : 1f;
            result.ExperienceFactor = settings.UseExperience ? ExperienceFactor(work) : 1f;
            result.HaulingFactor = settings.UseHauling ? Mathf.Clamp01(1f - settings.HaulingPercent / 100f) : 1f;

            float producingGtsPerWorker = result.WorkHoursPerDay * hourGts * result.MoodFactor * result.WeatherFactor * result.ExperienceFactor * result.HaulingFactor;
            result.PerWorkerPerDay = producingGtsPerWorker / cycleGts * result.OutputPerCycle;
            result.RequiredWorkers = result.PerWorkerPerDay > 0f && goalPerDay > 0f
                ? Mathf.CeilToInt(goalPerDay / result.PerWorkerPerDay)
                : 0;

            result.SlotLimit = work.InitialMaxWorkers;
            result.CurrentMax = work.MaxWorkers;
            result.MaxDayProduction = work.MaxDayProduction;
            result.ProducedToday = ProducedToday(work, out result.ProducedFullDay);
            result.BuildingVolume = work.Structure.Volume;

            float reachable = result.PerWorkerPerDay * result.SlotLimit;
            if (result.MaxDayProduction > 0) reachable = Mathf.Min(reachable, result.MaxDayProduction);
            result.ReachablePerDay = reachable;

            float workersPer100 = work.Structure.Draft.HasWorkersModule ? work.Structure.Draft.MaxWorkers : 0f;
            result.VolumeForWorkers = workersPer100 > 0f ? result.RequiredWorkers / (workersPer100 * 0.01f) : 0f;
            result.VolumeForDailyCap = data.maxDayProductionPer100m3 > 0f ? goalPerDay / (data.maxDayProductionPer100m3 * 0.01f) : 0f;

            var stock = (StockroomsField?.GetValue(work) as StockroomsController)?.Container;
            float cyclesPerDay = goalPerDay / Mathf.Max(0.001f, result.OutputPerCycle);
            foreach (KeyValuePair<ResourceData, float> cost in data.GetCostPairs())
            {
                if (cost.Key == null || cost.Value <= 0f) continue;
                ResourceID id = cost.Key.ResourceType;
                float inStock = stock != null && stock.HasResource(id) ? stock.GetResourceQuantity(id) : 0f;
                result.Inputs.Add(new InputLine { Name = SafeName(cost.Key), PerDay = cost.Value * cyclesPerDay, InStock = inStock });
            }
            return result;
        }

        public static bool TryColonyFoodNeed(out float perDay, out int people)
        {
            perDay = 0f;
            people = 0;
            var citizens = Resolve<CitizensController>();
            if (citizens?.Citizens == null) return false;
            var config = Resolve<FoodConsumingConfig>();
            if (config == null)
            {
                var found = Resources.FindObjectsOfTypeAll<FoodConsumingConfig>();
                if (found.Length > 0) config = found[0];
            }
            if (config == null) return false;
            var food = config.GetFoodConsumingData();
            var laws = Resolve<LawsController>();
            float modifier = laws?.GetConsumptionModifier() ?? 1f;
            foreach (Character c in citizens.Citizens)
            {
                if (c == null) continue;
                people++;
                perDay += c.IsChild ? food.ChildConsumePerDay : (c.IsSoldier ? food.SoldierConsumePerDay : food.WorkerConsumePerDay);
            }
            perDay *= modifier;
            return true;
        }

        public static bool OutputIsRations(ProductionWork work)
        {
            var data = work.ProductionData;
            if (data == null) return false;
            foreach (var profit in data.GetProfitPairs())
            {
                if (profit.Key != null && profit.Key.ResourceType == ResourceID.res_food_rations) return true;
            }
            return false;
        }

        public static string SafeName(ResourceData data)
        {
            if (data == null) return "?";
            try
            {
                string name = data.GetName();
                if (!string.IsNullOrEmpty(name)) return name;
            }
            catch (Exception)
            {
            }
            return data.ResourceType.ToString();
        }

        public static string BuildingName(Structure structure)
        {
            try
            {
                string name = structure.Draft.GetNameBuilding();
                if (!string.IsNullOrEmpty(name)) return name;
            }
            catch (Exception)
            {
            }
            return structure.Draft != null ? structure.Draft.Id : "Building";
        }
    }
}
