using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BepInEx;
using Controllers.Time;
using Gameplay.Rebuilding;
using Gameplay.Units.Characters;
using Gameplay.Units.Player.Workers;
using Gameplay.Units.Player.Workers.WorkSystem;
using Gameplay.Units.Workers;
using UnityEngine;

namespace IFZ.ProductionPlanner
{
    internal enum Level { None = 0, Novice = 1, Moderate = 2, Expert = 3 }

    internal sealed class JobExperience
    {
        public string Job;
        public float Days;
        public int LearnedOrder;
        public bool Learned => ExperienceRules.IsForemanEntry(Job) || Days >= ExperienceRules.NoviceDays;
    }

    internal static class Experience
    {
        public const float NoviceDays = ExperienceRules.NoviceDays;
        public const float ModerateDays = ExperienceRules.ModerateDays;
        public const float ExpertDays = ExperienceRules.ExpertDays;
        public const int MaxJobs = ExperienceRules.MaxJobs;
        public static readonly string[] LevelNames = { "None", "Novice", "Moderate", "Expert" };
        private static readonly float[] Boosts = { 0f, 0.10f, 0.25f, 0.50f };

        private static readonly string FilePath = Path.Combine(Paths.ConfigPath, Plugin.Guid + ".experience.txt");
        private static readonly Dictionary<string, List<JobExperience>> Data = new Dictionary<string, List<JobExperience>>();
        private static readonly Dictionary<string, string> JobNames = new Dictionary<string, string>();
        private static bool _loaded;
        private static bool _dirty;
        private static float _lastGameplayTime = -1f;
        private static float _nextSave;
        private static int _orderCounter;

        public static Level LevelFor(float days) => ExperienceRules.LevelFor(days);

        public static float Boost(Level level) => Boosts[(int)level];

        public static string JobId(Structure structure) => structure?.Draft != null ? structure.Draft.Id : null;

        public static string JobName(string job)
        {
            if (job == null) return "?";
            if (ExperienceRules.IsForemanEntry(job)) return "Foreman: " + JobName(job.Substring(ExperienceRules.ForemanPrefix.Length));
            return JobNames.TryGetValue(job, out var name) ? name : job;
        }

        public static string CurrentJob(Character c)
        {
            var work = c?.WorkModule?.CurrentWork;
            var structure = work?.GetRelatedStructure();
            if (structure == null || Crews.StaffWork(structure) != work) return null;
            return JobId(structure);
        }

        public static IReadOnlyList<JobExperience> Jobs(Character c)
        {
            Load();
            return c != null && Data.TryGetValue(Key(c), out var list) ? list : (IReadOnlyList<JobExperience>)Array.Empty<JobExperience>();
        }

        public static float Days(Character c, string job)
        {
            if (c == null || job == null) return 0f;
            foreach (var j in Jobs(c))
            {
                if (j.Job == job) return j.Days;
            }
            return 0f;
        }

        public static Level LevelOf(Character c, string job) => LevelFor(Days(c, job));

        public static float BoostFor(Character c)
        {
            string job = CurrentJob(c);
            if (job == null || ForemanOf(c) == job) return 0f;
            return Boost(LevelOf(c, job));
        }

        public static string ForemanOf(Character c)
        {
            if (c == null) return null;
            Load();
            return Data.TryGetValue(Key(c), out var list) ? ExperienceRules.ForemanJob(list) : null;
        }

        public static Level ForemanLevel(Character c) => LevelFor(Days(c, ExperienceRules.ForemanPrefix + ForemanOf(c)));

        public static Level SelectionLevel(Character c, string job) => job != null && ForemanOf(c) == job ? Level.Expert : LevelOf(c, job);

        public static string Title(Character c)
        {
            string job = ForemanOf(c);
            if (job == null) return null;
            var level = ForemanLevel(c);
            return level == Level.None ? $"Foreman in training, {JobName(job)}" : $"{LevelNames[(int)level]} Foreman of the {JobName(job)}";
        }

        public static bool MakeForeman(Character c, string job)
        {
            Load();
            if (c == null || !Data.TryGetValue(Key(c), out var list) || !ExperienceRules.MakeForeman(list, job)) return false;
            _dirty = true;
            Plugin.Log.LogInfo($"{c.Name} is now foreman in training for {JobName(job)}.");
            return true;
        }

        public static void StopForeman(Character c)
        {
            Load();
            if (c != null && Data.TryGetValue(Key(c), out var list) && ExperienceRules.StopForeman(list)) _dirty = true;
        }

        private static readonly Dictionary<WorkBase, KeyValuePair<Character, Level>> Foremen = new Dictionary<WorkBase, KeyValuePair<Character, Level>>();

