using System.Collections.Generic;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Spartan ships with mixamorig skin weights while the Humanoid Animator drives
    /// the mapped skeleton (Hips, Left_UpperLeg, …). Rebinds SkinnedMeshRenderer
    /// bones to the animated hierarchy so locomotion clips deform the mesh.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class CharacterRigBinder : MonoBehaviour
    {
        private static readonly Dictionary<string, string> SourceToHumanoid = new()
        {
            // Mixamo
            { "mixamorig:Hips", "Hips" },
            { "mixamorig:Spine", "Spine" },
            { "mixamorig:Spine1", "Chest" },
            { "mixamorig:Spine2", "UpperChest" },
            { "mixamorig:Neck", "Neck" },
            { "mixamorig:Head", "Head" },
            { "mixamorig:LeftShoulder", "Left_Shoulder" },
            { "mixamorig:LeftArm", "Left_UpperArm" },
            { "mixamorig:LeftForeArm", "Left_LowerArm" },
            { "mixamorig:LeftHand", "Left_Hand" },
            { "mixamorig:RightShoulder", "Right_Shoulder" },
            { "mixamorig:RightArm", "Right_UpperArm" },
            { "mixamorig:RightForeArm", "Right_LowerArm" },
            { "mixamorig:RightHand", "Right_Hand" },
            { "mixamorig:LeftUpLeg", "Left_UpperLeg" },
            { "mixamorig:LeftLeg", "Left_LowerLeg" },
            { "mixamorig:LeftFoot", "Left_Foot" },
            { "mixamorig:LeftToeBase", "Left_Toes" },
            { "mixamorig:RightUpLeg", "Right_UpperLeg" },
            { "mixamorig:RightLeg", "Right_LowerLeg" },
            { "mixamorig:RightFoot", "Right_Foot" },
            { "mixamorig:RightToeBase", "Right_Toes" },

            // Reallusion Character Creator (mecha enemy)
            { "CC_Base_Hip", "Hips" },
            { "CC_Base_Waist", "Spine" },
            { "CC_Base_Spine01", "Chest" },
            { "CC_Base_Spine02", "UpperChest" },
            { "CC_Base_NeckTwist01", "Neck" },
            { "CC_Base_Head", "Head" },
            { "CC_Base_L_Clavicle", "Left_Shoulder" },
            { "CC_Base_L_Upperarm", "Left_UpperArm" },
            { "CC_Base_L_Forearm", "Left_LowerArm" },
            { "CC_Base_L_Hand", "Left_Hand" },
            { "CC_Base_R_Clavicle", "Right_Shoulder" },
            { "CC_Base_R_Upperarm", "Right_UpperArm" },
            { "CC_Base_R_Forearm", "Right_LowerArm" },
            { "CC_Base_R_Hand", "Right_Hand" },
            { "CC_Base_L_Thigh", "Left_UpperLeg" },
            { "CC_Base_L_Calf", "Left_LowerLeg" },
            { "CC_Base_L_Foot", "Left_Foot" },
            { "CC_Base_L_ToeBase", "Left_Toes" },
            { "CC_Base_R_Thigh", "Right_UpperLeg" },
            { "CC_Base_R_Calf", "Right_LowerLeg" },
            { "CC_Base_R_Foot", "Right_Foot" },
            { "CC_Base_R_ToeBase", "Right_Toes" },
        };

        [SerializeField] private bool rebindOnAwake = true;

        private void Awake()
        {
            if (rebindOnAwake) RebindSkinnedMeshes();
        }

        public bool RebindSkinnedMeshes()
        {
            var boneLookup = BuildBoneLookup(transform);
            bool changed = false;

            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (TryRebind(smr, boneLookup))
                    changed = true;
            }

            return changed;
        }

        private static Dictionary<string, Transform> BuildBoneLookup(Transform root)
        {
            var lookup = new Dictionary<string, Transform>();
            foreach (Transform bone in root.GetComponentsInChildren<Transform>(true))
            {
                if (!lookup.ContainsKey(bone.name))
                    lookup.Add(bone.name, bone);
            }
            return lookup;
        }

        private static bool TryRebind(SkinnedMeshRenderer smr, Dictionary<string, Transform> boneLookup)
        {
            if (smr == null || smr.bones == null || smr.bones.Length == 0)
                return false;

            bool needsRebind = smr.rootBone != null && NeedsRigRebind(smr.rootBone.name);
            if (!needsRebind)
            {
                foreach (Transform bone in smr.bones)
                {
                    if (bone != null && NeedsRigRebind(bone.name))
                    {
                        needsRebind = true;
                        break;
                    }
                }
            }

            if (!needsRebind) return false;

            Transform[] newBones = new Transform[smr.bones.Length];
            bool anyMapped = false;

            for (int i = 0; i < smr.bones.Length; i++)
            {
                Transform source = smr.bones[i];
                if (source == null) continue;

                string sourceName = source.name;
                if (SourceToHumanoid.TryGetValue(sourceName, out string mappedName) &&
                    boneLookup.TryGetValue(mappedName, out Transform mapped))
                {
                    newBones[i] = mapped;
                    anyMapped = true;
                }
                else if (boneLookup.TryGetValue(sourceName, out Transform sameName))
                {
                    newBones[i] = sameName;
                }
                else
                {
                    newBones[i] = source;
                }
            }

            if (!anyMapped) return false;

            smr.bones = newBones;

            if (smr.rootBone != null && SourceToHumanoid.TryGetValue(smr.rootBone.name, out string rootMapped) &&
                boneLookup.TryGetValue(rootMapped, out Transform newRoot))
            {
                smr.rootBone = newRoot;
            }

            return true;
        }

        private static bool NeedsRigRebind(string boneName) =>
            boneName.StartsWith("mixamorig:") || boneName.StartsWith("CC_Base_");
    }
}
