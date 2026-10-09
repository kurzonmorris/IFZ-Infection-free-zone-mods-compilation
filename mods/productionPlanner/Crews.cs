using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using BepInEx;
using Controllers.CharacterLogic;
using Gameplay.Rebuilding;
using Gameplay.Units.Characters;
using Gameplay.Units.Player.Workers;
using Gameplay.Units.Player.Workers.WorkSystem;
using Gameplay.Units.Player.Workers.WorkSystem.Signals;
using Gameplay.Units.Workers.WorkSystem;
using Zenject;

namespace IFZ.ProductionPlanner
{
    internal static class Crews
    {
        public const int MaxPriority = 9;

        private static readonly string FilePath = Path.Combine(Paths.ConfigPath, Plugin.Guid + ".crews.txt");
        private static readonly Dictionary<string, List<string>> Locks = new Dictionary<string, List<string>>();
        private static readonly Dictionary<string, int> PriorityBeforeOff = new Dictionary<string, int>();
        private static readonly Dictionary<string, string> LockedTo = new Dictionary<string, string>();
        private static string _indexedSave;
        private static bool _loaded;

        public static WorkBase StaffWork(Structure structure)
        {
            var work = structure?.CurrentWork;
            if (work == null || work.InitialMaxWorkers <= 0) return null;
            switch (work.WorkType)
            {
                case WorkType.Build:
                case WorkType.Deconstruct:
                case WorkType.Area:
                case WorkType.Mend:
                case WorkType.PlantTree:
                case WorkType.ReplantTree:
                case WorkType.Nothing:
                    return null;
            }
            return work;
        }

        public static string Key(Structure structure) => GoalStore.Key(structure);

        public static bool IsLocked(Character character)
        {
            if (character == null) return false;
            EnsureIndex();
            string id = character.Id;
            return !string.IsNullOrEmpty(id) && LockedTo.ContainsKey(id);
        }

        public static bool IsLockedTo(Character character, Structure structure)
        {
            if (character == null || structure == null) return false;
            EnsureIndex();
            string id = character.Id;
            return !string.IsNullOrEmpty(id) && LockedTo.TryGetValue(id, out var key) && key == Key(structure);
        }

        public static IReadOnlyList<string> LockList(Structure structure)
        {
            Load();
            return Locks.TryGetValue(Key(structure), out var list) ? list : (IReadOnlyList<string>)Array.Empty<string>();
        }

        public static bool Lock(Structure structure, WorkBase work, Character character)
        {
            if (character == null || string.IsNullOrEmpty(character.Id) || IsLocked(character) || Guards.IsGuard(character)) return false;
            var list = GetOrCreate(Key(structure));
            if (list.Count >= work.InitialMaxWorkers) return false;
            list.Add(character.Id);
            if (work.MaxWorkers < list.Count) work.MaxWorkers = list.Count;
            Changed();
            return true;
        }

        public static void Unlock(Structure structure, string characterId)
        {
            Load();
            if (Locks.TryGetValue(Key(structure), out var list) && list.Remove(characterId)) Changed();
        }

        public static void UnlockAll(Structure structure)
        {
            Load();
            if (Locks.Remove(Key(structure))) Changed();
        }

        public static void Move(Structure structure, int index, int delta)
        {
            Load();
            if (!Locks.TryGetValue(Key(structure), out var list)) return;
            int target = index + delta;
            if (index < 0 || index >= list.Count || target < 0 || target >= list.Count) return;
            (list[index], list[target]) = (list[target], list[index]);
            Changed();
        }

        public static void LockCurrent(Structure structure, WorkBase work)
        {
            foreach (var worker in new List<Character>(work.Workers))
            {
                if (!IsLocked(worker)) Lock(structure, work, worker);
            }
        }

        public static bool IsOff(WorkBase work) => work.Priority == 0;

        public static void SetPriority(WorkBase work, int priority)
        {
            var bus = Planner.Resolve<SignalBus>();
            if (bus == null) return;
            bus.Fire(new WorkPriorityChangedSignal(work, priority, false));
        }

        public static void TurnOff(Structure structure, WorkBase work)
        {
            Load();
            if (work.Priority > 0) PriorityBeforeOff[Key(structure)] = work.Priority;
            Locks.Remove(Key(structure));
            SetPriority(work, 0);
            Changed();
        }

