using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Copies the Osprey X-80 FBX and PBR textures into the project, configures import
    /// settings, and creates a URP Lit material.
    /// </summary>
    public static class ImportOspreyAircraft
    {
        public const string OspreyRootFolder   = "Assets/Art/Vehicles/OspreyX80";
        public const string ModelsFolder       = OspreyRootFolder + "/Models";
        public const string TexturesFolder     = OspreyRootFolder + "/Textures";
        public const string MaterialsFolder    = OspreyRootFolder + "/Materials";
        public const string OspreyFbxPath      = ModelsFolder + "/Osprey.fbx";
        public const string OspreyMaterialPath = MaterialsFolder + "/Osprey.mat";

        public const string DefaultSourceRoot =
            @"c:\Users\judah\Desktop\-\Annex\Relax Time\PC Related Stuff\Games\[NEW] ACTIVE\Built Games\Creation Kit\[Models]\[Models] Aircraft\osprey-x-80";

        public const string DefaultSourceFbx =
            DefaultSourceRoot + @"\source\Osprey.fbx";

        public const string DefaultSourceTextures =
            DefaultSourceRoot + @"\textures";

        [MenuItem("Tools/Game/Import Osprey Aircraft")]
        public static GameObject ImportMenu()
        {
            GameObject model = EnsureImported(forceReimport: true);
            if (model != null)
                Debug.Log("[ImportOspreyAircraft] Osprey model and material ready.");
            return model;
        }

        public static GameObject EnsureImported(bool forceReimport = false)
        {
            EnsureFolder(ModelsFolder);
            EnsureFolder(TexturesFolder);
            EnsureFolder(MaterialsFolder);

            CopyFromSourceIfMissing();
            RefreshAssets();

            if (!AssetExists(OspreyFbxPath))
            {
                Debug.LogError($"[ImportOspreyAircraft] Missing {OspreyFbxPath}. " +
                               "Place the FBX or run Import after verifying the source path.");
                return null;
            }

            ConfigureFbxImport(forceReimport);
            EnsureMaterial();
            RefreshAssets();

            return AssetDatabase.LoadAssetAtPath<GameObject>(OspreyFbxPath);
        }

        public static Material EnsureMaterial()
        {
            EnsureFolder(MaterialsFolder);
            RefreshAssets();

            Texture2D albedo    = LoadTexture("Osprey_Albedo");
            Texture2D normal    = LoadTexture("Osprey_Normal", normalMap: true);
            Texture2D metallic  = LoadTexture("Osprey_Metalic");
            Texture2D roughness = LoadTexture("Osprey_Roughness");
            Texture2D ao        = LoadTexture("Osprey_AO");

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(OspreyMaterialPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard");
                mat = new Material(shader) { name = "Osprey" };
                AssetDatabase.CreateAsset(mat, OspreyMaterialPath);
            }

            if (albedo != null)
            {
                mat.SetTexture("_BaseMap", albedo);
                mat.SetColor("_BaseColor", Color.white);
            }

            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }

            if (ao != null)
                mat.SetTexture("_OcclusionMap", ao);

            if (metallic != null)
            {
                mat.SetTexture("_MetallicGlossMap", metallic);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            else
            {
                mat.SetFloat("_Metallic", 0.75f);
            }

            if (roughness != null)
            {
                float smoothness = EstimateSmoothnessFromRoughness(roughness);
                mat.SetFloat("_Smoothness", smoothness);
            }
            else
            {
                mat.SetFloat("_Smoothness", 0.42f);
            }

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static void CopyFromSourceIfMissing()
        {
            CopyFbxIfMissing();
            CopyTexturesIfMissing();
        }

        private static void CopyFbxIfMissing()
        {
            if (AssetExists(OspreyFbxPath))
                return;

            if (!File.Exists(DefaultSourceFbx))
            {
                Debug.LogWarning($"[ImportOspreyAircraft] Source FBX not found at:\n{DefaultSourceFbx}");
                return;
            }

            string destPath = ProjectPath(OspreyFbxPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath) ?? ProjectRoot);
            File.Copy(DefaultSourceFbx, destPath, overwrite: true);
            CopyEmbeddedTextures(DefaultSourceFbx);
            Debug.Log($"[ImportOspreyAircraft] Copied FBX to {OspreyFbxPath}");
        }

        private static void CopyTexturesIfMissing()
        {
            if (!Directory.Exists(DefaultSourceTextures))
            {
                Debug.LogWarning($"[ImportOspreyAircraft] Source textures folder not found at:\n{DefaultSourceTextures}");
                return;
            }

            string destDir = ProjectPath(TexturesFolder);
            Directory.CreateDirectory(destDir);

            int copied = 0;
            foreach (string source in Directory.GetFiles(DefaultSourceTextures))
            {
                string ext = Path.GetExtension(source).ToLowerInvariant();
                if (ext is not ".png" and not ".jpg" and not ".jpeg" and not ".tga")
                    continue;

                string destFile = Path.Combine(destDir, Path.GetFileName(source));
                if (File.Exists(destFile))
                    continue;

                File.Copy(source, destFile, overwrite: true);
                copied++;
            }

            if (copied > 0)
                Debug.Log($"[ImportOspreyAircraft] Copied {copied} texture(s) to {TexturesFolder}");
        }

        private static void CopyEmbeddedTextures(string sourceFbxPath)
        {
            string sourceDir = Path.GetDirectoryName(sourceFbxPath) ?? string.Empty;
            string fbmName = Path.GetFileNameWithoutExtension(sourceFbxPath) + ".fbm";
            string sourceFbm = Path.Combine(sourceDir, fbmName);
            if (!Directory.Exists(sourceFbm))
                return;

            string destFbm = Path.Combine(ProjectPath(TexturesFolder), fbmName);
            Directory.CreateDirectory(destFbm);

            foreach (string file in Directory.GetFiles(sourceFbm))
            {
                string destFile = Path.Combine(destFbm, Path.GetFileName(file));
                if (!File.Exists(destFile))
                    File.Copy(file, destFile, overwrite: true);
            }
        }

        private static void ConfigureFbxImport(bool forceReimport)
        {
            var importer = AssetImporter.GetAtPath(OspreyFbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError("[ImportOspreyAircraft] Osprey FBX has no ModelImporter.");
                return;
            }

            bool needsReimport =
                forceReimport
                || importer.globalScale != 1f
                || importer.animationType != ModelImporterAnimationType.Generic
                || importer.importAnimation;

            if (!needsReimport)
                return;

            importer.globalScale       = 1f;
            importer.bakeAxisConversion = true;
            importer.importAnimation   = false;
            importer.animationType     = ModelImporterAnimationType.Generic;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.SaveAndReimport();
        }

        private static Texture2D LoadTexture(string baseName, bool normalMap = false)
        {
            foreach (string ext in new[] { ".jpeg", ".jpg", ".png", ".tga" })
            {
                string assetPath = $"{TexturesFolder}/{baseName}{ext}";
                if (!AssetExists(assetPath))
                    continue;

                ConfigureTextureImport(assetPath, normalMap);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            }

            return null;
        }

        private static void ConfigureTextureImport(string assetPath, bool normalMap)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;

            bool changed = false;

            if (normalMap && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                changed = true;
            }
            else if (!normalMap && importer.textureType == TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.Default;
                changed = true;
            }

            if (!normalMap && importer.sRGBTexture != (assetPath.Contains("Albedo", System.StringComparison.OrdinalIgnoreCase)))
            {
                importer.sRGBTexture = assetPath.Contains("Albedo", System.StringComparison.OrdinalIgnoreCase);
                changed = true;
            }

            if (changed)
                importer.SaveAndReimport();
        }

        private static float EstimateSmoothnessFromRoughness(Texture2D roughness)
        {
            if (roughness == null)
                return 0.42f;

            try
            {
                RenderTexture rt = RenderTexture.GetTemporary(4, 4, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(roughness, rt);

                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = rt;

                Texture2D sample = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                sample.ReadPixels(new Rect(0, 0, 4, 4), 0, 0);
                sample.Apply();

                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);

                Color[] pixels = sample.GetPixels();
                float avgRoughness = 0f;
                for (int i = 0; i < pixels.Length; i++)
                    avgRoughness += pixels[i].grayscale;
                avgRoughness /= pixels.Length;

                Object.DestroyImmediate(sample);
                return Mathf.Clamp01(1f - avgRoughness);
            }
            catch
            {
                return 0.42f;
            }
        }

        public static void ApplyMaterialToRenderers(GameObject root, Material material)
        {
            if (root == null || material == null)
                return;

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = material;
                renderer.sharedMaterials = mats;
            }
        }

        private static string ProjectRoot =>
            Path.GetDirectoryName(Application.dataPath) ?? string.Empty;

        private static string ProjectPath(string assetPath) =>
            Path.Combine(ProjectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));

        private static bool AssetExists(string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
                return true;

            return File.Exists(ProjectPath(assetPath));
        }

        private static void RefreshAssets()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
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
