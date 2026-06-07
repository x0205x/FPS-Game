using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Common;
using Game.Player;
using Game.WaveSystem;
using Game.Weapons;

namespace Game.UI
{
    /// <summary>
    /// Canvas-based HUD. Wires Health, current weapon, and wave info to a few
    /// inspector-assigned UI widgets. Any of the references can be left empty —
    /// the script no-ops cleanly if a widget is missing.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private PlayerController player;
        [SerializeField] private WeaponManager    weaponManager;
        [SerializeField] private WaveManager      waveManager;

        [Header("Health / Shield")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private Slider shieldBar;
        [SerializeField] private TextMeshProUGUI healthLabel;

        [Header("Ammo")]
        [SerializeField] private TextMeshProUGUI ammoLabel;

        [Header("Wave")]
        [SerializeField] private TextMeshProUGUI waveLabel;
        [SerializeField] private TextMeshProUGUI enemiesLeftLabel;

        [Header("Crosshair")]
        [SerializeField] private GameObject crosshair;

        private Health _health;
        private WeaponBase _currentWeapon;

        private void Start()
        {
            if (player != null) _health = player.Health;
            if (_health != null)
            {
                _health.OnHealthChanged.AddListener(UpdateHealth);
                _health.OnShieldChanged.AddListener(UpdateShield);
                UpdateHealth(_health.CurrentHealth, _health.MaxHealth);
                UpdateShield(_health.CurrentShield, _health.MaxShield);
            }

            if (weaponManager != null)
            {
                weaponManager.OnWeaponEquipped += BindWeapon;
                BindWeapon(weaponManager.CurrentWeapon);
            }

            if (waveManager != null)
            {
                waveManager.OnWaveStarted.AddListener(UpdateWave);
                waveManager.OnEnemyCountChanged.AddListener(UpdateEnemiesLeft);
            }
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnHealthChanged.RemoveListener(UpdateHealth);
                _health.OnShieldChanged.RemoveListener(UpdateShield);
            }
            if (weaponManager != null) weaponManager.OnWeaponEquipped -= BindWeapon;
            if (waveManager != null)
            {
                waveManager.OnWaveStarted.RemoveListener(UpdateWave);
                waveManager.OnEnemyCountChanged.RemoveListener(UpdateEnemiesLeft);
            }
            if (_currentWeapon != null) _currentWeapon.OnAmmoChanged -= UpdateAmmoFromCurrent;
        }

        private void UpdateHealth(float cur, float max)
        {
            if (healthBar   != null) healthBar.value = max > 0f ? cur / max : 0f;
            if (healthLabel != null) healthLabel.text = $"{Mathf.CeilToInt(cur)} / {Mathf.CeilToInt(max)}";
        }

        private void UpdateShield(float cur, float max)
        {
            if (shieldBar == null) return;
            shieldBar.gameObject.SetActive(max > 0f);
            shieldBar.value = max > 0f ? cur / max : 0f;
        }

        private void BindWeapon(WeaponBase weapon)
        {
            if (_currentWeapon != null) _currentWeapon.OnAmmoChanged -= UpdateAmmoFromCurrent;
            _currentWeapon = weapon;
            if (_currentWeapon != null) _currentWeapon.OnAmmoChanged += UpdateAmmoFromCurrent;
            UpdateAmmoFromCurrent();
        }

        private void UpdateAmmoFromCurrent()
        {
            if (ammoLabel == null) return;
            ammoLabel.text = _currentWeapon != null
                ? $"{_currentWeapon.CurrentAmmo} / {_currentWeapon.ReserveAmmo}"
                : "-- / --";
        }

        private void UpdateWave(int idx)
        {
            if (waveLabel != null) waveLabel.text = $"Wave {idx + 1}";
        }

        private void UpdateEnemiesLeft(int alive, int total)
        {
            if (enemiesLeftLabel != null) enemiesLeftLabel.text = $"{alive} / {total}";
        }
    }
}
