using System.Collections.Generic;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Reads movement state from <see cref="PlayerMovement"/> and pushes it into an Animator.
    /// Parameter names are configurable in the inspector. Missing parameters are silently
    /// skipped, so this works with whatever animator setup your character has.
    ///
    /// Recommended Animator layout for a TPS rig:
    ///   Layer 0 (Base):       locomotion blend tree driven by Speed / IsAiming
    ///   Layer 1 (Upper Body): aim/fire/reload (mask hides legs)
    ///   Layer 2 (Weapon):     weapon-specific idle/fire/reload poses
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMovement movement;

        [Header("Float Parameters")]
        [SerializeField] private string speedParam            = "Speed";
        [SerializeField] private string verticalVelocityParam = "VerticalVelocity";

        [Header("Bool Parameters")]
        [SerializeField] private string groundedParam = "Grounded";
        [SerializeField] private string aimingParam   = "Aiming";

        [Header("Trigger Parameters")]
        [SerializeField] private string jumpTrigger = "Jump";
        [SerializeField] private string landTrigger = "Land";

        [Header("Smoothing")]
        [SerializeField, Min(0f)] private float speedDamp = 0.1f;

        private int _speedHash, _vertVelHash, _groundedHash, _aimingHash, _jumpHash, _landHash;
        private HashSet<int> _availableParams;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (movement == null) movement = GetComponent<PlayerMovement>();

            _speedHash    = Animator.StringToHash(speedParam);
            _vertVelHash  = Animator.StringToHash(verticalVelocityParam);
            _groundedHash = Animator.StringToHash(groundedParam);
            _aimingHash   = Animator.StringToHash(aimingParam);
            _jumpHash     = Animator.StringToHash(jumpTrigger);
            _landHash     = Animator.StringToHash(landTrigger);

            CacheAvailableParameters();
        }

        private void OnEnable()
        {
            if (movement == null) return;
            movement.OnJumped += HandleJump;
            movement.OnLanded += HandleLand;
        }

        private void OnDisable()
        {
            if (movement == null) return;
            movement.OnJumped -= HandleJump;
            movement.OnLanded -= HandleLand;
        }

        private void Update()
        {
            if (animator == null || movement == null) return;

            if (Has(_speedHash))
                animator.SetFloat(_speedHash, movement.LocomotionBlendSpeed, speedDamp, Time.deltaTime);
            if (Has(_vertVelHash))
                animator.SetFloat(_vertVelHash, movement.VerticalVelocity);
            if (Has(_groundedHash))
                animator.SetBool(_groundedHash, movement.IsGrounded);
            if (Has(_aimingHash))
                animator.SetBool(_aimingHash, movement.IsAiming);
        }

        private void HandleJump()
        {
            if (animator != null && Has(_jumpHash)) animator.SetTrigger(_jumpHash);
        }

        private void HandleLand()
        {
            if (animator != null && Has(_landHash)) animator.SetTrigger(_landHash);
        }

        private void CacheAvailableParameters()
        {
            _availableParams = new HashSet<int>();
            if (animator == null) return;
            foreach (var p in animator.parameters) _availableParams.Add(p.nameHash);
        }

        private bool Has(int hash) => _availableParams != null && _availableParams.Contains(hash);
    }
}
