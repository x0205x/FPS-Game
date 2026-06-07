using UnityEngine;

namespace Game.AI.States
{
    public class DeadState : IEnemyState
    {
        public void Enter(EnemyController enemy)
        {
            if (enemy.Agent != null && enemy.Agent.enabled)
            {
                enemy.Agent.isStopped = true;
                enemy.Agent.ResetPath();
                enemy.Agent.enabled = false;
            }
            // Disable colliders that interfere with NavMesh after death; let
            // the death animation play out via Animator's Death state.
            foreach (var col in enemy.GetComponentsInChildren<Collider>())
                col.enabled = false;

            Object.Destroy(enemy.gameObject, 4f);
        }

        public void Tick(EnemyController enemy, float dt) { }
        public void Exit(EnemyController enemy) { }
    }
}
