using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Removes broken script references from legacy AlterunaFPS prefabs so Unity
    /// can save scenes and prefab stages again.
    /// </summary>
    public static class FixAlterunaPrefabs
    {
        private const string PrefabRoot = "Assets/AlterunaFPS";

        [MenuItem("Tools/Game/Fix Alteruna Missing Scripts")]
        public static void FixMenu()
        {
            int removed = FixAll();
            Debug.Log(removed > 0
                ? $"[FixAlterunaPrefabs] Removed {removed} missing script(s) under {PrefabRoot}."
                : $"[FixAlterunaPrefabs] No missing scripts found under {PrefabRoot}.");
        }

        /// <summary>Strip missing scripts and return to the main stage if needed.</summary>
        public static void PrepareEditorForSceneBuild()
        {
            FixAll();

            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null) return;

            if (!string.IsNullOrEmpty(stage.assetPath))
                AssetDatabase.ImportAsset(stage.assetPath, ImportAssetOptions.ForceUpdate);

            try
            {
                StageUtility.GoToMainStage();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[FixAlterunaPrefabs] Could not return to main stage: {ex.Message}");
            }
        }

        public static int FixAll()
        {
            int totalRemoved = 0;
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                totalRemoved += FixPrefab(path);
            }

            if (totalRemoved > 0)
                AssetDatabase.SaveAssets();
            return totalRemoved;
        }

        private static int FixPrefab(string assetPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(assetPath);
            if (root == null) return 0;

            int removed = RemoveMissingRecursively(root);
            if (removed > 0)
                PrefabUtility.SaveAsPrefabAsset(root, assetPath);

            PrefabUtility.UnloadPrefabContents(root);
            return removed;
        }

        private static int RemoveMissingRecursively(GameObject go)
        {
            int removed = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (removed > 0)
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

            foreach (Transform child in go.transform)
                removed += RemoveMissingRecursively(child.gameObject);

            return removed;
        }
    }
}
