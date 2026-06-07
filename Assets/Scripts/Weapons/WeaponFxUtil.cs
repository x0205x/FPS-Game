using UnityEngine;

namespace Game.Weapons
{
    internal static class WeaponFxUtil
    {
        public static GameObject SpawnAttached(GameObject prefab, Transform parent, float lifetime)
        {
            if (prefab == null || parent == null) return null;

            GameObject fx = Object.Instantiate(prefab, parent);
            fx.transform.localPosition = Vector3.zero;
            fx.transform.localRotation = Quaternion.identity;
            fx.transform.localScale    = Vector3.one;
            PlayParticles(fx);
            if (lifetime > 0f) Object.Destroy(fx, lifetime);
            return fx;
        }

        public static GameObject SpawnAt(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
        {
            if (prefab == null) return null;

            GameObject fx = Object.Instantiate(prefab, position, rotation);
            PlayParticles(fx);
            if (lifetime > 0f) Object.Destroy(fx, lifetime);
            return fx;
        }

        public static void PlayParticles(GameObject root)
        {
            if (root == null) return;

            foreach (ParticleSystem ps in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Clear(true);
                ps.Play(true);
            }
        }
    }
}
