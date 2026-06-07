using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Cinemachine;
using Game.Common;
using Game.Player;
using Game.Weapons;
using InputActionAsset = UnityEngine.InputSystem.InputActionAsset;
using GamePlayerInput = Game.Player.PlayerInput;

namespace Game.EditorTools
{
    /// <summary>
    /// One-click test playground builder. Creates and saves a fresh scene at
    /// <c>Assets/Scenes/TestPlayground.unity</c> with a fully wired Player +
    /// Cinemachine 3 rig + ground + obstacles. Auto-builds the humanoid
    /// locomotion clips on first run. Press Play after the menu runs.
    ///
    /// Lives in an Editor/ folder so it never ships in builds.
    /// </summary>
    public static class BuildTestScene
    {
        private const string ScenePath        = "Assets/Scenes/TestPlayground.unity";
        private const string InputAssetPath   = "Assets/Scripts/Player/Input/PlayerInputActions.inputactions";
        private const string SpartanFbxPath   = ImportSpartanCharacter.SpartanFbxPath;
        private const string FallbackFbxPath  = "Assets/Core/Art/Models/Armature.fbx";

        [MenuItem("Tools/Game/Build Test Playground Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // Make sure the humanoid locomotion controller exists before we try to wire it.
            BuildHumanoidAnimations.EnsureLocomotionAssets(forceRebuild: true);
            var locomotionController = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(
                BuildHumanoidAnimations.ControllerPath);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateLighting();
            CreateGround();
            CreateObstacles();

            GameObject player      = CreatePlayer(out var playerInput, out var movement, out var cameraTarget, out var orbit);
            CreateCameraRig(cameraTarget, out CinemachineCamera hipCam, out CinemachineCamera aimCam, out Transform mainCamTransform);
            PlayerCamera playerCam = player.AddComponent<PlayerCamera>();

            // Swap placeholder capsule for Spartan (or fallback humanoid) + live Animator.
            GameObject character = AttachCharacterVisual(player, movement, locomotionController);

            WireReferences(playerInput, movement, orbit, playerCam, hipCam, aimCam, mainCamTransform);

            WirePlayerWeapons.Wire(player, character, playerInput, mainCamTransform);
            WirePlayerWeapons.CreateHud(player);
            WirePlayerWeapons.MakeShootable(GameObject.Find("Crate_1"), 200f);

            BuildEnemies.EnsureGameTags();
            player.tag = "Player";
            TagCoverCrates();
            BuildEnemies.SetupTestArenaEnemies();

            EnsureFolder("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.OpenScene(ScenePath);

            Debug.Log($"[BuildTestScene] Built and saved: {ScenePath}. Press Play to test.");
        }

        // ---------------------------------------------------------------------
        // World

        private static void CreateLighting()
        {
            var lightGo = new GameObject("Directional Light");
            var light   = lightGo.AddComponent<Light>();
            light.type      = LightType.Directional;
            light.intensity = 1.2f;
            light.color     = new Color(1f, 0.95f, 0.85f);
            light.shadows   = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientMode         = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor     = new Color(0.45f, 0.55f, 0.65f);
            RenderSettings.ambientEquatorColor = new Color(0.30f, 0.30f, 0.30f);
            RenderSettings.ambientGroundColor  = new Color(0.15f, 0.15f, 0.15f);
        }

        private static void CreateGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(8f, 1f, 8f);
            ground.isStatic = true;
        }

        private static void CreateObstacles()
        {
            var root = new GameObject("Obstacles").transform;
            CreateBox(root, "Wall_North", new Vector3(0f, 1.5f, 30f),  new Vector3(60f, 3f, 1f));
            CreateBox(root, "Wall_South", new Vector3(0f, 1.5f, -30f), new Vector3(60f, 3f, 1f));
            CreateBox(root, "Wall_East",  new Vector3(30f, 1.5f, 0f), new Vector3(1f, 3f, 60f));
            CreateBox(root, "Wall_West",  new Vector3(-30f, 1.5f, 0f), new Vector3(1f, 3f, 60f));
            CreateBox(root, "Crate_1",    new Vector3(5f, 0.5f, 5f),   Vector3.one);
            CreateBox(root, "Crate_2",    new Vector3(-3f, 1f, 8f),    Vector3.one * 2f);
            CreateBox(root, "Crate_3",    new Vector3(8f, 1.5f, -4f),  new Vector3(3f, 3f, 1f));
            CreateBox(root, "Ramp",       new Vector3(-8f, 0.5f, -6f), new Vector3(4f, 1f, 4f));

            // Stairs to test step-up.
            for (int i = 0; i < 4; i++)
            {
                CreateBox(root, $"Step_{i}",
                    new Vector3(-12f, 0.15f + i * 0.3f, 4f + i * 0.6f),
                    new Vector3(4f, 0.3f, 0.6f));
            }
        }

        private static void CreateBox(Transform parent, string name, Vector3 pos, Vector3 size)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, worldPositionStays: false);
            box.transform.position   = pos;
            box.transform.localScale = size;
            box.isStatic = true;
        }

