using UnityEngine;
using Game.Common;

namespace Game.Weapons
{
    /// <summary>
    /// Optional projectile bullet for non-hitscan weapons. Travels along its
    /// forward axis at <see cref="speed"/> until it hits something or its
    /// lifetime expires.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Bullet : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float damage   = 25f;
        [SerializeField, Min(0f)] private float speed    = 80f;
        [SerializeField, Min(0f)] private float lifetime = 4f;
        [SerializeField] private GameObject impactPrefab;

        private GameObject _shooter;
        private Rigidbody  _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
        }

        public void Launch(GameObject shooter, Vector3 direction)
        {
            _shooter = shooter;
            transform.forward = direction.normalized;
            _rigidbody.linearVelocity = direction.normalized * speed;
            Destroy(gameObject, lifetime);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_shooter != null && collision.collider.transform.IsChildOf(_shooter.transform)) return;

            if (collision.collider.TryGetComponent<IDamageable>(out var dmg))
            {
                dmg.ApplyDamage(damage, _shooter);
            }

            if (impactPrefab != null)
            {
                var contact = collision.GetContact(0);
                Instantiate(impactPrefab, contact.point, Quaternion.LookRotation(contact.normal));
            }

            Destroy(gameObject);
        }
    }
}
