using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Ensures the skinned mesh Animator uses this model's own Humanoid Avatar
    /// (not another FBX's avatar) and rebinds mixamorig skin weights before play.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [DefaultExecutionOrder(-200)]
    public class CharacterAnimatorSetup : MonoBehaviour
    {
        [SerializeField] private Avatar humanoidAvatar;

        private void Awake()
        {
            var animator = GetComponent<Animator>();
            if (animator == null) return;

            if (humanoidAvatar != null)
                animator.avatar = humanoidAvatar;

            if (animator.avatar == null)
            {
                Debug.LogError(
                    $"[{nameof(CharacterAnimatorSetup)}] Humanoid Avatar is missing on '{name}'. " +
                    "Run Tools → Game → Fix Character Locomotion or rebuild the test scene.",
                    this);
                return;
            }

            if (!animator.avatar.isValid || !animator.avatar.isHuman)
            {
                Debug.LogError(
                    $"[{nameof(CharacterAnimatorSetup)}] Avatar on '{name}' is not a valid Humanoid avatar.",
                    this);
            }

            var binder = GetComponent<CharacterRigBinder>();
            if (binder == null)
                binder = gameObject.AddComponent<CharacterRigBinder>();
            binder.RebindSkinnedMeshes();
        }
    }
}
