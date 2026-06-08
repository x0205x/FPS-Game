using UnityEngine;

namespace Game.Environment
{
    /// <summary>
    /// Keeps distant sky planets facing the active camera so they stay visible while exploring.
    /// </summary>
    public class SpaceSkyController : MonoBehaviour
    {
        [SerializeField] private float skyDistance = 900f;

        private Transform[] _planets;

        private void Awake()
        {
            int count = transform.childCount;
            _planets = new Transform[count];
            for (int i = 0; i < count; i++)
                _planets[i] = transform.GetChild(i);
        }

        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 origin = cam.transform.position;
            for (int i = 0; i < _planets.Length; i++)
            {
                Transform planet = _planets[i];
                if (planet == null) continue;

                Vector3 dir = planet.position - origin;
                if (dir.sqrMagnitude < 0.001f) continue;
                dir.Normalize();
                planet.position = origin + dir * skyDistance;
                planet.LookAt(origin + dir * (skyDistance + 10f), Vector3.up);
            }
        }
    }
}
