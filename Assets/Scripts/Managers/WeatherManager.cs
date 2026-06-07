using System.Collections;
using UnityEngine;

namespace Game.Managers
{
    /// <summary>
    /// Thunderstorm controller. Random lightning flashes drive a directional
    /// light's intensity, optional screen flash, and a delayed thunder clip.
    /// Toggle <see cref="enableLightning"/> to gate the system.
    /// </summary>
    public class WeatherManager : MonoBehaviour
    {
        [Header("Rain")]
        [SerializeField] private ParticleSystem rainEmitter;
        [SerializeField] private ParticleSystem splashEmitter;

        [Header("Lightning")]
        [SerializeField] private bool enableLightning = true;
        [SerializeField] private Light directionalLight;
        [SerializeField, Min(0f)] private float baseLightIntensity   = 0.4f;
        [SerializeField, Min(0f)] private float flashLightIntensity  = 4f;
        [SerializeField, Min(0f)] private float minSecondsBetween    = 8f;
        [SerializeField, Min(0f)] private float maxSecondsBetween    = 22f;
        [SerializeField, Min(0f)] private float flashDuration        = 0.18f;

        [Header("Thunder")]
        [SerializeField] private AudioSource thunderSource;
        [SerializeField] private AudioClip[] thunderClips;
        [SerializeField, Min(0f)] private float thunderDelayMin = 1.2f;
        [SerializeField, Min(0f)] private float thunderDelayMax = 3.5f;

        private Coroutine _loop;

        private void OnEnable()
        {
            if (directionalLight != null) directionalLight.intensity = baseLightIntensity;
            if (rainEmitter   != null) rainEmitter.Play();
            if (splashEmitter != null) splashEmitter.Play();
            if (enableLightning) _loop = StartCoroutine(LightningLoop());
        }

        private void OnDisable()
        {
            if (_loop != null) StopCoroutine(_loop);
            if (rainEmitter   != null) rainEmitter.Stop();
            if (splashEmitter != null) splashEmitter.Stop();
        }

        private IEnumerator LightningLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(minSecondsBetween, maxSecondsBetween));
                yield return StartCoroutine(Flash());
                StartCoroutine(PlayThunderAfterDelay());
            }
        }

        private IEnumerator Flash()
        {
            if (directionalLight == null) yield break;
            directionalLight.intensity = flashLightIntensity;
            yield return new WaitForSeconds(flashDuration * 0.5f);
            directionalLight.intensity = flashLightIntensity * 0.5f;
            yield return new WaitForSeconds(flashDuration * 0.5f);
            directionalLight.intensity = baseLightIntensity;
        }

        private IEnumerator PlayThunderAfterDelay()
        {
            yield return new WaitForSeconds(Random.Range(thunderDelayMin, thunderDelayMax));
            if (thunderSource != null && thunderClips != null && thunderClips.Length > 0)
            {
                thunderSource.PlayOneShot(thunderClips[Random.Range(0, thunderClips.Length)]);
            }
        }
    }
}
