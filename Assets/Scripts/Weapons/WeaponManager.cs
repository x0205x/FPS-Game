using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Player;

namespace Game.Weapons
{
    /// <summary>
    /// Owns the player's weapon loadout. Routes Fire/Reload input from
    /// <see cref="PlayerInput"/> to the currently equipped weapon and exposes
    /// hooks for HUD/animator to subscribe to weapon state.
    /// </summary>
    public class WeaponManager : MonoBehaviour
    {
        [SerializeField] private PlayerInput input;
        [SerializeField] private List<WeaponBase> weapons = new();
        [SerializeField, Min(0)] private int initialWeaponIndex = 0;

        public WeaponBase CurrentWeapon { get; private set; }
        public int CurrentIndex { get; private set; }

        public event Action<WeaponBase> OnWeaponEquipped;

        private void Awake()
        {
            if (input == null) input = GetComponentInParent<PlayerInput>();
            EquipWeapon(initialWeaponIndex);
        }

        private void OnEnable()
        {
            if (input == null) return;
            input.OnFirePerformed   += HandleFirePressed;
            input.OnReloadPerformed += HandleReload;
        }

        private void OnDisable()
        {
            if (input == null) return;
            input.OnFirePerformed   -= HandleFirePressed;
            input.OnReloadPerformed -= HandleReload;
        }

        private void Update()
        {
            if (CurrentWeapon == null || input == null) return;
            if (CurrentWeapon.FullAuto && input.FireHeld) CurrentWeapon.TryFire();
        }

        public void EquipWeapon(int index)
        {
            if (weapons.Count == 0) return;
            CurrentIndex = Mathf.Clamp(index, 0, weapons.Count - 1);
            for (int i = 0; i < weapons.Count; i++)
            {
                if (weapons[i] == null) continue;
                weapons[i].gameObject.SetActive(i == CurrentIndex);
            }
            CurrentWeapon = weapons[CurrentIndex];
            OnWeaponEquipped?.Invoke(CurrentWeapon);
        }

        public void NextWeapon()     => EquipWeapon((CurrentIndex + 1) % Mathf.Max(weapons.Count, 1));
        public void PreviousWeapon() => EquipWeapon((CurrentIndex - 1 + weapons.Count) % Mathf.Max(weapons.Count, 1));

        private void HandleFirePressed()
        {
            // Single-shot path; full-auto is handled in Update via FireHeld.
            if (CurrentWeapon != null) CurrentWeapon.TryFire();
        }

        private void HandleReload()
        {
            if (CurrentWeapon != null) CurrentWeapon.StartReload();
        }
    }
}
