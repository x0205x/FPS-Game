using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Ensures main menu assets, Resources copies, and Build Settings are configured.
    /// The scene UI is built at runtime by <see cref="Game.UI.MainMenuBootstrap"/>.
    /// </summary>
    public static class BuildMainMenuScene
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string PrologueScenePath = "Assets/Scenes/TestPlayground.unity";
        private const string ArtFolder = "Assets/Art/UI/MainMenu";
        private const string ResourcesFolder = "Assets/Resources/UI/MainMenu";
        private const string BackgroundsArtFolder = ArtFolder + "/Backgrounds";
        private const string BackgroundsResourcesFolder = ResourcesFolder + "/Backgrounds";
        private const string MenuMusicArtPath = "Assets/Art/Audio/MainMenu/menu_theme.mp3";
        private const string MenuMusicResourcesPath = ResourcesFolder + "/Music/menu_theme.mp3";

        [MenuItem("Tools/Game/Build Main Menu Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureBackgroundArt();
            EnsureResourcesArt();
            AssetDatabase.Refresh();
            ConfigureBuildSettings();

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Debug.Log($"[BuildMainMenuScene] Opened {ScenePath}. UI builds at runtime via MainMenuBootstrap.");
            }
            else
            {
                Debug.LogWarning($"[BuildMainMenuScene] {ScenePath} is missing.");
            }
        }

        private static void EnsureBackgroundArt()
        {
            EnsureFolder("Assets/Art/UI");
            EnsureFolder(ArtFolder);
            EnsureFolder(BackgroundsArtFolder);
        }

        private static void EnsureResourcesArt()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/UI");
            EnsureFolder(ResourcesFolder);
            EnsureFolder(BackgroundsResourcesFolder);

            CopyBackgroundSlides();

            EnsureFolder(ResourcesFolder + "/Music");
            CopyAssetFile(MenuMusicArtPath, MenuMusicResourcesPath);
        }

        private static void CopyBackgroundSlides()
        {
            string artFull = Path.GetFullPath(BackgroundsArtFolder);
            if (!Directory.Exists(artFull)) return;

            foreach (string source in Directory.GetFiles(artFull, "background_slide_*.*"))
            {
                string fileName = Path.GetFileName(source);
                CopyAssetFile(BackgroundsArtFolder + "/" + fileName, BackgroundsResourcesFolder + "/" + fileName);
            }
        }

        private static void CopyAssetFile(string sourceAssetPath, string destAssetPath)
        {
            string sourceFull = Path.GetFullPath(sourceAssetPath);
            if (!File.Exists(sourceFull)) return;

            string destFull = Path.GetFullPath(destAssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destFull)!);
            File.Copy(sourceFull, destFull, overwrite: true);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(PrologueScenePath, true),
            };
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
