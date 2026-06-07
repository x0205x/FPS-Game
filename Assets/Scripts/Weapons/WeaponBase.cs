using System;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// Base class for every firearm in the project. Holds shared state
    /// (ammo, fire rate, ADS state) and exposes events that the UI, audio,
    /// VFX and animation layers can subscribe to. Subclasses implement
    /// <see cref="OnFire"/> for projectile vs hitscan behaviour.
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] protected string weaponName = "Weapon";

        [Header("Ammo")]
        [SerializeField, Min(1)] protected int magazineSize  = 30;
        [SerializeField, Min(0)] protected int reserveAmmoMax = 120;
        [SerializeField, Min(0)] protected int currentAmmo    = 30;
        [SerializeField, Min(0)] protected int reserveAmmo    = 120;

        [Header("Fire")]
        [SerializeField, Min(0f)] protected float roundsPerMinute = 600f;
        [SerializeField] protected bool fullAuto = true;
        [SerializeField, Min(0f)] protected float damage = 25f;
        [SerializeField, Min(0f)] protected float range  = 100f;

        [Header("Reload")]
        [SerializeField, Min(0f)] protected float reloadSeconds = 2.0f;

        [Header("Muzzle")]
        [SerializeField] protected Transform muzzle;

        public string WeaponName => weaponName;
        public int    CurrentAmmo => currentAmmo;
        public int    ReserveAmmo => reserveAmmo;
        public int    MagazineSize => magazineSize;
        public bool   IsReloading { get; private set; }
        public bool   FullAuto    => fullAuto;
        public bool   CanFire => !IsReloading && currentAmmo > 0 && Time.time >= _nextFireAllowedAt;

        public event Action OnFired;
        public event Action OnReloadStarted;
        public event Action OnReloadCompleted;
        public event Action OnAmmoChanged;

        protected float _nextFireAllowedAt;
        protected float _secondsBetweenShots => roundsPerMinute > 0f ? 60f / roundsPerMinute : 0f;

        public virtual void TryFire()
        {
            if (!CanFire) return;
            currentAmmo--;
            _nextFireAllowedAt = Time.time + _secondsBetweenShots;
            OnFire();
            OnFired?.Invoke();
            OnAmmoChanged?.Invoke();
        }

        /// <summary>Subclasses spawn projectiles or run hitscan here.</summary>
        protected abstract void OnFire();

        public virtual void StartReload()
        {
            if (IsReloading || currentAmmo == magazineSize || reserveAmmo <= 0) return;
            IsReloading = true;
            OnReloadStarted?.Invoke();
            Invoke(nameof(FinishReload), reloadSeconds);
        }

        protected virtual void FinishReload()
        {
            int needed = magazineSize - currentAmmo;
            int take   = Mathf.Min(needed, reserveAmmo);
            currentAmmo += take;
            reserveAmmo -= take;
            IsReloading  = false;
            OnReloadCompleted?.Invoke();
            OnAmmoChanged?.Invoke();
        }

        public void AddReserveAmmo(int amount)
        {
            reserveAmmo = Mathf.Clamp(reserveAmmo + amount, 0, reserveAmmoMax);
            OnAmmoChanged?.Invoke();
        }
    }
}
