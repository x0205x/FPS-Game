using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Receives AnimationEvents from Core locomotion clips (Walk, Run_Fwd).
    /// Must live on the same GameObject as the <see cref="Animator"/>.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AnimationFootstepReceiver : MonoBehaviour
    {
        [SerializeField] private AudioClip[] walkClips;
        [SerializeField] private AudioClip[] runClips;
        [SerializeField, Range(0f, 1f)] private float walkVolume = 0.75f;
        [SerializeField, Range(0f, 1f)] private float runVolume  = 1f;
        [SerializeField, Min(0.1f)] private float walkPitch = 1f;
        [SerializeField, Min(0.1f)] private float runPitch  = 1.05f;

        private AudioSource _audio;

        private void Awake()
        {
            _audio = GetComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 1f;
        }

        public void OnFootstepWalk(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight < 0.5f) return;
            PlayRandom(walkClips, walkVolume, walkPitch);
        }

        public void OnFootstepRun(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight <= 0.5f) return;
            PlayRandom(runClips, runVolume, runPitch);
        }

        private void PlayRandom(AudioClip[] clips, float volume, float pitch)
        {
            if (clips == null || clips.Length == 0 || _audio == null) return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            _audio.pitch = pitch;
            _audio.PlayOneShot(clip, volume);
        }
    }
}