        public static void TurnOn(Structure structure, WorkBase work)
        {
            Load();
            string key = Key(structure);
            int priority = PriorityBeforeOff.TryGetValue(key, out int stored) && stored > 0 ? stored : 1;
            PriorityBeforeOff.Remove(key);
            SetPriority(work, Math.Min(priority, MaxPriority));
            Changed();
        }

        private sealed class Limit
        {
            public int MinLevel;
            public int Desired;
            public int LastSet;
        }

        private static readonly Dictionary<string, Limit> Limits = new Dictionary<string, Limit>();
        private static readonly FieldInfo MaxWorkersField = typeof(WorkBase).GetField("_maxWorkers", BindingFlags.Instance | BindingFlags.NonPublic);
        private const int MovesPerCheck = 3;
        private static int _moves;

        public static bool AutoPick = true;

        public static Level MinLevel(Structure structure)
        {
            Load();
            return Limits.TryGetValue(Key(structure), out var limit) ? (Level)limit.MinLevel : Level.None;
        }

        public static bool IsRestricted(Structure structure) => MinLevel(structure) != Level.None;

        public static int WantedMax(Structure structure, WorkBase work)
        {
            Load();
            return Limits.TryGetValue(Key(structure), out var limit) ? limit.Desired : work.MaxWorkers;
        }

        public static void SetMinLevel(Structure structure, WorkBase work, Level level)
        {
            Load();
            string key = Key(structure);
            if (level == Level.None)
            {
                if (Limits.TryGetValue(key, out var old))
                {
                    Limits.Remove(key);
                    work.MaxWorkers = Math.Min(old.Desired, work.InitialMaxWorkers);
                }
            }
            else if (Limits.TryGetValue(key, out var limit))
            {
                limit.MinLevel = (int)level;
            }
            else
            {
                Limits[key] = new Limit { MinLevel = (int)level, Desired = work.MaxWorkers, LastSet = work.MaxWorkers };
            }
            Changed();
        }

        public static void SetWantedMax(Structure structure, WorkBase work, int max)
        {
            Load();
            max = Math.Max(0, Math.Min(max, work.InitialMaxWorkers));
            if (Limits.TryGetValue(Key(structure), out var limit))
            {
                limit.Desired = max;
                Changed();
            }
            else if (work.MaxWorkers != max)
            {
                work.MaxWorkers = max;
            }
        }

        private static void SetMaxSilently(WorkBase work, int max)
        {
            if (MaxWorkersField != null) MaxWorkersField.SetValue(work, Math.Max(0, max));
        }

        public static void Maintain()
        {
            Load();
            _moves = 0;
            string save = GoalStore.SaveId() + "|";
            var works = Planner.Resolve<WorkController>()?.Works;
            var workersController = Planner.Resolve<WorkersController>();
            var citizens = Planner.Resolve<CitizensController>();
            if (works == null || workersController?.Workers == null || citizens?.Citizens == null) return;

            var buildings = new List<KeyValuePair<Structure, WorkBase>>();
            var liveKeys = new HashSet<string>();
            foreach (var work in works)
            {
                var structure = work?.GetRelatedStructure();
                if (structure == null || StaffWork(structure) != work) continue;
                buildings.Add(new KeyValuePair<Structure, WorkBase>(structure, work));
                liveKeys.Add(Key(structure));
            }
            var byId = new Dictionary<string, Character>();
            foreach (var citizen in citizens.Citizens)
            {
                if (citizen != null && !string.IsNullOrEmpty(citizen.Id)) byId[citizen.Id] = citizen;
            }
            var pool = new List<Character>(workersController.Workers);
            var available = new HashSet<Character>(pool);

            bool changed = false;
            foreach (var key in new List<string>(Locks.Keys))
            {
                if (key.StartsWith(save, StringComparison.Ordinal) && !liveKeys.Contains(key))
                {
                    Locks.Remove(key);
                    changed = true;
                }
            }
            foreach (var key in new List<string>(Limits.Keys))
            {
                if (key.StartsWith(save, StringComparison.Ordinal) && !liveKeys.Contains(key))
                {
                    Limits.Remove(key);
                    changed = true;
                }
            }

            Experience.RefreshForemen(buildings);
            foreach (var pair in buildings)
            {
                var structure = pair.Key;
                var work = pair.Value;
                string key = Key(structure);
                if (IsOff(work))
                {
                    if (Locks.Remove(key)) changed = true;
                    continue;
                }
                Limits.TryGetValue(key, out var limit);
                if (limit != null)
                {
                    if (work.MaxWorkers != limit.LastSet)
                    {
                        limit.Desired = Math.Max(0, Math.Min(work.InitialMaxWorkers, limit.Desired + work.MaxWorkers - limit.LastSet));
                        changed = true;
                    }
                    SetMaxSilently(work, limit.Desired);
                }

                Locks.TryGetValue(key, out var list);
                if (list != null) changed |= KeepLocked(work, list, byId, available);

                try
                {
                    if (limit != null) ManageRestricted(structure, work, limit, list, pool);
                    else if (AutoPick) PickExperienced(structure, work, list, pool);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning($"Worker management failed for {Planner.BuildingName(structure)}: {e.Message}");
                }

                if (limit != null)
                {
                    limit.LastSet = work.Workers.Count;
                    SetMaxSilently(work, limit.LastSet);
                }
                if (list != null && list.Count == 0)
                {
                    Locks.Remove(key);
                    changed = true;
                }
            }
            if (changed) Changed();
        }

