using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Controllers;
using Controllers.CharacterLogic;
using Controllers.Time;
using Data.Resorces;
using Gameplay.GameResources;
using Gameplay.Rebuilding;
using Gameplay.Units;
using Gameplay.Units.Characters;
using Gameplay.Units.Enemy;
using Gameplay.Units.Equipment;
using UnityEngine;

namespace IFZ.ProductionPlanner
{
    internal static class Guards
    {
        public const int SpacesPerGuard = 25;
        public static float KillsPerGuardHour = 1f;
        public static float ShootRadius = 35f;
        public static ResourceID WeaponId = ResourceID.eq_pistol;

        private static readonly string FilePath = Path.Combine(Paths.ConfigPath, Plugin.Guid + ".guards.txt");
        private static readonly Dictionary<string, List<string>> GuardIds = new Dictionary<string, List<string>>();
        private static readonly Dictionary<string, float> KillProgress = new Dictionary<string, float>();
        private static readonly Dictionary<Character, float> NextShot = new Dictionary<Character, float>();
        private static bool _loaded;
        private static float _lastGameplayTime = -1f;

        private static int _sickDeaths;
        private static float _sickTurnWeight;

        public static int MaxGuards(Structure house) => Math.Max(1, Mathf.CeilToInt(house.GetCitizensCapacity() / (float)SpacesPerGuard));

        public static IReadOnlyList<string> GuardList(Structure house)
        {
            Load();
            return GuardIds.TryGetValue(Houses.Key(house), out var list) ? list : (IReadOnlyList<string>)Array.Empty<string>();
        }

        private static readonly HashSet<string> Index = new HashSet<string>();
        private static string _indexedSave;

        public static bool IsGuard(Character c)
        {
            if (c == null || string.IsNullOrEmpty(c.Id)) return false;
            Load();
            string save = GoalStore.SaveId();
            if (_indexedSave != save)
            {
                _indexedSave = save;
                Index.Clear();
                string prefix = save + "|";
                foreach (var pair in GuardIds)
                {
                    if (pair.Key.StartsWith(prefix, StringComparison.Ordinal)) Index.UnionWith(pair.Value);
                }
            }
            return Index.Contains(c.Id);
        }

        public static bool IsThreat(Group g)
        {
            if (g == null || g.Fraction == Fraction.Player || g.AffiliationProvider == null) return false;
            return g.AffiliationProvider.Get(Fraction.Player) == Affiliation.Hostile;
        }

        public static bool IsFullyGuarded(Structure house) => house != null && Houses.IsHouse(house) && GuardList(house).Count >= MaxGuards(house);

        public static bool MakeGuard(Structure house, Character c)
        {
            if (c == null || c.IsChild || c.IsSoldier || IsGuard(c) || Crews.IsLocked(c) || Houses.IsOff(house)) return false;
            var list = GetOrCreate(house);
            if (list.Count >= MaxGuards(house)) return false;
            if (!Houses.IsLockedTo(c, house) && !Houses.Lock(house, c)) return false;
            list.Add(c.Id);
            Save();
            if (c.WorkModule.CurrentWork != null) c.WorkModule.UnassignWork(true);
            return true;
        }

        public static void RemoveGuard(Structure house, string id)
        {
            Load();
            if (GuardIds.TryGetValue(Houses.Key(house), out var list) && list.Remove(id)) Save();
        }

        public static void Tick()
        {
            Load();
            float now = TimeController.GameplayTime;
            float delta = _lastGameplayTime < 0f || now < _lastGameplayTime ? 0f : now - _lastGameplayTime;
            _lastGameplayTime = now;

            var citizens = Planner.Resolve<CitizensController>()?.Citizens;
            if (citizens == null) return;
            var byId = new Dictionary<string, Character>();
            foreach (var c in citizens)
            {
                if (c != null && !string.IsNullOrEmpty(c.Id)) byId[c.Id] = c;
            }
            var houses = new Dictionary<string, Structure>();
            var buildings = Planner.Resolve<BuildingsController>()?.AdaptedBuildings;
            if (buildings != null)
            {
                foreach (var b in buildings)
                {
                    if (b != null && Houses.IsHouse(b)) houses[Houses.Key(b)] = b;
                }
            }

            string prefix = GoalStore.SaveId() + "|";
            bool changed = false;
            var all = Planner.Resolve<GroupsController>()?.Groups;
            List<Group> infected = null;
            if (all != null)
            {
                infected = new List<Group>();
                foreach (var g in all)
                {
                    if (IsThreat(g)) infected.Add(g);
                }
            }
            foreach (var key in new List<string>(GuardIds.Keys))
            {
                if (!key.StartsWith(prefix, StringComparison.Ordinal)) continue;
                var list = GuardIds[key];
                if (!houses.TryGetValue(key, out var house) || Houses.IsOff(house))
                {
                    if (houses.Count > 0)
                    {
                        GuardIds.Remove(key);
                        changed = true;
                    }
                    continue;
                }
                var inside = new List<Character>();
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (!byId.TryGetValue(list[i], out var g) || g.IsSoldier || g.IsUnderSquadProduction || !Houses.IsLockedTo(g, house))
                    {
                        list.RemoveAt(i);
                        changed = true;
                        continue;
                    }
                    if (g.WorkModule.CurrentWork != null) g.WorkModule.UnassignWork(true);
                    if (!g.IsSick && g.Enterable == (Gameplay.Core.EnterableSystem.IEnterable)house) inside.Add(g);
                }
                if (delta > 0f && inside.Count > 0 && infected != null)
                {
                    ClearInside(key, house, inside, infected, delta);
                    Defend(house, inside, infected, now);
                }
            }
            if (changed) Save();
        }

