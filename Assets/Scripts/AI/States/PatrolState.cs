using UnityEngine;

namespace Game.AI.States
{
    public class PatrolState : IEnemyState
    {
        private int _index;

        public void Enter(EnemyController enemy)
        {
            enemy.Agent.speed = enemy.WalkSpeed;
            GoToNext(enemy);
        }

        public void Tick(EnemyController enemy, float dt)
        {
            if (enemy.Vision != null && enemy.Vision.HasTarget)
            {
                enemy.Target = enemy.Vision.CurrentTarget;
                enemy.States.ChangeState<AttackState>();
                return;
            }

            if (!enemy.Agent.pathPending && enemy.Agent.remainingDistance < 0.5f)
            {
                _index = (_index + 1) % Mathf.Max(enemy.PatrolPoints.Length, 1);
                GoToNext(enemy);
            }
        }

        public void Exit(EnemyController enemy) { }

        private void GoToNext(EnemyController enemy)
        {
            if (enemy.PatrolPoints == null || enemy.PatrolPoints.Length == 0) return;
            Transform p = enemy.PatrolPoints[_index];
            if (p != null) enemy.Agent.SetDestination(p.position);
        }
    }
}
