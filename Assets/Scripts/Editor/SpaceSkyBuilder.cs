using System.IO;
using Game.Environment;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.EditorTools
{
    /// <summary>
    /// Creates a star-field skybox and distant planets for outer-space horizons.
    /// </summary>
    public static class SpaceSkyBuilder
    {
        private const string TexturePath = "Assets/Art/Environment/Textures/SpaceStarsPanorama.png";
        private const string MaterialPath = "Assets/Art/Environment/Materials/SpaceSkybox.mat";
        private const string PlanetsFolder = "Assets/Art/Environment/Materials";

        public static void Build(Transform parent, float skyDistance = 900f)
        {
            ApplyRenderSettings(lunar: false);
            CreatePlanets(parent, skyDistance);
        }

        public static void BuildLunar(
            Transform parent, float skyDistance, Material shipHullMaterial, Material shipAccentMaterial,
            GameObject[] orbitalShipPrefabs = null)
        {
            ApplyRenderSettings(lunar: true);
            CreatePlanets(parent, skyDistance);
            CreateOrbitalTraffic(parent, skyDistance, shipHullMaterial, shipAccentMaterial, orbitalShipPrefabs);
        }

        private static void ApplyRenderSettings(bool lunar)
        {
            Material sky = EnsureSkyboxMaterial();
            if (sky != null)
                RenderSettings.skybox = sky;

            if (lunar)
            {
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.06f, 0.06f, 0.08f);
                RenderSettings.ambientEquatorColor = new Color(0.04f, 0.04f, 0.05f);
                RenderSettings.ambientGroundColor = new Color(0.12f, 0.12f, 0.13f);

                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogDensity = 0.00045f;
                RenderSettings.fogColor = new Color(0.04f, 0.04f, 0.05f);
            }
            else
            {
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.10f, 0.12f, 0.22f);
                RenderSettings.ambientEquatorColor = new Color(0.06f, 0.07f, 0.12f);
                RenderSettings.ambientGroundColor = new Color(0.18f, 0.14f, 0.10f);

                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogDensity = 0.0016f;
                RenderSettings.fogColor = new Color(0.42f, 0.34f, 0.28f);
            }
        }

        private static void CreateOrbitalTraffic(
            Transform parent, float skyDistance, Material hullMat, Material accentMat, GameObject[] shipPrefabs)
        {
            var trafficGo = new GameObject("OrbitalTraffic");
            trafficGo.transform.SetParent(parent, worldPositionStays: false);
            var traffic = trafficGo.AddComponent<OrbitalTrafficController>();
            SetField(traffic, "skyRadius", skyDistance);
            SetField(traffic, "shipHullMaterial", hullMat);
            SetField(traffic, "shipAccentMaterial", accentMat);
            SetFieldArray(traffic, "shipPrefabs", shipPrefabs);
        }

        private static void CreatePlanets(Transform parent, float skyDistance)
        {
            var skyRoot = new GameObject("SpaceSky");
            skyRoot.transform.SetParent(parent, worldPositionStays: false);
            skyRoot.AddComponent<SpaceSkyController>();
            SetField(skyRoot.GetComponent<SpaceSkyController>(), "skyDistance", skyDistance);

            (Vector3 dir, float size, Color color, string name)[] planets =
            {
                (new Vector3(0.25f, 0.92f, 0.18f), 95f, new Color(0.85f, 0.35f, 0.18f), "Planet_Mars"),
                (new Vector3(-0.55f, 0.78f, 0.28f), 140f, new Color(0.92f, 0.72f, 0.42f), "Planet_GasGiant"),
                (new Vector3(0.62f, 0.55f, -0.42f), 70f, new Color(0.35f, 0.55f, 0.95f), "Planet_Ice"),
                (new Vector3(-0.18f, 0.88f, -0.35f), 55f, new Color(0.55f, 0.58f, 0.62f), "Planet_Moon"),
                (new Vector3(0.78f, 0.35f, 0.52f), 110f, new Color(0.78f, 0.42f, 0.22f), "Planet_Rust"),
            };

            for (int i = 0; i < planets.Length; i++)
            {
                var def = planets[i];
                Vector3 direction = def.dir.normalized;
                CreatePlanet(skyRoot.transform, def.name, direction * skyDistance, def.size, def.color);
            }
        }

        private static void CreatePlanet(
            Transform parent, string name, Vector3 position, float diameter, Color baseColor)
        {
            var planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planet.name = name;
            planet.transform.SetParent(parent, worldPositionStays: false);
            planet.transform.position = position;
            planet.transform.localScale = Vector3.one * diameter;
            Object.DestroyImmediate(planet.GetComponent<Collider>());

            Material mat = EnsurePlanetMaterial(name, baseColor);
            if (planet.TryGetComponent<Renderer>(out var renderer))
                renderer.sharedMaterial = mat;
        }

        private static Material EnsureSkyboxMaterial()
        {
            EnsureFolder("Assets/Art/Environment/Textures");

            string fullTexPath = Path.GetFullPath(TexturePath);
            if (!File.Exists(fullTexPath))
            {
                Texture2D stars = GenerateStarPanorama(2048, 1024);
                Directory.CreateDirectory(Path.GetDirectoryName(fullTexPath)!);
                File.WriteAllBytes(fullTexPath, stars.EncodeToPNG());
                Object.DestroyImmediate(stars);
                AssetDatabase.ImportAsset(TexturePath);
            }

            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null) return existing;

            EnsureFolder(PlanetsFolder);
            Shader shader = Shader.Find("Skybox/Panoramic");
            if (shader == null) shader = Shader.Find("Skybox/6 Sided");

            var mat = new Material(shader);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (tex != null)
            {
                mat.SetTexture("_MainTex", tex);
                mat.SetTexture("_Tex", tex);
            }

            mat.SetColor("_Tint", new Color(0.55f, 0.65f, 1f, 1f));
            mat.SetFloat("_Exposure", 1.15f);
            mat.SetFloat("_Rotation", 35f);
            AssetDatabase.CreateAsset(mat, MaterialPath);
            return mat;
        }

        private static Material EnsurePlanetMaterial(string name, Color baseColor)
        {
            string fileName = $"SpacePlanet_{name}.mat";
            string path = $"{PlanetsFolder}/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var mat = new Material(shader);
            Color emissive = baseColor * 0.35f;
            mat.SetColor("_BaseColor", baseColor);
            mat.SetColor("_Color", baseColor);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissive);
            }

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static Texture2D GenerateStarPanorama(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color(0.008f, 0.012f, 0.035f);

            var rng = new System.Random(73129);
            int stars = 4200;
            for (int i = 0; i < stars; i++)
            {
                int x = rng.Next(width);
                int y = rng.Next(height);
                float brightness = 0.45f + (float)rng.NextDouble() * 0.55f;
                float tint = 0.9f + (float)rng.NextDouble() * 0.2f;
                pixels[y * width + x] = new Color(brightness * tint, brightness, brightness);

                if (x + 1 < width)
                    pixels[y * width + x + 1] = Color.Lerp(pixels[y * width + x + 1], pixels[y * width + x], 0.65f);
                if (y + 1 < height)
                    pixels[(y + 1) * width + x] = Color.Lerp(pixels[(y + 1) * width + x], pixels[y * width + x], 0.65f);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
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
            if (target == null) return;
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
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

        private static void SetFieldArray(Object target, string fieldName, Object[] values)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null || !prop.isArray) return;
            prop.arraySize = values?.Length ?? 0;
            if (values == null) return;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
