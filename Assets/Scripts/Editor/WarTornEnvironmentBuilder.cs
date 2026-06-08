using Game.Environment;
using Game.Managers;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.EditorTools
{
    /// <summary>
    /// Builds a large war-torn outdoor arena: dusty ground, grass, wind trees,
    /// ruined cover, smoke, ground fog, and an outer-space sky with planets.
    /// </summary>
    public static class WarTornEnvironmentBuilder
    {
        /// <summary>Half-size of the square playable ground (800×800 m at 400).</summary>
        public const float ArenaHalfExtent = 400f;

        private static float MapScale => ArenaHalfExtent / 60f;
        private const string SmokePrefabPath =
            "Assets/Core/Art/ParticlePack/EffectExamples/Smoke & Steam Effects/Prefabs/SmokeEffect.prefab";
        private const string RibbonSmokePath =
            "Assets/Core/Art/ParticlePack/EffectExamples/Fire & Explosion Effects/Prefabs/RibbonSmoke.prefab";
        private const string MaterialsFolder = "Assets/Art/Environment/Materials";

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public static Light Build(Transform root, out GameObject primaryCover)
        {
            EnsureMaterialsFolder();
            Material groundMat = EnsureMaterial("WarTorn_Ground.mat", new Color(0.42f, 0.34f, 0.22f));
            Material grassMat = EnsureMaterial("WarTorn_Grass.mat", new Color(0.28f, 0.36f, 0.14f));
            Material trunkMat = EnsureMaterial("WarTorn_Trunk.mat", new Color(0.28f, 0.20f, 0.12f));
            Material foliageMat = EnsureMaterial("WarTorn_Foliage.mat", new Color(0.22f, 0.32f, 0.12f));
            Material ruinMat = EnsureMaterial("WarTorn_Ruin.mat", new Color(0.32f, 0.30f, 0.28f));
            Material scorchMat = EnsureMaterial("WarTorn_Scorch.mat", new Color(0.18f, 0.14f, 0.10f));

            var skyRoot = CreateChild(root, "SpaceSky");
            SpaceSkyBuilder.Build(skyRoot, skyDistance: ArenaHalfExtent * 2.25f);

            var terrainRoot = CreateChild(root, "Terrain");
            CreateGround(terrainRoot, groundMat);
            CreateScorchMarks(terrainRoot, scorchMat);
            CreateGrassField(terrainRoot, grassMat);

            var natureRoot = CreateChild(root, "Nature");
            CreateTreeField(natureRoot, trunkMat, foliageMat);

            var structuresRoot = CreateChild(root, "Structures");
            primaryCover = CreateRuinedCover(structuresRoot, ruinMat);
            CreateBoundaryRuins(structuresRoot, ruinMat);
            CreateCombatSteps(structuresRoot, ruinMat);

            var fxRoot = CreateChild(root, "AtmosphereFX");
            Light sun = CreateSun(fxRoot);
            CreateDistantSmoke(fxRoot);
            CreateAmbientDust(fxRoot);
            WireWeather(sun, fxRoot);

            return sun;
        }

        private static Light CreateSun(Transform parent)
        {
            var lightGo = new GameObject("Sun");
            lightGo.transform.SetParent(parent, worldPositionStays: false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.88f, 0.72f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.85f;
            lightGo.transform.rotation = Quaternion.Euler(22f, -48f, 0f);
            return light;
        }

        private static void CreateGround(Transform parent, Material material)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(parent, worldPositionStays: false);
            ground.transform.localScale = new Vector3(ArenaHalfExtent * 0.2f, 1f, ArenaHalfExtent * 0.2f);
            ground.isStatic = true;
            ApplyMaterial(ground, material);
        }

        private static void CreateScorchMarks(Transform parent, Material material)
        {
            var scorchRoot = CreateChild(parent, "ScorchMarks");
            int scorchCount = Mathf.Clamp(Mathf.RoundToInt(6f * MapScale), 6, 36);

            for (int i = 0; i < scorchCount; i++)
            {
                Vector2 center = RandomPointInArena(0.82f);
                var crater = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                crater.name = $"Scorch_{i}";
                crater.transform.SetParent(scorchRoot, worldPositionStays: false);
                float radius = Random.Range(2.5f, 6.5f) * Mathf.Lerp(1f, 1.35f, MapScale / 6.67f);
                crater.transform.position = new Vector3(center.x, 0.02f, center.y);
                crater.transform.localScale = new Vector3(radius, 0.02f, radius);
                crater.isStatic = true;
                ApplyMaterial(crater, material);
                Object.DestroyImmediate(crater.GetComponent<Collider>());
            }
        }

        private static void CreateGrassField(Transform parent, Material material)
        {
            var grassRoot = CreateChild(parent, "Grass");
            int clusterCount = Mathf.Clamp(Mathf.RoundToInt(220f * MapScale), 220, 900);
            float innerClear = 14f * MapScale;

            for (int i = 0; i < clusterCount; i++)
            {
                Vector2 pos = RandomPointInArena(0.92f);
                if (pos.sqrMagnitude < innerClear * innerClear) continue;

                int blades = Random.Range(4, 9);
                for (int b = 0; b < blades; b++)
                {
                    Vector2 offset = Random.insideUnitCircle * 0.8f;
                    CreateGrassTuft(grassRoot, new Vector3(pos.x + offset.x, 0f, pos.y + offset.y), material);
                }
            }
        }

        private static void CreateGrassTuft(Transform parent, Vector3 position, Material material)
        {
            var tuft = new GameObject("GrassTuft");
            tuft.transform.SetParent(parent, worldPositionStays: false);
            tuft.transform.position = position;
            float yaw = Random.Range(0f, 180f);
            CreateGrassBlade(tuft.transform, yaw, material);
            CreateGrassBlade(tuft.transform, yaw + 90f, material);
        }

        private static void CreateGrassBlade(Transform parent, float yaw, Material material)
        {
            var blade = GameObject.CreatePrimitive(PrimitiveType.Quad);
            blade.transform.SetParent(parent, worldPositionStays: false);
            blade.transform.localPosition = Vector3.zero;
            blade.transform.rotation = Quaternion.Euler(90f, yaw, 0f);
            blade.transform.localScale = new Vector3(
                Random.Range(0.25f, 0.45f),
                Random.Range(0.35f, 0.75f),
                1f);
            Object.DestroyImmediate(blade.GetComponent<Collider>());
            ApplyMaterial(blade, material);
        }

        private static void CreateTreeField(Transform parent, Material trunkMat, Material foliageMat)
        {
            int treeCount = Mathf.Clamp(Mathf.RoundToInt(72f * MapScale), 72, 320);
            float edgeBias = 0.55f;
            float innerClearRadius = 4f * MapScale;

            for (int i = 0; i < treeCount; i++)
            {
                Vector2 pos = RandomPointInArena(1f);
                if (Random.value < edgeBias)
                {
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float radius = Random.Range(ArenaHalfExtent * 0.55f, ArenaHalfExtent * 0.88f);
                    pos = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                }

                if (pos.sqrMagnitude < innerClearRadius * innerClearRadius) continue;
                CreateTree(parent, new Vector3(pos.x, 0f, pos.y), trunkMat, foliageMat, i);
            }
        }

        private static void CreateTree(
            Transform parent, Vector3 position, Material trunkMat, Material foliageMat, int index)
        {
            float scale = Random.Range(0.85f, 1.35f);
            float trunkHeight = Random.Range(2.8f, 4.2f) * scale;

            var tree = new GameObject($"Tree_{index}");
            tree.transform.SetParent(parent, worldPositionStays: false);
            tree.transform.position = position;
            tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform, worldPositionStays: false);
            trunk.transform.localPosition = new Vector3(0f, trunkHeight * 0.5f, 0f);
            trunk.transform.localScale = new Vector3(0.35f * scale, trunkHeight * 0.5f, 0.35f * scale);
            trunk.isStatic = true;
            ApplyMaterial(trunk, trunkMat);
            Object.DestroyImmediate(trunk.GetComponent<Collider>());

            var foliage = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            foliage.name = "Foliage";
            foliage.transform.SetParent(tree.transform, worldPositionStays: false);
            float canopyHeight = Random.Range(2.2f, 3.4f) * scale;
            foliage.transform.localPosition = new Vector3(0f, trunkHeight + canopyHeight * 0.35f, 0f);
            foliage.transform.localScale = new Vector3(2.4f * scale, canopyHeight * 0.5f, 2.4f * scale);
            foliage.isStatic = true;
            ApplyMaterial(foliage, foliageMat);
            Object.DestroyImmediate(foliage.GetComponent<Collider>());

            var sway = tree.AddComponent<TreeSway>();
            SetField(sway, "foliage", foliage.transform);
            SetField(sway, "swaySpeed", Random.Range(0.65f, 1.05f));
            SetField(sway, "trunkSwayDegrees", Random.Range(1.8f, 3.2f));
            SetField(sway, "foliageSwayDegrees", Random.Range(4f, 7f));
        }

        private static GameObject CreateRuinedCover(Transform parent, Material material)
        {
            var coverRoot = CreateChild(parent, "Cover").gameObject;

            float s = MapScale;
            GameObject crate1 = CreateRuinBlock(coverRoot.transform, "Crate_1", new Vector3(12f * s, 0.6f, 10f * s),
                new Vector3(1.6f, 1.2f, 1.4f), Quaternion.Euler(0f, 18f, 4f), material);
            CreateRuinBlock(coverRoot.transform, "Crate_2", new Vector3(-18f * s, 0.9f, 14f * s),
                new Vector3(2.2f, 1.8f, 1.6f), Quaternion.Euler(-3f, -24f, 0f), material);
            CreateRuinBlock(coverRoot.transform, "Crate_3", new Vector3(22f * s, 0.75f, -16f * s),
                new Vector3(2.8f, 1.5f, 1.2f), Quaternion.Euler(2f, 35f, -5f), material);

            CreateRuinBlock(coverRoot.transform, "Debris_A", new Vector3(-8f * s, 0.35f, -20f * s),
                new Vector3(3f, 0.7f, 1.2f), Quaternion.Euler(0f, 60f, 8f), material);
            CreateRuinBlock(coverRoot.transform, "Debris_B", new Vector3(35f * s, 0.5f, 8f * s),
                new Vector3(2.4f, 1f, 2f), Quaternion.Euler(12f, -15f, 0f), material);
            CreateRuinBlock(coverRoot.transform, "Debris_C", new Vector3(-32f * s, 0.45f, -8f * s),
                new Vector3(1.8f, 0.9f, 2.6f), Quaternion.Euler(0f, 110f, 6f), material);

            CreateRuinBlock(coverRoot.transform, "Ramp", new Vector3(-20f * s, 0.45f, -22f * s),
                new Vector3(5f, 0.9f, 4f), Quaternion.Euler(0f, 25f, 0f), material);

            return crate1;
        }

        private static void CreateBoundaryRuins(Transform parent, Material material)
        {
            var boundsRoot = CreateChild(parent, "BoundaryRuins");
            float limit = ArenaHalfExtent - 1f;
            float wallLength = ArenaHalfExtent * 2f - 4f;
            int segments = Mathf.Clamp(Mathf.RoundToInt(wallLength / 8f), 14, 48);
            PlaceRuinWall(boundsRoot, "North", new Vector3(0f, 1.2f, limit), new Vector3(wallLength, 2.4f, 2f), 0f, material, segments);
            PlaceRuinWall(boundsRoot, "South", new Vector3(0f, 1.2f, -limit), new Vector3(wallLength, 2.4f, 2f), 0f, material, segments);
            PlaceRuinWall(boundsRoot, "East", new Vector3(limit, 1.2f, 0f), new Vector3(2f, 2.4f, wallLength), 90f, material, segments);
            PlaceRuinWall(boundsRoot, "West", new Vector3(-limit, 1.2f, 0f), new Vector3(2f, 2.4f, wallLength), 90f, material, segments);
        }

        private static void PlaceRuinWall(
            Transform parent, string label, Vector3 center, Vector3 totalSize, float yaw,
            Material material, int segments)
        {
            var wallRoot = CreateChild(parent, $"Wall_{label}");
            float segLength = (Mathf.Abs(totalSize.x) > Mathf.Abs(totalSize.z) ? totalSize.x : totalSize.z) / segments;
            bool alongX = Mathf.Abs(totalSize.x) > Mathf.Abs(totalSize.z);
            Vector3 segScale = alongX
                ? new Vector3(segLength * 0.85f, totalSize.y, totalSize.z)
                : new Vector3(totalSize.x, totalSize.y, segLength * 0.85f);

            for (int i = 0; i < segments; i++)
            {
                if (i % 3 == 1) continue;

                float t = (i + 0.5f) / segments - 0.5f;
                Vector3 offset = alongX
                    ? new Vector3(t * totalSize.x, 0f, 0f)
                    : new Vector3(0f, 0f, t * totalSize.z);

                CreateRuinBlock(
                    wallRoot,
                    $"Segment_{i}",
                    center + offset,
                    segScale,
                    Quaternion.Euler(Random.Range(-4f, 4f), yaw + Random.Range(-6f, 6f), Random.Range(-2f, 2f)),
                    material);
            }
        }

        private static void CreateCombatSteps(Transform parent, Material material)
        {
            var stepsRoot = CreateChild(parent, "Steps");
            float s = MapScale;
            for (int i = 0; i < 5; i++)
            {
                CreateRuinBlock(
                    stepsRoot,
                    $"Step_{i}",
                    new Vector3(-28f * s, 0.18f + i * 0.32f, 24f * s + i * 0.65f * s),
                    new Vector3(5f, 0.36f, 0.75f),
                    Quaternion.identity,
                    material);
            }
        }

        private static GameObject CreateRuinBlock(
            Transform parent, string name, Vector3 position, Vector3 size, Quaternion rotation, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, worldPositionStays: false);
            block.transform.position = position;
            block.transform.rotation = rotation;
            block.transform.localScale = size;
            block.isStatic = true;
            ApplyMaterial(block, material);
            return block;
        }

        private static void CreateDistantSmoke(Transform parent)
        {
            var smokeRoot = CreateChild(parent, "DistantSmoke");
            GameObject smokePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SmokePrefabPath);
            GameObject ribbonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RibbonSmokePath);

            Vector3[] positions = new Vector3[8];
            for (int i = 0; i < positions.Length; i++)
            {
                float angle = i * (Mathf.PI * 2f / positions.Length) + Random.Range(-0.2f, 0.2f);
                float radius = Random.Range(ArenaHalfExtent * 0.72f, ArenaHalfExtent * 0.88f);
                positions[i] = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            }

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject prefab = i % 2 == 0 ? smokePrefab : ribbonPrefab;
                if (prefab == null) continue;

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, smokeRoot);
                instance.name = $"Smoke_{i}";
                instance.transform.position = positions[i] + Vector3.up * 0.5f;
                instance.transform.localScale = Vector3.one * Random.Range(1.4f, 2.2f);
            }
        }

        private static void CreateAmbientDust(Transform parent)
        {
            var dustGo = new GameObject("AmbientDust");
            dustGo.transform.SetParent(parent, worldPositionStays: false);
            dustGo.transform.position = new Vector3(0f, 4f, 0f);

            var ps = dustGo.AddComponent<ParticleSystem>();
            dustGo.AddComponent<AmbientDust>();

            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.7f);
            main.maxParticles = Mathf.Clamp(Mathf.RoundToInt(120f * MapScale), 120, 260);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.75f, 0.62f, 0.45f, 0.35f),
                new Color(0.55f, 0.45f, 0.32f, 0.2f));

            var emission = ps.emission;
            emission.rateOverTime = 18f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(ArenaHalfExtent * 1.6f, 6f, ArenaHalfExtent * 1.6f);

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.7f, 0.58f, 0.42f), 0f),
                    new GradientColorKey(new Color(0.55f, 0.45f, 0.32f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.35f, 0.15f),
                    new GradientAlphaKey(0.15f, 1f),
                });
            colorOverLifetime.color = gradient;

            var renderer = dustGo.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            Material dustMat = EnsureMaterial("WarTorn_DustParticle.mat", new Color(0.7f, 0.58f, 0.42f, 0.35f), particle: true);
            if (dustMat != null) renderer.sharedMaterial = dustMat;
        }

        private static void WireWeather(Light sun, Transform parent)
        {
            var weatherGo = new GameObject("WarWeather");
            weatherGo.transform.SetParent(parent, worldPositionStays: false);
            var weather = weatherGo.AddComponent<WeatherManager>();
            SetField(weather, "enableLightning", false);
            SetField(weather, "directionalLight", sun);
            SetField(weather, "baseLightIntensity", sun.intensity);
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
            if (AssetDatabase.IsValidFolder(MaterialsFolder)) return;
            if (!AssetDatabase.IsValidFolder("Assets/Art"))
                AssetDatabase.CreateFolder("Assets", "Art");
            if (!AssetDatabase.IsValidFolder("Assets/Art/Environment"))
                AssetDatabase.CreateFolder("Assets/Art", "Environment");
            AssetDatabase.CreateFolder("Assets/Art/Environment", "Materials");
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
