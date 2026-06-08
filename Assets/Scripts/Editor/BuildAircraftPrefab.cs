using System.IO;
using Game.Player;
using Game.Vehicles;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Builds the Osprey X-80 flyable prefab with flight controller, thruster VFX mounts,
    /// and pilot enter/exit wiring.
    /// </summary>
    public static class BuildAircraftPrefab
    {
        public const string PrefabFolder = "Assets/Prefabs/Vehicles";
        public const string PrefabPath   = PrefabFolder + "/OspreyX80.prefab";

        private const float TargetLengthMeters = 15f;
        private const float MinLengthMeters    = 12f;
        private const float MaxLengthMeters    = 18f;

        [MenuItem("Tools/Game/Build Osprey Prefab")]
        public static GameObject BuildPrefabMenu() => EnsurePrefab(forceRebuild: true);

        public static GameObject EnsurePrefab(bool forceRebuild = false)
        {
            ImportOspreyAircraft.EnsureImported(forceReimport: false);
            Material material = ImportOspreyAircraft.EnsureMaterial();

            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (!forceRebuild && existing != null && PrefabLooksComplete(existing))
                return existing;

            EnsureFolder(PrefabFolder);

            if (existing != null)
                AssetDatabase.DeleteAsset(PrefabPath);

            GameObject root = BuildOspreyInstance(material);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            Debug.Log($"[BuildAircraftPrefab] Saved Osprey prefab at {PrefabPath}");
            return prefab;
        }

        public static GameObject PlaceInScene(Vector3 position, Quaternion rotation, PlayerController player)
        {
            GameObject prefab = EnsurePrefab(forceRebuild: false);
            if (prefab == null)
                return null;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetPositionAndRotation(position, rotation);

            AircraftPilot pilot = instance.GetComponent<AircraftPilot>();
            if (pilot != null && player != null)
                pilot.BindPlayer(player);

            return instance;
        }

        private static GameObject BuildOspreyInstance(Material material)
        {
            GameObject modelPrefab = ImportOspreyAircraft.EnsureImported(forceReimport: false);
            if (modelPrefab == null)
            {
                Debug.LogError("[BuildAircraftPrefab] Osprey model missing — run Tools → Game → Import Osprey Aircraft.");
                return new GameObject("OspreyX80");
            }

            var root = new GameObject("OspreyX80");
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = 8500f;
            rb.useGravity = false;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.4f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            AircraftFlightController flight = root.AddComponent<AircraftFlightController>();
            AircraftThrusterVfx vfx = root.AddComponent<AircraftThrusterVfx>();
            AircraftFlightAudio audio = root.AddComponent<AircraftFlightAudio>();
            AircraftPilot pilot = root.AddComponent<AircraftPilot>();

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, worldPositionStays: false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            float scale = ComputeVisualScale(visual.transform, TargetLengthMeters);
            visual.transform.localScale = Vector3.one * scale;

            ImportOspreyAircraft.ApplyMaterialToRenderers(visual, material);

            Bounds bounds = CalculateBounds(visual.transform);
            Vector3 center = bounds.center - root.transform.position;

            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = center;
            box.size = bounds.size;

            Transform exitPoint = CreateChild(root.transform, "ExitPoint");
            exitPoint.localPosition = new Vector3(bounds.extents.x + 2.5f, bounds.extents.y * 0.15f, 0f);

            AircraftThrusterPoint[] thrusters = CreateThrusterPoints(root.transform, bounds);
            WireThrusterVfx(vfx, flight, thrusters);
            WireFlightAudio(audio, flight);
            WirePilot(pilot, flight, exitPoint);

            return root;
        }

        private static AircraftThrusterPoint[] CreateThrusterPoints(Transform root, Bounds bounds)
        {
            Vector3 ext = bounds.extents;
            Vector3 center = bounds.center - root.position;

            return new[]
            {
                CreateThruster(root, "MainEngine_L",
                    center + new Vector3(-ext.x * 0.42f, -ext.y * 0.05f, -ext.z * 0.88f),
                    Vector3.back, AircraftThrusterPoint.ThrusterKind.Main),
                CreateThruster(root, "MainEngine_R",
                    center + new Vector3(ext.x * 0.42f, -ext.y * 0.05f, -ext.z * 0.88f),
                    Vector3.back, AircraftThrusterPoint.ThrusterKind.Main),
                CreateThruster(root, "Maneuver_F",
                    center + new Vector3(0f, 0f, ext.z * 0.92f),
                    Vector3.forward, AircraftThrusterPoint.ThrusterKind.Maneuver),
                CreateThruster(root, "Maneuver_B",
                    center + new Vector3(0f, 0f, -ext.z * 0.92f),
                    Vector3.back, AircraftThrusterPoint.ThrusterKind.Maneuver),
                CreateThruster(root, "Maneuver_L",
                    center + new Vector3(-ext.x * 0.92f, 0f, 0f),
                    Vector3.left, AircraftThrusterPoint.ThrusterKind.Maneuver),
                CreateThruster(root, "Maneuver_R",
                    center + new Vector3(ext.x * 0.92f, 0f, 0f),
                    Vector3.right, AircraftThrusterPoint.ThrusterKind.Maneuver),
                CreateThruster(root, "Maneuver_U",
                    center + new Vector3(0f, ext.y * 0.88f, 0f),
                    Vector3.up, AircraftThrusterPoint.ThrusterKind.Maneuver),
                CreateThruster(root, "Maneuver_D",
                    center + new Vector3(0f, -ext.y * 0.88f, 0f),
                    Vector3.down, AircraftThrusterPoint.ThrusterKind.Maneuver),
            };
        }

        private static AircraftThrusterPoint CreateThruster(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 thrustDirection,
            AircraftThrusterPoint.ThrusterKind kind)
        {
            Transform point = CreateChild(parent, name);
            point.localPosition = localPosition;

            var thruster = point.gameObject.AddComponent<AircraftThrusterPoint>();
            var so = new SerializedObject(thruster);
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.FindProperty("localThrustDirection").vector3Value = thrustDirection.normalized;
            so.ApplyModifiedPropertiesWithoutUndo();
            return thruster;
        }

        private static void WireThrusterVfx(
            AircraftThrusterVfx vfx,
            AircraftFlightController flight,
            AircraftThrusterPoint[] thrusters)
        {
            var so = new SerializedObject(vfx);
            so.FindProperty("flight").objectReferenceValue = flight;

            SerializedProperty array = so.FindProperty("thrusters");
            array.arraySize = thrusters.Length;
            for (int i = 0; i < thrusters.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = thrusters[i];

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireFlightAudio(AircraftFlightAudio audio, AircraftFlightController flight)
        {
            var so = new SerializedObject(audio);
            so.FindProperty("flight").objectReferenceValue = flight;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WirePilot(
            AircraftPilot pilot,
            AircraftFlightController flight,
            Transform exitPoint)
        {
            var so = new SerializedObject(pilot);
            so.FindProperty("flight").objectReferenceValue = flight;
            so.FindProperty("exitPoint").objectReferenceValue = exitPoint;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static float ComputeVisualScale(Transform visual, float targetLength)
        {
            Bounds bounds = CalculateBounds(visual);
            float longest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (longest < 0.001f)
                return 1f;

            float scale = targetLength / longest;
            float scaledLength = longest * scale;
            if (scaledLength < MinLengthMeters)
                scale *= MinLengthMeters / scaledLength;
            else if (scaledLength > MaxLengthMeters)
                scale *= MaxLengthMeters / scaledLength;

            return scale;
        }

        private static Bounds CalculateBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(root.position, Vector3.one * 4f);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, worldPositionStays: false);
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
            return child;
        }

        private static bool PrefabLooksComplete(GameObject prefab)
        {
            if (prefab.GetComponent<AircraftFlightController>() == null) return false;
            if (prefab.GetComponent<AircraftThrusterVfx>() == null) return false;
            if (prefab.GetComponent<AircraftFlightAudio>() == null) return false;
            if (prefab.GetComponent<AircraftPilot>() == null) return false;
            if (prefab.transform.Find("Visual") == null) return false;
            if (prefab.transform.Find("ExitPoint") == null) return false;
            if (prefab.transform.Find("MainEngine_L") == null) return false;
            if (prefab.transform.Find("Maneuver_U") == null) return false;
            return true;
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return;

            string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            string leaf   = Path.GetFileName(assetPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(leaf))
                AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
