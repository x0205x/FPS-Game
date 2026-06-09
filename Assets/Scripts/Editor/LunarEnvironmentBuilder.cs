using System.IO;
using Game.Environment;
using Game.Managers;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.EditorTools
{
    /// <summary>
    /// Builds a large lunar FPS arena: grey regolith, impact craters, industrial ruins,
    /// space skybox, and orbital traffic when the player looks up.
    /// </summary>
    public static class LunarEnvironmentBuilder
    {
        /// <summary>Half-size of the square playable ground (1200×1200 m at 600).</summary>
        public const float ArenaHalfExtent = 600f;

        private static float MapScale => ArenaHalfExtent / 60f;
        private const string SmokePrefabPath =
            "Assets/Core/Art/ParticlePack/EffectExamples/Smoke & Steam Effects/Prefabs/SmokeEffect.prefab";
        private const string MaterialsFolder = "Assets/Art/Environment/Materials";
        private const string TexturesFolder = "Assets/Art/Environment/Textures";
        private const string RegolithTexturePath = "Assets/Art/Environment/Textures/LunarRegolith_Albedo.png";

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        public static Light Build(Transform root, out GameObject primaryCover)
        {
            EnsureMaterialsFolder();
            KenneySpaceKitLibrary.EnsurePrefabs();

            Texture2D regolithTex = EnsureRegolithTexture();
            Material regolithMat = EnsureTexturedMaterial(
                "Lunar_Regolith.mat", regolithTex, new Color(0.58f, 0.58f, 0.60f));
            Material ruinMat = EnsureMaterial("Lunar_Structure.mat", new Color(0.45f, 0.46f, 0.48f));
            Material shipHullMat = EnsureMaterial("OrbitalShip_Hull.mat", new Color(0.72f, 0.74f, 0.78f));
            Material shipAccentMat = EnsureMaterial("OrbitalShip_Accent.mat", new Color(0.35f, 0.55f, 0.85f));

            var skyRoot = CreateChild(root, "SpaceSky");
            SpaceSkyBuilder.BuildLunar(
                skyRoot,
                skyDistance: ArenaHalfExtent * 2.5f,
                shipHullMat,
                shipAccentMat,
                KenneySpaceKitLibrary.GetOrbitalShipPrefabs());

            var terrainRoot = CreateChild(root, "Terrain");
            Terrain terrain = LunarTerrainHeightmapBuilder.CreateTerrain(terrainRoot, regolithMat, ArenaHalfExtent);
            CreateKenneyCraterProps(terrainRoot, terrain);
            CreateKenneyRocks(terrainRoot, terrain);

            var structuresRoot = CreateChild(root, "Structures");
            primaryCover = CreateKenneyCover(structuresRoot, terrain, ruinMat);
            CreateKenneyBoundary(structuresRoot, terrain, ruinMat);
            CreateKenneySteps(structuresRoot, terrain, ruinMat);
            CreateKenneyBeacons(structuresRoot, terrain, ruinMat);

            var fxRoot = CreateChild(root, "AtmosphereFX");
            Light sun = CreateSun(fxRoot);
            CreateVentPlumes(fxRoot, terrain);
            CreateLunarDust(fxRoot);
            WireWeather(sun, fxRoot);

            return sun;
        }

        private static Light CreateSun(Transform parent)
        {
            var lightGo = new GameObject("Sun");
            lightGo.transform.SetParent(parent, worldPositionStays: false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;
            light.color = new Color(0.98f, 0.97f, 0.94f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.92f;
            lightGo.transform.rotation = Quaternion.Euler(38f, -55f, 0f);
            return light;
        }

        private static void CreateKenneyCraterProps(Transform parent, Terrain terrain)
        {
            var propsRoot = CreateChild(parent, "CraterProps");
            int count = Mathf.Clamp(Mathf.RoundToInt(10f * MapScale), 10, 28);
            KenneySpaceKitLibrary.ScatterPrefabs(
                propsRoot,
                KenneySpaceKitLibrary.CraterModelsForScatter(),
                count,
                ArenaHalfExtent,
                0.82f,
                terrain,
                minScale: 2.5f,
                maxScale: 6f,
                labelPrefix: "CraterProp");
        }

        private static void CreateKenneyRocks(Transform parent, Terrain terrain)
        {
            var rocksRoot = CreateChild(parent, "RegolithRocks");
            int rockCount = Mathf.Clamp(Mathf.RoundToInt(90f * MapScale), 90, 280);
            KenneySpaceKitLibrary.ScatterPrefabs(
                rocksRoot,
                KenneySpaceKitLibrary.RockModelsForScatter(),
                rockCount,
                ArenaHalfExtent,
                0.94f,
                terrain,
                minScale: 0.8f,
                maxScale: 2.4f,
                labelPrefix: "Rock");
        }

        private static GameObject CreateKenneyCover(Transform parent, Terrain terrain, Material fallbackMat)
        {
            var coverRoot = CreateChild(parent, "Cover").gameObject;
            float s = MapScale;

            GameObject crate1 = KenneySpaceKitLibrary.PlaceStructure(
                coverRoot.transform, "structure_detailed",
                new Vector3(18f * s, 0f, 14f * s), Quaternion.Euler(0f, 18f, 0f), 2.2f,
                "Crate_1", terrain, fallbackMat, new Vector3(1.8f, 1.3f, 1.5f));

            KenneySpaceKitLibrary.PlaceStructure(
                coverRoot.transform, "structure_closed",
                new Vector3(-24f * s, 0f, 20f * s), Quaternion.Euler(0f, -24f, 0f), 2f,
                "Crate_2", terrain, fallbackMat, new Vector3(2.4f, 1.9f, 1.7f));

            KenneySpaceKitLibrary.PlaceStructure(
                coverRoot.transform, "platform_large",
                new Vector3(28f * s, 0f, -22f * s), Quaternion.Euler(0f, 35f, 0f), 2.4f,
                "Crate_3", terrain, fallbackMat, new Vector3(3f, 1.6f, 1.3f));

            KenneySpaceKitLibrary.PlaceStructure(
                coverRoot.transform, "hangar_smallA",
                new Vector3(-38f * s, 0f, -12f * s), Quaternion.Euler(0f, 110f, 0f), 1.8f,
                "Hab_Module", terrain, fallbackMat, new Vector3(4.5f, 2.2f, 3.2f));

            KenneySpaceKitLibrary.PlaceStructure(
                coverRoot.transform, "terrain_rampLarge",
                new Vector3(-28f * s, 0f, -30f * s), Quaternion.Euler(0f, 25f, 0f), 2f,
                "Ramp", terrain, fallbackMat, new Vector3(6f, 0.95f, 4.5f));

            return crate1;
        }

        private static void CreateKenneyBoundary(Transform parent, Terrain terrain, Material fallbackMat)
        {
            var boundsRoot = CreateChild(parent, "BoundaryStructures");
            float limit = ArenaHalfExtent - 8f;
            float spacing = 24f;
            int segments = Mathf.Clamp(Mathf.RoundToInt((ArenaHalfExtent * 2f) / spacing), 16, 56);
            string[] segmentModels = { "platform_straight", "supports_high", "platform_corner", "supports_low" };

            PlaceBoundaryLine(boundsRoot, "North", new Vector3(-limit, 0f, limit), new Vector3(limit, 0f, limit), segments, segmentModels, terrain, fallbackMat);
            PlaceBoundaryLine(boundsRoot, "South", new Vector3(-limit, 0f, -limit), new Vector3(limit, 0f, -limit), segments, segmentModels, terrain, fallbackMat);
            PlaceBoundaryLine(boundsRoot, "East", new Vector3(limit, 0f, -limit), new Vector3(limit, 0f, limit), segments, segmentModels, terrain, fallbackMat);
            PlaceBoundaryLine(boundsRoot, "West", new Vector3(-limit, 0f, -limit), new Vector3(-limit, 0f, limit), segments, segmentModels, terrain, fallbackMat);
        }

        private static void PlaceBoundaryLine(
            Transform parent, string label, Vector3 start, Vector3 end, int segments,
            string[] models, Terrain terrain, Material fallbackMat)
        {
            var lineRoot = CreateChild(parent, $"Wall_{label}");
            for (int i = 0; i < segments; i++)
            {
                if (i % 3 == 1) continue;
                float t = (i + 0.5f) / segments;
                Vector3 pos = Vector3.Lerp(start, end, t);
                string model = models[i % models.Length];
                KenneySpaceKitLibrary.PlaceStructure(
                    lineRoot, model, pos,
                    Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                    Random.Range(1.6f, 2.2f),
                    $"Segment_{i}", terrain, fallbackMat, new Vector3(4f, 2.5f, 2f));
            }
        }

        private static void CreateKenneySteps(Transform parent, Terrain terrain, Material fallbackMat)
        {
            var stepsRoot = CreateChild(parent, "Steps");
            float s = MapScale;
            for (int i = 0; i < 5; i++)
            {
                KenneySpaceKitLibrary.PlaceStructure(
                    stepsRoot, i % 2 == 0 ? "stairs" : "stairs_short",
                    new Vector3(-36f * s, 0f, 30f * s + i * 2.2f * s),
                    Quaternion.Euler(0f, 90f, 0f),
                    2f,
                    $"Step_{i}", terrain, fallbackMat, new Vector3(5.5f, 0.36f, 0.85f));
            }
        }

        private static void CreateKenneyBeacons(Transform parent, Terrain terrain, Material fallbackMat)
        {
            var beaconRoot = CreateChild(parent, "Beacons");
            float h = ArenaHalfExtent * 0.72f;
            string[] models = { "satelliteDish_large", "satelliteDish", "turret_single" };
            Vector3[] positions =
            {
                new(h, 0f, h),
                new(-h, 0f, h),
                new(h, 0f, -h),
                new(-h, 0f, -h),
                new(0f, 0f, h * 0.92f),
                new(0f, 0f, -h * 0.92f),
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject beacon = KenneySpaceKitLibrary.PlaceStructure(
                    beaconRoot,
                    models[i % models.Length],
                    positions[i],
                    Quaternion.Euler(0f, i * 60f, 0f),
                    Random.Range(1.4f, 2f),
                    $"Beacon_{i}",
                    terrain,
                    fallbackMat,
                    new Vector3(0.6f, 2.5f, 0.6f));

                var lightGo = new GameObject("BeaconLight");
                lightGo.transform.SetParent(beacon.transform, false);
                lightGo.transform.localPosition = Vector3.up * 2f;
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 22f;
                light.intensity = 1.5f;
                light.color = new Color(0.55f, 0.75f, 1f);
            }
        }

        private static void CreateVentPlumes(Transform parent, Terrain terrain)
        {
            var smokeRoot = CreateChild(parent, "VentPlumes");
            GameObject smokePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SmokePrefabPath);
            if (smokePrefab == null) return;

            int ventCount = 6;
            for (int i = 0; i < ventCount; i++)
            {
                float angle = i * (Mathf.PI * 2f / ventCount) + Random.Range(-0.15f, 0.15f);
                float radius = Random.Range(ArenaHalfExtent * 0.55f, ArenaHalfExtent * 0.82f);
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                pos.y = LunarTerrainHeightmapBuilder.SampleHeight(terrain, pos);

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(smokePrefab, smokeRoot);
                instance.name = $"Vent_{i}";
                instance.transform.position = pos + Vector3.up * 0.4f;
                instance.transform.localScale = Vector3.one * Random.Range(0.8f, 1.4f);
            }
        }

        private static void CreateLunarDust(Transform parent)
        {
            var dustGo = new GameObject("LunarDust");
            dustGo.transform.SetParent(parent, worldPositionStays: false);
            dustGo.transform.position = new Vector3(0f, 2f, 0f);

            var ps = dustGo.AddComponent<ParticleSystem>();
            dustGo.AddComponent<AmbientDust>();

            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 14f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            main.maxParticles = Mathf.Clamp(Mathf.RoundToInt(90f * MapScale), 90, 200);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.72f, 0.72f, 0.74f, 0.22f),
                new Color(0.55f, 0.55f, 0.58f, 0.12f));

            var emission = ps.emission;
            emission.rateOverTime = 10f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(ArenaHalfExtent * 1.85f, 4f, ArenaHalfExtent * 1.85f);

            var renderer = dustGo.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            Material dustMat = EnsureMaterial("Lunar_DustParticle.mat", new Color(0.65f, 0.65f, 0.68f, 0.2f), particle: true);
            if (dustMat != null) renderer.sharedMaterial = dustMat;
        }

        private static void WireWeather(Light sun, Transform parent)
        {
            var weatherGo = new GameObject("LunarWeather");
            weatherGo.transform.SetParent(parent, worldPositionStays: false);
            var weather = weatherGo.AddComponent<WeatherManager>();
            SetField(weather, "enableLightning", false);
            SetField(weather, "directionalLight", sun);
            SetField(weather, "baseLightIntensity", sun.intensity);
        }

        private static Texture2D EnsureRegolithTexture()
        {
            EnsureFolder(TexturesFolder);
            string fullPath = Path.GetFullPath(RegolithTexturePath);
            if (!File.Exists(fullPath))
            {
                Texture2D tex = GenerateRegolithTexture(512, 512);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                File.WriteAllBytes(fullPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(RegolithTexturePath);
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(RegolithTexturePath);
        }

        private static Texture2D GenerateRegolithTexture(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGB24, true);
            var rng = new System.Random(44021);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = x / (float)width;
                    float ny = y / (float)height;
                    float n1 = Mathf.PerlinNoise(nx * 8f, ny * 8f);
                    float n2 = Mathf.PerlinNoise(nx * 22f + 3.7f, ny * 22f + 1.2f);
                    float n3 = Mathf.PerlinNoise(nx * 55f + 9.1f, ny * 55f + 4.4f);
                    float grey = 0.42f + n1 * 0.18f + n2 * 0.08f + n3 * 0.04f;
                    grey += ((float)rng.NextDouble() - 0.5f) * 0.015f;
                    tex.SetPixel(x, y, new Color(grey, grey, grey * 1.01f));
                }
            }

            tex.Apply();
            return tex;
        }

        private static Vector2 RandomPointInArena(float normalizedRadius)
        {
            float r = ArenaHalfExtent * normalizedRadius * Mathf.Sqrt(Random.value);
            float a = Random.Range(0f, Mathf.PI * 2f);
            return new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, worldPositionStays: false);
            return child.transform;
        }

        private static void ApplyMaterial(GameObject go, Material material)
        {
            if (go.TryGetComponent<Renderer>(out var renderer))
                renderer.sharedMaterial = material;
        }

        private static void EnsureMaterialsFolder()
        {
            EnsureFolder(MaterialsFolder);
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

        private static Material EnsureMaterial(string fileName, Color baseColor, bool particle = false)
        {
            string path = $"{MaterialsFolder}/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.SetColor(BaseColorId, baseColor);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            Shader shader = particle
                ? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                : Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = particle ? Shader.Find("Particles/Standard Unlit") : Shader.Find("Standard");

            var mat = new Material(shader) { color = baseColor };
            mat.SetColor(BaseColorId, baseColor);
            if (particle)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.renderQueue = (int)RenderQueue.Transparent;
            }

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static Material EnsureTexturedMaterial(string fileName, Texture2D texture, Color baseColor)
        {
            string path = $"{MaterialsFolder}/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            if (existing == null)
            {
                existing = new Material(shader);
                AssetDatabase.CreateAsset(existing, path);
            }

            existing.SetColor(BaseColorId, baseColor);
            if (texture != null)
            {
                existing.SetTexture(BaseMapId, texture);
                existing.SetTexture("_MainTex", texture);
            }

            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static void SetField(Object target, string fieldName, Object value)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetField(Object target, string fieldName, float value)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetField(Object target, string fieldName, bool value)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
