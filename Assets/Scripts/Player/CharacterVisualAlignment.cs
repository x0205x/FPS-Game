using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Scales a character visual to target height and lifts it so the feet sit on
    /// the parent ground plane. Optional rotation correction for FBX axis quirks.
    /// </summary>
    public class CharacterVisualAlignment : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float targetHeight = 1.8f;
        [SerializeField] private float feetYOffset;
        [SerializeField] private Vector3 rotationCorrection;
        [SerializeField] private bool alignOnAwake = true;

        private void Awake()
        {
            Transform visual = visualRoot != null ? visualRoot : transform;
            StripDisplayStandMeshes(visual);
            if (alignOnAwake) Align();
        }

        public void Align()
        {
            Transform visual = visualRoot != null ? visualRoot : transform;
            StripDisplayStandMeshes(visual);
            AlignFeetAndScale(visual, targetHeight, feetYOffset);

            if (rotationCorrection.sqrMagnitude > 0.0001f)
                visual.localRotation = Quaternion.Euler(rotationCorrection);
        }

        /// <summary>
        /// Hides Sketchfab / marketplace display stands that ship inside character FBXs.
        /// </summary>
        public static void StripDisplayStandMeshes(Transform root)
        {
            if (root == null) return;

            string[] standNames =
            {
                "Floor", "floor", "cube", "Cube", "Base", "base",
                "Platform", "platform", "Pedestal", "pedestal", "Stand", "stand"
            };

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child == root) continue;

                foreach (string standName in standNames)
                {
                    if (child.name != standName) continue;
                    child.gameObject.SetActive(false);
                    break;
                }
            }
        }

        public static void AlignFeetAndScale(Transform visual, float height, float extraYOffset = 0f)
        {
            if (visual == null) return;

            var renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds worldBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    worldBounds.Encapsulate(renderers[i].bounds);

                float currentHeight = Mathf.Max(worldBounds.size.y, 0.01f);
                float scale = height / currentHeight;
                visual.localScale = Vector3.one * scale;
            }

            SnapFeetToParentFloor(visual, extraYOffset);
        }

        /// <summary>
        /// Moves the visual so the lowest foot bone sits on the parent's Y plane.
        /// Uses localPosition so the offset survives play mode and saves correctly.
        /// </summary>
        public static void SnapFeetToParentFloor(Transform visual, float extraYOffset = 0f)
        {
            if (visual == null) return;

            Transform parent = visual.parent != null ? visual.parent : visual;
            float floorY = parent.position.y;
            float lowestFootY = GetLowestFootWorldY(visual);

            if (lowestFootY < float.PositiveInfinity)
            {
                float lift = floorY - lowestFootY + extraYOffset;
                visual.localPosition += new Vector3(0f, lift, 0f);
                return;
            }

            // Fallback when foot bones are not found (e.g. before avatar is assigned).
            var renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float liftFromBounds = floorY - bounds.min.y + extraYOffset;
            visual.localPosition += new Vector3(0f, liftFromBounds, 0f);
        }

        private static float GetLowestFootWorldY(Transform visual)
        {
            float minY = float.PositiveInfinity;

            foreach (Transform bone in visual.GetComponentsInChildren<Transform>(true))
            {
                if (!IsFootBone(bone.name)) continue;
                minY = Mathf.Min(minY, bone.position.y);
            }

            return minY;
        }

        private static bool IsFootBone(string boneName)
        {
            if (string.IsNullOrEmpty(boneName)) return false;

            return boneName is "Left_Foot" or "Right_Foot"
                or "Left_Toes" or "Right_Toes"
                or "mixamorig:LeftFoot" or "mixamorig:RightFoot"
                or "mixamorig:LeftToeBase" or "mixamorig:RightToeBase";
        }
    }
}
