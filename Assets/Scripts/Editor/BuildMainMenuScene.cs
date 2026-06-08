using System.IO;
using Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.EditorTools
{
    /// <summary>
    /// Ensures main menu assets, Resources copies, scene bootstrap, and Build Settings.
    /// The scene UI is built at runtime by <see cref="MainMenuBootstrap"/>.
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
            FixAlterunaPrefabs.PrepareEditorForSceneBuild();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureBackgroundArt();
            EnsureResourcesArt();
            AssetDatabase.Refresh();

            Scene scene;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EnsureFolder("Assets/Scenes");
            }
            else
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            EnsureMenuBootstrapInScene();
            EditorSceneManager.SaveScene(scene, ScenePath);
            ConfigureBuildSettings();

            Debug.Log(
                $"[BuildMainMenuScene] Saved {ScenePath}. Press Play on MainMenu — UI builds at runtime via MainMenuBootstrap.");
        }

        private static void EnsureMenuBootstrapInScene()
        {
            GameObject bootstrap = GameObject.Find("MenuBootstrap");
            if (bootstrap != null && bootstrap.GetComponent<MainMenuBootstrap>() != null)
                return;

            if (bootstrap != null)
                Object.DestroyImmediate(bootstrap);

            bootstrap = new GameObject("MenuBootstrap");
            bootstrap.AddComponent<MainMenuBootstrap>();
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
