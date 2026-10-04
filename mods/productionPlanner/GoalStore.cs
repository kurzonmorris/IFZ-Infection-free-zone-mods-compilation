using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BepInEx;
using Gameplay.Rebuilding;

namespace IFZ.ProductionPlanner
{
    internal sealed class Goal
    {
        public float PerDay;
        public bool Auto;
    }

    internal static class GoalStore
    {
        private static readonly string FilePath = Path.Combine(Paths.ConfigPath, Plugin.Guid + ".goals.txt");
        private static readonly Dictionary<string, Goal> Goals = new Dictionary<string, Goal>();
        private static bool _loaded;

        public static string SaveId()
        {
            string id = SaveHandler.SaveData?.GameplayId;
            if (!string.IsNullOrEmpty(id)) return "game:" + id;
            if (!string.IsNullOrEmpty(SaveHandler.SelectedTilesCoordinates)) return "zone:" + SaveHandler.SelectedTilesCoordinates;
            return "unsaved";
        }

        public static string Key(Structure structure) => SaveId() + "|" + structure.GetId();

        public static Goal Get(Structure structure)
        {
            Load();
            Goals.TryGetValue(Key(structure), out var goal);
            return goal;
        }

        public static Goal GetOrCreate(Structure structure)
        {
            Load();
            string key = Key(structure);
            if (!Goals.TryGetValue(key, out var goal))
            {
                goal = new Goal();
                Goals[key] = goal;
            }
            return goal;
        }

        public static void Save()
        {
            try
            {
                var lines = new List<string>();
                foreach (var pair in Goals)
                {
                    if (pair.Value.PerDay <= 0f && !pair.Value.Auto) continue;
                    lines.Add(pair.Key + "\t" + pair.Value.PerDay.ToString(CultureInfo.InvariantCulture) + "\t" + (pair.Value.Auto ? "1" : "0"));
                }
                File.WriteAllLines(FilePath, lines.ToArray());
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Could not save goals: " + e.Message);
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
                    if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float perDay)) continue;
                    Goals[parts[0]] = new Goal { PerDay = perDay, Auto = parts[2] == "1" };
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Could not load goals: " + e.Message);
            }
        }
    }
}
