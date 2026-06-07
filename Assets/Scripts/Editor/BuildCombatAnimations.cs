using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Builds third-person combat upper-body animation clips, avatar mask, and
    /// extends <see cref="BuildHumanoidAnimations.ControllerPath"/> with a Combat layer.
    /// </summary>
    public static class BuildCombatAnimations
    {
        public const string OutputFolder = "Assets/Animations/Combat";
        public const string AimClipPath = OutputFolder + "/TP_Rifle_Aim.anim";
        public const string FireClipPath = OutputFolder + "/TP_Rifle_Fire.anim";
        public const string ReloadClipPath = OutputFolder + "/TP_Rifle_Reload.anim";
        public const string MaskPath = OutputFolder + "/UpperBody.mask";
        public const string PistolControllerPath = OutputFolder + "/PistolCombat.controller";

        private const string AlterunaFirePath =
            "Assets/AlterunaFPS/Animations/Pistol/Pistol Fire.anim";
        private const string AlterunaReloadPath =
            "Assets/AlterunaFPS/Animations/Pistol/Pistol Reload.anim";
        private const string AlterunaIdlePath =
            "Assets/AlterunaFPS/Animations/Pistol/Pistol Idle.anim";

        private const float FireDuration = 0.25f;

        // Approximate humanoid muscle values for a rifle aim pose.
        private const float AimRightArmFrontBack = -0.55f;
        private const float AimLeftArmFrontBack = -0.35f;
        private const float AimRightArmDownUp = 0.15f;
        private const float AimLeftArmDownUp = 0.10f;
        private const float AimLeftArmInOut = 0.08f;
        private const float AimChestFrontBack = 0.02f;
        private const float AimSpineFrontBack = 0.01f;

        [MenuItem("Tools/Game/Build Combat Animations")]
        public static void BuildMenu() => Build();

        /// <summary>Builds combat assets if clips or the pistol controller are missing.</summary>
        public static void EnsureCombatAssets()
        {
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(AimClipPath) == null
                || AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath) == null)
            {
                Build();
                return;
            }

            AnimatorController locomotion = BuildHumanoidAnimations.EnsureLocomotionAssets();
            if (locomotion != null && FindLayerIndex(locomotion, "Combat") < 0)
                Build();
        }

        /// <summary>Animator controller for the pistol mesh (fire / reload weapon transforms).</summary>
        public static RuntimeAnimatorController EnsurePistolController()
        {
            RuntimeAnimatorController existing =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PistolControllerPath);
            if (existing != null) return existing;

            EnsureFolder(OutputFolder);

            AnimationClip idleClip   = AssetDatabase.LoadAssetAtPath<AnimationClip>(AlterunaIdlePath);
            AnimationClip fireClip   = AssetDatabase.LoadAssetAtPath<AnimationClip>(AlterunaFirePath);
            AnimationClip reloadClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AlterunaReloadPath);

            if (fireClip == null)
            {
                Debug.LogWarning("[BuildCombatAnimations] Pistol Fire clip missing; pistol mesh will not animate.");
                return null;
            }

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(PistolControllerPath) != null)
                AssetDatabase.DeleteAsset(PistolControllerPath);

            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(PistolControllerPath);
            controller.AddParameter("Fire",   AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Reload", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine root = controller.layers[0].stateMachine;

            AnimatorState idleState = root.AddState("Idle", new Vector3(300f, 0f, 0f));
            idleState.motion = idleClip != null ? idleClip : fireClip;
            root.defaultState = idleState;

            AnimatorState fireState = root.AddState("Fire", new Vector3(300f, 100f, 0f));
            fireState.motion = fireClip;

            AnimatorState reloadState = root.AddState("Reload", new Vector3(300f, 200f, 0f));
            reloadState.motion = reloadClip != null ? reloadClip : fireClip;

            AnimatorStateTransition anyToFire = root.AddAnyStateTransition(fireState);
            anyToFire.AddCondition(AnimatorConditionMode.If, 0f, "Fire");
            anyToFire.duration = 0.02f;
            anyToFire.hasExitTime = false;
            anyToFire.canTransitionToSelf = true;

            AnimatorStateTransition fireToIdle = fireState.AddTransition(idleState);
            fireToIdle.duration = 0.08f;
            fireToIdle.hasExitTime = true;
            fireToIdle.exitTime = 0.9f;

            if (reloadClip != null)
            {
                AnimatorStateTransition anyToReload = root.AddAnyStateTransition(reloadState);
                anyToReload.AddCondition(AnimatorConditionMode.If, 0f, "Reload");
                anyToReload.duration = 0.05f;
                anyToReload.hasExitTime = false;

                AnimatorStateTransition reloadToIdle = reloadState.AddTransition(idleState);
                reloadToIdle.duration = 0.1f;
                reloadToIdle.hasExitTime = true;
                reloadToIdle.exitTime = 0.92f;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        public static void Build()
        {
            EnsureFolder(OutputFolder);

            AnimationClip aimClip = CreateOrReplaceClip(AimClipPath, BuildAimClip);
            AnimationClip fireClip = CreateOrReplaceClip(FireClipPath, BuildFireClip);
            AnimationClip reloadClip = ResolveReloadClip();

            AvatarMask upperBodyMask = CreateOrReplaceUpperBodyMask();

            AnimatorController controller = BuildHumanoidAnimations.EnsureLocomotionAssets();
            if (controller == null)
            {
                Debug.LogError("[BuildCombatAnimations] Locomotion controller missing; run Build Humanoid Locomotion first.");
                return;
            }

            EnsureCombatParameters(controller);
            BuildCombatLayer(controller, upperBodyMask, aimClip, fireClip, reloadClip);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[BuildCombatAnimations] Combat clips, UpperBody mask, and Combat layer updated.");
        }

        private static AnimationClip BuildAimClip()
        {
            AnimationClip clip = NewHumanoidClip("TP_Rifle_Aim", 1f / 60f);
            ApplyAimPose(clip, 0f, 1f / 60f);
            return clip;
        }

        private static AnimationClip BuildFireClip()
        {
            AnimationClip clip = NewHumanoidClip("TP_Rifle_Fire", FireDuration);

            float recoilTime = 0.05f;
            float endTime = FireDuration;

            SetMuscleCurve(clip, "Right Arm Front-Back", 0f, AimRightArmFrontBack, recoilTime, AimRightArmFrontBack + 0.08f, endTime, AimRightArmFrontBack);
            SetMuscleCurve(clip, "Right Arm Down-Up", 0f, AimRightArmDownUp, recoilTime, AimRightArmDownUp - 0.03f, endTime, AimRightArmDownUp);
            SetMuscleCurve(clip, "Left Arm Front-Back", 0f, AimLeftArmFrontBack, endTime, AimLeftArmFrontBack);
            SetMuscleCurve(clip, "Left Arm Down-Up", 0f, AimLeftArmDownUp, endTime, AimLeftArmDownUp);
            SetMuscleCurve(clip, "Left Arm In-Out", 0f, AimLeftArmInOut, endTime, AimLeftArmInOut);
            SetMuscleCurve(clip, "Chest Front-Back", 0f, AimChestFrontBack, recoilTime, AimChestFrontBack + 0.035f, endTime, AimChestFrontBack);
            SetMuscleCurve(clip, "Spine Front-Back", 0f, AimSpineFrontBack, recoilTime, AimSpineFrontBack + 0.03f, endTime, AimSpineFrontBack);

            return clip;
        }

        private static AnimationClip BuildReloadPlaceholderClip()
        {
            const float duration = 1.2f;
            AnimationClip clip = NewHumanoidClip("TP_Rifle_Reload", duration);

            float mid = duration * 0.45f;
            float end = duration;

            SetMuscleCurve(clip, "Right Arm Front-Back", 0f, AimRightArmFrontBack, end, AimRightArmFrontBack);
            SetMuscleCurve(clip, "Right Arm Down-Up", 0f, AimRightArmDownUp, end, AimRightArmDownUp);
            SetMuscleCurve(clip, "Left Arm Front-Back", 0f, AimLeftArmFrontBack, mid, -0.05f, end, AimLeftArmFrontBack);
            SetMuscleCurve(clip, "Left Arm Down-Up", 0f, AimLeftArmDownUp, mid, -0.12f, end, AimLeftArmDownUp);
            SetMuscleCurve(clip, "Left Arm In-Out", 0f, AimLeftArmInOut, mid, 0.22f, end, AimLeftArmInOut);
            SetMuscleCurve(clip, "Chest Front-Back", 0f, AimChestFrontBack, end, AimChestFrontBack);
            SetMuscleCurve(clip, "Spine Front-Back", 0f, AimSpineFrontBack, end, AimSpineFrontBack);

            return clip;
        }

        private static AnimationClip ResolveReloadClip()
        {
            AnimationClip alterunaReload = AssetDatabase.LoadAssetAtPath<AnimationClip>(AlterunaReloadPath);
            if (IsHumanoidMuscleClip(alterunaReload))
            {
                Debug.Log("[BuildCombatAnimations] Using humanoid-compatible Alteruna Pistol Reload clip.");
                return alterunaReload;
            }

            if (alterunaReload != null)
                Debug.LogWarning("[BuildCombatAnimations] Alteruna Pistol Reload is transform-based, not humanoid muscle. " +
                                 "Generating TP_Rifle_Reload placeholder.");

            return CreateOrReplaceClip(ReloadClipPath, BuildReloadPlaceholderClip);
        }

        private static void ApplyAimPose(AnimationClip clip, float startTime, float endTime)
        {
            SetMuscleCurve(clip, "Right Arm Front-Back", startTime, AimRightArmFrontBack, endTime, AimRightArmFrontBack);
            SetMuscleCurve(clip, "Left Arm Front-Back", startTime, AimLeftArmFrontBack, endTime, AimLeftArmFrontBack);
            SetMuscleCurve(clip, "Right Arm Down-Up", startTime, AimRightArmDownUp, endTime, AimRightArmDownUp);
            SetMuscleCurve(clip, "Left Arm Down-Up", startTime, AimLeftArmDownUp, endTime, AimLeftArmDownUp);
            SetMuscleCurve(clip, "Left Arm In-Out", startTime, AimLeftArmInOut, endTime, AimLeftArmInOut);
            SetMuscleCurve(clip, "Chest Front-Back", startTime, AimChestFrontBack, endTime, AimChestFrontBack);
            SetMuscleCurve(clip, "Spine Front-Back", startTime, AimSpineFrontBack, endTime, AimSpineFrontBack);
        }

        private static AnimationClip NewHumanoidClip(string clipName, float length)
        {
            AnimationClip clip = new AnimationClip
            {
                name = clipName,
                frameRate = 60f
            };

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            settings.stopTime = length;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            return clip;
        }

        private static void SetMuscleCurve(
            AnimationClip clip,
            string muscleName,
            float t0, float v0,
            float t1, float v1)
        {
            clip.SetCurve(string.Empty, typeof(Animator), muscleName, AnimationCurve.Linear(t0, v0, t1, v1));
        }

        private static void SetMuscleCurve(
            AnimationClip clip,
            string muscleName,
            float t0, float v0,
            float tMid, float vMid,
            float t1, float v1)
        {
            AnimationCurve curve = new AnimationCurve(
                new Keyframe(t0, v0),
                new Keyframe(tMid, vMid),
                new Keyframe(t1, v1));

            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            }

            clip.SetCurve(string.Empty, typeof(Animator), muscleName, curve);
        }

        private static AvatarMask CreateOrReplaceUpperBodyMask()
        {
            AvatarMask existing = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);
            if (existing != null)
                AssetDatabase.DeleteAsset(MaskPath);

            AvatarMask mask = new AvatarMask { name = "UpperBody" };
            ConfigureUpperBodyMask(mask);
            AssetDatabase.CreateAsset(mask, MaskPath);
            return mask;
        }

        private static void ConfigureUpperBodyMask(AvatarMask mask)
        {
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Root, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);

            // Unity 6 humanoid mask: feet/toes are included in the leg body parts.
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg, false);
        }

        private static void EnsureCombatParameters(AnimatorController controller)
        {
            EnsureParameter(controller, "HasWeapon", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Aiming", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Fire", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Reload", AnimatorControllerParameterType.Trigger);
        }

        private static void EnsureParameter(
            AnimatorController controller,
            string name,
            AnimatorControllerParameterType type)
        {
            foreach (AnimatorControllerParameter parameter in controller.parameters)
            {
                if (parameter.name == name)
                    return;
            }

            controller.AddParameter(name, type);
        }

        private static void BuildCombatLayer(
            AnimatorController controller,
            AvatarMask mask,
            AnimationClip aimClip,
            AnimationClip fireClip,
            AnimationClip reloadClip)
        {
            int layerIndex = FindLayerIndex(controller, "Combat");
            if (layerIndex < 0)
            {
                controller.AddLayer("Combat");
                layerIndex = controller.layers.Length - 1;
            }
            else
            {
                AnimatorStateMachine oldMachine = controller.layers[layerIndex].stateMachine;
                if (oldMachine != null)
                    Object.DestroyImmediate(oldMachine, true);
            }

            AnimatorStateMachine stateMachine = new AnimatorStateMachine
            {
                name = "Combat",
                hideFlags = HideFlags.HideInHierarchy
            };
            AssetDatabase.AddObjectToAsset(stateMachine, controller);

            AnimatorControllerLayer layer = controller.layers[layerIndex];
            layer.stateMachine = stateMachine;
            layer.defaultWeight = 1f;
            layer.avatarMask = mask;
            layer.blendingMode = AnimatorLayerBlendingMode.Override;
            controller.layers[layerIndex] = layer;

            AnimatorState empty = stateMachine.AddState("Empty", new Vector3(250f, 0f, 0f));
            empty.writeDefaultValues = false;
            stateMachine.defaultState = empty;

            AnimatorState aim = stateMachine.AddState("Aim", new Vector3(250f, 100f, 0f));
            aim.motion = aimClip;
            aim.writeDefaultValues = false;

            AnimatorState fire = stateMachine.AddState("Fire", new Vector3(500f, 100f, 0f));
            fire.motion = fireClip;
            fire.writeDefaultValues = false;

            AnimatorState reload = stateMachine.AddState("Reload", new Vector3(500f, 200f, 0f));
            reload.motion = reloadClip;
            reload.writeDefaultValues = false;

            AnimatorStateTransition emptyToAim = empty.AddTransition(aim);
            emptyToAim.AddCondition(AnimatorConditionMode.If, 0f, "HasWeapon");
            emptyToAim.AddCondition(AnimatorConditionMode.If, 0f, "Aiming");
            emptyToAim.duration = 0.15f;
            emptyToAim.hasExitTime = false;

            AnimatorStateTransition aimToEmptyNoWeapon = aim.AddTransition(empty);
            aimToEmptyNoWeapon.AddCondition(AnimatorConditionMode.IfNot, 0f, "HasWeapon");
            aimToEmptyNoWeapon.duration = 0.15f;
            aimToEmptyNoWeapon.hasExitTime = false;

            AnimatorStateTransition aimToEmptyNotAiming = aim.AddTransition(empty);
            aimToEmptyNotAiming.AddCondition(AnimatorConditionMode.IfNot, 0f, "Aiming");
            aimToEmptyNotAiming.duration = 0.15f;
            aimToEmptyNotAiming.hasExitTime = false;

            AnimatorStateTransition aimToFire = aim.AddTransition(fire);
            aimToFire.AddCondition(AnimatorConditionMode.If, 0f, "Fire");
            aimToFire.duration = 0.05f;
            aimToFire.hasExitTime = false;

            AnimatorStateTransition fireToAim = fire.AddTransition(aim);
            fireToAim.duration = 0.08f;
            fireToAim.hasExitTime = true;
            fireToAim.exitTime = 0.95f;

            AnimatorStateTransition aimToReload = aim.AddTransition(reload);
            aimToReload.AddCondition(AnimatorConditionMode.If, 0f, "Reload");
            aimToReload.duration = 0.08f;
            aimToReload.hasExitTime = false;

            AnimatorStateTransition reloadToAim = reload.AddTransition(aim);
            reloadToAim.duration = 0.12f;
            reloadToAim.hasExitTime = true;
            reloadToAim.exitTime = 0.92f;
        }

        private static int FindLayerIndex(AnimatorController controller, string layerName)
        {
            for (int i = 0; i < controller.layers.Length; i++)
            {
                if (controller.layers[i].name == layerName)
                    return i;
            }

            return -1;
        }

        private static bool IsHumanoidMuscleClip(AnimationClip clip)
        {
            if (clip == null)
                return false;

            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.type == typeof(Animator) && string.IsNullOrEmpty(binding.path))
                    return true;
            }

            return false;
        }

        private static AnimationClip CreateOrReplaceClip(string assetPath, System.Func<AnimationClip> builder)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);

            AnimationClip clip = builder();
            AssetDatabase.CreateAsset(clip, assetPath);
            return clip;
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return;

            string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            string leaf = Path.GetFileName(assetPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(leaf))
                AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
