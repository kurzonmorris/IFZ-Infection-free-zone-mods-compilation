using System;
using System.Collections.Generic;
using Controllers.CharacterLogic;
using Gameplay.Rebuilding;
using Gameplay.Units.Characters;
using Gameplay.Units.Player.Workers;
using Gameplay.Units.Player.Workers.WorkSystem;
using UnityEngine;

namespace IFZ.ProductionPlanner
{
    internal static class WorkerWindow
    {
        private enum SortMode { Experience, Name, Gender, Job }

        private const float MinWidth = 520f;
        private const float MinHeight = 300f;
        private static readonly int WindowId = (Plugin.Guid + ".workers").GetHashCode();

        public static bool Open;
        private static Structure _structure;
        private static Rect _rect = new Rect(480f, 120f, 640f, 460f);
        private static bool _resizing;
        private static Vector2 _leftScroll;
        private static Vector2 _rightScroll;
        private static SortMode _sort = SortMode.Experience;
        private static bool _descending;
        private static string _filter = "";
        private static Character _details;
        private static string _job;

        public static void Show(Structure structure)
        {
            _structure = structure;
            Open = true;
        }

        public static void OnGUI()
        {
            if (!Open) return;
            HandleResize();
            _rect = GUI.Window(WindowId, _rect, Draw, "Workers");
        }

        private static void HandleResize()
        {
            var e = Event.current;
            var handle = new Rect(_rect.xMax - 18f, _rect.yMax - 18f, 18f, 18f);
            if (e.type == EventType.MouseDown && e.button == 0 && handle.Contains(e.mousePosition))
            {
                _resizing = true;
                e.Use();
            }
            else if (_resizing && e.type == EventType.MouseDrag)
            {
                _rect.width = Mathf.Max(MinWidth, e.mousePosition.x - _rect.x);
                _rect.height = Mathf.Max(MinHeight, e.mousePosition.y - _rect.y);
                e.Use();
            }
            else if (_resizing && (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp))
            {
                _resizing = false;
            }
        }

        private static void Draw(int id)
        {
            try
            {
                DrawContent();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Worker window failed: " + e);
                Open = false;
            }
            GUI.Box(new Rect(_rect.width - 18f, _rect.height - 18f, 18f, 18f), "◢");
            GUI.DragWindow(new Rect(0f, 0f, _rect.width - 30f, 20f));
        }

        private static void DrawContent()
        {
            GUILayout.BeginArea(new Rect(8f, 22f, _rect.width - 16f, _rect.height - 44f));
            var work = Crews.StaffWork(_structure);
            GUILayout.BeginHorizontal();
            GUILayout.Label(_structure != null ? "<b>" + Planner.BuildingName(_structure) + "</b>" : "No building");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(60f))) Open = false;
            GUILayout.EndHorizontal();

            if (work == null)
            {
                GUILayout.Label("This building has no permanent workers.");
                GUILayout.EndArea();
                return;
            }
            if (Crews.IsOff(work))
            {
                GUILayout.Label("This building is turned off. Turn it on in the planner window first.");
                GUILayout.EndArea();
                return;
            }

