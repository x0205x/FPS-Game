using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Builds a WebGL player into <c>docs/</c> for GitHub Pages hosting.
    /// Preserves <c>docs/index.html</c> shell unless Unity overwrites it — re-copy from repo after build if needed.
    /// </summary>
    public static class BuildWebGLDemo
    {
        private const string OutputFolder = "docs";
        private const string MainMenuScene = "Assets/Scenes/MainMenu.unity";

        [MenuItem("Tools/Game/Build WebGL Demo")]
        public static void Build()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            string outputPath = Path.Combine(projectRoot, OutputFolder);

            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            // GitHub Pages does not set Content-Encoding: gzip for .gz assets.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

            string[] scenes = ResolveScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("[BuildWebGLDemo] No scenes in Build Settings.");
                return;
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[BuildWebGLDemo] Build failed: {report.summary.result}");
                return;
            }

            Debug.Log($"[BuildWebGLDemo] WebGL demo built to {outputPath}. Commit and push docs/ for GitHub Pages.");
        }

        private static string[] ResolveScenes()
        {
            var scenes = new System.Collections.Generic.List<string>();
            foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
            {
                if (entry.enabled && !string.IsNullOrEmpty(entry.path))
                    scenes.Add(entry.path);
            }

            if (scenes.Count == 0 && File.Exists(Path.Combine(Application.dataPath, "../", MainMenuScene).Replace('\\', '/')))
                scenes.Add(MainMenuScene);

            return scenes.ToArray();
        }
    }
}