        private static void TagCoverCrates()
        {
            foreach (string crateName in new[] { "Crate_1", "Crate_2", "Crate_3" })
            {
                GameObject crate = GameObject.Find(crateName);
                if (crate != null) crate.tag = "Cover";
            }
        }

        // ---------------------------------------------------------------------
        // Player

        private static GameObject CreatePlayer(
            out GamePlayerInput playerInput,
            out PlayerMovement movement,
            out Transform   cameraTarget,
            out CameraOrbit orbit)
        {
            var player = new GameObject("Player");
            player.transform.position = Vector3.zero;

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.slopeLimit = 50f;
            cc.stepOffset = 0.4f;

            playerInput = player.AddComponent<GamePlayerInput>();
            movement    = player.AddComponent<PlayerMovement>();
            player.AddComponent<Health>();
            player.AddComponent<PlayerController>();

            // Camera target (orbit pivot at head height)
            var targetGo = new GameObject("CameraTarget");
            targetGo.transform.SetParent(player.transform, worldPositionStays: false);
            targetGo.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            cameraTarget = targetGo.transform;
            orbit = targetGo.AddComponent<CameraOrbit>();

            return player;
        }

        // ---------------------------------------------------------------------
        // Character visual (Spartan humanoid + Animator + PlayerAnimator)

        private static GameObject AttachCharacterVisual(
            GameObject player,
            PlayerMovement movement,
            UnityEditor.Animations.AnimatorController controller)
        {
            if (controller == null)
            {
                Debug.LogError("[BuildTestScene] Locomotion controller missing — animations will not play.");
                return null;
            }

            GameObject modelPrefab = ImportSpartanCharacter.EnsureImported(forceReimport: true);
            string modelPath = SpartanFbxPath;

            if (modelPrefab == null)
            {
                modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FallbackFbxPath);
                modelPath = FallbackFbxPath;
            }

            if (modelPrefab == null)
            {
                Debug.LogWarning("[BuildTestScene] No character model found. Keeping placeholder capsule.");
                return null;
            }

            Avatar humanoidAvatar = ImportSpartanCharacter.EnsureHumanoidAvatar(modelPath, forceReimport: true);

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
            visual.name = "Character";
            visual.transform.SetParent(player.transform, worldPositionStays: false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            CharacterVisualAlignment.StripDisplayStandMeshes(visual.transform);

            var alignment = visual.AddComponent<CharacterVisualAlignment>();
            SetField(alignment, "targetHeight", 1.8f);
            SetField(alignment, "feetYOffset", 0f);
            SetField(alignment, "rotationCorrection", Vector3.zero);
            SetField(alignment, "alignOnAwake", true);

            Animator animator = visual.GetComponent<Animator>();
            if (animator == null)
                animator = visual.AddComponent<Animator>();

            if (humanoidAvatar == null)
            {
                Debug.LogError("[BuildTestScene] No Humanoid Avatar found — run Tools → Game → Import Spartan Character.");
                return null;
            }

            ImportSpartanCharacter.AssignHumanoidAvatar(animator, humanoidAvatar);

            var animatorSetup = visual.GetComponent<CharacterAnimatorSetup>();
            if (animatorSetup == null)
                animatorSetup = visual.AddComponent<CharacterAnimatorSetup>();
            SetField(animatorSetup, "humanoidAvatar", humanoidAvatar);

            var rigBinder = visual.GetComponent<CharacterRigBinder>();
            if (rigBinder == null)
                rigBinder = visual.AddComponent<CharacterRigBinder>();
            rigBinder.RebindSkinnedMeshes();

            animator.enabled                   = true;
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                BuildHumanoidAnimations.ControllerPath);
            animator.applyRootMotion           = false;
            animator.cullingMode               = AnimatorCullingMode.AlwaysAnimate;

            alignment.Align();

            var footstepReceiver = visual.AddComponent<AnimationFootstepReceiver>();
            WireFootstepClips(footstepReceiver);

            SetupSpartanMaterials.EnsureMaterials(out Material armorGreen, out Material undersuitBlack, out Material helmetDarkGreen);
            var materialStyler = visual.AddComponent<CharacterMaterialStyler>();
            SetupSpartanMaterials.WireStyler(materialStyler, armorGreen, undersuitBlack, helmetDarkGreen);
            materialStyler.Apply();

            var playerAnim = player.GetComponent<PlayerAnimator>();
            if (playerAnim == null) playerAnim = player.AddComponent<PlayerAnimator>();
            SetField(playerAnim, "animator", animator);
            SetField(playerAnim, "movement", movement);

            var combatAnim = player.GetComponent<PlayerCombatAnimator>();
            if (combatAnim == null) combatAnim = player.AddComponent<PlayerCombatAnimator>();
            SetField(combatAnim, "animator", animator);
            SetField(combatAnim, "movement", movement);
            SetField(combatAnim, "weaponManager", player.GetComponent<WeaponManager>());
            SetField(combatAnim, "input", player.GetComponent<GamePlayerInput>());