            _job = Experience.JobId(_structure);
            var lockList = Crews.LockList(_structure);
            GUILayout.Label($"Working: {work.Workers.Count} · Max workers: {work.MaxWorkers} · Slots: {work.InitialMaxWorkers} · Locked: {lockList.Count}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Lock current workers")) Crews.LockCurrent(_structure, work);
            if (GUILayout.Button("Unlock all")) Crews.UnlockAll(_structure);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Sort:", GUILayout.Width(36f));
            SortButton("Experience", SortMode.Experience);
            SortButton("Name", SortMode.Name);
            SortButton("Gender", SortMode.Gender);
            SortButton("Job", SortMode.Job);
            GUILayout.Label("Find:", GUILayout.Width(34f));
            _filter = GUILayout.TextField(_filter, 20, GUILayout.Width(100f));
            GUILayout.EndHorizontal();

            float columnWidth = (_rect.width - 40f) / 2f;
            if (_details != null) DrawDetails();
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(columnWidth));
            GUILayout.Label("<b>Available</b>");
            _leftScroll = GUILayout.BeginScrollView(_leftScroll);
            foreach (var c in AvailableWorkers())
            {
                GUILayout.BeginHorizontal();
                bool lockedElsewhere = Crews.IsLocked(c);
                if (GUILayout.Button("i", GUILayout.Width(20f))) _details = c;
                GUILayout.Label(Row(c, lockedElsewhere));
                GUI.enabled = !lockedElsewhere && lockList.Count < work.InitialMaxWorkers;
                if (GUILayout.Button("→", GUILayout.Width(28f))) Crews.Lock(_structure, work, c);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(columnWidth));
            GUILayout.Label("<b>Assigned and locked</b> (top = kept longest)");
            _rightScroll = GUILayout.BeginScrollView(_rightScroll);
            var byId = CitizensById();
            for (int i = 0; i < lockList.Count; i++)
            {
                string cid = lockList[i];
                byId.TryGetValue(cid, out var c);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("←", GUILayout.Width(28f)))
                {
                    Crews.Unlock(_structure, cid);
                    GUILayout.EndHorizontal();
                    break;
                }
                if (c != null && GUILayout.Button("i", GUILayout.Width(20f))) _details = c;
                GUILayout.Label($"{i + 1}. " + (c != null ? Row(c, false) : "(missing)"));
                if (GUILayout.Button("▲", GUILayout.Width(24f))) Crews.Move(_structure, i, -1);
                if (GUILayout.Button("▼", GUILayout.Width(24f))) Crews.Move(_structure, i, 1);
                GUILayout.EndHorizontal();
            }
            var unlockedHere = new List<Character>();
            foreach (var w in work.Workers)
            {
                if (w != null && !Crews.IsLockedTo(w, _structure)) unlockedHere.Add(w);
            }
            if (unlockedHere.Count > 0)
            {
                GUILayout.Space(6f);
                GUILayout.Label("<b>Working here, not locked</b>");
                foreach (var w in unlockedHere)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Row(w, Crews.IsLocked(w)));
                    GUI.enabled = !Crews.IsLocked(w) && lockList.Count < work.InitialMaxWorkers;
                    if (GUILayout.Button("Lock", GUILayout.Width(50f))) Crews.Lock(_structure, work, w);
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private static void DrawDetails()
        {
            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            string title = Experience.Title(_details);
            GUILayout.Label($"<b>{_details.Name}</b>{(title != null ? " — " + title : "")} — jobs (max {Experience.MaxJobs}; learning a new one forgets the oldest)");
            if (GUILayout.Button("x", GUILayout.Width(22f))) _details = null;
            GUILayout.EndHorizontal();
            if (_details == null)
            {
                GUILayout.EndVertical();
                return;
            }
            float marksmanHours = Combat.MarksmanHoursOf(_details);
            if (marksmanHours > 0f)
                GUILayout.Label($"Marksman: {Experience.LevelNames[(int)Combat.MarksmanLevel(_details)]} ({marksmanHours:0.0} h of shooting; Novice 6, Moderate 21, Expert 61)");
            var jobs = Experience.Jobs(_details);
            if (jobs.Count == 0) GUILayout.Label("No job experience yet.");
            foreach (var j in new List<JobExperience>(jobs))
            {
                GUILayout.BeginHorizontal();
                bool foreman = ExperienceRules.IsForemanEntry(j.Job);
                var level = Experience.LevelFor(j.Days);
                string state = foreman || j.Learned ? Experience.LevelNames[(int)level] : "learning";
                GUILayout.Label($"{Experience.JobName(j.Job)}: {state} ({j.Days:0.0} of {Experience.ExpertDays:0} days)");
                if (!foreman && level == Level.Expert && Experience.ForemanOf(_details) == null
                    && GUILayout.Button("Make foreman", GUILayout.Width(100f)))
                    Experience.MakeForeman(_details, j.Job);
                if (foreman)
                {
                    if (GUILayout.Button("Stop foreman", GUILayout.Width(100f))) Experience.StopForeman(_details);
                }
                else if (GUILayout.Button("Forget", GUILayout.Width(60f))) Experience.Forget(_details, j.Job);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private static void SortButton(string label, SortMode mode)
        {
            string text = _sort == mode ? label + (_descending ? " ▼" : " ▲") : label;
            if (!GUILayout.Button(text)) return;
            if (_sort == mode) _descending = !_descending;
            else
            {
                _sort = mode;
                _descending = false;
            }
        }

        private static List<Character> AvailableWorkers()
        {
            var result = new List<Character>();
            var workers = Planner.Resolve<WorkersController>()?.Workers;
            if (workers == null) return result;
            foreach (var c in workers)
            {
                if (c == null || Crews.IsLockedTo(c, _structure)) continue;
                if (_filter.Length > 0 && (c.Name ?? "").IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                result.Add(c);
            }
            Comparison<Character> compare;
            switch (_sort)
            {
                case SortMode.Experience:
                    compare = (a, b) => Experience.Days(b, _job).CompareTo(Experience.Days(a, _job)) is int x && x != 0 ? x : string.CompareOrdinal(a.Name, b.Name);
                    break;
                case SortMode.Gender:
                    compare = (a, b) => string.CompareOrdinal(Gender(a), Gender(b)) is int g && g != 0 ? g : string.CompareOrdinal(a.Name, b.Name);
                    break;
                case SortMode.Job:
                    compare = (a, b) => string.CompareOrdinal(JobLabel(a), JobLabel(b)) is int j && j != 0 ? j : string.CompareOrdinal(a.Name, b.Name);
                    break;
                default:
                    compare = (a, b) => string.CompareOrdinal(a.Name, b.Name);
                    break;
            }
            result.Sort(compare);
            if (_descending) result.Reverse();
            return result;
        }

        private static Dictionary<string, Character> CitizensById()
        {
            var map = new Dictionary<string, Character>();
            var citizens = Planner.Resolve<CitizensController>()?.Citizens;
            if (citizens == null) return map;
            foreach (var c in citizens)
            {
                if (c != null && !string.IsNullOrEmpty(c.Id)) map[c.Id] = c;
            }
            return map;
        }

        private static string Row(Character c, bool lockedElsewhere)
        {
            float days = Experience.Days(c, _job);
            string exp = days > 0f ? $"{Experience.LevelNames[(int)Experience.LevelFor(days)]} {days:0.0}d" : "no exp";
            if (Experience.ForemanOf(c) == _job) exp = Experience.Title(c);
            string row = $"{c.Name} · {Gender(c)} · {c.CharacterInfo.Age} · {exp} · {JobLabel(c)}";
            if (lockedElsewhere) row = "<color=grey>" + row + " · locked</color>";
            return row;
        }

        private static string Gender(Character c) => c.CharacterInfo.Gender.ToString();

        private static string JobLabel(Character c)
        {
            if (c.IsSick) return "In hospital";
            var current = c.WorkModule.CurrentWork;
            if (current == null) return "Free";
            var structure = current.GetRelatedStructure();
            return structure != null ? Planner.BuildingName(structure) : current.WorkType.ToString();
        }
    }
}
