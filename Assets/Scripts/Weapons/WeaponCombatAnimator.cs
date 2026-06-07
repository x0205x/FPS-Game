using System.Collections.Generic;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// Drives the weapon mesh Animator for fire/reload poses.
    /// Subscribes to the co-located <see cref="WeaponBase"/> weapon events.
    /// </summary>
    public class WeaponCombatAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WeaponBase weapon;
        [SerializeField] private Animator animator;

        [Header("Trigger Parameters")]
        [SerializeField] private string fireTrigger   = "Fire";
        [SerializeField] private string reloadTrigger = "Reload";

        [Header("State Fallbacks")]
        [SerializeField] private string fireStateName   = "Fire";
        [SerializeField] private string reloadStateName = "Reload";

        private int _fireHash, _reloadHash;
        private int _fireStateHash, _reloadStateHash;
        private HashSet<int> _availableParams;

        private void Awake()
        {
            if (weapon == null) weapon = GetComponent<WeaponBase>();
            if (animator == null) animator = GetComponentInChildren<Animator>(true);

            _fireHash        = Animator.StringToHash(fireTrigger);
            _reloadHash      = Animator.StringToHash(reloadTrigger);
            _fireStateHash   = Animator.StringToHash(fireStateName);
            _reloadStateHash = Animator.StringToHash(reloadStateName);

            CacheAvailableParameters();
        }

        private void OnEnable()
        {
            if (weapon == null) return;
            weapon.OnFired += HandleFire;
            weapon.OnReloadStarted += HandleReload;
        }

        private void OnDisable()
        {
            if (weapon == null) return;
            weapon.OnFired -= HandleFire;
            weapon.OnReloadStarted -= HandleReload;
        }

        public void PlayFire() => PlayCombatMotion(_fireHash, _fireStateHash);

        private void HandleFire() => PlayFire();

        private void HandleReload()
        {
            PlayCombatMotion(_reloadHash, _reloadStateHash);
        }

        private void PlayCombatMotion(int triggerHash, int stateHash)
        {
            if (animator == null) return;

            if (Has(triggerHash))
            {
                animator.SetTrigger(triggerHash);
                return;
            }

            if (animator.HasState(0, stateHash))
                animator.Play(stateHash, 0, 0f);
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
