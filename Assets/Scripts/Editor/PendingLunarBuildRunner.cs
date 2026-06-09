using System;
using System.IO;
using UnityEditor;

namespace Game.EditorTools
{
    /// <summary>
    /// Runs pending builds from flag files while the Unity Editor is open.
    /// </summary>
    [InitializeOnLoad]
    public static class PendingLunarBuildRunner
    {
        private const string FlagPath = "Temp/PendingLunarBuild.flag";
        private const string FullPipelineFlagPath = "Temp/RunFullPipeline.flag";
        private const string CompletePath = "Temp/PipelineComplete.txt";

        static PendingLunarBuildRunner()
        {
            EditorApplication.delayCall += TryRunPending;
            EditorApplication.update += PollPipelineFlags;
        }

        private static void PollPipelineFlags()
        {
            if (!File.Exists(FullPipelineFlagPath)) return;

            EditorApplication.update -= PollPipelineFlags;
            File.Delete(FullPipelineFlagPath);
            RunFullPipeline();
        }

        private static void TryRunPending()
        {
            if (!File.Exists(FlagPath)) return;

            File.Delete(FlagPath);
            UnityEngine.Debug.Log("[PendingLunarBuildRunner] Building lunar TestPlayground scene...");
            BuildTestScene.Build();
        }

        [MenuItem("Tools/Game/Run Full Pipeline (Scene + WebGL)")]
        public static void RunFullPipeline()
        {
            UnityEngine.Debug.Log("[PendingLunarBuildRunner] Full pipeline: scene build, then WebGL...");
            BuildTestScene.Build();
            BuildWebGLDemo.Build();
            Directory.CreateDirectory("Temp");
            File.WriteAllText(CompletePath, DateTime.UtcNow.ToString("O"));
            UnityEngine.Debug.Log("[PendingLunarBuildRunner] Full pipeline complete.");
        }

        /// <summary>Unity batchmode: -executeMethod Game.EditorTools.PendingLunarBuildRunner.RunFullPipelineFromCommandLine</summary>
        public static void RunFullPipelineFromCommandLine() => RunFullPipeline();

        public static void RequestBuild()
        {
            Directory.CreateDirectory("Temp");
            File.WriteAllText(FlagPath, "build");
            TryRunPending();
        }

        [MenuItem("Tools/Game/Execute Lunar Environment Build")]
        public static void ExecuteFromMenu() => BuildTestScene.Build();
    }
}
