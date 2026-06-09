using UnityEngine;

namespace Game.Environment
{
    /// <summary>
    /// Loops a soft dust particle field for outdoor scenes (lunar regolith or war-torn dust).
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class AmbientDust : MonoBehaviour
    {
        private void Awake()
        {
            var ps = GetComponent<ParticleSystem>();
            if (ps.isPlaying) return;
            ps.Play();
        }
    }
}