        public static void RefreshForemen(IEnumerable<KeyValuePair<Structure, WorkBase>> buildings)
        {
            Foremen.Clear();
            foreach (var pair in buildings)
            {
                string job = JobId(pair.Key);
                Character best = null;
                float bestDays = -1f;
                foreach (var w in pair.Value.Workers)
                {
                    if (w == null || ForemanOf(w) != job) continue;
                    float days = Days(w, ExperienceRules.ForemanPrefix + job);
                    if (days > bestDays)
                    {
                        best = w;
                        bestDays = days;
                    }
                }
                if (best != null) Foremen[pair.Value] = new KeyValuePair<Character, Level>(best, LevelFor(bestDays));
            }
        }

        public static bool TryGetForeman(WorkBase work, out Character foreman, out Level level)
        {
            foreman = null;
            level = Level.None;
            if (work == null || !Foremen.TryGetValue(work, out var pair)) return false;
            foreman = pair.Key;
            level = pair.Value;
            return true;
        }

        public static float ForemanBoost(WorkBase work) => TryGetForeman(work, out _, out var level) ? Boost(level) : 0f;

        public static void Forget(Character c, string job)
        {
            Load();
            if (c == null || !Data.TryGetValue(Key(c), out var list)) return;
            if (list.RemoveAll(j => j.Job == job) > 0) _dirty = true;
        }

        public static void Accrue()
        {
            Load();
            float now = TimeController.GameplayTime;
            if (_lastGameplayTime < 0f || now < _lastGameplayTime)
            {
                _lastGameplayTime = now;
                return;
            }
            float deltaGts = now - _lastGameplayTime;
            _lastGameplayTime = now;
            if (deltaGts <= 0f) return;
            float hourGts = Mathf.Max(1, TimeController.HourLengthInGts);
            var workers = Planner.Resolve<WorkersController>()?.Workers;
            if (workers == null) return;
            foreach (var c in workers)
            {
                if (c == null || c.IsTired || !c.WorkModule.IsWorking()) continue;
                var work = c.WorkModule.CurrentWork;
                var structure = work?.GetRelatedStructure();
                if (structure == null || Crews.StaffWork(structure) != work) continue;
                string job = JobId(structure);
                if (job == null) continue;
                if (!JobNames.ContainsKey(job)) JobNames[job] = Planner.BuildingName(structure);
                float shiftHours = work is Work w ? Planner.WorkHoursPerDay(w) : 12f;
                string track = ForemanOf(c) == job ? ExperienceRules.ForemanPrefix + job : job;
                Add(c, track, deltaGts / hourGts / Mathf.Max(0.5f, shiftHours));
            }
            if (_dirty && Time.unscaledTime >= _nextSave)
            {
                _nextSave = Time.unscaledTime + 30f;
                Save();
            }
        }

        private static void Add(Character c, string job, float days)
        {
            string key = Key(c);
            if (!Data.TryGetValue(key, out var list))
            {
                list = new List<JobExperience>();
                Data[key] = list;
            }
            string forgotten = ExperienceRules.AddDays(list, job, days, ref _orderCounter);
            _dirty = true;
            if (forgotten != null) Plugin.Log.LogInfo($"{c.Name} forgot {JobName(forgotten)} after learning {JobName(job)}.");
        }

        private static string Key(Character c) => GoalStore.SaveId() + "|" + c.Id;

        public static void Save()
        {
            _dirty = false;
            try
            {
                var lines = new List<string>();
                foreach (var pair in Data)
                {
                    if (pair.Value.Count == 0) continue;
                    var parts = new List<string>();
                    foreach (var j in pair.Value)
                        parts.Add(j.Job + ":" + j.Days.ToString("0.###", CultureInfo.InvariantCulture) + ":" + j.LearnedOrder.ToString(CultureInfo.InvariantCulture));
                    lines.Add(pair.Key + "\t" + string.Join(";", parts.ToArray()));
                }
                File.WriteAllLines(FilePath, lines.ToArray());
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Could not save experience: " + e.Message);
            }
        }

        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    string[] parts = line.Split('\t');
                    if (parts.Length < 2) continue;
                    var list = new List<JobExperience>();
                    foreach (string item in parts[1].Split(';'))
                    {
                        string[] f = item.Split(':');
                        if (f.Length < 3) continue;
                        if (!float.TryParse(f[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float days)) continue;
                        int.TryParse(f[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int order);
                        _orderCounter = Math.Max(_orderCounter, order);
                        list.Add(new JobExperience { Job = f[0], Days = days, LearnedOrder = order });
                    }
                    Data[parts[0]] = list;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Could not load experience: " + e.Message);
            }
        }
    }
}
