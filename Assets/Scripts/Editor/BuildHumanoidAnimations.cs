using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Builds a locomotion <see cref="AnimatorController"/> using the project's
    /// real humanoid animation clips (Idle / Walk / Run / Jump) from
    /// <c>Assets/Core/Art/Animations/Custom/</c>.
    /// </summary>
    public static class BuildHumanoidAnimations
    {
        public const string OutputFolder   = "Assets/Animations/Locomotion";
        public const string ControllerPath = OutputFolder + "/PlayerLocomotion.controller";

        private const string IdleFbxPath      = "Assets/Core/Art/Animations/Custom/Idle.fbx";
        private const string WalkFbxPath      = "Assets/Core/Art/Animations/Custom/Walk.fbx";
        private const string RunFbxPath       = "Assets/Core/Art/Animations/Custom/Run_Fwd.fbx";
        private const string JumpFbxPath      = "Assets/Core/Art/Animations/Custom/Jump.fbx";
        private const string JumpInAirFbxPath = "Assets/Core/Art/Animations/Custom/Jump_InAir.fbx";
        private const string JumpLandFbxPath  = "Assets/Core/Art/Animations/Custom/Armature_Jump_Land.fbx";

        [MenuItem("Tools/Game/Build Humanoid Locomotion")]
        public static AnimatorController BuildMenu() => Build();

        public static AnimatorController EnsureLocomotionAssets(bool forceRebuild = false)
        {
            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (!forceRebuild && existing != null && HasJumpCycle(existing))
                return existing;

            return Build();
        }

        public static AnimatorController Build()
        {
            EnsureFolder(OutputFolder);

            AnimationClip idle      = LoadClip(IdleFbxPath, "Idle");
            AnimationClip walk      = LoadClip(WalkFbxPath, "Walk");
            AnimationClip run       = LoadClip(RunFbxPath,  "Run_Fwd");
            AnimationClip jumpStart = LoadClip(JumpFbxPath, "JumpStart") ?? LoadClip(JumpFbxPath, "Jump");
            AnimationClip jumpInAir = LoadClip(JumpInAirFbxPath, "Jump_InAir");
            AnimationClip jumpLand  = LoadClip(JumpLandFbxPath, "Armature_Jump_Land")
                                 ?? LoadClip(JumpLandFbxPath, "Take 001");

            if (idle == null || walk == null || run == null || jumpStart == null || jumpInAir == null || jumpLand == null)
            {
                Debug.LogError("[BuildHumanoidAnimations] Missing Core locomotion clips. " +
                               "Expected Idle, Walk, Run_Fwd, Jump/JumpStart, Jump_InAir, Armature_Jump_Land.");
                return null;
            }

            EnsureClipLoops(jumpInAir, loop: true);

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
                AssetDatabase.DeleteAsset(ControllerPath);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed",    AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Jump",     AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Land",     AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine root = controller.layers[0].stateMachine;

            BlendTree blendTree = new BlendTree
            {
                name                   = "Locomotion",
                blendType              = BlendTreeType.Simple1D,
                blendParameter         = "Speed",
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(blendTree, controller);
            blendTree.AddChild(idle, 0f);
            blendTree.AddChild(walk, 0.5f);
            blendTree.AddChild(run,  1f);

            AnimatorState locomotionState = root.AddState("Locomotion", new Vector3(300f, 0f, 0f));
            locomotionState.motion    = blendTree;
            locomotionState.iKOnFeet  = true;
            root.defaultState         = locomotionState;

            BuildJumpStates(root, locomotionState, jumpStart, jumpInAir, jumpLand);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            BuildCombatAnimations.EnsureCombatAssets();

            Debug.Log($"[BuildHumanoidAnimations] Built locomotion + jump cycle at {ControllerPath}");
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        }

        private static void BuildJumpStates(
            AnimatorStateMachine root,
            AnimatorState locomotionState,
            AnimationClip jumpStart,
            AnimationClip jumpInAir,
            AnimationClip jumpLand)
        {
            AnimatorState jumpStartState = root.AddState("JumpStart", new Vector3(300f, 120f, 0f));
            jumpStartState.motion   = jumpStart;
            jumpStartState.iKOnFeet = true;

            AnimatorState jumpInAirState = root.AddState("JumpInAir", new Vector3(300f, 220f, 0f));
            jumpInAirState.motion = jumpInAir;

            AnimatorState jumpLandState = root.AddState("JumpLand", new Vector3(300f, 320f, 0f));
            jumpLandState.motion   = jumpLand;
            jumpLandState.iKOnFeet = true;

            AnimatorStateTransition anyToJumpStart = root.AddAnyStateTransition(jumpStartState);
            anyToJumpStart.AddCondition(AnimatorConditionMode.If, 0f, "Jump");
            anyToJumpStart.duration            = 0.05f;
            anyToJumpStart.hasExitTime         = false;
            anyToJumpStart.canTransitionToSelf = false;

            AnimatorStateTransition startToAir = jumpStartState.AddTransition(jumpInAirState);
            startToAir.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
            startToAir.duration    = 0.08f;
            startToAir.hasExitTime = true;
            startToAir.exitTime    = 0.65f;

            AnimatorStateTransition airToLandGrounded = jumpInAirState.AddTransition(jumpLandState);
            airToLandGrounded.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
            airToLandGrounded.duration    = 0.1f;
            airToLandGrounded.hasExitTime = false;

            AnimatorStateTransition airToLandTrigger = jumpInAirState.AddTransition(jumpLandState);
            airToLandTrigger.AddCondition(AnimatorConditionMode.If, 0f, "Land");
            airToLandTrigger.duration    = 0.05f;
            airToLandTrigger.hasExitTime = false;

            AnimatorStateTransition landToLocomotion = jumpLandState.AddTransition(locomotionState);
            landToLocomotion.duration    = 0.12f;
            landToLocomotion.hasExitTime = true;
            landToLocomotion.exitTime    = 0.88f;
        }

        private static bool HasJumpCycle(AnimatorController controller)
        {
            if (controller == null || controller.layers.Length == 0)
                return false;

            ChildAnimatorState[] states = controller.layers[0].stateMachine.states;
            bool hasInAir = false;
            bool hasLand  = false;

            foreach (ChildAnimatorState child in states)
            {
                if (child.state.name == "JumpInAir") hasInAir = true;
                if (child.state.name == "JumpLand")  hasLand  = true;
            }

            return hasInAir && hasLand && HasParameter(controller, "Land");
        }

        private static bool HasParameter(AnimatorController controller, string name)
        {
            foreach (AnimatorControllerParameter parameter in controller.parameters)
            {
                if (parameter.name == name)
                    return true;
            }

            return false;
        }

        private static AnimationClip LoadClip(string fbxPath, string preferredName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            AnimationClip fallback = null;

            foreach (Object asset in assets)
            {
                if (asset is not AnimationClip clip) continue;
                if (clip.name.StartsWith("__")) continue;

                if (clip.name == preferredName) return clip;
                fallback ??= clip;
            }

            if (fallback != null)
                Debug.LogWarning($"[BuildHumanoidAnimations] Clip '{preferredName}' not found in {fbxPath}; using '{fallback.name}'.");

            return fallback;
        }

        private static void EnsureClipLoops(AnimationClip clip, bool loop)
        {
            if (clip == null) return;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            string leaf   = Path.GetFileName(assetPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(leaf))
                AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
