using System.Collections.Generic;
using UnityEngine;
using Game.Weapons;

namespace Game.Player
{
    /// <summary>
    /// Drives combat-related Animator parameters from weapon state and input.
    /// Missing parameters are silently skipped, matching <see cref="PlayerAnimator"/>.
    /// </summary>
    [DefaultExecutionOrder(10)]
    public class PlayerCombatAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private WeaponManager weaponManager;
        [SerializeField] private PlayerInput input;

        [Header("Bool Parameters")]
        [SerializeField] private string hasWeaponParam = "HasWeapon";
        [SerializeField] private string aimingParam    = "Aiming";

        [Header("Trigger Parameters")]
        [SerializeField] private string fireTrigger   = "Fire";
        [SerializeField] private string reloadTrigger = "Reload";

        private int _hasWeaponHash, _aimingHash, _fireHash, _reloadHash;
        private HashSet<int> _availableParams;
        private WeaponBase _subscribedWeapon;

        private void Awake()
        {
            if (animator == null && movement != null)
                animator = movement.GetComponentInChildren<Animator>();
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (weaponManager == null) weaponManager = GetComponent<WeaponManager>();
            if (input == null) input = GetComponent<PlayerInput>();

            _hasWeaponHash = Animator.StringToHash(hasWeaponParam);
            _aimingHash    = Animator.StringToHash(aimingParam);
            _fireHash      = Animator.StringToHash(fireTrigger);
            _reloadHash    = Animator.StringToHash(reloadTrigger);

            CacheAvailableParameters();
        }

        private void OnEnable()
        {
            if (weaponManager != null)
                weaponManager.OnWeaponEquipped += HandleWeaponEquipped;

            HandleWeaponEquipped(weaponManager != null ? weaponManager.CurrentWeapon : null);
        }

        private void OnDisable()
        {
            if (weaponManager != null)
                weaponManager.OnWeaponEquipped -= HandleWeaponEquipped;

            UnsubscribeWeapon(_subscribedWeapon);
            _subscribedWeapon = null;
        }

        private void Update()
        {
            if (animator == null) return;

            bool hasWeapon = weaponManager != null && weaponManager.CurrentWeapon != null;
            if (Has(_hasWeaponHash))
                animator.SetBool(_hasWeaponHash, hasWeapon);

            // Combat-ready upper-body pose whenever armed; ADS still handled by movement/camera.
            bool aiming = hasWeapon
                          || (movement != null && movement.IsAiming)
                          || (input != null && input.FireHeld);
            if (Has(_aimingHash))
                animator.SetBool(_aimingHash, aiming);
        }

        private void HandleWeaponEquipped(WeaponBase weapon)
        {
            UnsubscribeWeapon(_subscribedWeapon);
            _subscribedWeapon = weapon;

            if (weapon == null) return;
            weapon.OnFired += HandleFire;
            weapon.OnReloadStarted += HandleReload;
        }

        private void UnsubscribeWeapon(WeaponBase weapon)
        {
            if (weapon == null) return;
            weapon.OnFired -= HandleFire;
            weapon.OnReloadStarted -= HandleReload;
        }

        private void HandleFire()
        {
            if (animator != null && Has(_fireHash))
                animator.SetTrigger(_fireHash);
        }

        private void HandleReload()
        {
            if (animator != null && Has(_reloadHash))
                animator.SetTrigger(_reloadHash);
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
