using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Game.AI
{
    /// <summary>
    /// Drives humanoid locomotion parameters from <see cref="NavMeshAgent"/> velocity,
    /// matching the player's <c>Speed</c> blend thresholds (0 idle / 0.5 walk / 1 run).
    /// </summary>
    [DefaultExecutionOrder(5)]
    public class EnemyLocomotionAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private EnemyController enemy;

        [Header("Float Parameters")]
        [SerializeField] private string speedParam = "Speed";

        [Header("Bool Parameters")]
        [SerializeField] private string groundedParam = "Grounded";

        [Header("Smoothing")]
        [SerializeField, Min(0f)] private float speedDamp = 0.12f;

        private int _speedHash, _groundedHash;
        private HashSet<int> _availableParams;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (enemy == null) enemy = GetComponentInParent<EnemyController>();

            _speedHash    = Animator.StringToHash(speedParam);
            _groundedHash = Animator.StringToHash(groundedParam);
            CacheAvailableParameters();
        }

        private void Update()
        {
            if (animator == null || enemy == null || enemy.Agent == null) return;

            NavMeshAgent agent = enemy.Agent;
            float planarSpeed = new Vector3(agent.velocity.x, 0f, agent.velocity.z).magnitude;

            float blend;
            if (planarSpeed < 0.05f || agent.isStopped)
            {
                blend = 0f;
            }
            else if (agent.speed > enemy.WalkSpeed + 0.15f)
            {
                float runSpan = Mathf.Max(enemy.ChaseSpeed - enemy.WalkSpeed, 0.01f);
                blend = Mathf.Lerp(0.5f, 1f, Mathf.Clamp01((planarSpeed - enemy.WalkSpeed) / runSpan));
            }
            else
            {
                blend = Mathf.Lerp(0f, 0.5f, Mathf.Clamp01(planarSpeed / Mathf.Max(enemy.WalkSpeed, 0.01f)));
            }

            if (Has(_speedHash))
                animator.SetFloat(_speedHash, blend, speedDamp, Time.deltaTime);
            if (Has(_groundedHash))
                animator.SetBool(_groundedHash, true);
        }

        private void CacheAvailableParameters()
        {
            _availableParams = new HashSet<int>();
            if (animator == null) return;
            foreach (AnimatorControllerParameter p in animator.parameters)
                _availableParams.Add(p.nameHash);
        }

        private bool Has(int hash) => _availableParams != null && _availableParams.Contains(hash);
    }
}
