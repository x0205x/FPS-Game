using UnityEngine;
using UnityEngine.AI;
using Game.AI.States;
using Game.Common;

namespace Game.AI
{
    /// <summary>
    /// The enemy's "self". Owns the FSM and references to all the perception,
    /// combat and movement components. State classes read/mutate this object.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Health))]
    public class EnemyController : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField, Min(0f)] private float walkSpeed   = 2.5f;
        [SerializeField, Min(0f)] private float chaseSpeed  = 5f;

        [Header("Modules")]
        [SerializeField] private EnemyVision      vision;
        [SerializeField] private EnemyCoverSystem cover;

        [Header("Patrol")]
        [SerializeField] private Transform[] patrolPoints;

        public NavMeshAgent       Agent     { get; private set; }
        public Health             Health    { get; private set; }
        public EnemyStateMachine  States    { get; private set; }
        public EnemyVision        Vision    => vision;
        public EnemyCoverSystem   Cover     => cover;
        public Transform[]        PatrolPoints => patrolPoints;
        public float WalkSpeed   => walkSpeed;
        public float ChaseSpeed  => chaseSpeed;

        public Transform Target { get; set; }
        public Vector3?  LastKnownTargetPosition { get; set; }

        private void Awake()
        {
            Agent  = GetComponent<NavMeshAgent>();
            Health = GetComponent<Health>();
            if (vision == null) vision = GetComponentInChildren<EnemyVision>();
            if (cover  == null) cover  = GetComponentInChildren<EnemyCoverSystem>();

            States = new EnemyStateMachine(this);
            States.Register(new IdleState());
            States.Register(new PatrolState());
            States.Register(new InvestigateState());
            States.Register(new SeekCoverState());
            States.Register(new AttackState());
            States.Register(new DeadState());
        }

        private void Start() => States.ChangeState<IdleState>();

        private void OnEnable()
        {
            if (Health != null) Health.OnDied.AddListener(HandleDied);
        }

        private void OnDisable()
        {
            if (Health != null) Health.OnDied.RemoveListener(HandleDied);
        }

        private void Update() => States.Tick(Time.deltaTime);

        private void HandleDied() => States.ChangeState<DeadState>();
    }
}
