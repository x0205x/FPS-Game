using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Audio
{
    /// <summary>
    /// Plays the ElevenLabs ambient bed once in TestPlayground after Start Prologue.
    /// </summary>
    public class SpaceAmbienceController : MonoBehaviour
    {
        public const string GameplaySceneName = "TestPlayground";
        public const string AmbientResourcePath = "Audio/ambient_background";
        public const string AmbientAssetPath = "Assets/Resources/Audio/ambient_background.mp3";

        [SerializeField] private AudioClip ambientClip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        private AudioSource _source;
        private bool _hasPlayed;
        private static bool _sceneHookRegistered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void RegisterSceneHook()
        {
            if (_sceneHookRegistered) return;
            _sceneHookRegistered = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != GameplaySceneName) return;
            EnsurePlaying();
        }

        public static void EnsurePlaying()
        {
            if (SceneManager.GetActiveScene().name != GameplaySceneName) return;

            SpaceAmbienceController existing = FindAnyObjectByType<SpaceAmbienceController>();
            if (existing != null)
            {
                existing.BeginPlayback();
                return;
            }

            var go = new GameObject("GameplayAmbience");
            go.AddComponent<SpaceAmbienceController>();
        }

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            if (_source == null)
                _source = gameObject.AddComponent<AudioSource>();

            _source.loop = false;
            _source.spatialBlend = 0f;
            _source.playOnAwake = false;
            _source.priority = 0;
            _source.volume = volume;
            _source.ignoreListenerVolume = true;
            _source.bypassListenerEffects = true;
            _source.bypassReverbZones = true;

            ambientClip ??= Resources.Load<AudioClip>(AmbientResourcePath);
        }

        private void Start() => BeginPlayback();

        public void BeginPlayback()
        {
            if (_source == null || _hasPlayed) return;

            ambientClip ??= Resources.Load<AudioClip>(AmbientResourcePath);
            if (ambientClip == null)
            {
                Debug.LogError(
                    $"[{nameof(SpaceAmbienceController)}] Could not load ambient clip at Resources/{AmbientResourcePath}.");
                return;
            }

            if (ambientClip.loadState == AudioDataLoadState.Unloaded)
                ambientClip.LoadAudioData();

            _source.clip = ambientClip;
            _source.volume = volume;

            if (!_source.isPlaying)
            {
                _source.Play();
                _hasPlayed = true;
            }
        }
    }
}
