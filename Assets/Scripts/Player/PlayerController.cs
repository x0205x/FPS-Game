using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Player façade. Add this to the player GameObject and <see cref="RequireComponent"/>
    /// will pull in the input + movement modules. Other systems (UI, weapons, AI,
    /// networking, gameplay scripts) should reference this single component instead
    /// of poking the individual modules directly.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerController : MonoBehaviour
    {
        public PlayerInput     Input    { get; private set; }
        public PlayerMovement  Movement { get; private set; }
        public PlayerAnimator  Animator { get; private set; }
        public PlayerCamera    Camera   { get; private set; }
        public Game.Common.Health Health { get; private set; }

        private void Awake()
        {
            Input    = GetComponent<PlayerInput>();
            Movement = GetComponent<PlayerMovement>();
            Animator = GetComponent<PlayerAnimator>();
            Camera   = GetComponentInChildren<PlayerCamera>();
            Health   = GetComponent<Game.Common.Health>();
        }
    }
}
