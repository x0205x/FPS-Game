using System.IO;
using Game.AI;
using Game.Player;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Imports the Mecha BOT XT-3478 enemy FBX as Humanoid and wires the same
    /// locomotion avatar pipeline used by the player Spartan.
    /// </summary>
    public static class ImportMechaEnemyCharacter
    {
        public const string MechaFbxPath = "Assets/Art/Characters/Enemies/MechaBot_XT-3478.fbx";
        public const string MechaFolder  = "Assets/Art/Characters/Enemies";

        public const string DefaultSourceFbx =
            @"c:\Users\judah\Desktop\-\Annex\Relax Time\PC Related Stuff\Games\[NEW] ACTIVE\Built Games\Creation Kit\[Models]\[Models]Character Files\mecha-character-xt-3478-cyber-by-oscar-creativo\source\MECHA BOT XT-3478.fbx";

        [MenuItem("Tools/Game/Import Mecha Enemy Character")]
        public static GameObject ImportMenu()
        {
            Avatar avatar = EnsureHumanoidAvatar(forceReimport: true);
            if (avatar != null)
                Debug.Log($"[ImportMechaEnemyCharacter] Mecha ready (avatar: {avatar.name}).");
            return AssetDatabase.LoadAssetAtPath<GameObject>(MechaFbxPath);
        }

        public static GameObject EnsureImported(bool forceReimport = false)
        {
            EnsureFolder(MechaFolder);
            CopyFromSourceIfMissing();

            if (!AssetExists(MechaFbxPath))
            {
                Debug.LogError($"[ImportMechaEnemyCharacter] Missing {MechaFbxPath}. " +
                               "Run Tools → Game → Import Mecha Enemy Character after placing the FBX.");
                return null;
            }

            EnsureHumanoidAvatar(forceReimport);
            return AssetDatabase.LoadAssetAtPath<GameObject>(MechaFbxPath);
        }

        public static Avatar EnsureHumanoidAvatar(bool forceReimport = false)
        {
            if (!AssetExists(MechaFbxPath))
                return null;

            if (!forceReimport)
            {
                Avatar existing = LoadAvatar(MechaFbxPath);
                if (existing != null && existing.isValid)
                    return existing;
            }

            ReimportMecha();
            RefreshAssets();

            Avatar avatar = LoadAvatar(MechaFbxPath);
            if (avatar != null && avatar.isValid)
                return avatar;

            avatar = LoadAvatar(ImportSpartanCharacter.SourceAvatarFbx);
            if (avatar != null)
                Debug.LogWarning("[ImportMechaEnemyCharacter] CC_Base mapping failed; using Armature avatar fallback.");

            return avatar;
        }

        [MenuItem("Tools/Game/Fix Enemy Locomotion")]
        public static void FixEnemyLocomotionMenu()
        {
            Avatar avatar = EnsureHumanoidAvatar(forceReimport: false);
            if (avatar == null) return;

            BuildHumanoidAnimations.EnsureLocomotionAssets(forceRebuild: false);
            var locomotion = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                BuildHumanoidAnimations.ControllerPath);

            int fixedCount = 0;
            foreach (EnemyController enemy in UnityEngine.Object.FindObjectsByType<EnemyController>())
            {
                Transform character = enemy.transform.Find("Character");
                if (character == null) continue;

                Animator animator = character.GetComponent<Animator>();
                if (animator == null) continue;

                AssignHumanoidAvatar(animator, avatar);
                animator.runtimeAnimatorController = locomotion;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                var setup = character.GetComponent<CharacterAnimatorSetup>();
                if (setup == null)
                    setup = character.gameObject.AddComponent<CharacterAnimatorSetup>();
                SetField(setup, "humanoidAvatar", avatar);

                var binder = character.GetComponent<CharacterRigBinder>();
                if (binder == null)
                    binder = character.gameObject.AddComponent<CharacterRigBinder>();
                binder.RebindSkinnedMeshes();

                fixedCount++;
            }

            Debug.Log($"[ImportMechaEnemyCharacter] Fixed locomotion on {fixedCount} enemy character(s).");
        }

        public static bool AssignHumanoidAvatar(Animator animator, Avatar avatar) =>
            ImportSpartanCharacter.AssignHumanoidAvatar(animator, avatar);

        private static void CopyFromSourceIfMissing()
        {
            if (AssetExists(MechaFbxPath))
                return;

            if (!File.Exists(DefaultSourceFbx))
            {
                Debug.LogWarning($"[ImportMechaEnemyCharacter] Source FBX not found at:\n{DefaultSourceFbx}");
                return;
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            string destPath = Path.Combine(projectRoot, MechaFbxPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(destPath) ?? projectRoot);
            File.Copy(DefaultSourceFbx, destPath, overwrite: true);
            CopyEmbeddedTextures(DefaultSourceFbx);
            RefreshAssets();
            Debug.Log($"[ImportMechaEnemyCharacter] Copied FBX to {MechaFbxPath}");
        }

        private static void CopyEmbeddedTextures(string sourceFbxPath)
        {
            string sourceDir = Path.GetDirectoryName(sourceFbxPath) ?? string.Empty;
            string fbmName = Path.GetFileNameWithoutExtension(sourceFbxPath) + ".fbm";
            string sourceFbm = Path.Combine(sourceDir, fbmName);
            if (!Directory.Exists(sourceFbm))
                return;

            string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            string destFbm = Path.Combine(
                projectRoot,
                MechaFolder.Replace('/', Path.DirectorySeparatorChar),
                fbmName);

            Directory.CreateDirectory(destFbm);
            foreach (string file in Directory.GetFiles(sourceFbm))
            {
                string destFile = Path.Combine(destFbm, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite: true);
            }

            Debug.Log($"[ImportMechaEnemyCharacter] Copied {Directory.GetFiles(destFbm).Length} texture(s) to {MechaFolder}/{fbmName}");
        }

        private static void SetField(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ReimportMecha()
        {
            var importer = AssetImporter.GetAtPath(MechaFbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError("[ImportMechaEnemyCharacter] Mecha FBX has no ModelImporter.");
                return;
            }

            ApplyBaseHumanoidSettings(importer);
            importer.avatarSetup  = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.sourceAvatar = null;
            importer.SaveAndReimport();
            RefreshAssets();

            Avatar auto = LoadAvatar(MechaFbxPath);
            if (auto != null && auto.isValid)
                return;

            importer = AssetImporter.GetAtPath(MechaFbxPath) as ModelImporter;
            if (importer == null) return;

            HumanDescription desc = importer.humanDescription;
            desc.human = BuildCcBaseHumanBones();
            importer.humanDescription = desc;
            importer.SaveAndReimport();
            RefreshAssets();
        }

        /// <summary>Reallusion Character Creator (CC_Base_*) skeleton used by the mecha FBX.</summary>
        private static HumanBone[] BuildCcBaseHumanBones()
        {
            return new[]
            {
                Map("CC_Base_Hip",            "Hips"),
                Map("CC_Base_Waist",          "Spine"),
                Map("CC_Base_Spine01",        "Chest"),
                Map("CC_Base_Spine02",        "UpperChest"),
                Map("CC_Base_NeckTwist01",    "Neck"),
                Map("CC_Base_Head",           "Head"),
                Map("CC_Base_L_Clavicle",     "LeftShoulder"),
                Map("CC_Base_L_Upperarm",     "LeftUpperArm"),
                Map("CC_Base_L_Forearm",      "LeftLowerArm"),
                Map("CC_Base_L_Hand",         "LeftHand"),
                Map("CC_Base_R_Clavicle",     "RightShoulder"),
                Map("CC_Base_R_Upperarm",     "RightUpperArm"),
                Map("CC_Base_R_Forearm",      "RightLowerArm"),
                Map("CC_Base_R_Hand",         "RightHand"),
                Map("CC_Base_L_Thigh",        "LeftUpperLeg"),
                Map("CC_Base_L_Calf",         "LeftLowerLeg"),
                Map("CC_Base_L_Foot",         "LeftFoot"),
                Map("CC_Base_L_ToeBase",      "LeftToes"),
                Map("CC_Base_R_Thigh",        "RightUpperLeg"),
                Map("CC_Base_R_Calf",         "RightLowerLeg"),
                Map("CC_Base_R_Foot",         "RightFoot"),
                Map("CC_Base_R_ToeBase",      "RightToes"),
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

        private static bool AssetExists(string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
                return true;

            string fullPath = Path.Combine(
                Path.GetDirectoryName(Application.dataPath) ?? string.Empty,
                assetPath.Replace('/', Path.DirectorySeparatorChar));

            return File.Exists(fullPath);
        }

        private static void RefreshAssets()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
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
