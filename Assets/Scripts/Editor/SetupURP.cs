using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.EditorTools
{
    /// <summary>
    /// One-shot URP wiring. Creates a default <see cref="UniversalRendererData"/> +
    /// <see cref="UniversalRenderPipelineAsset"/> under <c>Assets/Settings/</c>, then
    /// assigns the pipeline asset to <see cref="GraphicsSettings.defaultRenderPipeline"/>
    /// and to every Quality Level. Auto-runs once when no pipeline is set; safe to
    /// re-run via <b>Tools → Game → Configure URP</b>.
    ///
    /// Lives in an Editor/ folder so it never ships in builds.
    /// </summary>
    public static class SetupURP
    {
        private const string SettingsFolder    = "Assets/Settings";
        private const string RendererAssetPath = SettingsFolder + "/UniversalRenderer.asset";
        private const string PipelineAssetPath = SettingsFolder + "/URP_PipelineAsset.asset";

        [InitializeOnLoadMethod]
        private static void EnsureUrpOnLoad()
        {
            if (GraphicsSettings.defaultRenderPipeline != null) return;
            EditorApplication.delayCall += () => Configure(silent: false);
        }

        [MenuItem("Tools/Game/Configure URP")]
        public static void ConfigureMenuItem() => Configure(silent: false);

        private static void Configure(bool silent)
        {
            EnsureFolder(SettingsFolder);

            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                rendererData.name = "UniversalRenderer";
                AssetDatabase.CreateAsset(rendererData, RendererAssetPath);
            }

            UniversalRenderPipelineAsset pipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (pipelineAsset == null)
            {
                pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
                pipelineAsset.name = "URP_PipelineAsset";
                AssetDatabase.CreateAsset(pipelineAsset, PipelineAssetPath);
            }

            GraphicsSettings.defaultRenderPipeline = pipelineAsset;

            int initialQuality = QualitySettings.GetQualityLevel();
            int levels = QualitySettings.names.Length;
            for (int i = 0; i < levels; i++)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                QualitySettings.renderPipeline = pipelineAsset;
            }
            QualitySettings.SetQualityLevel(initialQuality, applyExpensiveChanges: false);

            EditorUtility.SetDirty(pipelineAsset);
            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!silent)
            {
                Debug.Log($"[SetupURP] URP configured. Pipeline asset: {AssetDatabase.GetAssetPath(pipelineAsset)}");
            }
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
