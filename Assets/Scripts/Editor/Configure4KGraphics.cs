using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Game.EditorTools
{
    /// <summary>
    /// URP + quality + post-processing preset tuned for native 4K presentation.
    /// </summary>
    public static class Configure4KGraphics
    {
        private const string SettingsFolder = "Assets/Settings";
        private const string PipelineAssetPath = SettingsFolder + "/URP_PipelineAsset.asset";
        private const string RendererAssetPath = SettingsFolder + "/UniversalRenderer.asset";
        private const string VolumeProfilePath = SettingsFolder + "/Gameplay4KVolumeProfile.asset";
        private const string QualityLevelName = "4K Ultra";

        [MenuItem("Tools/Game/Configure 4K Graphics")]
        public static void ConfigureMenu()
        {
            SetupURP.ConfigureMenuItem();
            EnsureApplied(verbose: true);
        }

        public static void EnsureApplied(bool verbose = false)
        {
            EnsureQualityTier();
            UniversalRenderPipelineAsset pipeline = LoadPipeline();
            UniversalRendererData renderer = LoadRenderer();
            VolumeProfile profile = EnsureVolumeProfile();

            if (pipeline != null) ApplyPipelineSettings(pipeline, profile);
            if (renderer != null) ApplyRendererSettings(renderer);

            ActivateQualityTier();
            EnsureGlobalVolumeInActiveScene(profile);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (verbose)
                Debug.Log("[Configure4KGraphics] Applied 4K URP, post-processing, and quality settings.");
        }

        public static void ConfigureMainCamera(Camera camera)
        {
            if (camera == null) return;

            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.depthTextureMode |= DepthTextureMode.Depth;

            UniversalAdditionalCameraData urp = camera.GetComponent<UniversalAdditionalCameraData>();
            if (urp == null)
                urp = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();

            urp.renderPostProcessing = true;
            urp.antialiasing = AntialiasingMode.TemporalAntiAliasing;
            urp.antialiasingQuality = AntialiasingQuality.High;
            urp.stopNaN = true;
            urp.dithering = true;
            urp.volumeLayerMask = ~0;
        }

        public static void EnsureScenePresentation(Transform environmentRoot, Camera mainCamera)
        {
            EnsureApplied(verbose: false);
            ConfigureMainCamera(mainCamera);

            if (environmentRoot != null)
            {
                EnsureGlobalVolume(environmentRoot, LoadVolumeProfile());
                EnsureBootstrap(environmentRoot);
            }
        }

        private static void ApplyPipelineSettings(UniversalRenderPipelineAsset pipeline, VolumeProfile profile)
        {
            var so = new SerializedObject(pipeline);
            SetInt(so, "m_MSAA", 1);
            SetFloat(so, "m_RenderScale", 1f);
            SetBool(so, "m_SupportsHDR", true);
            SetInt(so, "m_HDRColorBufferPrecision", 1);
            SetBool(so, "m_RequireDepthTexture", true);
            SetBool(so, "m_RequireOpaqueTexture", false);
            SetInt(so, "m_MainLightShadowmapResolution", 4096);
            SetInt(so, "m_AdditionalLightsPerObjectLimit", 8);
            SetFloat(so, "m_ShadowDistance", 400f);
            SetInt(so, "m_ShadowCascadeCount", 4);
            SetBool(so, "m_SoftShadowsSupported", true);
            SetInt(so, "m_SoftShadowQuality", 3);
            SetBool(so, "m_ReflectionProbeBlending", true);
            SetBool(so, "m_ReflectionProbeBoxProjection", true);
            SetInt(so, "m_ColorGradingLutSize", 32);
            SetBool(so, "m_UseAdaptivePerformance", false);
            so.FindProperty("m_VolumeProfile").objectReferenceValue = profile;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
        }

        private static void ApplyRendererSettings(UniversalRendererData renderer)
        {
            var so = new SerializedObject(renderer);
            SetInt(so, "m_RenderingMode", 0);
            SetInt(so, "m_DepthPrimingMode", 1);
            SetInt(so, "m_CopyDepthMode", 1);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(renderer);
        }

        private static VolumeProfile EnsureVolumeProfile()
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                EnsureFolder(SettingsFolder);
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "Gameplay4KVolumeProfile";
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }

            ConfigureTonemapping(profile);
            ConfigureBloom(profile);
            ConfigureColorAdjustments(profile);
            ConfigureVignette(profile);
            ConfigureFilmGrain(profile);

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void ConfigureTonemapping(VolumeProfile profile)
        {
            if (!profile.TryGet(out Tonemapping tonemapping))
                tonemapping = profile.Add<Tonemapping>(true);

            tonemapping.active = true;
            tonemapping.mode.Override(TonemappingMode.ACES);
        }

        private static void ConfigureBloom(VolumeProfile profile)
        {
            if (!profile.TryGet(out Bloom bloom))
                bloom = profile.Add<Bloom>(true);

            bloom.active = true;
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.28f);
            bloom.scatter.Override(0.72f);
            bloom.clamp.Override(65472f);
            bloom.highQualityFiltering.Override(true);
        }

        private static void ConfigureColorAdjustments(VolumeProfile profile)
        {
            if (!profile.TryGet(out ColorAdjustments color))
                color = profile.Add<ColorAdjustments>(true);

            color.active = true;
            color.postExposure.Override(0.08f);
            color.contrast.Override(8f);
            color.saturation.Override(8f);
            color.colorFilter.Override(Color.white);
        }

        private static void ConfigureVignette(VolumeProfile profile)
        {
            if (!profile.TryGet(out Vignette vignette))
                vignette = profile.Add<Vignette>(true);

            vignette.active = true;
            vignette.intensity.Override(0.18f);
            vignette.smoothness.Override(0.35f);
        }

        private static void ConfigureFilmGrain(VolumeProfile profile)
        {
            if (!profile.TryGet(out FilmGrain grain))
                grain = profile.Add<FilmGrain>(true);

            grain.active = true;
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.intensity.Override(0.08f);
            grain.response.Override(0.45f);
        }

        private static void EnsureQualityTier()
        {
            if (TryGetQualityIndex(QualityLevelName, out _)) return;
            if (!TryGetQualityIndex("Ultra", out int ultraIndex)) return;
            RenameQualityLevel(ultraIndex, QualityLevelName);
        }

        private static void ActivateQualityTier()
        {
            if (!TryGetQualityIndex(QualityLevelName, out int index)
                && !TryGetQualityIndex("Ultra", out index))
            {
                index = QualitySettings.names.Length - 1;
            }

            QualitySettings.SetQualityLevel(index, applyExpensiveChanges: false);
            QualitySettings.pixelLightCount = 4;
            QualitySettings.shadows = UnityEngine.ShadowQuality.All;
            QualitySettings.shadowResolution = UnityEngine.ShadowResolution.VeryHigh;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.shadowCascades = 4;
            QualitySettings.shadowDistance = 400f;
            QualitySettings.shadowNearPlaneOffset = 2f;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.antiAliasing = 0;
            QualitySettings.softParticles = true;
            QualitySettings.realtimeReflectionProbes = true;
            QualitySettings.lodBias = 2.5f;
            QualitySettings.maximumLODLevel = 0;
            QualitySettings.vSyncCount = 1;
            QualitySettings.renderPipeline = LoadPipeline();
            QualitySettings.SetQualityLevel(index, applyExpensiveChanges: true);
            SetStandaloneDefaultQuality(index);
        }

        private static bool TryGetQualityIndex(string levelName, out int index)
        {
            string[] names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] != levelName) continue;
                index = i;
                return true;
            }

            index = -1;
            return false;
        }

        private static void RenameQualityLevel(int index, string newName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if (assets == null || assets.Length == 0) return;

            var so = new SerializedObject(assets[0]);
            SerializedProperty levels = so.FindProperty("m_QualitySettings");
            if (levels == null || index < 0 || index >= levels.arraySize) return;

            SerializedProperty nameProp = levels.GetArrayElementAtIndex(index).FindPropertyRelative("name");
            if (nameProp == null) return;

            nameProp.stringValue = newName;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetStandaloneDefaultQuality(int qualityIndex)
        {
            var qualityAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if (qualityAsset == null || qualityAsset.Length == 0) return;

            var so = new SerializedObject(qualityAsset[0]);
            SerializedProperty perPlatform = so.FindProperty("m_PerPlatformDefaultQuality");
            if (perPlatform != null)
            {
                SerializedProperty standalone = perPlatform.FindPropertyRelative("Standalone");
                if (standalone != null)
                    standalone.intValue = qualityIndex;
            }

            SerializedProperty current = so.FindProperty("m_CurrentQuality");
            if (current != null)
                current.intValue = qualityIndex;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureGlobalVolumeInActiveScene(VolumeProfile profile)
        {
            if (profile == null) return;
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform env = root.transform.Find("Environment");
                if (env != null)
                {
                    EnsureGlobalVolume(env, profile);
                    EnsureBootstrap(env);
                    return;
                }
            }
        }

        private static void EnsureGlobalVolume(Transform parent, VolumeProfile profile)
        {
            if (profile == null) return;

            Transform existing = parent.Find("GlobalVolume_4K");
            if (existing != null) return;

            var go = new GameObject("GlobalVolume_4K");
            go.transform.SetParent(parent, worldPositionStays: false);
            Volume volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.weight = 1f;
            volume.profile = profile;
        }

        private static void EnsureBootstrap(Transform parent)
        {
            if (Object.FindAnyObjectByType<Game.Rendering.Graphics4KBootstrap>() != null)
                return;

            var go = new GameObject("Graphics4KBootstrap");
            go.transform.SetParent(parent, worldPositionStays: false);
            go.AddComponent<Game.Rendering.Graphics4KBootstrap>();
        }

        private static UniversalRenderPipelineAsset LoadPipeline() =>
            AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);

        private static UniversalRendererData LoadRenderer() =>
            AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);

        private static VolumeProfile LoadVolumeProfile() =>
            AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);

        private static void SetInt(SerializedObject so, string property, int value)
        {
            SerializedProperty prop = so.FindProperty(property);
            if (prop != null) prop.intValue = value;
        }

        private static void SetFloat(SerializedObject so, string property, float value)
        {
            SerializedProperty prop = so.FindProperty(property);
            if (prop != null) prop.floatValue = value;
        }

        private static void SetBool(SerializedObject so, string property, bool value)
        {
            SerializedProperty prop = so.FindProperty(property);
            if (prop != null) prop.boolValue = value;
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
