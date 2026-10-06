using UnityEngine;

namespace IFZ.ProductionPlanner
{
    internal static class UiStyle
    {
        public static float Opacity = 0.95f;
        private static Texture2D _background;
        private static GUIStyle _window;
        private static float _builtFor = -1f;

        public static GUIStyle Window
        {
            get
            {
                if (_window == null || !Mathf.Approximately(_builtFor, Opacity) || _background == null)
                {
                    _builtFor = Opacity;
                    if (_background == null)
                    {
                        _background = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                    }
                    _background.SetPixel(0, 0, new Color(0.10f, 0.11f, 0.13f, Opacity));
                    _background.Apply();
                    _window = new GUIStyle(GUI.skin.window);
                    _window.normal.background = _background;
                    _window.onNormal.background = _background;
                    _window.focused.background = _background;
                    _window.onFocused.background = _background;
                    _window.normal.textColor = Color.white;
                    _window.onNormal.textColor = Color.white;
                    _window.alignment = TextAnchor.UpperLeft;
                }
                return _window;
            }
        }

        public static void OpacitySlider(float windowWidth)
        {
            var label = new Rect(windowWidth - 172f, 2f, 52f, 16f);
            var slider = new Rect(windowWidth - 118f, 5f, 90f, 12f);
            GUI.Label(label, "<size=10>Opacity</size>");
            float value = GUI.HorizontalSlider(slider, Opacity, 0.3f, 1f);
            if (!Mathf.Approximately(value, Opacity))
            {
                Opacity = value;
                Plugin.OpacityChanged(value);
            }
        }
    }
}
