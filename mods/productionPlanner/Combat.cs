using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BepInEx;
using Controllers.CharacterLogic;
using Controllers.Time;
using Gameplay.Units;
using Gameplay.Units.Characters;
using Gameplay.Units.Virtual;
using Gameplay.Units.Skills;
using UnityEngine;

namespace IFZ.ProductionPlanner
{
    internal struct CombatBonus
    {
        public float Damage;
        public float FireRate;
        public float Range;
    }

    internal static class Combat
    {
        public static readonly float[] DamageByLevel = { 0f, 0.10f, 0.20f, 0.40f };
        public static readonly float[] FireRateByLevel = { 0f, 0.05f, 0.10f, 0.20f };
        public static readonly float[] RangeByLevel = { 0f, 0.10f, 0.20f, 0.30f };
        public static readonly float[] SkillBoostByLevel = { 0f, 0.10f, 0.25f, 0.50f };

        private static readonly string FilePath = Path.Combine(Paths.ConfigPath, Plugin.Guid + ".combat.txt");
        private static readonly Dictionary<string, float> MarksmanHours = new Dictionary<string, float>();
        private static readonly Dictionary<string, Dictionary<SkillId, float>> SkillHours = new Dictionary<string, Dictionary<SkillId, float>>();
        private static readonly Dictionary<ICharacter, CombatBonus> Bonuses = new Dictionary<ICharacter, CombatBonus>();
        private static readonly Dictionary<SkillsHandler, Character> SkillOwners = new Dictionary<SkillsHandler, Character>();
        private static readonly Dictionary<ICharacter, float> LastShot = new Dictionary<ICharacter, float>();
        private static bool _loaded;
        private static bool _dirty;
        private static float _lastGameplayTime = -1f;
        private static float _nextSave;

        public static bool IsCombatSkill(SkillId id) => id == SkillId.Slasher || id == SkillId.SharpShooter;

        public static Level MarksmanLevel(Character c) => CombatRules.MarksmanLevel(Hours(MarksmanHours, Key(c)));

        public static float MarksmanHoursOf(Character c) => Hours(MarksmanHours, Key(c));

        public static float SkillHoursOf(Character c, SkillId id)
        {
            Load();
            return c != null && SkillHours.TryGetValue(Key(c), out var map) && map.TryGetValue(id, out float h) ? h : 0f;
        }

        public static Level SkillLevel(Character c, SkillId id)
        {
            if (c?.SkillsHandler == null || !c.SkillsHandler.HasActiveSkill(id)) return Level.None;
            return CombatRules.SquadSkillLevel(SkillHoursOf(c, id), CombatRules.ShiftHours);
        }

        public static bool TryGetBonus(ICharacter c, out CombatBonus bonus) => Bonuses.TryGetValue(c, out bonus);

        public static float SkillMultiplier(SkillsHandler handler, SkillId id)
        {
            if (handler == null || IsCombatSkill(id) || !SkillOwners.TryGetValue(handler, out var c)) return 1f;
            return 1f + SkillBoostByLevel[(int)SkillLevel(c, id)];
        }

        public static void Tick()
        {
            Load();
            float now = TimeController.GameplayTime;
            float delta = _lastGameplayTime < 0f || now < _lastGameplayTime ? 0f : now - _lastGameplayTime;
            _lastGameplayTime = now;
            float hourGts = Mathf.Max(1, TimeController.HourLengthInGts);

            if (delta > 0f)
            {
                var squads = Planner.Resolve<SquadsController>()?.Squads;
                if (squads != null)
                {
                    foreach (var squad in squads)
                    {
                        if (squad == null || !IsActiveOutside(squad)) continue;
                        foreach (var c in squad.Characters)
                        {
                            if (c?.SkillsHandler == null) continue;
                            foreach (var skill in c.SkillsHandler.Skills)
                            {
                                if (skill == null || !skill.IsActivated) continue;
                                AddSkillHours(c, skill.Id, delta / hourGts);
                            }
                        }
                    }
                }
            }

            Bonuses.Clear();
            SkillOwners.Clear();
            var citizens = Planner.Resolve<CitizensController>()?.Citizens;
            if (citizens != null)
            {
                foreach (var c in citizens)
                {
                    if (c == null) continue;
                    if (c.SkillsHandler != null) SkillOwners[c.SkillsHandler] = c;
                    var level = MarksmanLevel(c);
                    var meleeLevel = SkillLevel(c, SkillId.Slasher);
                    var rangedLevel = SkillLevel(c, SkillId.SharpShooter);
                    bool melee = c.Weapon != null && c.Weapon.IsMelee;
                    var skillLevel = melee ? meleeLevel : rangedLevel;
                    if (skillLevel > level) level = skillLevel;
                    if (level == Level.None) continue;
                    Bonuses[c] = new CombatBonus
                    {
                        Damage = DamageByLevel[(int)level],
                        FireRate = FireRateByLevel[(int)level],
                        Range = RangeByLevel[(int)level]
                    };
                }
            }

            if (_dirty && Time.unscaledTime >= _nextSave)
            {
                _nextSave = Time.unscaledTime + 30f;
                Save();
            }
        }

