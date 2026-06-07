using System.IO;
using Game.Common;
using Game.Player;
using Game.UI;
using Game.Weapons;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.EditorTools
{
    /// <summary>
    /// Attaches a hitscan pistol, WeaponManager, and minimal HUD to the test player.
    /// </summary>
    public static class WirePlayerWeapons
    {
        private const string PistolFbxPath    = "Assets/AlterunaFPS/Models/Pistol_Compact_East.Rig.fbx";
        private const string ShellCasingPath  = "Assets/Core/Art/ParticlePack/EffectExamples/WeaponEffects/Models/ShellCasing.FBX";
        private const string MuzzleFxPath     = "Assets/AlterunaFPS/Efx/BulletFX.prefab";
        private const string ImpactFxPath     = "Assets/AlterunaFPS/Efx/Stone Impact.prefab";
        private const string GunShotPath      = "Assets/AlterunaFPS/Sfx/GunShot.wav";

        [MenuItem("Tools/Game/Wire Player Weapons")]
        public static void WireMenu()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("[WirePlayerWeapons] No GameObject named 'Player' in the scene.");
                return;
            }

            Transform character = player.transform.Find("Character");
            Camera cam = Camera.main;
            Transform aim = cam != null ? cam.transform : null;
            var input = player.GetComponent<PlayerInput>();

            Wire(player, character != null ? character.gameObject : null, input, aim);
            CreateHud(player);
            MarkSceneDirty();
            Debug.Log("[WirePlayerWeapons] Weapons + HUD wired on Player.");
        }

        public static WeaponManager Wire(
            GameObject player,
            GameObject character,
            PlayerInput input,
            Transform aimSource)
        {
            if (player == null) return null;

            if (input == null) input = player.GetComponent<PlayerInput>();

            Transform characterRoot = character != null ? character.transform : player.transform;
            Transform mount = FindWeaponMount(characterRoot);
            RifleWeapon rifle = CreatePistolWeapon(mount, aimSource);
            EnsureHandAttach(characterRoot);
            EnsureWeaponCombatAnimator(rifle);

            var manager = player.GetComponent<WeaponManager>();
            if (manager == null) manager = player.AddComponent<WeaponManager>();

            SetField(manager, "input", input);
            SetWeaponList(manager, rifle);

            var movement = player.GetComponent<PlayerMovement>();
            EnsurePlayerCombatAnimator(player, character, movement, manager, input);

            return manager;
        }

        public static void CreateHud(GameObject player)
        {
            if (player == null) return;
            if (Object.FindAnyObjectByType<HUD>() != null) return;

            var canvasGo = new GameObject("HUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            var crosshair = new GameObject("Crosshair");
            crosshair.transform.SetParent(canvasGo.transform, false);
            var crossRect = crosshair.AddComponent<RectTransform>();
            crossRect.anchorMin = crossRect.anchorMax = new Vector2(0.5f, 0.5f);
            crossRect.sizeDelta = new Vector2(8f, 8f);
            var crossImg = crosshair.AddComponent<Image>();
            crossImg.color = new Color(1f, 1f, 1f, 0.85f);

            var ammoGo = new GameObject("AmmoLabel");
            ammoGo.transform.SetParent(canvasGo.transform, false);
            var ammoRect = ammoGo.AddComponent<RectTransform>();
            ammoRect.anchorMin = ammoRect.anchorMax = new Vector2(1f, 0f);
            ammoRect.pivot = new Vector2(1f, 0f);
            ammoRect.anchoredPosition = new Vector2(-32f, 32f);
            ammoRect.sizeDelta = new Vector2(320f, 48f);
            var ammoLabel = ammoGo.AddComponent<TextMeshProUGUI>();
            ammoLabel.fontSize = 28f;
            ammoLabel.alignment = TextAlignmentOptions.BottomRight;
            ammoLabel.text = "-- / --";

            var hud = canvasGo.AddComponent<HUD>();
            SetField(hud, "player", player.GetComponent<PlayerController>());
            SetField(hud, "weaponManager", player.GetComponent<WeaponManager>());
            SetField(hud, "ammoLabel", ammoLabel);
            SetField(hud, "crosshair", crosshair);
        }

        public static void MakeShootable(GameObject target, float health = 150f)
        {
            if (target == null) return;

            var hp = target.GetComponent<Health>();
            if (hp == null) hp = target.AddComponent<Health>();

            SetField(hp, "maxHealth", health);
            SetField(hp, "currentHealth", health);
        }

        private static RifleWeapon CreatePistolWeapon(Transform mount, Transform aimSource)
        {
            var existing = mount.GetComponentInChildren<RifleWeapon>(true);
            if (existing != null)
            {
                Transform existingMesh = existing.transform.Find("PistolMesh") ?? existing.transform;
                AttachWeaponFxComponents(existing.gameObject, existingMesh);
                return existing;
            }

            var weaponGo = new GameObject("Pistol");
            weaponGo.transform.SetParent(mount, worldPositionStays: false);
            weaponGo.transform.localPosition = Vector3.zero;
            weaponGo.transform.localRotation = Quaternion.identity;
            weaponGo.transform.localScale    = Vector3.one;

            Transform pistolMesh = AttachPistolMesh(weaponGo.transform);
            Transform muzzle   = FindMuzzle(pistolMesh != null ? pistolMesh : weaponGo.transform);
            if (muzzle == null)
            {
                muzzle = new GameObject("Muzzle").transform;
                muzzle.SetParent(weaponGo.transform, worldPositionStays: false);
                muzzle.localPosition = new Vector3(0f, 0f, 0.12f);
                muzzle.localRotation = Quaternion.identity;
            }

            var rifle = weaponGo.AddComponent<RifleWeapon>();
            SetField(rifle, "weaponName", "Pistol");
            SetField(rifle, "magazineSize", 12);
            SetField(rifle, "currentAmmo", 12);
            SetField(rifle, "reserveAmmo", 48);
            SetField(rifle, "reserveAmmoMax", 120);
            SetField(rifle, "roundsPerMinute", 420f);
            SetField(rifle, "fullAuto", false);
            SetField(rifle, "damage", 22f);
            SetField(rifle, "range", 120f);
            SetField(rifle, "reloadSeconds", 1.6f);
            SetField(rifle, "muzzle", muzzle);
            SetField(rifle, "aimSource", aimSource);
            SetField(rifle, "muzzleFlashPrefab", LoadPrefab(MuzzleFxPath));
            SetField(rifle, "impactPrefab", LoadPrefab(ImpactFxPath));
            SetField(rifle, "fireClip", LoadAudio(GunShotPath));

            AttachWeaponFxComponents(weaponGo, pistolMesh != null ? pistolMesh : weaponGo.transform);

            return rifle;
        }

        private static Transform AttachPistolMesh(Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PistolFbxPath);
            if (prefab == null) return null;

            GameObject mesh = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            mesh.name = "PistolMesh";
            mesh.transform.localPosition = Vector3.zero;
            mesh.transform.localRotation = Quaternion.identity;
            mesh.transform.localScale    = Vector3.one;

            Animator meshAnimator = mesh.GetComponentInChildren<Animator>(true);
            if (meshAnimator == null)
                meshAnimator = mesh.AddComponent<Animator>();

            BuildCombatAnimations.EnsureCombatAssets();
            meshAnimator.runtimeAnimatorController = BuildCombatAnimations.EnsurePistolController();
            meshAnimator.cullingMode               = AnimatorCullingMode.AlwaysAnimate;

            // Alteruna pistol rig is authored in centimetres; scale the rig root like their player prefab.
            Transform rigRoot = mesh.transform;
            foreach (Transform child in mesh.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Contains("Pistol") && child.name.Contains("Rig"))
                {
                    rigRoot = child;
                    break;
                }
            }

            // Match Alteruna player prefab: rig is authored in cm, inner rig node is scaled up.
            if (rigRoot != mesh.transform)
            {
                rigRoot.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                rigRoot.localScale    = Vector3.one * 100f;
            }
            else if (mesh.transform.childCount > 0)
            {
                Transform inner = mesh.transform.GetChild(0);
                inner.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                inner.localScale    = Vector3.one * 100f;
            }

            return mesh.transform;
        }

        private static void AttachWeaponFxComponents(GameObject weaponGo, Transform weaponMesh)
        {
            var recoil = weaponGo.GetComponent<WeaponRecoil>();
            if (recoil == null) recoil = weaponGo.AddComponent<WeaponRecoil>();
            SetField(recoil, "kickBack", 0.032f);
            SetField(recoil, "kickUp", 0.016f);
            SetField(recoil, "recoveryDuration", 0.18f);

            var shellEject = weaponGo.GetComponent<WeaponShellEject>();
            if (shellEject == null) shellEject = weaponGo.AddComponent<WeaponShellEject>();

            Transform ejectPoint = WeaponShellEject.FindEjectionPoint(weaponMesh);
            GameObject shellPrefab = LoadPrefab(ShellCasingPath);

            SetField(shellEject, "shellPrefab", shellPrefab);
            SetField(shellEject, "ejectionPoint", ejectPoint);
            SetField(shellEject, "gripFallbackLocalOffset", new Vector3(0.045f, 0.035f, -0.025f));
            SetField(shellEject, "shellScale", 0.015f);
            SetField(shellEject, "shellLifetime", 5f);
            SetField(shellEject, "ejectImpulse", 1.6f);
            SetField(shellEject, "ejectUpImpulse", 0.3f);
            SetField(shellEject, "ejectSpin", 3.5f);
        }

        private static Transform FindMuzzle(Transform weaponRoot)
        {
            foreach (Transform t in weaponRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name is "Attach_Muzzle" or "FirePoint" or "Muzzle")
                    return t;
            }
            return null;
        }

        private static void EnsureHandAttach(Transform characterRoot)
        {
            if (characterRoot == null) return;

            var attach = characterRoot.GetComponent<WeaponHandAttach>();
            if (attach == null)
                attach = characterRoot.gameObject.AddComponent<WeaponHandAttach>();
        }

        private static void EnsureWeaponCombatAnimator(RifleWeapon weapon)
        {
            if (weapon == null) return;

            var combatAnim = weapon.GetComponent<WeaponCombatAnimator>();
            if (combatAnim == null)
                combatAnim = weapon.gameObject.AddComponent<WeaponCombatAnimator>();

            Animator meshAnimator = weapon.GetComponentInChildren<Animator>(true);
            SetField(combatAnim, "weapon", weapon);
            SetField(combatAnim, "animator", meshAnimator);
        }

        private static void EnsurePlayerCombatAnimator(
            GameObject player,
            GameObject character,
            PlayerMovement movement,
            WeaponManager weaponManager,
            PlayerInput input)
        {
            if (player == null) return;

            var combatAnim = player.GetComponent<PlayerCombatAnimator>();
            if (combatAnim == null)
                combatAnim = player.AddComponent<PlayerCombatAnimator>();

            Animator animator = character != null
                ? character.GetComponentInChildren<Animator>()
                : player.GetComponentInChildren<Animator>();

            SetField(combatAnim, "animator", animator);
            SetField(combatAnim, "movement", movement);
            SetField(combatAnim, "weaponManager", weaponManager);
            SetField(combatAnim, "input", input);
        }

        private static Transform FindWeaponMount(Transform characterRoot)
        {
            Transform socket = FindBone(characterRoot, "WeaponSocket");
            if (socket != null) return socket;

            // Prefer animated humanoid bones over static mixamorig bind-pose bones.
            Transform hand = FindBone(characterRoot,
                "Right_Hand", "RightHand", "Right Hand", "mixamorig:RightHand");

            if (hand != null)
            {
                var socketGo = new GameObject("WeaponSocket");
                socketGo.transform.SetParent(hand, worldPositionStays: false);
                socketGo.transform.localPosition = Vector3.zero;
                socketGo.transform.localRotation = Quaternion.identity;
                return socketGo.transform;
            }

            var fallback = new GameObject("WeaponSocket").transform;
            fallback.SetParent(characterRoot, worldPositionStays: false);
            fallback.localPosition = new Vector3(0.28f, 1.05f, 0.12f);
            fallback.localRotation = Quaternion.identity;
            return fallback;
        }

        private static Transform FindBone(Transform root, params string[] names)
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

        private static void SetWeaponList(WeaponManager manager, RifleWeapon weapon)
        {
            var so = new SerializedObject(manager);
            SerializedProperty list = so.FindProperty("weapons");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = weapon;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject LoadPrefab(string path) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(path);

        private static AudioClip LoadAudio(string path) =>
            AssetDatabase.LoadAssetAtPath<AudioClip>(path);

        private static void SetField(Object target, string fieldName, Object value) =>
            SetProperty(target, fieldName, prop => prop.objectReferenceValue = value);

        private static void SetField(Object target, string fieldName, string value) =>
            SetProperty(target, fieldName, prop => prop.stringValue = value);

        private static void SetField(Object target, string fieldName, float value) =>
            SetProperty(target, fieldName, prop => prop.floatValue = value);

        private static void SetField(Object target, string fieldName, int value) =>
            SetProperty(target, fieldName, prop => prop.intValue = value);

        private static void SetField(Object target, string fieldName, bool value) =>
            SetProperty(target, fieldName, prop => prop.boolValue = value);

        private static void SetField(Object target, string fieldName, Vector3 value) =>
            SetProperty(target, fieldName, prop => prop.vector3Value = value);

        private static void SetProperty(Object target, string fieldName, System.Action<SerializedProperty> assign)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) return;
            assign(prop);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void MarkSceneDirty()
        {
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
    }
}
