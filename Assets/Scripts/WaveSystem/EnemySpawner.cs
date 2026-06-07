using UnityEngine;
using UnityEngine.AI;

namespace Game.WaveSystem
{
    /// <summary>
    /// Picks a random spawn point and instantiates the prefab on it.
    /// Snaps to the NavMesh so the enemy's NavMeshAgent starts validly placed.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField, Min(0f)] private float navMeshSnapRadius = 1.5f;

        public Transform[] SpawnPoints { get => spawnPoints; set => spawnPoints = value; }

        public GameObject Spawn(GameObject prefab)
        {
            if (prefab == null || spawnPoints == null || spawnPoints.Length == 0) return null;
            Transform pt = spawnPoints[Random.Range(0, spawnPoints.Length)];
            Vector3 pos  = pt.position;

            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, navMeshSnapRadius, NavMesh.AllAreas))
                pos = hit.position;

            return Instantiate(prefab, pos, pt.rotation);
        }
    }
}
