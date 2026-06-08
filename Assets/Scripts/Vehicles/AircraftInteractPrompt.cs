using UnityEngine;

namespace Game.Vehicles
{
    /// <summary>
    /// Screen hint for aircraft enter/exit via Interact (F).
    /// Auto-added by <see cref="AircraftPilot"/> if missing.
    /// </summary>
    public class AircraftInteractPrompt : MonoBehaviour
    {
        [SerializeField] private AircraftPilot pilot;

        private GUIStyle _labelStyle;
        private GUIStyle _boxStyle;

        private void Awake()
        {
            if (pilot == null) pilot = GetComponent<AircraftPilot>();
        }

        private void OnGUI()
        {
            if (pilot == null) return;

            bool showEnter = pilot.CanEnter;
            bool showExit = pilot.IsPiloting;
            if (!showEnter && !showExit) return;

            EnsureStyles();

            string key = pilot.InteractKeyLabel;
            string text = showExit
                ? $"Press {key} — Exit Aircraft"
                : $"Press {key} — Enter Aircraft";

            const float width = 320f;
            const float height = 36f;
            var rect = new Rect((Screen.width - width) * 0.5f, Screen.height - 96f, width, height);

            GUI.Box(rect, GUIContent.none, _boxStyle);
            GUI.Label(rect, text, _labelStyle);
        }

        private void EnsureStyles()
        {
            if (_labelStyle != null) return;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeBackground(new Color(0f, 0f, 0f, 0.55f)) }
            };
        }

        private static Texture2D MakeBackground(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}
