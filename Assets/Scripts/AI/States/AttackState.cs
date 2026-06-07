using UnityEngine;

namespace Game.AI.States
{
    public class AttackState : IEnemyState
    {
        [System.Serializable]
        public class Config
        {
            public float preferredDistance = 8f;
            public float seekCoverChance = 0.3f;
            public float seekCoverEvalEvery = 4f;
        }

        private float _nextCoverEvalAt;

        public void Enter(EnemyController enemy)
        {
            enemy.Agent.speed = enemy.ChaseSpeed;
            enemy.Agent.updateRotation = false;
            _nextCoverEvalAt = Time.time + 1.5f;
        }

        public void Tick(EnemyController enemy, float dt)
        {
            Transform target = enemy.Target;
            if (target == null)
            {
                enemy.States.ChangeState<IdleState>();
                return;
            }

            enemy.LastKnownTargetPosition = target.position;
            FaceTarget(enemy, target);

            float dist = Vector3.Distance(enemy.transform.position, target.position);
            float preferred = 8f;
            if (dist > preferred + 1f) enemy.Agent.SetDestination(target.position);
            else                       enemy.Agent.ResetPath();

            if (Time.time >= _nextCoverEvalAt)
            {
                _nextCoverEvalAt = Time.time + 4f;
                if (Random.value < 0.3f) enemy.States.ChangeState<SeekCoverState>();
            }

            if (enemy.Vision != null && !enemy.Vision.HasTarget)
            {
                enemy.States.ChangeState<InvestigateState>();
            }
        }

        public void Exit(EnemyController enemy)
        {
            if (enemy.Agent != null)
                enemy.Agent.updateRotation = true;
        }

        private static void FaceTarget(EnemyController enemy, Transform target)
        {
            Vector3 to = target.position - enemy.transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.001f) return;
            Quaternion look = Quaternion.LookRotation(to.normalized);
            enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, look, Time.deltaTime * 8f);
        }
    }
}
