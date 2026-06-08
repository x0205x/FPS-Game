using Game.Player;
using Game.Weapons;
using Unity.Cinemachine;
using UnityEngine;

namespace Game.Vehicles
{
    /// <summary>
    /// Enter/exit the Osprey and route player input into flight controls.
    /// Interact (F) enters when within range; exit always works while piloting.
    /// </summary>
    public class AircraftPilot : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AircraftFlightController flight;
        [SerializeField] private Transform exitPoint;
        [SerializeField, Min(0.5f)] private float enterRadius = 6f;

        [Header("Debug")]
        [SerializeField] private bool debugLogs;

        private PlayerController _player;
        private PlayerInput _input;
        private AircraftCameraFollow _aircraftCamera;
        private CinemachineBrain _cinemachineBrain;
        private CharacterController _characterController;
        private Transform _characterVisual;
        private WeaponManager _weaponManager;
        private PlayerCombatAnimator _combatAnimator;
        private PlayerAnimator _playerAnimator;
        private bool _piloting;

        public bool IsPiloting => _piloting;
        public string InteractKeyLabel => "F";

        public bool CanEnter =>
            !_piloting
            && _player != null
            && Vector3.Distance(_player.transform.position, transform.position) <= enterRadius;

        private void Awake()
        {
            if (flight == null) flight = GetComponent<AircraftFlightController>();
            if (exitPoint == null)
            {
                var exit = transform.Find("ExitPoint");
                if (exit != null) exitPoint = exit;
            }

            if (GetComponent<AircraftInteractPrompt>() == null)
                gameObject.AddComponent<AircraftInteractPrompt>();
        }

        private void Start()
        {
            GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null) BindPlayer(playerGo.GetComponent<PlayerController>());
        }

        public void BindPlayer(PlayerController player)
        {
            if (player == null) return;
            if (_player == player && _input != null) return;

            if (_input != null)
                _input.OnInteractPerformed -= TogglePilot;

            _player = player;

            _input = _player.Input;
            _characterController = _player.GetComponent<CharacterController>();

            Transform character = _player.transform.Find("Character");
            _characterVisual = character != null ? character : _player.transform;

            _weaponManager = _player.GetComponentInChildren<WeaponManager>();
            _combatAnimator = _player.GetComponentInChildren<PlayerCombatAnimator>();
            _playerAnimator = _player.Animator;

            Camera main = Camera.main;
            if (main != null)
            {
                _cinemachineBrain = main.GetComponent<CinemachineBrain>();
                _aircraftCamera = main.GetComponent<AircraftCameraFollow>();
                if (_aircraftCamera == null)
                    _aircraftCamera = main.gameObject.AddComponent<AircraftCameraFollow>();
                _aircraftCamera.SetTarget(transform);
            }

            if (_input != null)
                _input.OnInteractPerformed += TogglePilot;
        }

        private void OnDestroy()
        {
            if (_input != null)
                _input.OnInteractPerformed -= TogglePilot;
        }

        private void Update()
        {
            if (!_piloting || flight == null || _input == null) return;

            flight.SetMoveInput(_input.MoveInput);
            flight.SetLookInput(_input.LookInput);
            flight.SetBoostHeld(_input.RunHeld);
            flight.SetAscendHeld(_input.JumpHeld);
        }

        private void TogglePilot()
        {
            if (_piloting)
            {
                ExitAircraft();
                return;
            }

            if (CanEnter) EnterAircraft();
        }

        private void EnterAircraft()
        {
            if (_player == null || _piloting) return;

            _piloting = true;
            flight.SetPiloted(true);

            _characterController.enabled = false;
            _player.Movement.enabled = false;
            if (_player.Camera != null) _player.Camera.enabled = false;

            if (_weaponManager != null) _weaponManager.enabled = false;
            if (_combatAnimator != null) _combatAnimator.enabled = false;
            if (_playerAnimator != null) _playerAnimator.enabled = false;

            _characterVisual.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (_cinemachineBrain != null) _cinemachineBrain.enabled = false;
            if (_aircraftCamera != null) _aircraftCamera.SetActive(true);

            if (debugLogs)
                Debug.Log($"[{nameof(AircraftPilot)}] Entered aircraft.", this);
        }

        private void ExitAircraft()
        {
            if (!_piloting || _player == null) return;

            _piloting = false;
            flight.SetPiloted(false);

            Vector3 exitPos = exitPoint != null ? exitPoint.position : transform.position + transform.right * 4f;
            _player.transform.SetPositionAndRotation(exitPos, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));

            _characterController.enabled = true;
            _player.Movement.enabled = true;
            _characterVisual.gameObject.SetActive(true);

            if (_weaponManager != null) _weaponManager.enabled = true;
            if (_combatAnimator != null) _combatAnimator.enabled = true;
            if (_playerAnimator != null) _playerAnimator.enabled = true;

            if (_player.Camera != null) _player.Camera.enabled = true;
            if (_cinemachineBrain != null) _cinemachineBrain.enabled = true;
            if (_aircraftCamera != null) _aircraftCamera.SetActive(false);

            if (debugLogs)
                Debug.Log($"[{nameof(AircraftPilot)}] Exited aircraft.", this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, enterRadius);
        }
    }
}
