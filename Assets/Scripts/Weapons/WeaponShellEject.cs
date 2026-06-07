using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// Spawns a physics-driven shell casing from the weapon ejection port on each shot.
    /// </summary>
    [RequireComponent(typeof(WeaponBase))]
    public class WeaponShellEject : MonoBehaviour
    {
        private static readonly string[] EjectionNameHints = { "Eject", "Shell", "Port" };

        [Header("Shell")]
        [SerializeField] private GameObject shellPrefab;
        [SerializeField, Min(0.001f)] private float shellScale = 1f;
        [SerializeField, Min(0.1f)] private float shellLifetime = 5f;

        [Header("Ejection")]
        [SerializeField] private Transform ejectionPoint;
        [SerializeField] private Vector3 gripFallbackLocalOffset = new Vector3(0.04f, 0.03f, -0.02f);
        [SerializeField, Min(0f)] private float ejectImpulse = 1.8f;
        [SerializeField, Min(0f)] private float ejectUpImpulse = 0.35f;
        [SerializeField, Min(0f)] private float ejectSpin = 4f;

        private WeaponBase _weapon;

        private void Awake()
        {
            _weapon = GetComponent<WeaponBase>();
            if (ejectionPoint == null)
                ejectionPoint = FindEjectionPoint(transform);
        }

        private void OnEnable()
        {
            if (_weapon != null) _weapon.OnFired += HandleFired;
        }

        private void OnDisable()
        {
            if (_weapon != null) _weapon.OnFired -= HandleFired;
        }

        private void HandleFired()
        {
            if (shellPrefab == null) return;

            Vector3 position;
            Quaternion rotation;
            Vector3 ejectRight;
            Vector3 ejectUp;

            if (ejectionPoint != null)
            {
                position = ejectionPoint.position;
                rotation = ejectionPoint.rotation;
                ejectRight = ejectionPoint.right;
                ejectUp    = ejectionPoint.up;
            }
            else
            {
                position = transform.TransformPoint(gripFallbackLocalOffset);
                rotation = transform.rotation;
                ejectRight = transform.right;
                ejectUp    = transform.up;
            }

            GameObject shell = Instantiate(shellPrefab, position, rotation);
            shell.name = "ShellCasing";

            if (!Mathf.Approximately(shellScale, 1f))
                shell.transform.localScale = Vector3.one * shellScale;

            EnsurePhysics(shell);

            if (shell.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.AddForce(ejectRight * ejectImpulse + ejectUp * ejectUpImpulse, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * ejectSpin, ForceMode.Impulse);
            }

            Destroy(shell, shellLifetime);
        }

        private static void EnsurePhysics(GameObject shell)
        {
            if (!shell.TryGetComponent<Rigidbody>(out var rb))
            {
                rb = shell.AddComponent<Rigidbody>();
                rb.mass = 0.008f;
                rb.linearDamping = 0.15f;
                rb.angularDamping = 0.05f;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }

            if (shell.GetComponentInChildren<Collider>() == null)
            {
                foreach (MeshFilter filter in shell.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh == null) continue;
                    var col = filter.gameObject.AddComponent<MeshCollider>();
                    col.sharedMesh = filter.sharedMesh;
                    col.convex = true;
                }
            }
        }

        public static Transform FindEjectionPoint(Transform weaponRoot)
        {
            if (weaponRoot == null) return null;

            foreach (Transform t in weaponRoot.GetComponentsInChildren<Transform>(true))
            {
                foreach (string hint in EjectionNameHints)
                {
                    if (t.name.Contains(hint))
                        return t;
                }
            }

            return null;
        }
    }
}
