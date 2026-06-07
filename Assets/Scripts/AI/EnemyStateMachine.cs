using System.Collections.Generic;
using Game.AI.States;

namespace Game.AI
{
    /// <summary>
    /// Lightweight FSM driver. The owning <see cref="EnemyController"/> calls
    /// <see cref="Tick"/> each frame and uses <see cref="ChangeState"/> to switch.
    /// </summary>
    public class EnemyStateMachine
    {
        private readonly EnemyController _enemy;
        private readonly Dictionary<System.Type, IEnemyState> _states = new();

        public IEnemyState Current { get; private set; }
        public System.Type CurrentType => Current?.GetType();

        public EnemyStateMachine(EnemyController enemy) => _enemy = enemy;

        public T Register<T>(T state) where T : IEnemyState
        {
            _states[typeof(T)] = state;
            return state;
        }

        public void ChangeState<T>() where T : IEnemyState
        {
            if (!_states.TryGetValue(typeof(T), out var next)) return;
            if (Current == next) return;
            Current?.Exit(_enemy);
            Current = next;
            Current.Enter(_enemy);
        }

        public void Tick(float deltaTime) => Current?.Tick(_enemy, deltaTime);
    }
}
