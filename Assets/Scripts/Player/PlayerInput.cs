using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    /// <summary>
    /// Reads from a Unity Input System asset and republishes the values as a stable,
    /// decoupled API (properties + events). All other player modules consume this
    /// component and never touch <see cref="UnityEngine.InputSystem"/> directly.
    /// </summary>
    public class PlayerInput : MonoBehaviour
    {
        [Header("Input Asset")]
        [Tooltip("Drag PlayerInputActions.inputactions here.")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";

        [Header("Action Names")]
        [SerializeField] private string moveActionName   = "Move";
        [SerializeField] private string lookActionName   = "Look";
        [SerializeField] private string jumpActionName   = "Jump";
        [SerializeField] private string runActionName    = "Run";
        [SerializeField] private string aimActionName    = "Aim";
        [SerializeField] private string fireActionName   = "Fire";
        [SerializeField] private string reloadActionName = "Reload";
        [SerializeField] private string interactActionName = "Interact";
        [SerializeField] private string pauseActionName  = "Pause";

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool RunHeld  { get; private set; }
        public bool AimHeld  { get; private set; }
        public bool FireHeld { get; private set; }
        public bool JumpHeld { get; private set; }

        public event Action OnJumpPerformed;
        public event Action OnFirePerformed;
        public event Action OnReloadPerformed;
        public event Action OnInteractPerformed;
        public event Action OnAimStarted;
        public event Action OnAimCanceled;
        public event Action OnPausePerformed;

        private InputActionMap _map;
        private InputAction _move, _look, _jump, _run, _aim, _fire, _reload, _interact, _pause;

        private void Awake()
        {
            if (inputActions == null)
            {
                Debug.LogError($"[{nameof(PlayerInput)}] InputActionAsset is not assigned.", this);
                enabled = false;
                return;
            }

            _map    = inputActions.FindActionMap(actionMapName, throwIfNotFound: true);
            _move   = _map.FindAction(moveActionName,   throwIfNotFound: true);
            _look   = _map.FindAction(lookActionName,   throwIfNotFound: true);
            _jump   = _map.FindAction(jumpActionName,   throwIfNotFound: true);
            _run    = _map.FindAction(runActionName,    throwIfNotFound: true);
            _aim    = _map.FindAction(aimActionName,    throwIfNotFound: true);
            _fire   = _map.FindAction(fireActionName,   throwIfNotFound: true);
            _reload = _map.FindAction(reloadActionName, throwIfNotFound: true);
            _interact = _map.FindAction(interactActionName, throwIfNotFound: true);
            _pause  = _map.FindAction(pauseActionName,  throwIfNotFound: true);

            _jump.performed   += ctx => OnJumpPerformed?.Invoke();
            _fire.performed   += ctx => OnFirePerformed?.Invoke();
            _reload.performed += ctx => OnReloadPerformed?.Invoke();
            _interact.performed += ctx => OnInteractPerformed?.Invoke();
            _aim.started      += ctx => OnAimStarted?.Invoke();
            _aim.canceled     += ctx => OnAimCanceled?.Invoke();
            _pause.performed  += ctx => OnPausePerformed?.Invoke();
        }

        private void OnEnable()  => _map?.Enable();
        private void OnDisable() => _map?.Disable();

        private void Update()
        {
            MoveInput = _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
            LookInput = _look != null ? _look.ReadValue<Vector2>() : Vector2.zero;
            RunHeld   = _run  != null && _run.IsPressed();
            AimHeld   = _aim  != null && _aim.IsPressed();
            FireHeld  = _fire != null && _fire.IsPressed();
            JumpHeld  = _jump != null && _jump.IsPressed();
        }
    }
}
