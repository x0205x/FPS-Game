using System;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Common
{
    /// <summary>Anything that can take damage.</summary>
    public interface IDamageable
    {
        void ApplyDamage(float amount, GameObject source = null);
    }

    /// <summary>
    /// Generic health/shield component shared by player and enemies. Emits typed
    /// events so HUD, audio, AI, and VFX can listen without coupling.
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField, Min(0f)] private float maxHealth = 100f;
        [SerializeField, Min(0f)] private float currentHealth = 100f;

        [Header("Shield (optional)")]
        [SerializeField, Min(0f)] private float maxShield = 0f;
        [SerializeField, Min(0f)] private float currentShield = 0f;
        [SerializeField, Min(0f)] private float shieldRegenDelay = 3f;
        [SerializeField, Min(0f)] private float shieldRegenPerSecond = 25f;

        [Header("Events")]
        public UnityEvent<float, float> OnHealthChanged; // (current, max)
        public UnityEvent<float, float> OnShieldChanged; // (current, max)
        public UnityEvent<GameObject>   OnDamaged;       // (source)
        public UnityEvent               OnDied;

        public float MaxHealth     => maxHealth;
        public float CurrentHealth => currentHealth;
        public float MaxShield     => maxShield;
        public float CurrentShield => currentShield;
        public bool  IsDead        => currentHealth <= 0f;
        public bool  HasShield     => maxShield > 0f;

        private float _shieldRegenAt;
        private bool  _diedFired;

        private void Start()
        {
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            currentShield = Mathf.Clamp(currentShield, 0f, maxShield);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnShieldChanged?.Invoke(currentShield, maxShield);
        }

        private void Update()
        {
            if (IsDead || !HasShield) return;
            if (Time.time >= _shieldRegenAt && currentShield < maxShield)
            {
                currentShield = Mathf.Min(maxShield, currentShield + shieldRegenPerSecond * Time.deltaTime);
                OnShieldChanged?.Invoke(currentShield, maxShield);
            }
        }

        public void ApplyDamage(float amount, GameObject source = null)
        {
            if (IsDead || amount <= 0f) return;

            float remaining = amount;
            if (HasShield && currentShield > 0f)
            {
                float absorbed = Mathf.Min(currentShield, remaining);
                currentShield -= absorbed;
                remaining     -= absorbed;
                _shieldRegenAt = Time.time + shieldRegenDelay;
                OnShieldChanged?.Invoke(currentShield, maxShield);
            }

            if (remaining > 0f)
            {
                currentHealth = Mathf.Max(0f, currentHealth - remaining);
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }

            OnDamaged?.Invoke(source);
            if (currentHealth <= 0f && !_diedFired)
            {
                _diedFired = true;
                OnDied?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void Revive(float? healthOverride = null)
        {
            currentHealth = Mathf.Clamp(healthOverride ?? maxHealth, 0f, maxHealth);
            currentShield = maxShield;
            _diedFired = false;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnShieldChanged?.Invoke(currentShield, maxShield);
        }
    }
}
