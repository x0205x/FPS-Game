using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// Parents the weapon on the animated humanoid right hand and aligns the pistol grip (HandIKRight) to the palm.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class WeaponHandAttach : MonoBehaviour
    {
        [SerializeField] private string socketName = "WeaponSocket";
        [SerializeField] private Vector3 fineTuneLocalPosition;
        [SerializeField] private Vector3 fineTuneLocalEuler;

        private void Start() => Attach();

        public void Attach()
        {
            Animator animator = GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator == null || !animator.isHuman) return;

            Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (hand == null)
                hand = FindChildByName(transform, "Right_Hand", "RightHand", "Right Hand");
            if (hand == null) return;

            Transform socket = FindChildByName(transform, socketName);
            if (socket == null) return;

            socket.SetParent(hand, worldPositionStays: false);
            socket.localPosition = Vector3.zero;
            socket.localRotation = Quaternion.identity;
            socket.localScale    = Vector3.one;

            Transform weaponRoot = socket.Find("Pistol");
            if (weaponRoot == null) return;

            Transform grip = WeaponGripAlign.FindGripPoint(weaponRoot);
            if (grip == null) return;

            WeaponGripAlign.AlignToAnchor(socket, weaponRoot, grip);

            if (fineTuneLocalPosition != Vector3.zero || fineTuneLocalEuler != Vector3.zero)
            {
                weaponRoot.localPosition += fineTuneLocalPosition;
                weaponRoot.localRotation *= Quaternion.Euler(fineTuneLocalEuler);
            }
        }

        private static Transform FindChildByName(Transform root, params string[] names)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                foreach (string name in names)
                {
                    if (t.name == name) return t;
                }
            }
            return null;
        }
    }
}
