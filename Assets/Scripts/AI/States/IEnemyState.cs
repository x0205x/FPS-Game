namespace Game.AI.States
{
    /// <summary>
    /// Minimal FSM state contract. Implementations live in the same namespace
    /// and operate on the owning <see cref="EnemyController"/> via the state machine.
    /// </summary>
    public interface IEnemyState
    {
        void Enter(EnemyController enemy);
        void Tick(EnemyController enemy, float deltaTime);
        void Exit(EnemyController enemy);
    }
}
