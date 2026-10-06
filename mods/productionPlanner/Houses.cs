using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Controllers;
using Controllers.CharacterLogic;
using Gameplay.Core.EnterableSystem;
using Gameplay.Rebuilding;
using Gameplay.Units.Characters;

namespace IFZ.ProductionPlanner
{
    internal static class Houses
    {
        private static readonly string FilePath = Path.Combine(Paths.ConfigPath, Plugin.Guid + ".houses.txt");
        private static readonly Dictionary<string, List<string>> Locks = new Dictionary<string, List<string>>();
        private static readonly HashSet<string> Off = new HashSet<string>();
        private static readonly Dictionary<string, string> LockedTo = new Dictionary<string, string>();
        private static readonly Dictionary<string, Structure> StructureByKey = new Dictionary<string, Structure>();
        private static string _indexedSave;
        private static bool _loaded;

        public static bool IsHouse(Structure structure)
        {
            if (structure?.Draft == null || structure.Draft.IsHq) return false;
            return structure.Draft.HasLivingQuartersModule && structure.Draft.HousePriority > 0 && structure.GetCitizensCapacity() > 0;
        }

        public static string Key(Structure structure) => GoalStore.Key(structure);

        public static bool IsOff(Structure structure)
        {
            Load();
            return structure != null && Off.Contains(Key(structure));
        }

        public static IReadOnlyList<string> LockList(Structure structure)
        {
            Load();
            return Locks.TryGetValue(Key(structure), out var list) ? list : (IReadOnlyList<string>)Array.Empty<string>();
        }

        public static bool IsLocked(Character c)
        {
            EnsureIndex();
            return c != null && !string.IsNullOrEmpty(c.Id) && LockedTo.ContainsKey(c.Id);
        }

        public static bool IsLockedTo(Character c, Structure house)
        {
            EnsureIndex();
            return c != null && house != null && LockedTo.TryGetValue(c.Id ?? "", out var key) && key == Key(house);
        }

        public static Structure LockedHouse(Character c)
        {
            EnsureIndex();
            if (c == null || string.IsNullOrEmpty(c.Id) || !LockedTo.TryGetValue(c.Id, out var key)) return null;
            return StructureByKey.TryGetValue(key, out var house) && house != null ? house : null;
        }

        public static bool Lock(Structure house, Character c)
        {
            Load();
            if (!IsHouse(house) || IsOff(house) || c == null || string.IsNullOrEmpty(c.Id) || IsLocked(c)) return false;
            var list = GetOrCreate(house);
            if (list.Count >= house.GetCitizensCapacity()) return false;
            list.Add(c.Id);
            StructureByKey[Key(house)] = house;
            Changed();
            MoveIn(house, c);
            return true;
        }

        public static void Unlock(Structure house, string id)
        {
            Load();
            if (Locks.TryGetValue(Key(house), out var list) && list.Remove(id)) Changed();
        }

        public static void UnlockAll(Structure house)
        {
            Load();
            if (Locks.Remove(Key(house))) Changed();
        }

        public static void LockResidents(Structure house)
        {
            foreach (var c in new List<Character>(house.LivingCitizens))
            {
                if (!IsLocked(c)) Lock(house, c);
            }
        }

        public static void TurnOff(Structure house)
        {
            Load();
            string key = Key(house);
            Off.Add(key);
            Locks.Remove(key);
            Changed();
            EvictAll(house);
        }

        public static void TurnOn(Structure house)
        {
            Load();
            if (Off.Remove(Key(house))) Changed();
            HqController.MainHeadquarter?.FindNewHousesForCitizens(house.GetCitizensCapacity());
        }

        public static void MoveIn(Structure house, Character c)
        {
            if (c.HouseData.House as Structure == house) return;
            if (!house.HasSpaceForCitizen()) EvictOneUnlocked(house);
            if (house.HasSpaceForCitizen() || IsLockedTo(c, house)) c.HouseData.House = house as IEnterable;
        }

        public static void Maintain()
        {
            Load();
            var citizens = Planner.Resolve<CitizensController>()?.Citizens;
            if (citizens == null) return;
            var buildings = Planner.Resolve<BuildingsController>()?.AdaptedBuildings;
            if (buildings != null)
            {
                foreach (var b in buildings)
                {
                    if (b != null && IsHouse(b)) Remember(b);
                }
            }
            string save = GoalStore.SaveId() + "|";
            var byId = new Dictionary<string, Character>();
            foreach (var c in citizens)
            {
                if (c != null && !string.IsNullOrEmpty(c.Id)) byId[c.Id] = c;
            }
            bool changed = false;
            foreach (var key in new List<string>(Locks.Keys))
            {
                if (!key.StartsWith(save, StringComparison.Ordinal)) continue;
                if (!StructureByKey.TryGetValue(key, out var house) || house == null || !IsHouse(house) || Off.Contains(key))
                {
                    if (house == null && !StructureByKey.ContainsKey(key)) continue;
                    Locks.Remove(key);
                    changed = true;
                    continue;
                }
                var list = Locks[key];
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (!byId.TryGetValue(list[i], out var c))
                    {
                        list.RemoveAt(i);
                        changed = true;
                        continue;
                    }
                    if (c.HouseData.House as Structure != house) MoveIn(house, c);
                }
                if (list.Count == 0)
                {
                    Locks.Remove(key);
                    changed = true;
                }
            }
            foreach (var key in Off)
            {
                if (key.StartsWith(save, StringComparison.Ordinal) && StructureByKey.TryGetValue(key, out var house) && house != null && house.LivingCitizens.Count > 0)
                    EvictAll(house);
            }
            if (changed) Changed();
        }

        public static void Remember(Structure house)
        {
            if (house != null) StructureByKey[Key(house)] = house;
        }

        public static void RememberAll(IEnumerable<Structure> houses)
        {
            foreach (var h in houses) Remember(h);
        }

        private static void EvictAll(Structure house)
        {
            var hq = HqController.MainHeadquarter;
            foreach (var c in new List<Character>(house.LivingCitizens))
            {
                if (c == null) continue;
                c.HouseData.House = hq as IEnterable;
                c.FindBestHouse();
            }
        }

        private static void EvictOneUnlocked(Structure house)
        {
            var hq = HqController.MainHeadquarter;
            for (int i = house.LivingCitizens.Count - 1; i >= 0; i--)
            {
                var c = house.LivingCitizens[i];
                if (c == null || IsLockedTo(c, house)) continue;
                c.HouseData.House = hq as IEnterable;
                c.FindBestHouse();
                return;
            }
        }

        private static List<string> GetOrCreate(Structure house)
        {
            string key = Key(house);
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
            try
            {
                var lines = new List<string>();
                foreach (var pair in Locks)
                {
                    if (pair.Value.Count > 0) lines.Add("L\t" + pair.Key + "\t" + string.Join(",", pair.Value.ToArray()));
                }
                foreach (var key in Off) lines.Add("X\t" + key);
                File.WriteAllLines(FilePath, lines.ToArray());
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Could not save houses: " + e.Message);
            }
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
                    if (parts.Length >= 3 && parts[0] == "L")
                        Locks[parts[1]] = new List<string>(parts[2].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                    else if (parts.Length >= 2 && parts[0] == "X")
                        Off.Add(parts[1]);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Could not load houses: " + e.Message);
            }
        }
    }
}
