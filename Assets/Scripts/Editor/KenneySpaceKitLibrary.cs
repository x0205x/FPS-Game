using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Imports Kenney Space Kit (CC0) FBX models as static prefabs for the lunar arena.
    /// Source: https://opengameart.org/content/space-kit-remade (Kenney.nl)
    /// </summary>
    public static class KenneySpaceKitLibrary
    {
        public const string FbxRoot = "Assets/Art/Environment/Kenney/_extract/Models/FBX format";
        public const string PrefabRoot = "Assets/Art/Environment/Kenney/Prefabs";

        private static readonly string[] StructureModels =
        {
            "structure", "structure_detailed", "structure_closed", "structure_diagonal",
            "platform_large", "platform_center", "platform_straight", "platform_corner",
            "supports_high", "supports_low", "hangar_smallA", "hangar_roundA",
            "stairs", "stairs_short", "terrain_rampLarge",
        };

        private static readonly string[] RockModels =
        {
            "rock_largeA", "rock_largeB", "rocks_smallA", "rocks_smallB", "rock",
        };

        private static readonly string[] CraterModels = { "crater", "craterLarge" };

        private static readonly string[] OrbitalShipModels =
        {
            "craft_speederA", "craft_speederB", "craft_cargoA", "craft_miner", "craft_racer",
        };

        private static readonly string[] BeaconModels =
        {
            "satelliteDish_large", "satelliteDish", "turret_single",
        };

        private static Dictionary<string, GameObject> _prefabCache;

        [MenuItem("Tools/Game/Import Kenney Space Kit Prefabs")]
        public static void ImportFromMenu() => EnsurePrefabs();

        public static string[] RockModelsForScatter() => RockModels;
        public static string[] CraterModelsForScatter() => CraterModels;

        public static void EnsurePrefabs()
        {
            EnsureFolder(PrefabRoot);
            ConfigureImports(StructureModels);
            ConfigureImports(RockModels);
            ConfigureImports(CraterModels);
            ConfigureImports(OrbitalShipModels);
            ConfigureImports(BeaconModels);

            BuildPrefabs(StructureModels);
            BuildPrefabs(RockModels);
            BuildPrefabs(CraterModels);
            BuildPrefabs(OrbitalShipModels);
            BuildPrefabs(BeaconModels);
            AssetDatabase.SaveAssets();
            _prefabCache = null;
        }

        public static GameObject[] GetOrbitalShipPrefabs()
        {
            var list = new List<GameObject>();
            foreach (string model in OrbitalShipModels)
            {
                if (TryGetPrefab(model, out GameObject prefab))
                    list.Add(prefab);
            }

            return list.ToArray();
        }

        public static bool TryGetPrefab(string modelName, out GameObject prefab)
        {
            EnsureCache();
            return _prefabCache.TryGetValue(modelName, out prefab) && prefab != null;
        }

        public static GameObject PlaceStructure(
            Transform parent,
            string modelName,
            Vector3 position,
            Quaternion rotation,
            float uniformScale,
            string instanceName,
            Terrain terrain,
            Material fallbackMaterial,
            Vector3 fallbackSize)
        {
            float y = LunarTerrainHeightmapBuilder.SampleHeight(terrain, position);
            position.y = y;

            if (TryGetPrefab(modelName, out GameObject prefab))
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                instance.name = instanceName;
                instance.transform.SetPositionAndRotation(position, rotation);
                instance.transform.localScale = Vector3.one * uniformScale;
                return instance;
            }

            return CreateFallbackCube(parent, instanceName, position, rotation, fallbackSize, fallbackMaterial);
        }

        public static void ScatterPrefabs(
            Transform parent,
            string[] modelNames,
            int count,
            float arenaHalfExtent,
            float normalizedRadius,
            Terrain terrain,
            float minScale,
            float maxScale,
            string labelPrefix)
        {
            for (int i = 0; i < count; i++)
            {
                string model = modelNames[Random.Range(0, modelNames.Length)];
                Vector2 point = RandomPointInArena(arenaHalfExtent, normalizedRadius);
                Vector3 pos = new Vector3(point.x, 0f, point.y);
                Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                float scale = Random.Range(minScale, maxScale);
                PlaceStructure(parent, model, pos, rot, scale, $"{labelPrefix}_{i}", terrain, null, Vector3.one);
            }
        }

        private static Vector2 RandomPointInArena(float arenaHalfExtent, float normalizedRadius)
        {
            float r = arenaHalfExtent * normalizedRadius * Mathf.Sqrt(Random.value);
            float a = Random.Range(0f, Mathf.PI * 2f);
            return new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }

        private static void EnsureCache()
        {
            if (_prefabCache != null) return;
            _prefabCache = new Dictionary<string, GameObject>();
            string[] all = Combine(StructureModels, RockModels, CraterModels, OrbitalShipModels, BeaconModels);
            foreach (string model in all)
            {
                string path = $"{PrefabRoot}/{model}.prefab";
                _prefabCache[model] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }

        private static string[] Combine(params string[][] groups)
        {
            var list = new List<string>();
            foreach (string[] group in groups)
                list.AddRange(group);
            return list.ToArray();
        }

        private static void ConfigureImports(string[] models)
        {
            foreach (string model in models)
            {
                string path = $"{FbxRoot}/{model}.fbx";
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;

                importer.globalScale = 1f;
                importer.useFileScale = true;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.importAnimation = false;
                importer.meshCompression = ModelImporterMeshCompression.Medium;
                importer.SaveAndReimport();
            }
        }

        private static void BuildPrefabs(string[] models)
        {
            foreach (string model in models)
            {
                string prefabPath = $"{PrefabRoot}/{model}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                    continue;

                string fbxPath = $"{FbxRoot}/{model}.fbx";
                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                if (source == null)
                {
                    Debug.LogWarning($"[KenneySpaceKitLibrary] Missing model: {fbxPath}");
                    continue;
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                instance.name = model;
                instance.isStatic = true;
                EnsureStaticColliders(instance);

                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                Object.DestroyImmediate(instance);
            }
        }

        private static void EnsureStaticColliders(GameObject root)
        {
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                GameObject go = filter.gameObject;
                if (go.GetComponent<Collider>() != null) continue;

                var collider = go.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                collider.convex = false;
            }
        }

        private static GameObject CreateFallbackCube(
            Transform parent, string name, Vector3 position, Quaternion rotation,
            Vector3 size, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, worldPositionStays: false);
            block.transform.SetPositionAndRotation(position + Vector3.up * size.y * 0.5f, rotation);
            block.transform.localScale = size;
            block.isStatic = true;
            if (material != null && block.TryGetComponent<Renderer>(out var renderer))
                renderer.sharedMaterial = material;
            return block;
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
    }
}
