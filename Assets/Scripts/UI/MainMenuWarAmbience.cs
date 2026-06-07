using System.Collections;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Continuous distant battlefield ambience on the main menu.
    /// Uses <see cref="AudioSource.ignoreListenerVolume"/> so master mute cannot silence it.
    /// </summary>
    public class MainMenuWarAmbience : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float masterVolume = 0.45f;

        private AudioSource _source;
        private AudioClip[] _battleClips;

        private void Awake()
        {
            _battleClips = Resources.LoadAll<AudioClip>("UI/MainMenu/Audio");
            if (_battleClips == null || _battleClips.Length == 0) return;

            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0f;
            _source.ignoreListenerVolume = true;
            _source.bypassListenerEffects = true;
            _source.bypassReverbZones = true;

            StartCoroutine(BattlefieldLoop());
        }

        private IEnumerator BattlefieldLoop()
        {
            while (enabled)
            {
                AudioClip clip = _battleClips[Random.Range(0, _battleClips.Length)];
                _source.clip = clip;
                _source.volume = masterVolume * Random.Range(0.2f, 0.5f);
                _source.pitch = Random.Range(0.68f, 1.02f);
                _source.Play();

                float wait = Mathf.Max(0.35f, clip.length * Random.Range(0.55f, 0.9f));
                yield return new WaitForSeconds(wait);
            }
        }
    }
}
