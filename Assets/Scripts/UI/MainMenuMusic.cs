using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Loops the main menu cinematic score in the background.
    /// </summary>
    public class MainMenuMusic : MonoBehaviour
    {
        private const string MusicResourcePath = "UI/MainMenu/Music/menu_theme";

        [SerializeField, Range(0f, 1f)] private float volume = 0.55f;

        private void Awake()
        {
            AudioClip theme = Resources.Load<AudioClip>(MusicResourcePath);
            if (theme == null)
            {
                Debug.LogWarning($"[MainMenuMusic] Missing clip at Resources/{MusicResourcePath}");
                return;
            }

            var source = gameObject.AddComponent<AudioSource>();
            source.clip = theme;
            source.loop = true;
            source.playOnAwake = true;
            source.volume = volume;
            source.spatialBlend = 0f;
            source.ignoreListenerVolume = true;
            source.bypassListenerEffects = true;
            source.bypassReverbZones = true;
            source.Play();
        }
    }
}
