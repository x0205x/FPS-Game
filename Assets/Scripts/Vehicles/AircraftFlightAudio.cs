using Game.Audio;
using UnityEngine;

namespace Game.Vehicles
{
    /// <summary>
    /// Osprey engine loops, boost layer, air rush, and maneuver thruster puffs.
    /// </summary>
    public class AircraftFlightAudio : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AircraftFlightController flight;

        [Header("Clips (Resources fallback to procedural)")]
        [SerializeField] private AudioClip engineLoopClip;
        [SerializeField] private AudioClip boostLayerClip;
        [SerializeField] private AudioClip thrusterPuffClip;
        [SerializeField] private AudioClip airRushClip;

        [Header("Levels")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 0.85f;
        [SerializeField, Range(0f, 1f)] private float idleVolume = 0.12f;
        [SerializeField, Range(0f, 120f)] private float maxAudibleDistance = 220f;

        private AudioSource _engineSource;
        private AudioSource _boostSource;
        private AudioSource _windSource;
        private AudioSource _maneuverSource;
        private Vector3 _lastLinearThrust;
        private Vector3 _lastAngularThrust;
        private float _maneuverCooldown;

        private void Awake()
        {
            if (flight == null) flight = GetComponent<AircraftFlightController>();

            engineLoopClip ??= LoadOrCreate("Audio/Aircraft/osprey_engine_loop", ProceduralAudioUtility.CreateOspreyEngineLoop);
            boostLayerClip ??= LoadOrCreate("Audio/Aircraft/osprey_boost_layer", ProceduralAudioUtility.CreateOspreyBoostLayer);
            thrusterPuffClip ??= LoadOrCreate("Audio/Aircraft/thruster_puff", ProceduralAudioUtility.CreateThrusterPuff);
            airRushClip ??= LoadOrCreate("Audio/Aircraft/air_rush_loop", ProceduralAudioUtility.CreateSpaceWindLoop);

            _engineSource = CreateLoopSource("EngineAudio", engineLoopClip, 0.55f);
            _boostSource = CreateLoopSource("BoostAudio", boostLayerClip, 0.35f);
            _windSource = CreateLoopSource("WindAudio", airRushClip, 0.25f);
            _maneuverSource = CreateOneShotSource("ManeuverAudio");
        }

        private void Update()
        {
            if (flight == null) return;

            float speed = flight.CurrentSpeed;
            float throttle = flight.Throttle01;
            bool piloted = flight.IsPiloted;
            bool boost = flight.BoostActive;
            bool active = piloted || speed > 2f;

            if (!active)
            {
                FadeSource(_engineSource, 0f);
                FadeSource(_boostSource, 0f);
                FadeSource(_windSource, 0f);
                return;
            }

            float speed01 = Mathf.Clamp01(speed / 90f);
            float engineVol = piloted
                ? Mathf.Lerp(idleVolume, 1f, Mathf.Max(throttle * 0.75f, speed01 * 0.5f))
                : idleVolume * 0.6f;
            float enginePitch = piloted
                ? Mathf.Lerp(0.72f, boost ? 1.35f : 1.08f, Mathf.Max(throttle, speed01 * 0.65f))
                : 0.65f;

            _engineSource.volume = Mathf.Lerp(_engineSource.volume, engineVol * masterVolume, Time.deltaTime * 6f);
            _engineSource.pitch = Mathf.Lerp(_engineSource.pitch, enginePitch, Time.deltaTime * 8f);
            if (!_engineSource.isPlaying) _engineSource.Play();

            float boostVol = boost && piloted ? 0.85f : 0f;
            _boostSource.volume = Mathf.Lerp(_boostSource.volume, boostVol * masterVolume, Time.deltaTime * 10f);
            _boostSource.pitch = boost ? 1.15f : 1f;
            if (boostVol > 0.01f && !_boostSource.isPlaying) _boostSource.Play();
            else if (boostVol <= 0.01f && _boostSource.volume < 0.02f) _boostSource.Stop();

            float windVol = piloted ? Mathf.Lerp(0.05f, 0.55f, speed01) : speed01 * 0.15f;
            _windSource.volume = Mathf.Lerp(_windSource.volume, windVol * masterVolume, Time.deltaTime * 4f);
            _windSource.pitch = Mathf.Lerp(0.85f, 1.35f, speed01);
            if (windVol > 0.02f && !_windSource.isPlaying) _windSource.Play();

            if (piloted) UpdateManeuverSounds();
        }

        private void UpdateManeuverSounds()
        {
            _maneuverCooldown -= Time.deltaTime;
            Vector3 linear = flight.LocalLinearThrust;
            Vector3 angular = flight.LocalAngularThrust;

            float linearDelta = (linear - _lastLinearThrust).magnitude;
            float angularDelta = (angular - _lastAngularThrust).magnitude;
            _lastLinearThrust = linear;
            _lastAngularThrust = angular;

            if (_maneuverCooldown > 0f) return;
            if (linearDelta < 0.35f && angularDelta < 0.25f) return;

            float intensity = Mathf.Clamp01(Mathf.Max(linearDelta, angularDelta));
            _maneuverSource.pitch = Random.Range(0.88f, 1.12f);
            _maneuverSource.PlayOneShot(thrusterPuffClip, intensity * 0.35f * masterVolume);
            _maneuverCooldown = 0.08f;
        }

        private static AudioClip LoadOrCreate(string resourcePath, System.Func<AudioClip> createFallback)
        {
            AudioClip clip = Resources.Load<AudioClip>(resourcePath);
            return clip != null ? clip : createFallback();
        }

        private AudioSource CreateLoopSource(string objectName, AudioClip clip, float volume)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.localPosition = Vector3.zero;

            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.minDistance = 8f;
            source.maxDistance = maxAudibleDistance;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.dopplerLevel = 0.6f;
            source.volume = volume;
            return source;
        }

        private AudioSource CreateOneShotSource(string objectName)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, worldPositionStays: false);

            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.minDistance = 6f;
            source.maxDistance = maxAudibleDistance * 0.75f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            return source;
        }

        private static void FadeSource(AudioSource source, float targetVolume)
        {
            if (source == null) return;
            source.volume = Mathf.MoveTowards(source.volume, targetVolume, Time.deltaTime * 2f);
            if (source.volume <= 0.001f && source.isPlaying) source.Stop();
        }
    }
}
