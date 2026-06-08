using UnityEngine;

namespace Game.Environment
{
    /// <summary>
    /// Gentle wind sway for procedural trees. Attach to the tree root; optional
    /// <see cref="foliage"/> rotates more than the trunk.
    /// </summary>
    public class TreeSway : MonoBehaviour
    {
        [SerializeField] private Transform foliage;
        [SerializeField] private float swaySpeed = 0.85f;
        [SerializeField] private float trunkSwayDegrees = 2.5f;
        [SerializeField] private float foliageSwayDegrees = 5f;

        private float _phase;
        private Quaternion _trunkBase;
        private Quaternion _foliageBase;

        private void Awake()
        {
            if (foliage == null)
            {
                Transform found = transform.Find("Foliage");
                if (found != null) foliage = found;
            }

            _phase = Random.Range(0f, 100f);
            _trunkBase = transform.localRotation;
            if (foliage != null) _foliageBase = foliage.localRotation;
        }

        private void Update()
        {
            float t = Time.time * swaySpeed + _phase;
            float trunkX = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * trunkSwayDegrees;
            float trunkZ = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * trunkSwayDegrees;
            transform.localRotation = _trunkBase * Quaternion.Euler(trunkX, 0f, trunkZ);

            if (foliage == null) return;

            float leafX = (Mathf.PerlinNoise(t * 1.3f, 1.7f) - 0.5f) * 2f * foliageSwayDegrees;
            float leafZ = (Mathf.PerlinNoise(2.1f, t * 1.1f) - 0.5f) * 2f * foliageSwayDegrees;
            foliage.localRotation = _foliageBase * Quaternion.Euler(leafX, 0f, leafZ);
        }
    }
}
