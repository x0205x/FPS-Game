using UnityEngine;
using UnityEngine.Audio;

namespace Game.Audio
{
    /// <summary>
    /// Thin wrapper over an Audio Mixer + a few global AudioSources.
    /// Provides volume helpers (in dB, the way mixers want it) and a one-shot
    /// SFX router. FMOD adapters can replace this without changing callers.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Mixer")]
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private string masterParam = "MasterVolume";
        [SerializeField] private string musicParam  = "MusicVolume";
        [SerializeField] private string sfxParam    = "SfxVolume";

        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (musicSource == null || clip == null) return;
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.Play();
        }

        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (sfxSource == null || clip == null) return;
            sfxSource.PlayOneShot(clip, volume);
        }

        public void SetMasterVolume(float linear01) => SetMixerVolume(masterParam, linear01);
        public void SetMusicVolume(float linear01)  => SetMixerVolume(musicParam,  linear01);
        public void SetSfxVolume(float linear01)    => SetMixerVolume(sfxParam,    linear01);

        private void SetMixerVolume(string param, float linear01)
        {
            if (mixer == null || string.IsNullOrEmpty(param)) return;
            float clamped = Mathf.Clamp(linear01, 0.0001f, 1f);
            mixer.SetFloat(param, Mathf.Log10(clamped) * 20f);
        }
    }
}
