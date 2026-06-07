using System.IO;
using System.Linq;
using Game.Player;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Imports Spartan as Humanoid. Uses mixamorig bone mapping; falls back to Core Armature Avatar.
    /// </summary>
    public static class ImportSpartanCharacter
    {
        public const string SpartanFbxPath    = "Assets/Art/Characters/Spartan/Spartan_Sketchfab.fbx";
        public const string SourceAvatarFbx = "Assets/Core/Art/Models/Armature.fbx";

        [MenuItem("Tools/Game/Import Spartan Character")]
        public static GameObject ImportMenu()
        {
            Avatar avatar = EnsureHumanoidAvatar(SpartanFbxPath, forceReimport: true);
            if (avatar != null)
                Debug.Log($"[ImportSpartanCharacter] Spartan ready (avatar: {avatar.name}).");
            return AssetDatabase.LoadAssetAtPath<GameObject>(SpartanFbxPath);
        }

        public static GameObject EnsureImported(bool forceReimport = false)
        {
            if (!AssetExists(SpartanFbxPath))
            {
                Debug.LogError($"[ImportSpartanCharacter] Missing {SpartanFbxPath}.");
                return null;
            }

            EnsureHumanoidAvatar(SpartanFbxPath, forceReimport);
            return AssetDatabase.LoadAssetAtPath<GameObject>(SpartanFbxPath);
        }

        public static Avatar EnsureHumanoidAvatar(string fbxPath = SpartanFbxPath, bool forceReimport = false)
        {
            if (!AssetExists(fbxPath))
            {
                Debug.LogError($"[ImportSpartanCharacter] Missing {fbxPath}.");
                return null;
            }

            if (!forceReimport)
            {
                Avatar existing = LoadAvatar(fbxPath);
                if (existing != null) return existing;
            }

            if (fbxPath == SpartanFbxPath)
                ReimportSpartan();

            RefreshAssets();

            Avatar avatar = LoadAvatar(fbxPath);
            if (avatar != null) return avatar;

            if (fbxPath == SpartanFbxPath)
                return LoadAvatar(SourceAvatarFbx);

            Debug.LogError($"[ImportSpartanCharacter] No Humanoid Avatar available for {fbxPath}.");
            return null;
        }

        public static Avatar GetHumanoidAvatar() =>
            EnsureHumanoidAvatar(SpartanFbxPath, forceReimport: false);

        [MenuItem("Tools/Game/Fix Character Locomotion")]
        public static void FixCharacterLocomotionMenu()
        {
            BuildHumanoidAnimations.EnsureLocomotionAssets(forceRebuild: true);
            BuildCombatAnimations.EnsureCombatAssets();

            Avatar avatar = EnsureHumanoidAvatar(SpartanFbxPath, forceReimport: false);
            if (avatar == null) return;

            Animator[] animators = Selection.gameObjects.Length > 0
                ? Selection.gameObjects
                    .SelectMany(go => go.GetComponentsInChildren<Animator>(true))
                    .ToArray()
                : UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsInactive.Include);

            int fixedCount = 0;
            foreach (Animator animator in animators)
            {
                if (FixAnimator(animator, avatar))
                    fixedCount++;
            }

            Debug.Log($"[ImportSpartanCharacter] Fixed locomotion on {fixedCount} Animator(s).");
        }

        public static bool AssignHumanoidAvatar(Animator animator, Avatar avatar)
        {
            if (animator == null || avatar == null) return false;
            return FixAnimator(animator, avatar);
        }

        private static void ReimportSpartan()
        {
            var importer = AssetImporter.GetAtPath(SpartanFbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError("[ImportSpartanCharacter] Spartan has no ModelImporter.");
                return;
            }

            ApplyBaseHumanoidSettings(importer);
            importer.avatarSetup  = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.sourceAvatar = null;

            // Map mixamorig bones from the FBX — do NOT copy Armature's skeleton block.
            HumanDescription desc = importer.humanDescription;
            desc.human = BuildMixamoHumanBones();
            importer.humanDescription = desc;

            importer.SaveAndReimport();
            RefreshAssets();
        }

        private static HumanBone[] BuildMixamoHumanBones()
        {
            return new[]
            {
                Map("mixamorig:Hips",         "Hips"),
                Map("mixamorig:Spine",        "Spine"),
                Map("mixamorig:Spine1",       "Chest"),
                Map("mixamorig:Spine2",       "UpperChest"),
                Map("mixamorig:Neck",         "Neck"),
                Map("mixamorig:Head",         "Head"),
                Map("mixamorig:LeftShoulder",  "LeftShoulder"),
                Map("mixamorig:LeftArm",       "LeftUpperArm"),
                Map("mixamorig:LeftForeArm",   "LeftLowerArm"),
                Map("mixamorig:LeftHand",      "LeftHand"),
                Map("mixamorig:RightShoulder", "RightShoulder"),
                Map("mixamorig:RightArm",      "RightUpperArm"),
                Map("mixamorig:RightForeArm",  "RightLowerArm"),
                Map("mixamorig:RightHand",     "RightHand"),
                Map("mixamorig:LeftUpLeg",     "LeftUpperLeg"),
                Map("mixamorig:LeftLeg",       "LeftLowerLeg"),
                Map("mixamorig:LeftFoot",      "LeftFoot"),
                Map("mixamorig:LeftToeBase",   "LeftToes"),
                Map("mixamorig:RightUpLeg",    "RightUpperLeg"),
                Map("mixamorig:RightLeg",      "RightLowerLeg"),
                Map("mixamorig:RightFoot",     "RightFoot"),
                Map("mixamorig:RightToeBase",  "RightToes"),
            };
        }

        private static HumanBone Map(string boneName, string humanName) =>
            new HumanBone
            {
                boneName  = boneName,
                humanName = humanName,
                limit     = new HumanLimit { useDefaultValues = true }
            };

        private static void ApplyBaseHumanoidSettings(ModelImporter importer)
        {
            importer.animationType      = ModelImporterAnimationType.Human;
            importer.bakeAxisConversion = true;
            importer.importAnimation    = false;
            importer.optimizeBones      = false;
        }

        private static void RefreshAssets()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        private static bool AssetExists(string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
                return true;

            string fullPath = Path.Combine(
                Path.GetDirectoryName(Application.dataPath) ?? string.Empty,
                assetPath.Replace('/', Path.DirectorySeparatorChar));

            return File.Exists(fullPath);
        }

        private static bool FixAnimator(Animator animator, Avatar avatar)
        {
            var so = new SerializedObject(animator);
            so.FindProperty("m_Avatar").objectReferenceValue = avatar;
            so.ApplyModifiedPropertiesWithoutUndo();

            animator.runtimeAnimatorController =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    BuildHumanoidAnimations.ControllerPath);

            var setup = animator.GetComponent<CharacterAnimatorSetup>();
            if (setup == null)
                setup = animator.gameObject.AddComponent<CharacterAnimatorSetup>();

            var setupSo = new SerializedObject(setup);
            setupSo.FindProperty("humanoidAvatar").objectReferenceValue = avatar;
            setupSo.ApplyModifiedPropertiesWithoutUndo();

            var binder = animator.GetComponent<CharacterRigBinder>();
            if (binder == null)
                binder = animator.gameObject.AddComponent<CharacterRigBinder>();

            if (Application.isPlaying)
                binder.RebindSkinnedMeshes();
            else
                RebindSkinnedMeshesInEditor(animator.gameObject);

            EditorUtility.SetDirty(animator);
            return true;
        }

        private static void RebindSkinnedMeshesInEditor(GameObject root)
        {
            var binder = root.GetComponent<CharacterRigBinder>();
            if (binder == null)
                binder = root.AddComponent<CharacterRigBinder>();

            if (binder.RebindSkinnedMeshes())
                EditorUtility.SetDirty(root);
        }

        private static Avatar LoadAvatar(string fbxPath)
        {
            Avatar best = null;

            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            {
                if (asset is not Avatar avatar || !avatar.isHuman) continue;
                if (avatar.isValid) return avatar;
                best ??= avatar;
            }

            return best;
        }
    }
}
