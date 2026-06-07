using Game.Player;
using UnityEngine;

namespace Game.AI
{
    /// <summary>
    /// Sight-cone perception. Tries to acquire a target each tick by checking the
    /// configured <see cref="targetMask"/> within view range/angle and verifying
    /// line-of-sight against <see cref="obstacleMask"/>.
    /// </summary>
    public class EnemyVision : MonoBehaviour
    {
        [Header("View Cone")]
        [SerializeField, Min(0f)] private float viewRadius = 18f;
        [SerializeField, Range(0f, 360f)] private float viewAngleDeg = 110f;
        [SerializeField] private Transform eye;

        [Header("Layers")]
        [SerializeField] private LayerMask targetMask;
        [SerializeField] private LayerMask obstacleMask;

        public Transform CurrentTarget { get; private set; }
        public bool HasTarget => CurrentTarget != null;

        private void Awake()
        {
            if (eye == null) eye = transform;
        }

        private void Update() => CurrentTarget = ScanForTarget();

        private Transform ScanForTarget()
        {
            Collider[] hits = Physics.OverlapSphere(eye.position, viewRadius, targetMask, QueryTriggerInteraction.Ignore);
            float halfAngle = viewAngleDeg * 0.5f;

            for (int i = 0; i < hits.Length; i++)
            {
                Transform t = hits[i].transform;
                Vector3 toTarget = (t.position - eye.position);
                float distance = toTarget.magnitude;
                if (distance < 0.01f) continue;

                Vector3 dir = toTarget / distance;
                if (Vector3.Angle(eye.forward, dir) > halfAngle) continue;
                if (Physics.Raycast(eye.position, dir, distance, obstacleMask, QueryTriggerInteraction.Ignore)) continue;
                Transform player = ResolvePlayerRoot(t);
                if (player == null) continue;

                return player;
            }
            return null;
        }

        private static Transform ResolvePlayerRoot(Transform t)
        {
            if (t == null) return null;
            if (t.CompareTag("Player")) return t;
            var pc = t.GetComponentInParent<PlayerController>();
            return pc != null ? pc.transform : null;
        }

        private void OnDrawGizmosSelected()
        {
            Transform e = eye != null ? eye : transform;
            Gizmos.color = new Color(1f, 0.7f, 0f, 0.4f);
            Gizmos.DrawWireSphere(e.position, viewRadius);

            Vector3 left  = Quaternion.Euler(0f, -viewAngleDeg * 0.5f, 0f) * e.forward;
            Vector3 right = Quaternion.Euler(0f,  viewAngleDeg * 0.5f, 0f) * e.forward;
            Gizmos.DrawLine(e.position, e.position + left  * viewRadius);
            Gizmos.DrawLine(e.position, e.position + right * viewRadius);
        }
    }
}
