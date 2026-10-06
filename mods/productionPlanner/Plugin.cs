using System;
using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Controllers;
using Gameplay.Production;
using Gameplay.Rebuilding;
using HarmonyLib;
using UnityEngine;

namespace IFZ.ProductionPlanner
{
    internal sealed class Settings
    {
        public bool UseMood;
        public bool UseWeather;
        public bool UseWorkHours;
        public bool UseHauling;
        public bool UseExperience;
        public float HaulingPercent;
    }

    [BepInPlugin(Guid, Name, Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "kurzon.ifz.productionPlanner";
        public const string Name = "IFZ Production Planner";
        public const string Version = "0.5.0";

        internal static ManualLogSource Log;
        internal static bool ExperienceEnabled = true;

        private ConfigEntry<bool> _enabled;
        private ConfigEntry<KeyboardShortcut> _toggleKey;
        private ConfigEntry<bool> _useMood;
        private ConfigEntry<bool> _useWeather;
        private ConfigEntry<bool> _useWorkHours;
        private ConfigEntry<bool> _useHauling;
        private ConfigEntry<float> _haulingPercent;
        private ConfigEntry<float> _autoInterval;
        private ConfigEntry<bool> _experience;
        private ConfigEntry<bool> _autoPick;
        private ConfigEntry<bool> _useExperience;

        private bool _show;
        private Rect _window = new Rect(80f, 120f, 380f, 10f);
        private Structure _shownStructure;
        private string _goalText = "";
        private float _nextAuto;
        private float _nextCrewCheck;
        private Harmony _harmony;
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
            _experience = Config.Bind("Experience", "Enabled", true, "Workers gain job experience and work faster: Novice +10 %, Moderate +25 %, Expert +50 %.");
            _autoPick = Config.Bind("Experience", "AutoPickExperienced", true, "Move the most experienced free or unlocked worker into a job when they beat a current worker there.");
            _useExperience = Config.Bind("Factors", "UseExperience", true, "Include the experience boost of the building's current workers.");
            _harmony = new Harmony(Guid);
            Patches.Apply(_harmony);
            Logger.LogInfo($"{Name} v{Version} loaded.");
        }

        private Settings CurrentSettings() => new Settings
        {
            UseMood = _useMood.Value,
            UseWeather = _useWeather.Value,
            UseWorkHours = _useWorkHours.Value,
            UseHauling = _useHauling.Value,
            HaulingPercent = _haulingPercent.Value,
            UseExperience = _useExperience.Value && _experience.Value
        };

        private void Update()
        {
            if (!_enabled.Value) return;
            try
            {
                if (_toggleKey.Value.IsDown()) _show = !_show;
                if (Time.unscaledTime >= _nextCrewCheck)
                {
                    _nextCrewCheck = Time.unscaledTime + 2f;
                    ExperienceEnabled = _experience.Value;
                    Crews.AutoPick = _experience.Value && _autoPick.Value;
                    if (ExperienceEnabled) Experience.Accrue();
                    Crews.Maintain();
                    Houses.Maintain();
                }
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
                if (Crews.WantedMax(building, work) != target) Crews.SetWantedMax(building, work, target);
            }
        }

        private void OnDestroy()
        {
            Experience.Save();
            _harmony?.UnpatchSelf();
        }

        private void OnApplicationQuit() => Experience.Save();

        private void OnGUI()
        {
            if (!_enabled.Value) return;
            try
            {
                if (_show) _window = GUILayout.Window(_windowId, _window, DrawWindow, $"Production Planner v{Version}");
                WorkerWindow.OnGUI();
                HouseWindow.OnGUI();
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
            if (Houses.IsHouse(structure))
            {
                DrawHouse(structure);
                Footer();
                return;
            }
            DrawStaff(structure);
            if (!(structure.CurrentWork is ProductionWork work) || work.ProductionData == null)
            {
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
            if (_experience.Value) _useExperience.Value = GUILayout.Toggle(_useExperience.Value, $" Experience ({plan.ExperienceFactor * 100f:0}%)");
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
                if (!tooFewSlots && !goal.Auto && plan.RequiredWorkers != Crews.WantedMax(structure, work))
                {
                    if (GUILayout.Button($"Set max workers to {plan.RequiredWorkers}")) Crews.SetWantedMax(structure, work, plan.RequiredWorkers);
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

        private void DrawHouse(Structure house)
        {
            Houses.Remember(house);
            bool off = Houses.IsOff(house);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Residents {house.LivingCitizens.Count}/{house.GetCitizensCapacity()} · locked {Houses.LockList(house).Count}");
            if (GUILayout.Button(off ? "Turn on" : "Turn off", GUILayout.Width(70f)))
            {
                if (off) Houses.TurnOn(house);
                else Houses.TurnOff(house);
            }
            GUI.enabled = !off;
            if (GUILayout.Button("Residents…", GUILayout.Width(90f))) HouseWindow.Show(house);
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            if (off) GUILayout.Label("<color=orange>Turned off: nobody lives here, locks cleared.</color>");
        }

        private void DrawStaff(Structure structure)
        {
            var staff = Crews.StaffWork(structure);
            if (staff == null)
            {
                GUILayout.Label("No permanent workers in this building.");
                return;
            }
            bool off = Crews.IsOff(staff);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Priority:", GUILayout.Width(60f));
            GUI.enabled = !off;
            for (int p = 1; p <= Crews.MaxPriority; p++)
            {
                var old = GUI.color;
                if (staff.Priority == p) GUI.color = Color.green;
                if (GUILayout.Button(p.ToString(), GUILayout.Width(24f)) && staff.Priority != p) Crews.SetPriority(staff, p);
                GUI.color = old;
            }
            GUI.enabled = true;
            if (GUILayout.Button(off ? "Turn on" : "Turn off", GUILayout.Width(70f)))
            {
                if (off) Crews.TurnOn(structure, staff);
                else Crews.TurnOff(structure, staff);
            }
            GUILayout.EndHorizontal();
            if (off)
            {
                GUILayout.Label("<color=orange>Turned off: no workers, locks cleared.</color>");
                return;
            }
            if (staff.Priority > Crews.MaxPriority)
                GUILayout.Label($"Priority {staff.Priority} (alarm boost).");
            bool restricted = Crews.IsRestricted(structure);
            int wanted = Crews.WantedMax(structure, staff);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Workers {staff.Workers.Count}/{wanted} (slots {staff.InitialMaxWorkers}) · locked {Crews.LockList(structure).Count}");
            if (restricted)
            {
                if (GUILayout.Button("-", GUILayout.Width(24f))) Crews.SetWantedMax(structure, staff, wanted - 1);
                if (GUILayout.Button("+", GUILayout.Width(24f))) Crews.SetWantedMax(structure, staff, wanted + 1);
            }
            if (GUILayout.Button("Workers…", GUILayout.Width(80f))) WorkerWindow.Show(structure);
            GUILayout.EndHorizontal();
            if (!_experience.Value) return;
            string job = Experience.JobId(structure);
            var counts = new int[4];
            foreach (var w in staff.Workers)
            {
                if (w != null) counts[(int)Experience.LevelOf(w, job)]++;
            }
            GUILayout.Label($"Experience here: Expert {counts[3]} · Moderate {counts[2]} · Novice {counts[1]} · None {counts[0]}");
            if (Experience.TryGetForeman(staff, out var foreman, out var foremanLevel))
                GUILayout.Label($"Foreman: {foreman.Name} — {Experience.Title(foreman)} (+{Experience.Boost(foremanLevel) * 100f:0} % to the building)");
            else
                GUILayout.Label("<size=11>No foreman. An Expert can be made foreman in the worker window (i).</size>");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Experience limit:", GUILayout.Width(110f));
            var min = Crews.MinLevel(structure);
            for (int i = 0; i < 4; i++)
            {
                var old = GUI.color;
                if ((int)min == i) GUI.color = Color.green;
                if (GUILayout.Button(i == 0 ? "Any" : Experience.LevelNames[i]) && (int)min != i) Crews.SetMinLevel(structure, staff, (Level)i);
                GUI.color = old;
            }
            GUILayout.EndHorizontal();
            if (restricted)
                GUILayout.Label($"<size=11>Limit on: the mod fills this building itself. Use - / + here for max workers (wanted {wanted}).</size>");
        }

        private void Footer()
        {
            GUILayout.Label($"<size=10>{_toggleKey.Value} closes this window.</size>");
            GUI.DragWindow();
        }
    }
}
