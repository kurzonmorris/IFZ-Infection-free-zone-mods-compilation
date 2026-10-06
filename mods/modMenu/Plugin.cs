using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using Controllers;
using HarmonyLib;
using UnityEngine;

namespace IFZ.ModMenu
{
    internal sealed class MenuEntry
    {
        public string Label;
        public KeyboardShortcut Shortcut;
        public Action Direct;
    }

    [BepInPlugin(Guid, Name, Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "kurzon.ifz.modMenu";
        public const string Name = "IFZ Mod Menu";
        public const string Version = "0.1.0";

        internal static ManualLogSource Log;

        private ConfigEntry<bool> _enabled;
        private ConfigEntry<float> _offsetX;
        private ConfigEntry<float> _offsetY;
        private ConfigEntry<string> _extraEntries;
        private ConfigEntry<float> _opacity;

        private readonly List<MenuEntry> _entries = new List<MenuEntry>();
        private bool _open;
        private float _nextScan;
        private Vector2 _scroll;
        private Texture2D _background;
        private GUIStyle _panel;
        private float _panelOpacity = -1f;

        private void Awake()
        {
            Log = Logger;
            _enabled = Config.Bind("General", "Enabled", true, "Master switch. False hides the button.");
            _offsetX = Config.Bind("Button", "OffsetX", 12f, "Distance of the button from the right edge of the screen, in pixels.");
            _offsetY = Config.Bind("Button", "OffsetY", 12f, "Distance of the button from the bottom edge of the screen, in pixels.");
            _opacity = Config.Bind("Button", "MenuOpacity", 0.95f, new ConfigDescription("Background opacity of the menu.", new AcceptableValueRange<float>(0.3f, 1f)));
            _extraEntries = Config.Bind("Menu", "ExtraEntries", "", "Extra menu items for mods whose keys are not in their settings. Format: Name=Key;Name=Ctrl+Key (for example: Mod Panels=F7).");
            try
            {
                new Harmony(Guid).CreateClassProcessor(typeof(FakeKeyPatch)).Patch();
                new Harmony(Guid + ".held").CreateClassProcessor(typeof(FakeHeldKeyPatch)).Patch();
            }
            catch (Exception e)
            {
                Logger.LogError("Key patch failed: " + e);
            }
            Logger.LogInfo($"{Name} v{Version} loaded.");
        }

        private void Update()
        {
            if (!_enabled.Value || Time.unscaledTime < _nextScan) return;
            _nextScan = Time.unscaledTime + 30f;
            Scan();
        }

        private void Scan()
        {
            _entries.Clear();
            try
            {
                foreach (var info in Chainloader.PluginInfos.Values)
                {
                    var plugin = info?.Instance;
                    if (plugin == null || info.Metadata.GUID == Guid) continue;
                    var found = new List<KeyValuePair<ConfigDefinition, KeyboardShortcut>>();
                    foreach (var pair in plugin.Config)
                    {
                        var entry = pair.Value;
                        KeyboardShortcut shortcut;
                        if (entry.SettingType == typeof(KeyboardShortcut)) shortcut = (KeyboardShortcut)entry.BoxedValue;
                        else if (entry.SettingType == typeof(KeyCode)) shortcut = new KeyboardShortcut((KeyCode)entry.BoxedValue);
                        else continue;
                        if (shortcut.MainKey == KeyCode.None) continue;
                        string text = (pair.Key.Section + " " + pair.Key.Key + " " + entry.Description?.Description).ToLowerInvariant();
                        if (!MenuRules.LooksLikeWindow(text)) continue;
                        found.Add(new KeyValuePair<ConfigDefinition, KeyboardShortcut>(pair.Key, shortcut));
                    }
                    var toggle = plugin.GetType().GetMethod("ToggleWindow", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance, null, Type.EmptyTypes, null);
                    foreach (var f in found)
                    {
                        string label = found.Count == 1 ? info.Metadata.Name : info.Metadata.Name + " — " + f.Key.Key;
                        Action direct = null;
                        if (toggle != null && found.Count == 1)
                        {
                            var target = toggle.IsStatic ? null : plugin;
                            direct = () => toggle.Invoke(target, null);
                        }
                        _entries.Add(new MenuEntry { Label = label, Shortcut = f.Value, Direct = direct });
                    }
                }
                foreach (string item in _extraEntries.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] parts = item.Split('=');
                    if (parts.Length != 2) continue;
                    var shortcut = KeyboardShortcut.Deserialize(parts[1].Trim());
                    if (shortcut.MainKey != KeyCode.None) _entries.Add(new MenuEntry { Label = parts[0].Trim(), Shortcut = shortcut });
                }
                _entries.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception e)
            {
                Logger.LogError("Scan failed: " + e);
            }
        }

        private void OnGUI()
        {
            if (!_enabled.Value) return;
            try
            {
                if (HqController.MainHeadquarter == null) return;
            }
            catch (Exception)
            {
                return;
            }

            var button = new Rect(Screen.width - _offsetX.Value - 72f, Screen.height - _offsetY.Value - 28f, 72f, 28f);
            if (GUI.Button(button, _open ? "Mods ▼" : "Mods ▲"))
            {
                _open = !_open;
                if (_open) Scan();
            }
            if (!_open) return;

            float rowHeight = 26f;
            float height = Mathf.Min(Screen.height * 0.6f, 40f + Mathf.Max(1, _entries.Count) * rowHeight);
            var panel = new Rect(Screen.width - _offsetX.Value - 260f, button.y - height - 4f, 260f, height);
            GUI.Box(panel, GUIContent.none, PanelStyle());
            GUILayout.BeginArea(new Rect(panel.x + 8f, panel.y + 6f, panel.width - 16f, panel.height - 12f));
            GUILayout.Label("<b>Mod windows</b>");
            _scroll = GUILayout.BeginScrollView(_scroll);
            if (_entries.Count == 0) GUILayout.Label("No mod windows found.");
            foreach (var entry in _entries)
            {
                if (GUILayout.Button($"{entry.Label}  <size=10>({entry.Shortcut})</size>"))
                {
                    if (entry.Direct != null)
                    {
                        try
                        {
                            entry.Direct();
                        }
                        catch (Exception e)
                        {
                            Logger.LogWarning($"Direct open failed for {entry.Label}, pressing the key instead: {e.Message}");
                            FakeKeys.Press(entry.Shortcut);
                        }
                    }
                    else
                    {
                        FakeKeys.Press(entry.Shortcut);
                    }
                    _open = false;
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private GUIStyle PanelStyle()
        {
            if (_panel != null && Mathf.Approximately(_panelOpacity, _opacity.Value) && _background != null) return _panel;
            _panelOpacity = _opacity.Value;
            if (_background == null) _background = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            _background.SetPixel(0, 0, new Color(0.10f, 0.11f, 0.13f, _panelOpacity));
            _background.Apply();
            _panel = new GUIStyle(GUI.skin.box);
            _panel.normal.background = _background;
            return _panel;
        }
    }

    internal static class MenuRules
    {
        private static readonly string[] WindowWords = { "toggle", "window", "menu", "open", "show", "panel", "dashboard", "gui", "ui" };

        public static bool LooksLikeWindow(string text)
        {
            foreach (string word in WindowWords)
            {
                if (word.Length <= 2)
                {
                    if ((" " + text + " ").Contains(" " + word + " ")) return true;
                }
                else if (text.Contains(word))
                {
                    return true;
                }
            }
            return false;
        }
    }

    internal static class FakeKeys
    {
        private static int _frame = -1;
        private static KeyboardShortcut _shortcut;

        public static void Press(KeyboardShortcut shortcut)
        {
            _shortcut = shortcut;
            _frame = Time.frameCount + 1;
        }

        public static bool IsDown(KeyCode key) => Time.frameCount == _frame && key == _shortcut.MainKey;

        public static bool IsHeld(KeyCode key)
        {
            if (Time.frameCount != _frame) return false;
            if (key == _shortcut.MainKey) return true;
            foreach (var modifier in _shortcut.Modifiers)
            {
                if (modifier == key) return true;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(Input), nameof(Input.GetKeyDown), new[] { typeof(KeyCode) })]
    internal static class FakeKeyPatch
    {
        private static void Postfix(KeyCode key, ref bool __result)
        {
            if (!__result && FakeKeys.IsDown(key)) __result = true;
        }
    }

    [HarmonyPatch(typeof(Input), nameof(Input.GetKey), new[] { typeof(KeyCode) })]
    internal static class FakeHeldKeyPatch
    {
        private static void Postfix(KeyCode key, ref bool __result)
        {
            if (!__result && FakeKeys.IsHeld(key)) __result = true;
        }
    }
}
