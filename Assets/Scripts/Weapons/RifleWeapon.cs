using UnityEngine;
using Game.Common;

namespace Game.Weapons
{
    /// <summary>
    /// Hitscan rifle. Casts a ray from the muzzle (or camera) forward and applies
    /// damage to anything implementing <see cref="IDamageable"/>. Spawn the bullet
    /// trail / impact VFX in <see cref="SpawnImpactEffects"/>.
    /// </summary>
    public class RifleWeapon : WeaponBase
    {
        [Header("Hitscan")]
        [Tooltip("If set, the ray uses this transform's forward (typically the camera). Falls back to the muzzle.")]
        [SerializeField] private Transform aimSource;
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("VFX / SFX (optional)")]
        [SerializeField] private GameObject impactPrefab;
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private AudioClip fireClip;
        [SerializeField, Range(0f, 1f)] private float fireVolume = 0.85f;
        [SerializeField, Min(0f)] private float effectLifetime = 2f;
        [SerializeField] private bool spawnTracer = true;
        [SerializeField, Min(0.01f)] private float tracerDuration = 0.06f;
        [SerializeField, Min(0.001f)] private float tracerWidth = 0.015f;

        private AudioSource _audio;
        private Material _tracerMaterial;

        private void Awake()
        {
            _audio = GetComponent<AudioSource>();
            if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 1f;
        }

        protected override void OnFire()
        {
            if (fireClip != null && _audio != null)
                _audio.PlayOneShot(fireClip, fireVolume);

            Transform source = aimSource != null ? aimSource : muzzle;
            if (source == null)
            {
                Debug.LogWarning($"[{nameof(RifleWeapon)}] No aim source or muzzle assigned.", this);
                return;
            }

            Transform fireOrigin = muzzle != null ? muzzle : source;
            if (muzzleFlashPrefab != null && fireOrigin != null)
                WeaponFxUtil.SpawnAttached(muzzleFlashPrefab, fireOrigin, effectLifetime);

            Transform owner = transform.root;
            bool hitSomething = TryRaycastIgnoringOwner(source.position, source.forward, owner, out RaycastHit hit);
            Vector3 shotEnd = hitSomething
                ? hit.point
                : source.position + source.forward * range;

            if (spawnTracer && fireOrigin != null)
                SpawnTracer(fireOrigin.position, shotEnd);

            if (!hitSomething) return;

            if (hit.collider.TryGetComponent<IDamageable>(out var dmg))
                dmg.ApplyDamage(damage, gameObject);

            SpawnImpactEffects(hit);
        }

        private bool TryRaycastIgnoringOwner(Vector3 origin, Vector3 direction, Transform owner, out RaycastHit hit)
        {
            hit = default;
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, range, hitMask, QueryTriggerInteraction.Ignore);
            float bestDist = float.MaxValue;
            bool found = false;

            foreach (RaycastHit candidate in hits)
            {
                if (owner != null &&
                    (candidate.collider.transform == owner || candidate.collider.transform.IsChildOf(owner)))
                    continue;

                if (candidate.distance >= bestDist) continue;
                bestDist = candidate.distance;
                hit = candidate;
                found = true;
            }

            return found;
        }

        protected virtual void SpawnImpactEffects(RaycastHit hit)
        {
            if (impactPrefab == null) return;
            WeaponFxUtil.SpawnAt(impactPrefab, hit.point, Quaternion.LookRotation(hit.normal), effectLifetime);
        }

        private void SpawnTracer(Vector3 from, Vector3 to)
        {
            var tracerGo = new GameObject("ShotTracer");
            var line = tracerGo.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.startWidth = tracerWidth;
            line.endWidth   = tracerWidth * 0.35f;
            line.numCapVertices = 4;

            if (_tracerMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    _tracerMaterial = new Material(shader);
                    _tracerMaterial.color = new Color(1f, 0.9f, 0.35f, 0.95f);
                }
            }

            if (_tracerMaterial != null)
                line.material = _tracerMaterial;

            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            Destroy(tracerGo, tracerDuration);
        }
    }
}
