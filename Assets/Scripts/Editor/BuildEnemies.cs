using System.Collections.Generic;
using System.IO;
using Game.AI;
using Game.Common;
using Game.Managers;
using Game.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Game.EditorTools
{
    /// <summary>
    /// Builds unarmed mecha enemy prefabs with humanoid locomotion only.
    /// </summary>
    public static class BuildEnemies
    {
        public const string PrefabFolder = "Assets/Prefabs/Enemies";
        public const string GruntPrefabPath = PrefabFolder + "/Enemy_Grunt.prefab";

        private const float MechaTargetHeight = 2.1f;

        private const string NavMeshAssetPath = "Assets/Scenes/NavMeshData.asset";

        [MenuItem("Tools/Game/Build Enemy Prefab")]
        public static GameObject BuildPrefabMenu() => EnsureGruntPrefab(forceRebuild: true);

        [MenuItem("Tools/Game/Fix Enemy Materials")]
        public static void FixEnemyMaterialsMenu() => RefreshEnemyVisualsInScene();

        [MenuItem("Tools/Game/Apply Enemy Black/Yellow Colors")]
        public static void ApplyEnemyColorsMenu() => RefreshEnemyVisualsInScene();

        private static void RefreshEnemyVisualsInScene()
        {
            int fixedCount = 0;
            foreach (EnemyController enemy in UnityEngine.Object.FindObjectsByType<EnemyController>())
            {
                StripEnemyCombatArtifacts(enemy.transform);
                Transform character = enemy.transform.Find("Character");
                if (character == null) continue;
                ApplyEnemyVisualFix(character.gameObject);
                fixedCount++;
            }

            MarkSceneDirty();
            Debug.Log($"[BuildEnemies] Applied black/yellow enemy visuals on {fixedCount} character(s).");
        }

        [MenuItem("Tools/Game/Add Enemies To Scene")]
        public static void AddEnemiesToSceneMenu()
        {
            EnsureTags();
            BakeSceneNavMesh();
            EnsureGruntPrefab(forceRebuild: false);
            PlaceEnemiesInScene();
            MarkSceneDirty();
            Debug.Log("[BuildEnemies] Enemies added to the active scene.");
        }

        public static void EnsureGameTags() => EnsureTags();

        public static void SetupTestArenaEnemies()
        {
            EnsureTags();
            ImportMechaEnemyCharacter.EnsureImported(forceReimport: false);
            BuildHumanoidAnimations.EnsureLocomotionAssets(forceRebuild: false);
            BakeSceneNavMesh();
            GameObject prefab = EnsureGruntPrefab(forceRebuild: false);
            PlaceEnemiesInScene(prefab);
        }

        public static GameObject EnsureGruntPrefab(bool forceRebuild = false)
        {
            EnsureTags();
            EnsureFolder(PrefabFolder);
            ImportMechaEnemyCharacter.EnsureImported(forceReimport: false);
            BuildHumanoidAnimations.EnsureLocomotionAssets(forceRebuild: false);

            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(GruntPrefabPath);
            if (!forceRebuild && existing != null && PrefabHasHumanoidRig(existing))
                return existing;

            if (existing != null)
                AssetDatabase.DeleteAsset(GruntPrefabPath);

            GameObject root = BuildGruntInstance(Vector3.zero, Quaternion.identity, null);
            root.name = "Enemy_Grunt";

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, GruntPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BuildEnemies] Saved mecha enemy prefab at {GruntPrefabPath}");
            return prefab;
        }

        public static void BakeSceneNavMesh()
        {
            var sources = new List<NavMeshBuildSource>();
            var markups = new List<NavMeshBuildMarkup>();
            var bounds  = new Bounds(
                Vector3.zero,
                new Vector3(LunarEnvironmentBuilder.ArenaHalfExtent * 2.2f, 50f,
                    LunarEnvironmentBuilder.ArenaHalfExtent * 2.2f));

            UnityEngine.AI.NavMeshBuilder.CollectSources(
                bounds,
                ~0,
                NavMeshCollectGeometry.RenderMeshes,
                0,
                markups,
                sources);

            if (sources.Count == 0)
            {
                Debug.LogWarning("[BuildEnemies] No NavMesh sources found in scene.");
                return;
            }

            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
            NavMeshData baked = UnityEngine.AI.NavMeshBuilder.BuildNavMeshData(
                settings,
                sources,
                bounds,
                Vector3.zero,
                Quaternion.identity);

            if (baked == null)
            {
                Debug.LogWarning("[BuildEnemies] NavMesh build failed.");
                return;
            }

            EnsureFolder("Assets/Scenes");
            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshAssetPath) != null)
                AssetDatabase.DeleteAsset(NavMeshAssetPath);

            baked.name = "NavMeshData";
            AssetDatabase.CreateAsset(baked, NavMeshAssetPath);
            AssetDatabase.SaveAssets();

            NavMeshData asset = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshAssetPath);
            SceneNavMeshHolder holder = UnityEngine.Object.FindAnyObjectByType<SceneNavMeshHolder>();
            if (holder == null)
            {
                var holderGo = new GameObject("NavMesh");
                holder = holderGo.AddComponent<SceneNavMeshHolder>();
            }

            holder.AssignNavMeshData(asset);
            EditorUtility.SetDirty(holder);

            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static bool PrefabHasHumanoidRig(GameObject prefab) =>
            prefab.GetComponentInChildren<EnemyLocomotionAnimator>(true) != null
            && prefab.GetComponentInChildren<CharacterAnimatorSetup>(true) != null
            && !PrefabHasLegacyCombat(prefab);

        private static bool PrefabHasLegacyCombat(GameObject prefab)
        {
            if (prefab.transform.Find("Eye/Muzzle") != null)
                return true;

            foreach (Component component in prefab.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                string name = component.GetType().Name;
                if (name is "EnemyCombat" or "EnemyCombatAnimator")
                    return true;
            }

            return false;
        }

        private static void PlaceEnemiesInScene(GameObject prefab = null)
        {
            prefab ??= EnsureGruntPrefab();
            if (prefab == null) return;

            Transform enemiesRoot = FindOrCreateRoot("Enemies");
            Transform patrolRoot  = FindOrCreateRoot("EnemyPatrolPoints");

            float h = LunarEnvironmentBuilder.ArenaHalfExtent;
            Vector3[] spawns =
            {
                new(h * 0.35f, 0f, h * 0.28f),
                new(-h * 0.42f, 0f, h * 0.22f),
                new(h * 0.22f, 0f, -h * 0.38f),
                new(-h * 0.30f, 0f, -h * 0.32f),
                new(h * 0.48f, 0f, -h * 0.14f),
            };

            Vector3[] patrol =
            {
                new(h * 0.30f, 0f, h * 0.20f),
                new(h * 0.46f, 0f, h * 0.34f),
                new(-h * 0.28f, 0f, h * 0.32f),
                new(-h * 0.50f, 0f, h * 0.12f),
                new(-h * 0.10f, 0f, -h * 0.36f),
                new(h * 0.18f, 0f, -h * 0.22f),
                new(h * 0.42f, 0f, -h * 0.30f),
                new(-h * 0.36f, 0f, -h * 0.26f),
            };

            var patrolPoints = new Transform[patrol.Length];
            for (int i = 0; i < patrol.Length; i++)
            {
                var pt = new GameObject($"Patrol_{i}");
                pt.transform.SetParent(patrolRoot, worldPositionStays: false);
                pt.transform.position = SnapToNavMesh(patrol[i]);
                patrolPoints[i] = pt.transform;
            }

            for (int i = 0; i < spawns.Length; i++)
            {
                string name = $"Enemy_{i + 1}";
                Transform existing = enemiesRoot.Find(name);
                if (existing != null)
                {
                    var existingController = existing.GetComponent<EnemyController>();
                    if (existingController != null)
                        SetPatrolPoints(existingController, patrolPoints);
                    continue;
                }

                Vector3 pos = SnapToNavMesh(spawns[i]);
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, enemiesRoot);
                instance.name = name;
                instance.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));

                var controller = instance.GetComponent<EnemyController>();
                if (controller != null)
                    SetPatrolPoints(controller, patrolPoints);
            }
        }

        private static GameObject BuildGruntInstance(Vector3 position, Quaternion rotation, Transform parent)
        {
            var root = new GameObject("Enemy_Grunt");
            if (parent != null) root.transform.SetParent(parent, worldPositionStays: false);
            root.transform.SetPositionAndRotation(position, rotation);
            root.tag = "Enemy";

            var agent = root.AddComponent<NavMeshAgent>();
            agent.height = MechaTargetHeight;
            agent.radius = 0.45f;
            agent.speed = 2.5f;
            agent.angularSpeed = 360f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 1.2f;

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = MechaTargetHeight;
            capsule.radius = 0.45f;
            capsule.center = new Vector3(0f, MechaTargetHeight * 0.5f, 0f);

            var health = root.AddComponent<Health>();
            SetHealth(health, 80f);

            var controller = root.AddComponent<EnemyController>();
            root.AddComponent<EnemyCoverSystem>();

            var eye = new GameObject("Eye").transform;
            eye.SetParent(root.transform, worldPositionStays: false);
            eye.localPosition = new Vector3(0f, MechaTargetHeight * 0.85f, 0.15f);
            var vision = eye.gameObject.AddComponent<EnemyVision>();
            SetVisionDefaults(vision);

            GameObject character = AttachHumanoidVisual(root.transform);

            WireEnemyModules(controller, vision);

            if (character != null)
                character.AddComponent<EnemyLocomotionAnimator>();

            return root;
        }

        private static GameObject AttachHumanoidVisual(Transform parent)
        {
            GameObject modelPrefab = ImportMechaEnemyCharacter.EnsureImported(forceReimport: false);
            if (modelPrefab == null)
            {
                Debug.LogWarning("[BuildEnemies] Mecha model missing; enemy will have no visual.");
                return null;
            }

            Avatar avatar = ImportMechaEnemyCharacter.EnsureHumanoidAvatar(forceReimport: false);
            if (avatar == null)
            {
                Debug.LogError("[BuildEnemies] No Humanoid Avatar for mecha — run Tools → Game → Import Mecha Enemy Character.");
                return null;
            }

            RuntimeAnimatorController locomotionController =
                BuildHumanoidAnimations.EnsureLocomotionAssets();

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, parent);
            visual.name = "Character";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            CharacterVisualAlignment.StripDisplayStandMeshes(visual.transform);

            var alignment = visual.AddComponent<CharacterVisualAlignment>();
            SetField(alignment, "targetHeight", MechaTargetHeight);
            SetField(alignment, "feetYOffset", 0f);
            SetField(alignment, "rotationCorrection", Vector3.zero);
            SetField(alignment, "alignOnAwake", true);

            Animator animator = visual.GetComponent<Animator>();
            if (animator == null) animator = visual.AddComponent<Animator>();

            ImportMechaEnemyCharacter.AssignHumanoidAvatar(animator, avatar);

            if (visual.GetComponent<CharacterRigBinder>() == null)
                visual.AddComponent<CharacterRigBinder>();

            var animatorSetup = visual.GetComponent<CharacterAnimatorSetup>();
            if (animatorSetup == null)
                animatorSetup = visual.AddComponent<CharacterAnimatorSetup>();
            SetField(animatorSetup, "humanoidAvatar", avatar);

            animator.enabled                   = true;
            animator.runtimeAnimatorController = locomotionController;
            animator.applyRootMotion           = false;
            animator.cullingMode               = AnimatorCullingMode.AlwaysAnimate;

            alignment.Align();
            ApplyEnemyVisualFix(visual);
            return visual;
        }

        private static void StripEnemyCombatArtifacts(Transform enemyRoot)
        {
            Transform eye = enemyRoot.Find("Eye");
            if (eye != null)
                DestroyChildIfExists(eye, "Muzzle");

            Transform character = enemyRoot.Find("Character");
            if (character != null)
            {
                DestroyChildIfExists(character, "WeaponSocket");
                DestroyChildIfExists(character, "Pistol");

                RemoveComponentByScriptName(character.gameObject, "EnemyCombatAnimator");
                RemoveComponentByScriptName(character.gameObject, "WeaponHandAttach");
            }

            RemoveComponentByScriptName(enemyRoot.gameObject, "EnemyCombat");
            RemoveComponentByScriptName(enemyRoot.gameObject, "EnemyCombatAnimator");
        }

        private static void RemoveComponentByScriptName(GameObject go, string scriptName)
        {
            foreach (Component component in go.GetComponents<Component>())
            {
                if (component == null || component.GetType().Name != scriptName) continue;
                UnityEngine.Object.DestroyImmediate(component);
            }
        }

        private static void DestroyChildIfExists(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
                UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        private static void ApplyEnemyVisualFix(GameObject visual)
        {
            var matFix = visual.GetComponent<EnemyUrpMaterialFix>();
            if (matFix == null)
                matFix = visual.AddComponent<EnemyUrpMaterialFix>();
            SetField(matFix, "applyOnAwake", true);
            SetField(matFix, "yellowTint", new Color(0.95f, 0.78f, 0.05f));
            SetField(matFix, "blackTint", new Color(0.05f, 0.05f, 0.05f));
            matFix.Apply();
        }

        private static void WireEnemyModules(EnemyController controller, EnemyVision vision)
        {
            var so = new SerializedObject(controller);
            so.FindProperty("vision").objectReferenceValue = vision;
            so.FindProperty("walkSpeed").floatValue = 2.5f;
            so.FindProperty("chaseSpeed").floatValue = 5f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetVisionDefaults(EnemyVision vision)
        {
            var so = new SerializedObject(vision);
            so.FindProperty("viewRadius").floatValue = 22f;
            so.FindProperty("viewAngleDeg").floatValue = 120f;
            so.FindProperty("eye").objectReferenceValue = vision.transform;
            so.FindProperty("targetMask").intValue = ~0;
            so.FindProperty("obstacleMask").intValue = ~0;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetPatrolPoints(EnemyController controller, Transform[] points)
        {
            var so = new SerializedObject(controller);
            SerializedProperty array = so.FindProperty("patrolPoints");
            array.arraySize = points.Length;
            for (int i = 0; i < points.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetHealth(Health health, float amount)
        {
            var so = new SerializedObject(health);
            so.FindProperty("maxHealth").floatValue = amount;
            so.FindProperty("currentHealth").floatValue = amount;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Vector3 SnapToNavMesh(Vector3 pos)
        {
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 12f, NavMesh.AllAreas))
                return hit.position;
            return pos;
        }

        private static Transform FindOrCreateRoot(string name)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null) return existing.transform;
            return new GameObject(name).transform;
        }

        private static void EnsureTags()
        {
            EnsureTag("Player");
            EnsureTag("Enemy");
            EnsureTag("Cover");
        }

        private static void EnsureTag(string tag)
        {
            if (string.IsNullOrEmpty(tag) || TagExists(tag))
                return;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning($"[BuildEnemies] Could not load TagManager.asset to add tag '{tag}'.");
                return;
            }

            var tagManager = new SerializedObject(assets[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            if (tagsProp == null)
            {
                Debug.LogWarning($"[BuildEnemies] TagManager.tags property missing; cannot add '{tag}'.");
                return;
            }

            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        private static bool TagExists(string tag)
        {
            foreach (string existing in UnityEditorInternal.InternalEditorUtility.tags)
            {
                if (existing == tag)
                    return true;
            }

            return false;
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            string leaf = Path.GetFileName(assetPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(leaf))
                AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void SetField(Object target, string fieldName, float value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetField(Object target, string fieldName, bool value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetField(Object target, string fieldName, Vector3 value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.vector3Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetField(Object target, string fieldName, Color value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.colorValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void MarkSceneDirty()
        {
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
    }
}
