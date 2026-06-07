using System;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Camera-relative movement on a CharacterController.
    /// Walking, running, jumping, gravity, ground detection, coyote time, jump buffering.
    /// While aiming the body locks to the camera yaw so the upper-body weapon layer
    /// can drive aim independently.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInput input;
        [Tooltip("Optional. Falls back to Camera.main if left empty.")]
        [SerializeField] private Transform cameraTransform;

        [Header("Speeds")]
        [SerializeField, Min(0f)] private float walkSpeed         = 2.5f;
        [SerializeField, Min(0f)] private float runSpeed          = 6f;
        [SerializeField, Min(0f)] private float aimSpeed          = 1.8f;
        [SerializeField, Min(0f)] private float speedSmoothTime    = 0.1f;
        [SerializeField, Min(0f)] private float rotationSmoothTime = 0.12f;
        [SerializeField, Min(0f)] private float aimRotationSmoothTime = 0.05f;

        [Header("Jump & Gravity")]
        [SerializeField, Min(0f)] private float jumpHeight        = 1.4f;
        [SerializeField] private float gravity                    = -19.62f;
        [SerializeField] private float groundedStickyGravity      = -2f;
        [SerializeField, Min(0f)] private float coyoteTime        = 0.12f;
        [SerializeField, Min(0f)] private float jumpBufferTime    = 0.15f;

        [Header("Ground Check")]
        [Tooltip("Optional probe transform. If null, falls back to CharacterController.isGrounded.")]
        [SerializeField] private Transform groundCheck;
        [SerializeField, Min(0f)] private float groundCheckRadius = 0.25f;
        [SerializeField] private LayerMask groundMask = ~0;

        public float CurrentSpeed { get; private set; }
        public float WalkSpeedValue => walkSpeed;
        public float RunSpeedValue  => runSpeed;
        public float NormalizedSpeed => runSpeed > 0f ? CurrentSpeed / runSpeed : 0f;

        /// <summary>Blend tree thresholds: 0 = idle, 0.5 = walk, 1 = run.</summary>
        public float LocomotionBlendSpeed
        {
            get
            {
                if (CurrentSpeed < 0.05f) return 0f;

                if (CurrentSpeed <= walkSpeed)
                    return Mathf.Lerp(0f, 0.5f, CurrentSpeed / Mathf.Max(walkSpeed, 0.01f));

                return Mathf.Lerp(0.5f, 1f,
                    Mathf.Clamp01((CurrentSpeed - walkSpeed) / Mathf.Max(runSpeed - walkSpeed, 0.01f)));
            }
        }
        public bool IsGrounded { get; private set; }
        public bool IsRunning => input != null && input.RunHeld && !input.AimHeld && input.MoveInput.sqrMagnitude > 0.01f;
        public bool IsAiming => input != null && input.AimHeld;
        public Vector3 Velocity => _controller != null ? _controller.velocity : Vector3.zero;
        public float VerticalVelocity => _verticalVelocity;

        public event Action OnJumped;
        public event Action OnLanded;

        private CharacterController _controller;
        private float _verticalVelocity;
        private float _targetSpeed;
        private float _speedSmoothVelocity;
        private float _rotationSmoothVelocity;
        private float _lastGroundedTime  = -999f;
        private float _jumpBufferedUntil = -999f;
        private bool  _wasGroundedLastFrame;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (input == null) input = GetComponent<PlayerInput>();
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        private void OnEnable()
        {
            if (input != null) input.OnJumpPerformed += BufferJump;
        }

        private void OnDisable()
        {
            if (input != null) input.OnJumpPerformed -= BufferJump;
        }

        private void Update()
        {
            UpdateGroundedState();
            Vector3 horizontal = ComputeHorizontalMovement();
            CurrentSpeed = new Vector2(horizontal.x, horizontal.z).magnitude;
            ApplyJumpAndGravity();

            Vector3 motion = horizontal + Vector3.up * _verticalVelocity;
            _controller.Move(motion * Time.deltaTime);

            DetectLanding();
        }

        private void UpdateGroundedState()
        {
            IsGrounded = groundCheck != null
                ? Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore)
                : _controller.isGrounded;

            if (IsGrounded) _lastGroundedTime = Time.time;
        }

        private Vector3 ComputeHorizontalMovement()
        {
            Vector2 moveInput = input != null ? input.MoveInput : Vector2.zero;
            float cameraYaw   = cameraTransform != null ? cameraTransform.eulerAngles.y : 0f;

            if (IsAiming)
            {
                // Lock body to camera yaw, strafe relative to camera.
                float yaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, cameraYaw, ref _rotationSmoothVelocity, aimRotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);

                if (moveInput.sqrMagnitude < 0.0001f)
                {
                    _targetSpeed = Mathf.SmoothDamp(_targetSpeed, 0f, ref _speedSmoothVelocity, speedSmoothTime);
                    return Vector3.zero;
                }

                float desiredAim = aimSpeed * Mathf.Clamp01(moveInput.magnitude);
                _targetSpeed = Mathf.SmoothDamp(_targetSpeed, desiredAim, ref _speedSmoothVelocity, speedSmoothTime);

                Vector3 strafeDir = Quaternion.Euler(0f, cameraYaw, 0f) * new Vector3(moveInput.x, 0f, moveInput.y);
                return strafeDir.normalized * _targetSpeed;
            }

            if (moveInput.sqrMagnitude < 0.0001f)
            {
                _targetSpeed = Mathf.SmoothDamp(_targetSpeed, 0f, ref _speedSmoothVelocity, speedSmoothTime);
                return Vector3.zero;
            }

            float maxSpeed = (input != null && input.RunHeld) ? runSpeed : walkSpeed;
            float desired  = maxSpeed * Mathf.Clamp01(moveInput.magnitude);
            _targetSpeed   = Mathf.SmoothDamp(_targetSpeed, desired, ref _speedSmoothVelocity, speedSmoothTime);

            float targetYaw = Mathf.Atan2(moveInput.x, moveInput.y) * Mathf.Rad2Deg + cameraYaw;
            float bodyYaw   = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetYaw, ref _rotationSmoothVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, bodyYaw, 0f);

            Vector3 forward = Quaternion.Euler(0f, targetYaw, 0f) * Vector3.forward;
            return forward * _targetSpeed;
        }

        private void BufferJump() => _jumpBufferedUntil = Time.time + jumpBufferTime;

        private void ApplyJumpAndGravity()
        {
            bool jumpRequested    = Time.time <= _jumpBufferedUntil;
            bool canJumpThisFrame = (Time.time - _lastGroundedTime) <= coyoteTime;

            if (jumpRequested && canJumpThisFrame)
            {
                _verticalVelocity  = Mathf.Sqrt(jumpHeight * -2f * gravity);
                _jumpBufferedUntil = -999f;
                _lastGroundedTime  = -999f;
                OnJumped?.Invoke();
                return;
            }

            if (IsGrounded && _verticalVelocity < 0f) _verticalVelocity = groundedStickyGravity;
            else                                       _verticalVelocity += gravity * Time.deltaTime;
        }

        private void DetectLanding()
        {
            if (IsGrounded && !_wasGroundedLastFrame) OnLanded?.Invoke();
            _wasGroundedLastFrame = IsGrounded;
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
