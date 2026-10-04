using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace IFZ.Template
{
    [BepInPlugin(Guid, Name, Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "kurzon.ifz.template";
        public const string Name = "IFZ Template";
        public const string Version = "0.1.0";

        internal static ConfigEntry<bool> Enabled;
        private Harmony _harmony;

        private void Awake()
        {
            Enabled = Config.Bind("General", "Enabled", true, "Master switch. False makes the mod do nothing.");
            _harmony = new Harmony(Guid);
            foreach (var type in typeof(Plugin).Assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0) continue;
                try { _harmony.CreateClassProcessor(type).Patch(); }
                catch (Exception e) { Logger.LogError($"Patch {type.Name} failed: {e}"); }
            }
            Logger.LogInfo($"{Name} v{Version} loaded.");
        }

        private void OnDestroy() => _harmony?.UnpatchSelf();
    }
}
