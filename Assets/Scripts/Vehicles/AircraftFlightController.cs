using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Vehicles
{
    /// <summary>
    /// Elite Dangerous-style 6DOF flight: momentum, flight assist, boost, and mouse flight.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class AircraftFlightController : MonoBehaviour
    {
        [Header("Speed")]
        [SerializeField, Min(1f)] private float maxSpeed = 90f;
        [SerializeField, Min(1f)] private float boostMaxSpeed = 145f;
        [SerializeField, Min(1f)] private float mainAcceleration = 42f;
        [SerializeField, Min(1f)] private float strafeAcceleration = 28f;
        [SerializeField, Min(1f)] private float verticalAcceleration = 24f;

        [Header("Rotation (deg/s)")]
        [SerializeField, Min(1f)] private float pitchRate = 48f;
        [SerializeField, Min(1f)] private float yawRate = 38f;
        [SerializeField, Min(1f)] private float rollRate = 95f;
        [SerializeField, Min(0f)] private float lookSensitivity = 1f;

        [Header("Flight Assist")]
        [SerializeField] private bool flightAssist = true;
        [SerializeField, Min(0f)] private float velocityDamping = 3.2f;
        [SerializeField, Min(0f)] private float angularDamping = 5.5f;
        [SerializeField, Min(1f)] private float boostMultiplier = 1.85f;

        [Header("Physics")]
        [SerializeField, Min(1f)] private float shipMass = 8500f;

        public bool IsPiloted { get; private set; }
        public bool BoostActive { get; private set; }
        public float Throttle01 { get; private set; }
        public Vector3 LocalLinearThrust { get; private set; }
        public Vector3 LocalAngularThrust { get; private set; }
        public float CurrentSpeed => _rb != null ? _rb.linearVelocity.magnitude : 0f;

        private Rigidbody _rb;
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private bool _boostHeld;
        private bool _ascendHeld;
        private bool _descendHeld;
        private float _rollInput;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.mass = shipMass;
            _rb.linearDamping = 0.05f;
            _rb.angularDamping = 0.4f;
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public void SetPiloted(bool piloted)
        {
            IsPiloted = piloted;
            if (!piloted)
            {
                LocalLinearThrust = Vector3.zero;
                LocalAngularThrust = Vector3.zero;
                Throttle01 = 0f;
                BoostActive = false;
            }
        }

        public void SetMoveInput(Vector2 move) => _moveInput = move;
        public void SetLookInput(Vector2 look) => _lookInput = look;
        public void SetBoostHeld(bool held) => _boostHeld = held;
        public void SetAscendHeld(bool held) => _ascendHeld = held;
        public void SetDescendHeld(bool held) => _descendHeld = held;
        public void SetRollInput(float roll) => _rollInput = roll;

        private void FixedUpdate()
        {
            if (!IsPiloted) return;

            ReadKeyboardFlightExtras();
            ApplyLinearForces();
            ApplyRotation();
            if (flightAssist) ApplyFlightAssist();
            ClampSpeed();
        }

        private void ReadKeyboardFlightExtras()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.qKey.isPressed) _rollInput = -1f;
            else if (kb.eKey.isPressed) _rollInput = 1f;
            else _rollInput = 0f;

            _descendHeld = kb.leftCtrlKey.isPressed || kb.cKey.isPressed;
        }

        private void ApplyLinearForces()
        {
            float forward = _moveInput.y;
            float strafe = _moveInput.x;
            float vertical = (_ascendHeld ? 1f : 0f) + (_descendHeld ? -1f : 0f);

            LocalLinearThrust = new Vector3(strafe, vertical, forward);
            Throttle01 = Mathf.Clamp01(Mathf.Abs(forward));
            BoostActive = _boostHeld && Throttle01 > 0.05f;

            float accelScale = BoostActive ? boostMultiplier : 1f;
            Vector3 localForce = new Vector3(
                strafe * strafeAcceleration,
                vertical * verticalAcceleration,
                forward * mainAcceleration) * accelScale;

            _rb.AddRelativeForce(localForce, ForceMode.Acceleration);
        }

        private void ApplyRotation()
        {
            float pitch = -_lookInput.y * lookSensitivity;
            float yaw = _lookInput.x * lookSensitivity;

            LocalAngularThrust = new Vector3(pitch, yaw, _rollInput);

            Vector3 targetAngularVelocity = new Vector3(
                pitch * pitchRate * Mathf.Deg2Rad,
                yaw * yawRate * Mathf.Deg2Rad,
                _rollInput * rollRate * Mathf.Deg2Rad);

            Vector3 delta = targetAngularVelocity - _rb.angularVelocity;
            _rb.AddTorque(delta, ForceMode.VelocityChange);
        }

        private void ApplyFlightAssist()
        {
            if (_moveInput.sqrMagnitude < 0.01f && !_ascendHeld && !_descendHeld)
            {
                Vector3 localVelocity = transform.InverseTransformDirection(_rb.linearVelocity);
                Vector3 counterAccel = new Vector3(
                    -localVelocity.x * velocityDamping,
                    -localVelocity.y * velocityDamping,
                    -localVelocity.z * velocityDamping);
                _rb.AddRelativeForce(counterAccel, ForceMode.Acceleration);
            }

            if (_lookInput.sqrMagnitude < 0.01f && Mathf.Abs(_rollInput) < 0.01f)
            {
                Vector3 counterTorque = -_rb.angularVelocity * angularDamping;
                _rb.AddTorque(counterTorque, ForceMode.Acceleration);
            }
        }

        private void ClampSpeed()
        {
            float cap = BoostActive ? boostMaxSpeed : maxSpeed;
            if (_rb.linearVelocity.sqrMagnitude <= cap * cap) return;
            _rb.linearVelocity = _rb.linearVelocity.normalized * cap;
        }
    }
}
