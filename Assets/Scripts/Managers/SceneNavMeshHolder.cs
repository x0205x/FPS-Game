using UnityEngine;
using UnityEngine.AI;

namespace Game.Managers
{
    /// <summary>
    /// Loads baked <see cref="NavMeshData"/> into the active scene for NavMeshAgents.
    /// </summary>
    public class SceneNavMeshHolder : MonoBehaviour
    {
        [SerializeField] private NavMeshData navMeshData;

        private NavMeshDataInstance _instance;

        public NavMeshData NavMeshData => navMeshData;

        public void AssignNavMeshData(NavMeshData data)
        {
            navMeshData = data;
            RefreshInstance();
        }

        private void OnEnable() => RefreshInstance();

        private void OnDisable() => RemoveInstance();

        private void RefreshInstance()
        {
            RemoveInstance();
            if (navMeshData != null)
                _instance = NavMesh.AddNavMeshData(navMeshData);
        }

        private void RemoveInstance()
        {
            if (_instance.valid)
                NavMesh.RemoveNavMeshData(_instance);
        }
    }
}
