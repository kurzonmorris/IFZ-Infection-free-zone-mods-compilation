using System;
using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Controllers;
using Gameplay.Production;
using Gameplay.Rebuilding;
using UnityEngine;

namespace IFZ.ProductionPlanner
{
    internal sealed class Settings
    {
        public bool UseMood;
        public bool UseWeather;
        public bool UseWorkHours;
        public bool UseHauling;
        public float HaulingPercent;
    }

    [BepInPlugin(Guid, Name, Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "kurzon.ifz.productionPlanner";
        public const string Name = "IFZ Production Planner";
        public const string Version = "0.1.0";

        internal static ManualLogSource Log;

        private ConfigEntry<bool> _enabled;
        private ConfigEntry<KeyboardShortcut> _toggleKey;
        private ConfigEntry<bool> _useMood;
        private ConfigEntry<bool> _useWeather;
        private ConfigEntry<bool> _useWorkHours;
        private ConfigEntry<bool> _useHauling;
        private ConfigEntry<float> _haulingPercent;
        private ConfigEntry<float> _autoInterval;

        private bool _show;
        private Rect _window = new Rect(80f, 120f, 380f, 10f);
        private Structure _shownStructure;
        private string _goalText = "";
        private float _nextAuto;
        private readonly int _windowId = Guid.GetHashCode();

        private void Awake()
        {
            Log = Logger;
            _enabled = Config.Bind("General", "Enabled", true, "Master switch. False makes the mod do nothing.");
            _toggleKey = Config.Bind("Controls", "ToggleWindow", new KeyboardShortcut(KeyCode.F6), "Open or close the planner window.");
            _useMood = Config.Bind("Factors", "UseMood", true, "Include the workers' mood efficiency.");
            _useWeather = Config.Bind("Factors", "UseWeather", true, "Include the current temperature efficiency of the building.");
            _useWorkHours = Config.Bind("Factors", "UseWorkHours", true, "Include the real work hours (sunrise, sunset, laws). False assumes 24 hours of work.");
            _useHauling = Config.Bind("Factors", "UseHauling", true, "Include an allowance for time workers spend carrying resources.");
            _haulingPercent = Config.Bind("Factors", "HaulingPercent", 20f, new ConfigDescription("Percent of work time lost to carrying resources. An estimate; tune it against 'Produced today'.", new AcceptableValueRange<float>(0f, 90f)));
            _autoInterval = Config.Bind("Auto", "RecheckSeconds", 5f, new ConfigDescription("Seconds between automatic worker adjustments.", new AcceptableValueRange<float>(1f, 120f)));
            Logger.LogInfo($"{Name} v{Version} loaded.");
        }

        private Settings CurrentSettings() => new Settings
        {
            UseMood = _useMood.Value,
            UseWeather = _useWeather.Value,
            UseWorkHours = _useWorkHours.Value,
            UseHauling = _useHauling.Value,
            HaulingPercent = _haulingPercent.Value
        };

        private void Update()
        {
            if (!_enabled.Value) return;
            try
            {
                if (_toggleKey.Value.IsDown()) _show = !_show;
                if (Time.unscaledTime >= _nextAuto)
                {
                    _nextAuto = Time.unscaledTime + _autoInterval.Value;
                    RunAuto();
                }
            }
            catch (Exception e)
            {
                Log.LogError("Update failed: " + e);
            }
        }

        private void RunAuto()
        {
            var buildings = Planner.Resolve<BuildingsController>();
            if (buildings?.AdaptedBuildings == null) return;
            var settings = CurrentSettings();
            foreach (var building in buildings.AdaptedBuildings)
            {
                if (building == null) continue;
                if (!(building.CurrentWork is ProductionWork work)) continue;
                Planner.TrackDay(work);
                var goal = GoalStore.Get(building);
                if (goal == null || !goal.Auto || goal.PerDay <= 0f) continue;
                var plan = Planner.Calculate(work, goal.PerDay, settings);
                if (plan == null || plan.RequiredWorkers <= 0) continue;
                int target = Mathf.Clamp(plan.RequiredWorkers, 1, work.InitialMaxWorkers);
                if (work.MaxWorkers != target) work.MaxWorkers = target;
            }
        }

        private void OnGUI()
        {
            if (!_enabled.Value || !_show) return;
            try
            {
                _window = GUILayout.Window(_windowId, _window, DrawWindow, $"Production Planner v{Version}");
            }
            catch (Exception e)
            {
                Log.LogError("Window failed: " + e);
                _show = false;
            }
        }

        private void DrawWindow(int id)
        {
            var structure = Planner.SelectedStructure();
            if (structure != _shownStructure)
            {
                _shownStructure = structure;
                var stored = structure != null ? GoalStore.Get(structure) : null;
                _goalText = stored != null && stored.PerDay > 0f ? stored.PerDay.ToString("0.#", CultureInfo.InvariantCulture) : "";
            }

            if (structure == null)
            {
                GUILayout.Label("Select a building.");
                Footer();
                return;
            }

            GUILayout.Label("<b>" + Planner.BuildingName(structure) + "</b>");
            if (!(structure.CurrentWork is ProductionWork work) || work.ProductionData == null)
            {
                GUILayout.Label("This building has no production. Not supported yet.");
                Footer();
                return;
            }

            var goal = GoalStore.GetOrCreate(structure);
            var settings = CurrentSettings();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Goal per day:", GUILayout.Width(100f));
            string text = GUILayout.TextField(_goalText, 8, GUILayout.Width(80f));
            if (text != _goalText)
            {
                _goalText = text;
                goal.PerDay = float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? Mathf.Max(0f, value) : 0f;
                GoalStore.Save();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            foreach (int step in new[] { -50, -10, 10, 50 })
            {
                if (GUILayout.Button(step > 0 ? "+" + step : step.ToString()))
                {
                    goal.PerDay = Mathf.Max(0f, goal.PerDay + step);
                    _goalText = goal.PerDay.ToString("0.#", CultureInfo.InvariantCulture);
                    GoalStore.Save();
                }
            }
            GUILayout.EndHorizontal();

            if (Planner.OutputIsRations(work) && Planner.TryColonyFoodNeed(out float need, out int people))
            {
                if (GUILayout.Button($"Use colony food need: {need:0.#}/day ({people} people)"))
                {
                    goal.PerDay = Mathf.Ceil(need);
                    _goalText = goal.PerDay.ToString("0", CultureInfo.InvariantCulture);
                    GoalStore.Save();
                }
            }

            GUILayout.BeginHorizontal();
            bool advice = GUILayout.Toggle(!goal.Auto, " Advice");
            bool auto = GUILayout.Toggle(goal.Auto, " Auto-set max workers");
            GUILayout.EndHorizontal();
            if (auto != goal.Auto)
            {
                goal.Auto = auto;
                GoalStore.Save();
            }
            else if (advice && goal.Auto)
            {
                goal.Auto = false;
                GoalStore.Save();
            }

            var plan = Planner.Calculate(work, goal.PerDay, settings);
            if (plan == null)
            {
                GUILayout.Label("No production selected in this building.");
                Footer();
                return;
            }

            GUILayout.Space(4f);
            GUILayout.Label($"Output: {plan.OutputName} x{plan.OutputPerCycle:0.#} per cycle, cycle {plan.CycleHours:0.##} work-hours");

            GUILayout.Label("<b>Factors</b>");
            _useWorkHours.Value = GUILayout.Toggle(_useWorkHours.Value, $" Work hours ({plan.WorkHoursPerDay:0.#} h/day)");
            _useMood.Value = GUILayout.Toggle(_useMood.Value, $" Mood ({plan.MoodFactor * 100f:0}%{(plan.MoodFromWorkers || !_useMood.Value ? "" : ", no workers yet")})");
            _useWeather.Value = GUILayout.Toggle(_useWeather.Value, $" Weather ({plan.WeatherFactor * 100f:0}%)");
            GUILayout.BeginHorizontal();
            _useHauling.Value = GUILayout.Toggle(_useHauling.Value, $" Hauling allowance {_haulingPercent.Value:0}%");
            if (GUILayout.Button("-", GUILayout.Width(24f))) _haulingPercent.Value = Mathf.Max(0f, _haulingPercent.Value - 5f);
            if (GUILayout.Button("+", GUILayout.Width(24f))) _haulingPercent.Value = Mathf.Min(90f, _haulingPercent.Value + 5f);
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);
            GUILayout.Label("<b>Result</b>");
            GUILayout.Label($"One worker makes about {plan.PerWorkerPerDay:0.#} per day.");
            GUILayout.Label($"Max workers now: {plan.CurrentMax} of {plan.SlotLimit} slots.");
            if (goal.PerDay <= 0f)
            {
                GUILayout.Label("Enter a goal to get a worker number.");
            }
            else
            {
                GUILayout.Label($"Workers needed for {goal.PerDay:0.#}/day: <b>{plan.RequiredWorkers}</b>");
                bool tooFewSlots = plan.RequiredWorkers > plan.SlotLimit;
                bool overCap = plan.MaxDayProduction > 0 && goal.PerDay > plan.MaxDayProduction;
                if (tooFewSlots)
                    GUILayout.Label($"<color=orange>Building too small: {plan.SlotLimit} slots. Needs about {plan.VolumeForWorkers:0} m³ (now {plan.BuildingVolume:0} m³), or a second building.</color>");
                if (overCap)
                    GUILayout.Label($"<color=orange>Daily cap of this building: {plan.MaxDayProduction}. Needs about {plan.VolumeForDailyCap:0} m³ for the goal.</color>");
                if (tooFewSlots || overCap)
                    GUILayout.Label($"This building can reach about {plan.ReachablePerDay:0.#}/day.");
                if (!tooFewSlots && !goal.Auto && plan.RequiredWorkers != plan.CurrentMax)
                {
                    if (GUILayout.Button($"Set max workers to {plan.RequiredWorkers}")) work.MaxWorkers = plan.RequiredWorkers;
                }
                if (goal.Auto) GUILayout.Label($"Auto: max workers set to {Mathf.Clamp(plan.RequiredWorkers, 1, plan.SlotLimit)} every {_autoInterval.Value:0} s.");
            }

            if (plan.Inputs.Count > 0 && goal.PerDay > 0f)
            {
                GUILayout.Space(4f);
                GUILayout.Label("<b>Inputs per day</b>");
                foreach (var input in plan.Inputs)
                {
                    string days = input.PerDay > 0f ? $" ({input.InStock / input.PerDay:0.#} days)" : "";
                    string color = input.InStock < input.PerDay ? "orange" : "white";
                    GUILayout.Label($"<color={color}>{input.Name}: {input.PerDay:0.#}/day, stock {input.InStock:0}{days}</color>");
                }
            }

            GUILayout.Space(4f);
            GUILayout.Label(plan.ProducedFullDay
                ? $"Produced today: {plan.ProducedToday}"
                : $"Produced since tracking began today: {plan.ProducedToday} (full count from tomorrow)");
            Footer();
        }

        private void Footer()
        {
            GUILayout.Label($"<size=10>{_toggleKey.Value} closes this window.</size>");
            GUI.DragWindow();
        }
    }
}
