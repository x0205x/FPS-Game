using UnityEngine;

namespace Game.Rendering
{
    /// <summary>
    /// Applies 4K target resolution and highest quality tier at startup (Standalone only).
    /// </summary>
    [DefaultExecutionOrder(-250)]
    public class Graphics4KBootstrap : MonoBehaviour
    {
        [SerializeField] private bool enable4KResolution = true;
        [SerializeField] private int targetWidth = 3840;
        [SerializeField] private int targetHeight = 2160;
        [SerializeField] private string qualityLevelName = "4K Ultra";

        private static bool _applied;

        private void Awake()
        {
            if (_applied) return;
            _applied = true;

            ApplyQualityLevel();
            ApplyResolution();
        }

        private void ApplyQualityLevel()
        {
            string[] levels = QualitySettings.names;
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] != qualityLevelName) continue;
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: true);
                return;
            }

            QualitySettings.SetQualityLevel(levels.Length - 1, applyExpensiveChanges: true);
        }

        private void ApplyResolution()
        {
            if (!enable4KResolution) return;
            if (Application.platform is not RuntimePlatform.WindowsPlayer
                and not RuntimePlatform.OSXPlayer
                and not RuntimePlatform.LinuxPlayer
                and not RuntimePlatform.WindowsEditor
                and not RuntimePlatform.OSXEditor
                and not RuntimePlatform.LinuxEditor)
            {
                return;
            }

            Resolution best = Screen.currentResolution;
            foreach (Resolution res in Screen.resolutions)
            {
                if (res.width >= targetWidth && res.height >= targetHeight)
                {
                    if (res.width >= best.width && res.height >= best.height)
                        best = res;
                }
            }

            int width = Mathf.Min(best.width, targetWidth);
            int height = Mathf.Min(best.height, targetHeight);
            Screen.SetResolution(width, height, Screen.fullScreenMode, best.refreshRateRatio);
        }
    }
}