        public static void OnShot(ICharacter shooter, float cooldownGts)
        {
            if (!(shooter is Character c) || c.WorkModule == null || !c.WorkModule.IsDefenceWorkAssigned) return;
            float now = TimeController.GameplayTime;
            if (LastShot.TryGetValue(shooter, out float last))
            {
                float gap = now - last;
                float cap = Mathf.Max(2f * cooldownGts, 10f);
                if (gap > 0f && gap <= cap)
                {
                    Load();
                    string key = Key(c);
                    MarksmanHours[key] = Hours(MarksmanHours, key) + gap / Mathf.Max(1, TimeController.HourLengthInGts);
                    _dirty = true;
                }
            }
            LastShot[shooter] = now;
        }

        private static bool IsActiveOutside(Group squad)
        {
            if (squad.IsOnExpedition()) return true;
            if (squad.IsInsideEnterable()) return false;
            return squad.IsMoving || squad.IsScavenging || (squad.EnemiesProvider != null && squad.EnemiesProvider.HaveEnemyInViewRange());
        }

        private static void AddSkillHours(Character c, SkillId id, float hours)
        {
            string key = Key(c);
            if (!SkillHours.TryGetValue(key, out var map))
            {
                map = new Dictionary<SkillId, float>();
                SkillHours[key] = map;
            }
            map.TryGetValue(id, out float h);
            map[id] = h + hours;
            _dirty = true;
        }

        private static float Hours(Dictionary<string, float> map, string key)
        {
            Load();
            return key != null && map.TryGetValue(key, out float h) ? h : 0f;
        }

        private static string Key(Character c) => c == null ? null : GoalStore.SaveId() + "|" + c.Id;

        public static void Save()
        {
            _dirty = false;
            try
            {
                var keys = new HashSet<string>(MarksmanHours.Keys);
                keys.UnionWith(SkillHours.Keys);
                var lines = new List<string>();
                foreach (var key in keys)
                {
                    MarksmanHours.TryGetValue(key, out float m);
                    var parts = new List<string>();
                    if (SkillHours.TryGetValue(key, out var map))
                        foreach (var p in map) parts.Add(((int)p.Key).ToString(CultureInfo.InvariantCulture) + "=" + p.Value.ToString("0.###", CultureInfo.InvariantCulture));
                    lines.Add(key + "\t" + m.ToString("0.###", CultureInfo.InvariantCulture) + "\t" + string.Join(",", parts.ToArray()));
                }
                File.WriteAllLines(FilePath, lines.ToArray());
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Could not save combat experience: " + e.Message);
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
                    if (float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float m) && m > 0f) MarksmanHours[parts[0]] = m;
                    if (parts.Length < 3 || parts[2].Length == 0) continue;
                    var map = new Dictionary<SkillId, float>();
                    foreach (string item in parts[2].Split(','))
                    {
                        string[] f = item.Split('=');
                        if (f.Length == 2 && int.TryParse(f[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)
                            && float.TryParse(f[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float h))
                            map[(SkillId)id] = h;
                    }
                    SkillHours[parts[0]] = map;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Could not load combat experience: " + e.Message);
            }
        }
    }

    internal static class CombatRules
    {
        public const float MarksmanNoviceHours = 6f;
        public const float MarksmanModerateHours = 21f;
        public const float MarksmanExpertHours = 61f;
        public const float ShiftHours = 12f;
        public const float SquadModerateShifts = 10f;
        public const float SquadExpertShifts = 24f;

        public static Level MarksmanLevel(float hours)
        {
            if (hours >= MarksmanExpertHours) return Level.Expert;
            if (hours >= MarksmanModerateHours) return Level.Moderate;
            if (hours >= MarksmanNoviceHours) return Level.Novice;
            return Level.None;
        }

        public static Level SquadSkillLevel(float activeHours, float shiftHours)
        {
            float shifts = activeHours / shiftHours;
            if (shifts >= SquadExpertShifts) return Level.Expert;
            if (shifts >= SquadModerateShifts) return Level.Moderate;
            return Level.Novice;
        }
    }
}
