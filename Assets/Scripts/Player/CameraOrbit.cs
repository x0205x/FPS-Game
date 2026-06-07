using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Camera orbit pivot. Attach to a transform at head height that the
    /// CinemachineCamera tracks. Look input (mouse / right-stick) drives this
    /// transform's yaw + pitch in world space, so the camera orbits the player
    /// independently of the player's body rotation.
    /// </summary>
    public class CameraOrbit : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInput input;

        [Header("Sensitivity")]
        [Tooltip("Multiplier on the look X input (mouse delta or right-stick X).")]
        [SerializeField, Min(0f)] private float yawSensitivity   = 1.2f;
        [Tooltip("Multiplier on the look Y input (mouse delta or right-stick Y).")]
        [SerializeField, Min(0f)] private float pitchSensitivity = 1.0f;
        [SerializeField] private bool invertY = false;

        [Header("Pitch Limits")]
        [SerializeField] private float minPitch = -45f;
        [SerializeField] private float maxPitch =  70f;

        [Header("Cursor")]
        [SerializeField] private bool lockCursor = true;

        private float _yaw;
        private float _pitch;

        private void Awake()
        {
            if (input == null) input = GetComponentInParent<PlayerInput>();
            Vector3 e = transform.eulerAngles;
            _yaw   = e.y;
            _pitch = NormalizeAngle(e.x);
        }

        private void OnEnable()
        {
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
            }
        }

        private void Update()
        {
            if (input == null) return;
            Vector2 look = input.LookInput;
            _yaw   += look.x * yawSensitivity;
            _pitch += (invertY ? look.y : -look.y) * pitchSensitivity;
            _pitch  = Mathf.Clamp(_pitch, minPitch, maxPitch);
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private static float NormalizeAngle(float a)
        {
            a %= 360f;
            if (a > 180f) a -= 360f;
            return a;
        }
    }
}
