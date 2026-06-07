using UnityEngine;
using Unity.Cinemachine;

namespace Game.Player
{
    /// <summary>
    /// Cinemachine 3 third-person camera controller. Holds two CinemachineCameras —
    /// a hip-fire shoulder cam and an ADS aim cam — and switches between them by
    /// toggling priorities when the player aims. FOV is also tweened for a subtle
    /// scope/zoom feel.
    ///
    /// Set up in the scene:
    ///   - Main Camera with CinemachineBrain
    ///   - Two CinemachineCamera GameObjects with CinemachineThirdPersonFollow,
    ///     each pointing at a different aim/follow target on the player.
    /// </summary>
    public class PlayerCamera : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInput input;

        [Header("Cinemachine Cameras")]
        [SerializeField] private CinemachineCamera hipCam;
        [SerializeField] private CinemachineCamera aimCam;

        [Header("Priorities")]
        [SerializeField] private int activePriority   = 20;
        [SerializeField] private int inactivePriority = 10;

        [Header("Field of View")]
        [SerializeField, Min(1f)] private float hipFov = 60f;
        [SerializeField, Min(1f)] private float aimFov = 40f;
        [SerializeField, Min(0f)] private float fovBlendSpeed = 12f;

        public bool IsAiming { get; private set; }

        private void Awake()
        {
            if (input == null) input = GetComponentInParent<PlayerInput>();
            ApplyPriorities(false);
            if (hipCam != null) hipCam.Lens.FieldOfView = hipFov;
            if (aimCam != null) aimCam.Lens.FieldOfView = aimFov;
        }

        private void OnEnable()
        {
            if (input == null) return;
            input.OnAimStarted  += HandleAimStart;
            input.OnAimCanceled += HandleAimEnd;
        }

        private void OnDisable()
        {
            if (input == null) return;
            input.OnAimStarted  -= HandleAimStart;
            input.OnAimCanceled -= HandleAimEnd;
        }

        private void Update()
        {
            float targetFov = IsAiming ? aimFov : hipFov;
            if (hipCam != null) hipCam.Lens.FieldOfView = Mathf.Lerp(hipCam.Lens.FieldOfView, targetFov, Time.deltaTime * fovBlendSpeed);
            if (aimCam != null) aimCam.Lens.FieldOfView = Mathf.Lerp(aimCam.Lens.FieldOfView, targetFov, Time.deltaTime * fovBlendSpeed);
        }

        private void HandleAimStart()
        {
            IsAiming = true;
            ApplyPriorities(true);
        }

        private void HandleAimEnd()
        {
            IsAiming = false;
            ApplyPriorities(false);
        }

        private void ApplyPriorities(bool aiming)
        {
            if (hipCam != null) hipCam.Priority = aiming ? inactivePriority : activePriority;
            if (aimCam != null) aimCam.Priority = aiming ? activePriority   : inactivePriority;
        }
    }
}
