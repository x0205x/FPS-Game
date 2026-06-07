using UnityEngine;

namespace Game.AI
{
    /// <summary>
    /// Finds the closest cover point that breaks line-of-sight from a threat.
    /// Cover candidates are GameObjects tagged "Cover".
    /// </summary>
    public class EnemyCoverSystem : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float searchRadius = 25f;
        [SerializeField] private string coverTag = "Cover";
        [SerializeField] private LayerMask obstacleMask = ~0;

        public bool TryFindCover(Vector3 selfPosition, Vector3 threatPosition, out Vector3 coverPoint)
        {
            coverPoint = selfPosition;
            GameObject[] candidates = GameObject.FindGameObjectsWithTag(coverTag);
            float bestSqr = float.PositiveInfinity;
            bool found = false;

            for (int i = 0; i < candidates.Length; i++)
            {
                Vector3 p = candidates[i].transform.position;
                float sqr = (p - selfPosition).sqrMagnitude;
                if (sqr > searchRadius * searchRadius) continue;
                if (sqr >= bestSqr) continue;

                Vector3 toThreat = threatPosition - p;
                float dist = toThreat.magnitude;
                if (dist < 0.01f) continue;

                if (Physics.Raycast(p + Vector3.up * 1.2f, toThreat / dist, dist, obstacleMask, QueryTriggerInteraction.Ignore))
                {
                    bestSqr   = sqr;
                    coverPoint = p;
                    found     = true;
                }
            }

            return found;
        }
    }
}
