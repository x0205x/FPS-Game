using Game.Player;
using UnityEngine;

namespace Game.Vehicles
{
    /// <summary>
    /// Third-person chase camera for piloted aircraft (Elite Dangerous-style rear view).
    /// </summary>
    public class AircraftCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 localOffset = new(0f, 3.5f, -16f);
        [SerializeField, Min(0f)] private float followSmooth = 6f;
        [SerializeField, Min(0f)] private float lookAhead = 24f;
        [SerializeField, Min(1f)] private float pilotFov = 72f;

        private Camera _camera;
        private float _defaultFov;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera != null) _defaultFov = _camera.fieldOfView;
        }

        public void SetTarget(Transform shipTarget) => target = shipTarget;

        public void SetActive(bool active)
        {
            enabled = active;
            if (_camera == null) return;
            _camera.fieldOfView = active ? pilotFov : _defaultFov;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = target.TransformPoint(localOffset);
            transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * followSmooth);
            Vector3 lookTarget = target.position + target.forward * lookAhead + Vector3.up * 1.5f;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookTarget - transform.position, Vector3.up),
                Time.deltaTime * followSmooth);
        }
    }
}
