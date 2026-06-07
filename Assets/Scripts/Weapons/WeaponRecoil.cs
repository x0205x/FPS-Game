using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// Applies a brief local kick to the weapon root on each shot and eases back to rest.
    /// Does not move the camera.
    /// </summary>
    [RequireComponent(typeof(WeaponBase))]
    [DefaultExecutionOrder(100)]
    public class WeaponRecoil : MonoBehaviour
    {
        [Header("Kick (local space, metres)")]
        [SerializeField, Min(0f)] private float kickBack = 0.035f;
        [SerializeField, Min(0f)] private float kickUp   = 0.018f;

        [Header("Recovery")]
        [SerializeField, Min(0.01f)] private float recoveryDuration = 0.2f;

        private WeaponBase _weapon;
        private Vector3 _restLocalPosition;
        private Vector3 _kickOffset;
        private float _recoveryStart = -1f;

        private void Awake()
        {
            _weapon = GetComponent<WeaponBase>();
        }

        private void OnEnable()
        {
            if (_weapon != null) _weapon.OnFired += HandleFired;
        }

        private void OnDisable()
        {
            if (_weapon != null) _weapon.OnFired -= HandleFired;
            transform.localPosition = _restLocalPosition;
            _recoveryStart = -1f;
        }

        private void Start()
        {
            _restLocalPosition = transform.localPosition;
        }

        private void LateUpdate()
        {
            if (_recoveryStart < 0f) return;

            float t = Mathf.Clamp01((Time.time - _recoveryStart) / recoveryDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            Vector3 offset = Vector3.Lerp(_kickOffset, Vector3.zero, eased);
            transform.localPosition = _restLocalPosition + offset;

            if (t >= 1f)
                _recoveryStart = -1f;
        }

        private void HandleFired()
        {
            _kickOffset = new Vector3(0f, kickUp, -kickBack);
            _recoveryStart = Time.time;
            transform.localPosition = _restLocalPosition + _kickOffset;
        }
    }
}