            Debug.Log($"[BuildTestScene] Character visual attached: {AssetDatabase.GetAssetPath(modelPrefab)}");
            return visual;
        }

        private const string FootstepClipsFolder =
            "Assets/Core/Audio/Clips/Player/Footsteps And Landing";

        private static void WireFootstepClips(AnimationFootstepReceiver receiver)
        {
            if (receiver == null) return;

            AudioClip[] clips = LoadFootstepClips();
            if (clips.Length == 0) return;

            var so = new SerializedObject(receiver);
            SetClipArray(so, "walkClips", clips);
            SetClipArray(so, "runClips", clips);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AudioClip[] LoadFootstepClips()
        {
            string[] guids = AssetDatabase.FindAssets("Player_Footstep_ t:AudioClip", new[] { FootstepClipsFolder });
            var clips = new AudioClip[guids.Length];
            for (int i = 0; i < guids.Length; i++)
                clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[i]));
            return clips;
        }

        private static void SetClipArray(SerializedObject so, string propertyName, AudioClip[] clips)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop == null) return;

            prop.arraySize = clips.Length;
            for (int i = 0; i < clips.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
        }

        // ---------------------------------------------------------------------
        // Camera rig

        private static GameObject CreateCameraRig(
            Transform cameraTarget,
            out CinemachineCamera hipCam,
            out CinemachineCamera aimCam,
            out Transform mainCamTransform)
        {
            var rig = new GameObject("CameraRig");

            var mainGo = new GameObject("Main Camera");
            mainGo.tag = "MainCamera";
            mainGo.transform.SetParent(rig.transform, worldPositionStays: false);
            var cam = mainGo.AddComponent<Camera>();
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.55f, 0.65f);
            mainGo.AddComponent<AudioListener>();
            mainGo.AddComponent<CinemachineBrain>();
            mainCamTransform = mainGo.transform;

            hipCam = CreateCmCamera(rig.transform, "CM_Hip", cameraTarget,
                priority: 20, fov: 60f,
                shoulder: new Vector3(0.6f, 0f, 0f),
                verticalArm: 0.4f,
                distance: 4f);

            aimCam = CreateCmCamera(rig.transform, "CM_ADS", cameraTarget,
                priority: 10, fov: 40f,
                shoulder: new Vector3(0.4f, 0.2f, 0f),
                verticalArm: 0.2f,
                distance: 2.2f);

            return rig;
        }

        private static CinemachineCamera CreateCmCamera(
            Transform parent, string name, Transform tracking,
            int priority, float fov, Vector3 shoulder, float verticalArm, float distance)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);

            var cm = go.AddComponent<CinemachineCamera>();
            cm.Priority = priority;
            var lens = cm.Lens;
            lens.FieldOfView = fov;
            cm.Lens = lens;
            cm.Target.TrackingTarget = tracking;

            var follow = go.AddComponent<CinemachineThirdPersonFollow>();
            follow.ShoulderOffset      = shoulder;
            follow.VerticalArmLength   = verticalArm;
            follow.CameraDistance      = distance;

            return cm;
        }

        // ---------------------------------------------------------------------
        // Wiring

        private static void WireReferences(
            GamePlayerInput  playerInput,
            PlayerMovement   movement,
            CameraOrbit      orbit,
            PlayerCamera     playerCam,
            CinemachineCamera hipCam,
            CinemachineCamera aimCam,
            Transform        mainCamTransform)
        {
            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            if (inputAsset == null)
            {
                Debug.LogError($"[BuildTestScene] Could not load input asset at {InputAssetPath}. " +
                               "Make sure the file imported. Player will not respond to input.");
            }

            SetField(playerInput, "inputActions", inputAsset);
            SetField(movement,    "input",        playerInput);
            SetField(movement,    "cameraTransform", mainCamTransform);
            SetField(orbit,       "input",        playerInput);
            SetField(playerCam,   "input",        playerInput);
            SetField(playerCam,   "hipCam",       hipCam);
            SetField(playerCam,   "aimCam",       aimCam);
        }

        private static void SetField(Object target, string fieldName, Object value) =>
            SetProperty(target, fieldName, prop => prop.objectReferenceValue = value);

        private static void SetField(Object target, string fieldName, float value) =>
            SetProperty(target, fieldName, prop => prop.floatValue = value);

        private static void SetField(Object target, string fieldName, bool value) =>
            SetProperty(target, fieldName, prop => prop.boolValue = value);

        private static void SetField(Object target, string fieldName, Vector3 value) =>
            SetProperty(target, fieldName, prop => prop.vector3Value = value);

        private static void SetProperty(Object target, string fieldName, System.Action<SerializedProperty> assign)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[BuildTestScene] Field '{fieldName}' not found on {target.GetType().Name}. Skipping.");
                return;
            }
            assign(prop);
            so.ApplyModifiedPropertiesWithoutUndo();
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