        private static bool KeepLocked(WorkBase work, List<string> list, Dictionary<string, Character> byId, HashSet<Character> available)
        {
            bool changed = false;
            while (list.Count > work.MaxWorkers)
            {
                list.RemoveAt(list.Count - 1);
                changed = true;
            }
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!byId.TryGetValue(list[i], out var c) || c.IsUnderSquadProduction || c.IsSoldier || (!c.IsSick && !available.Contains(c)))
                {
                    list.RemoveAt(i);
                    changed = true;
                }
            }
            foreach (string id in list)
            {
                var c = byId[id];
                if (c.IsSick || c.WorkModule.CurrentWork == work) continue;
                if (work.Workers.Count >= work.MaxWorkers) EvictUnlocked(work, list);
                if (work.Workers.Count >= work.MaxWorkers) continue;
                try
                {
                    c.WorkModule.UnassignWork(true);
                    if (c.WorkModule.CurrentWork != work) work.AddWorker(c);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning($"Could not return {c.Name} to their job: {e.Message}");
                }
            }
            return changed;
        }

        private static void ManageRestricted(Structure structure, WorkBase work, Limit limit, List<string> locks, List<Character> pool)
        {
            string job = Experience.JobId(structure);
            var min = (Level)limit.MinLevel;
            foreach (var w in new List<Character>(work.Workers))
            {
                if (w == null || (locks != null && locks.Contains(w.Id))) continue;
                if (Experience.SelectionLevel(w, job) < min) w.WorkModule.UnassignWork(true);
            }
            while (work.Workers.Count < limit.Desired && _moves < MovesPerCheck)
            {
                var recruit = BestCandidate(job, min, work, Level.None, pool);
                if (recruit == null) break;
                Recruit(work, recruit);
            }
        }

        private static void PickExperienced(Structure structure, WorkBase work, List<string> locks, List<Character> pool)
        {
            if (_moves >= MovesPerCheck || work.MaxWorkers <= 0) return;
            string job = Experience.JobId(structure);
            if (work.Workers.Count < work.MaxWorkers)
            {
                var recruit = BestCandidate(job, Level.Novice, work, Level.None, pool);
                if (recruit != null) Recruit(work, recruit);
                return;
            }
            Character weakest = null;
            var weakestLevel = Level.Expert;
            foreach (var w in work.Workers)
            {
                if (w == null || (locks != null && locks.Contains(w.Id))) continue;
                var level = Experience.SelectionLevel(w, job);
                if (weakest == null || level < weakestLevel)
                {
                    weakest = w;
                    weakestLevel = level;
                }
            }
            if (weakest == null || weakestLevel == Level.Expert) return;
            var better = BestCandidate(job, Level.Novice, work, weakestLevel, pool);
            if (better == null) return;
            int max = work.MaxWorkers;
            Recruit(work, better);
            if (better.WorkModule.CurrentWork != work) return;
            SetMaxSilently(work, max);
            weakest.WorkModule.UnassignWork(true);
        }

        private static void Recruit(WorkBase work, Character recruit)
        {
            _moves++;
            int max = work.MaxWorkers;
            if (work.Workers.Count >= max) SetMaxSilently(work, work.Workers.Count + 1);
            recruit.WorkModule.UnassignWork(true);
            if (recruit.WorkModule.CurrentWork != work) work.AddWorker(recruit);
            if (work.MaxWorkers > max && work.Workers.Count <= max) SetMaxSilently(work, max);
        }

        private static Character BestCandidate(string job, Level minLevel, WorkBase target, Level mustBeat, List<Character> pool)
        {
            Character best = null;
            Level bestLevel = Level.None;
            bool bestFree = false;
            float bestDays = 0f;
            foreach (var c in pool)
            {
                if (c == null || c.IsSick || c.IsUnderSquadProduction || IsLocked(c) || Guards.IsGuard(c)) continue;
                var current = c.WorkModule.CurrentWork;
                if (current == target) continue;
                float days = Experience.Days(c, job);
                var level = Experience.SelectionLevel(c, job);
                if (level < minLevel || level <= mustBeat) continue;
                string currentJob = Experience.CurrentJob(c);
                if (currentJob != null && currentJob != job && Experience.SelectionLevel(c, currentJob) >= level) continue;
                bool free = current == null || c.WorkModule.HasParentWork;
                bool better = best == null
                    || level > bestLevel
                    || (level == bestLevel && free && !bestFree)
                    || (level == bestLevel && free == bestFree && days > bestDays);
                if (!better) continue;
                best = c;
                bestLevel = level;
                bestFree = free;
                bestDays = days;
            }
            return best;
        }

        private static void EvictUnlocked(WorkBase work, List<string> list)
        {
            for (int i = work.Workers.Count - 1; i >= 0; i--)
            {
                var worker = work.Workers[i];
                if (worker != null && !list.Contains(worker.Id))
                {
                    worker.WorkModule.UnassignWork(true);
                    return;
                }
            }
        }

        private static List<string> GetOrCreate(string key)
        {
            Load();
            if (!Locks.TryGetValue(key, out var list))
            {
                list = new List<string>();
                Locks[key] = list;
            }
            return list;
        }

        private static void Changed()
        {
            _indexedSave = null;
            Save();
        }

        private static void EnsureIndex()
        {
            Load();
            string save = GoalStore.SaveId();
            if (_indexedSave == save) return;
            _indexedSave = save;
            LockedTo.Clear();
            string prefix = save + "|";
            foreach (var pair in Locks)
            {
                if (!pair.Key.StartsWith(prefix, StringComparison.Ordinal)) continue;
                foreach (string id in pair.Value) LockedTo[id] = pair.Key;
            }
        }

        private static void Save()
        {
            try
            {
                var lines = new List<string>();
                foreach (var pair in Locks)
                {
                    if (pair.Value.Count > 0) lines.Add("L\t" + pair.Key + "\t" + string.Join(",", pair.Value.ToArray()));
                }
                foreach (var pair in Limits)
                {
                    lines.Add("M\t" + pair.Key + "\t" + pair.Value.MinLevel + "\t" + pair.Value.Desired + "\t" + pair.Value.LastSet);
                }
                foreach (var pair in PriorityBeforeOff)
                {
                    lines.Add("O\t" + pair.Key + "\t" + pair.Value.ToString(CultureInfo.InvariantCulture));
                }
                File.WriteAllLines(FilePath, lines.ToArray());
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Could not save crews: " + e.Message);
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
                    if (parts.Length < 3) continue;
                    if (parts[0] == "L")
                        Locks[parts[1]] = new List<string>(parts[2].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                    else if (parts[0] == "M" && parts.Length >= 5
                        && int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int min)
                        && int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int desired)
                        && int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int last))
                        Limits[parts[1]] = new Limit { MinLevel = min, Desired = desired, LastSet = last };
                    else if (parts[0] == "O" && int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int p))
                        PriorityBeforeOff[parts[1]] = p;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Could not load crews: " + e.Message);
            }
        }
    }
}
