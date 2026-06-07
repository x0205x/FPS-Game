using UnityEngine;

namespace Game.AI.States
{
    public class SeekCoverState : IEnemyState
    {
        public void Enter(EnemyController enemy)
        {
            enemy.Agent.speed = enemy.ChaseSpeed;
            if (enemy.Cover != null && enemy.Target != null &&
                enemy.Cover.TryFindCover(enemy.transform.position, enemy.Target.position, out Vector3 spot))
            {
                enemy.Agent.SetDestination(spot);
            }
            else
            {
                enemy.States.ChangeState<AttackState>();
            }
        }

        public void Tick(EnemyController enemy, float dt)
        {
            if (enemy.Target == null)
            {
                enemy.States.ChangeState<IdleState>();
                return;
            }

            if (!enemy.Agent.pathPending && enemy.Agent.remainingDistance < 0.5f)
            {
                enemy.States.ChangeState<AttackState>();
            }
        }

        public void Exit(EnemyController enemy) { }
    }
}