        private static void ClearInside(string key, Structure house, List<Character> guards, List<Group> infected, float deltaGts)
        {
            Group group = null;
            foreach (var g in infected)
            {
                if (g != null && g.Characters.Count > 0 && g.Enterable == (Gameplay.Core.EnterableSystem.IEnterable)house)
                {
                    group = g;
                    break;
                }
            }
            if (group == null)
            {
                KillProgress.Remove(key);
                return;
            }
            float hours = deltaGts / Mathf.Max(1, TimeController.HourLengthInGts);
            KillProgress.TryGetValue(key, out float progress);
            progress += guards.Count * KillsPerGuardHour * hours;
            foreach (var guard in guards) Combat.AddMarksmanHours(guard, hours);
            while (progress >= 1f && group.Characters.Count > 0)
            {
                var victim = group.Characters[group.Characters.Count - 1];
                progress -= 1f;
                if (victim != null) victim.Kill(SubtractionHpReason.Enemy);
            }
            KillProgress[key] = progress;
        }

        private static void Defend(Structure house, List<Character> guards, List<Group> infected, float now)
        {
            var weapon = ResourcesDataContainer.Instance?.GetWeaponById(WeaponId);
            if (weapon?.Stats == null) return;
            var stock = Planner.Resolve<StockroomsController>()?.Container;
            Vector3 position = house.transform.position;
            float radius = Mathf.Min(ShootRadius, weapon.Stats.GetReach(false, 0f));
            foreach (var guard in guards)
            {
                if (NextShot.TryGetValue(guard, out float next) && now < next) continue;
                Character target = null;
                float best = radius * radius;
                foreach (var g in infected)
                {
                    if (g == null || g.IsInsideEnterable()) continue;
                    foreach (var c in g.Characters)
                    {
                        if (c == null || c.IsDead()) continue;
                        float d = (c.Position - position).sqrMagnitude;
                        if (d < best)
                        {
                            best = d;
                            target = c;
                        }
                    }
                }
                if (target == null) continue;
                int ammo = Math.Max(0, weapon.ammoAmountUsed);
                if (ammo > 0 && (stock == null || !stock.HasResource(ResourceID.res_ammo) || !stock.TryRemoveResource(ResourceID.res_ammo, ammo))) continue;
                Combat.TryGetBonus(guard, out var bonus);
                float distance = Mathf.Sqrt(best);
                float damage = weapon.Stats.GetDamageForDistance(distance, false, false, 0f) * (1f + bonus.Damage);
                target.ReceiveDamage(weapon, damage, SubtractionHpReason.Enemy);
                float cooldown = Mathf.Max(0.2f, weapon.Stats.AttackCooldownGts) / (1f + bonus.FireRate);
                NextShot[guard] = now + cooldown;
                Combat.AddMarksmanHours(guard, cooldown / Mathf.Max(1, TimeController.HourLengthInGts));
            }
        }

        public static void BeginSicknessDeaths()
        {
            _sickDeaths = 0;
            _sickTurnWeight = 0f;
        }

        public static void OnCitizenKilled(Character c, SubtractionHpReason reason)
        {
            if (reason != SubtractionHpReason.Sickness || c == null) return;
            _sickDeaths++;
            var house = c.HouseData?.House as Structure;
            _sickTurnWeight += IsFullyGuarded(house) ? 0.5f : 1f;
        }

        public static int AdjustHorde(int count)
        {
            if (_sickDeaths <= 0) return count;
            float factor = _sickTurnWeight / _sickDeaths;
            BeginSicknessDeaths();
            return Mathf.RoundToInt(count * factor);
        }

        private static List<string> GetOrCreate(Structure house)
        {
            Load();
            string key = Houses.Key(house);
            if (!GuardIds.TryGetValue(key, out var list))
            {
                list = new List<string>();
                GuardIds[key] = list;
            }
            return list;
        }

        private static void Save()
        {
            _indexedSave = null;
            try
            {
                var lines = new List<string>();
                foreach (var pair in GuardIds)
                {
                    if (pair.Value.Count > 0) lines.Add(pair.Key + "\t" + string.Join(",", pair.Value.ToArray()));
                }
                File.WriteAllLines(FilePath, lines.ToArray());
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Could not save guards: " + e.Message);
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
                    if (parts.Length >= 2)
                        GuardIds[parts[0]] = new List<string>(parts[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Could not load guards: " + e.Message);
            }
        }
    }
}
