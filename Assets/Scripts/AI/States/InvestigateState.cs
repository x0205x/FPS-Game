using UnityEngine;

namespace Game.AI.States
{
    public class InvestigateState : IEnemyState
    {
        private float _giveUpAt;

        public void Enter(EnemyController enemy)
        {
            enemy.Agent.speed = enemy.WalkSpeed;
            if (enemy.LastKnownTargetPosition.HasValue)
                enemy.Agent.SetDestination(enemy.LastKnownTargetPosition.Value);
            _giveUpAt = Time.time + 6f;
        }

        public void Tick(EnemyController enemy, float dt)
        {
            if (enemy.Vision != null && enemy.Vision.HasTarget)
            {
                enemy.Target = enemy.Vision.CurrentTarget;
                enemy.States.ChangeState<AttackState>();
                return;
            }

            if (Time.time >= _giveUpAt) enemy.States.ChangeState<IdleState>();
        }

        public void Exit(EnemyController enemy) { }
    }
}
