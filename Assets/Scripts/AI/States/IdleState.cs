using UnityEngine;

namespace Game.AI.States
{
    public class IdleState : IEnemyState
    {
        private float _idleUntil;

        public void Enter(EnemyController enemy)
        {
            enemy.Agent.isStopped = true;
            _idleUntil = Time.time + Random.Range(1.5f, 3.5f);
        }

        public void Tick(EnemyController enemy, float dt)
        {
            if (enemy.Vision != null && enemy.Vision.HasTarget)
            {
                enemy.Target = enemy.Vision.CurrentTarget;
                enemy.States.ChangeState<AttackState>();
                return;
            }

            if (Time.time >= _idleUntil && enemy.PatrolPoints != null && enemy.PatrolPoints.Length > 0)
            {
                enemy.States.ChangeState<PatrolState>();
            }
        }

        public void Exit(EnemyController enemy) => enemy.Agent.isStopped = false;
    }
}
