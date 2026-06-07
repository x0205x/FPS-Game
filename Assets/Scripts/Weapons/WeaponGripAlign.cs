using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// Aligns a weapon so its authored grip point (HandIKRight on Alteruna guns) sits on the character hand.
    /// </summary>
    public static class WeaponGripAlign
    {
        private static readonly string[] GripBoneNames =
        {
            "HandIKRight",
            "HandIK_Right",
            "Attach_Grip",
            "Grip",
        };

        public static Transform FindGripPoint(Transform weaponRoot)
        {
            if (weaponRoot == null) return null;

            foreach (string name in GripBoneNames)
            {
                Transform grip = FindChildByName(weaponRoot, name);
                if (grip != null) return grip;
            }

            // Trigger + magazine sit on the grip assembly on Alteruna pistols.
            Transform trigger = FindChildByName(weaponRoot, "Trigger");
            if (trigger != null) return trigger;

            return FindChildByName(weaponRoot, "Body");
        }

        /// <summary>
        /// Moves and rotates <paramref name="weaponRoot"/> so <paramref name="gripPoint"/> matches <paramref name="anchor"/>.
        /// </summary>
        public static void AlignToAnchor(Transform anchor, Transform weaponRoot, Transform gripPoint)
        {
            if (anchor == null || weaponRoot == null || gripPoint == null) return;

            weaponRoot.SetParent(anchor, worldPositionStays: true);

            Quaternion rotationDelta = anchor.rotation * Quaternion.Inverse(gripPoint.rotation);
            weaponRoot.rotation = rotationDelta * weaponRoot.rotation;

            gripPoint = FindChildByName(weaponRoot, gripPoint.name) ?? gripPoint;
            weaponRoot.position += anchor.position - gripPoint.position;

            weaponRoot.SetParent(anchor, worldPositionStays: false);
        }

        private static Transform FindChildByName(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) return t;
            }
            return null;
        }
    }
}
