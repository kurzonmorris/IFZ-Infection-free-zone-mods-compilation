using System;
using System.Collections.Generic;
using Controllers.CharacterLogic;
using Gameplay.Rebuilding;
using Gameplay.Units.Characters;
using UnityEngine;

namespace IFZ.ProductionPlanner
{
    internal static class HouseWindow
    {
        private enum SortMode { Name, Gender, House }

        private const float MinWidth = 520f;
        private const float MinHeight = 300f;
        private static readonly int WindowId = (Plugin.Guid + ".residents").GetHashCode();

        public static bool Open;
        private static Structure _house;
        private static Rect _rect = new Rect(500f, 140f, 640f, 460f);
        private static bool _resizing;
        private static Vector2 _leftScroll;
        private static Vector2 _rightScroll;
        private static SortMode _sort = SortMode.Name;
        private static bool _descending;
        private static string _filter = "";

        public static void Show(Structure house)
        {
            _house = house;
            Open = true;
        }

        public static void OnGUI()
        {
            if (!Open) return;
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
            _rect = GUI.Window(WindowId, _rect, Draw, "Residents");
        }

        private static void Draw(int id)
        {
            try
            {
                DrawContent();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("Residents window failed: " + ex);
                Open = false;
            }
            GUI.Box(new Rect(_rect.width - 18f, _rect.height - 18f, 18f, 18f), "◢");
            GUI.DragWindow(new Rect(0f, 0f, _rect.width - 30f, 20f));
        }

        private static void DrawContent()
        {
            GUILayout.BeginArea(new Rect(8f, 22f, _rect.width - 16f, _rect.height - 44f));
            GUILayout.BeginHorizontal();
            GUILayout.Label(_house != null ? "<b>" + Planner.BuildingName(_house) + "</b>" : "No house");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(60f))) Open = false;
            GUILayout.EndHorizontal();

            if (!Houses.IsHouse(_house))
            {
                GUILayout.Label("This building is not a house.");
                GUILayout.EndArea();
                return;
            }
            if (Houses.IsOff(_house))
            {
                GUILayout.Label("This house is turned off. Turn it on in the planner window first.");
                GUILayout.EndArea();
                return;
            }
            Houses.Remember(_house);
            int capacity = _house.GetCitizensCapacity();
            var lockList = Houses.LockList(_house);
            GUILayout.Label($"Residents: {_house.LivingCitizens.Count}/{capacity} · Locked: {lockList.Count}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Lock current residents")) Houses.LockResidents(_house);
            if (GUILayout.Button("Unlock all")) Houses.UnlockAll(_house);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Sort:", GUILayout.Width(36f));
            SortButton("Name", SortMode.Name);
            SortButton("Gender", SortMode.Gender);
            SortButton("House", SortMode.House);
            GUILayout.Label("Find:", GUILayout.Width(34f));
            _filter = GUILayout.TextField(_filter, 20, GUILayout.Width(100f));
            GUILayout.EndHorizontal();

            float columnWidth = (_rect.width - 40f) / 2f;
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(columnWidth));
            GUILayout.Label("<b>Citizens</b>");
            _leftScroll = GUILayout.BeginScrollView(_leftScroll);
            foreach (var c in Candidates())
            {
                bool lockedElsewhere = Houses.IsLocked(c);
                GUILayout.BeginHorizontal();
                GUILayout.Label(Row(c, lockedElsewhere));
                GUI.enabled = !lockedElsewhere && lockList.Count < capacity;
                if (GUILayout.Button("→", GUILayout.Width(28f))) Houses.Lock(_house, c);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(columnWidth));
            GUILayout.Label("<b>Locked residents</b>");
            _rightScroll = GUILayout.BeginScrollView(_rightScroll);
            var byId = CitizensById();
            foreach (string cid in new List<string>(lockList))
            {
                byId.TryGetValue(cid, out var c);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("←", GUILayout.Width(28f))) Houses.Unlock(_house, cid);
                GUILayout.Label(c != null ? Row(c, false) : "(missing)");
                GUILayout.EndHorizontal();
            }
            var unlocked = new List<Character>();
            foreach (var c in _house.LivingCitizens)
            {
                if (c != null && !Houses.IsLockedTo(c, _house)) unlocked.Add(c);
            }
            if (unlocked.Count > 0)
            {
                GUILayout.Space(6f);
                GUILayout.Label("<b>Living here, not locked</b>");
                foreach (var c in unlocked)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Row(c, Houses.IsLocked(c)));
                    GUI.enabled = !Houses.IsLocked(c) && lockList.Count < capacity;
                    if (GUILayout.Button("Lock", GUILayout.Width(50f))) Houses.Lock(_house, c);
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
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

        private static List<Character> Candidates()
        {
            var result = new List<Character>();
            var citizens = Planner.Resolve<CitizensController>()?.Citizens;
            if (citizens == null) return result;
            foreach (var c in citizens)
            {
                if (c == null || Houses.IsLockedTo(c, _house)) continue;
                if (_filter.Length > 0 && (c.Name ?? "").IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                result.Add(c);
            }
            Comparison<Character> compare;
            switch (_sort)
            {
                case SortMode.Gender:
                    compare = (a, b) => string.CompareOrdinal(a.CharacterInfo.Gender.ToString(), b.CharacterInfo.Gender.ToString()) is int g && g != 0 ? g : string.CompareOrdinal(a.Name, b.Name);
                    break;
                case SortMode.House:
                    compare = (a, b) => string.CompareOrdinal(HouseLabel(a), HouseLabel(b)) is int h && h != 0 ? h : string.CompareOrdinal(a.Name, b.Name);
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
            string who = c.IsChild ? "child" : (c.IsSoldier ? "soldier" : "worker");
            string row = $"{c.Name} · {c.CharacterInfo.Gender} · {c.CharacterInfo.Age} · {who} · {HouseLabel(c)}";
            if (lockedElsewhere) row = "<color=grey>" + row + " · locked</color>";
            return row;
        }

        private static string HouseLabel(Character c)
        {
            var house = c.HouseData.House as Structure;
            if (house == null) return "Homeless";
            if (house.Draft != null && house.Draft.IsHq) return "HQ";
            return Planner.BuildingName(house);
        }
    }
}
