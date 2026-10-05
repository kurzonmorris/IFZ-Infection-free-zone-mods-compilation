using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
            if (character == null || string.IsNullOrEmpty(character.Id) || IsLocked(character)) return false;
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

        public static void Maintain()
        {
            Load();
            string save = GoalStore.SaveId() + "|";
            var works = Planner.Resolve<WorkController>()?.Works;
            var workersController = Planner.Resolve<WorkersController>();
            var citizens = Planner.Resolve<CitizensController>();
            if (works == null || workersController?.Workers == null || citizens?.Citizens == null) return;

            var byKey = new Dictionary<string, KeyValuePair<Structure, WorkBase>>();
            foreach (var work in works)
            {
                var structure = work?.GetRelatedStructure();
                if (structure == null || StaffWork(structure) != work) continue;
                byKey[Key(structure)] = new KeyValuePair<Structure, WorkBase>(structure, work);
            }
            var byId = new Dictionary<string, Character>();
            foreach (var citizen in citizens.Citizens)
            {
                if (citizen != null && !string.IsNullOrEmpty(citizen.Id)) byId[citizen.Id] = citizen;
            }
            var available = new HashSet<Character>(workersController.Workers);

            bool changed = false;
            foreach (var key in new List<string>(Locks.Keys))
            {
                if (!key.StartsWith(save, StringComparison.Ordinal)) continue;
                var list = Locks[key];
                if (!byKey.TryGetValue(key, out var pair) || IsOff(pair.Value))
                {
                    Locks.Remove(key);
                    changed = true;
                    continue;
                }
                var work = pair.Value;
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
                        work.AddWorker(c);
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.LogWarning($"Could not return {c.Name} to their job: {e.Message}");
                    }
                }
                if (list.Count == 0)
                {
                    Locks.Remove(key);
                    changed = true;
                }
            }
            if (changed) Changed();
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
