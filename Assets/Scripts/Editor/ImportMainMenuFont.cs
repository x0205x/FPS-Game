using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Game.EditorTools
{
    /// <summary>
    /// Builds the Cinzel TMP font asset used by the main menu labels.
    /// </summary>
    public static class ImportMainMenuFont
    {
        private const string TtfPath = "Assets/Art/UI/MainMenu/Fonts/Cinzel-Regular.ttf";
        private const string FontAssetPath = "Assets/Resources/UI/MainMenu/MenuFont.asset";
        private const string MenuCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ ";

        [InitializeOnLoadMethod]
        private static void EnsureFontOnLoad()
        {
            EditorApplication.delayCall += EnsureFont;
        }

        [MenuItem("Tools/Game/Import Main Menu Font")]
        public static void EnsureFontMenu() => EnsureFont();

        private static void EnsureFont()
        {
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (IsFontAssetValid(existing)) return;

            if (existing != null)
            {
                AssetDatabase.DeleteAsset(FontAssetPath);
                AssetDatabase.Refresh();
            }

            if (!AssetExists(TtfPath))
            {
                Debug.LogWarning($"[ImportMainMenuFont] Missing font at {TtfPath}");
                return;
            }

            if (!LooksLikeTrueTypeFont(TtfPath))
            {
                Debug.LogWarning(
                    $"[ImportMainMenuFont] {TtfPath} is not a valid TTF/OTF file. " +
                    "Replace it with a real Cinzel-Regular.ttf download.");
                return;
            }

            EnsureFontImportSettings(TtfPath);
            EnsureFolder("Assets/Resources/UI/MainMenu");

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
            if (sourceFont == null)
            {
                Debug.LogWarning($"[ImportMainMenuFont] Could not load Unity Font at {TtfPath}");
                return;
            }

            if (!CreateAndSaveFontAsset(sourceFont))
            {
                Debug.LogWarning(
                    "[ImportMainMenuFont] Could not build TMP font. Menu text will fall back to the default TMP font.");
            }
        }

        private static bool CreateAndSaveFontAsset(Font sourceFont)
        {
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic);

            if (fontAsset == null)
                return false;

            fontAsset.name = "MenuFont";
            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            EmbedFontSubAssets(fontAsset);

            fontAsset.TryAddCharacters(MenuCharacters, out string missing);
            if (!string.IsNullOrEmpty(missing))
                Debug.LogWarning($"[ImportMainMenuFont] Missing glyphs: {missing}");

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(FontAssetPath);

            Debug.Log($"[ImportMainMenuFont] Created {FontAssetPath}");
            return IsFontAssetValid(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath));
        }

        private static void EmbedFontSubAssets(TMP_FontAsset fontAsset)
        {
            if (fontAsset.atlasTextures != null)
            {
                for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                {
                    Texture2D atlas = fontAsset.atlasTextures[i];
                    if (atlas == null) continue;
                    atlas.name = $"MenuFont Atlas {i}";
                    AssetDatabase.AddObjectToAsset(atlas, fontAsset);
                }
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = "MenuFont Atlas Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }
        }

        private static bool IsFontAssetValid(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null) return false;
            if (fontAsset.material == null) return false;
            if (fontAsset.atlasTextures == null || fontAsset.atlasTextures.Length == 0) return false;
            return fontAsset.atlasTextures[0] != null;
        }

        private static void EnsureFontImportSettings(string ttfAssetPath)
        {
            var importer = AssetImporter.GetAtPath(ttfAssetPath) as TrueTypeFontImporter;
            if (importer == null) return;

            bool changed = false;
            if (!importer.includeFontData)
            {
                importer.includeFontData = true;
                changed = true;
            }

            if (changed)
                importer.SaveAndReimport();
            else
                AssetDatabase.ImportAsset(ttfAssetPath, ImportAssetOptions.ForceUpdate);
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

        private static bool AssetExists(string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
                return true;

            string fullPath = Path.Combine(
                Path.GetDirectoryName(Application.dataPath) ?? string.Empty,
                assetPath.Replace('/', Path.DirectorySeparatorChar));

            return File.Exists(fullPath);
        }

        private static bool LooksLikeTrueTypeFont(string assetPath)
        {
            string fullPath = Path.Combine(
                Path.GetDirectoryName(Application.dataPath) ?? string.Empty,
                assetPath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(fullPath)) return false;

            byte[] header = new byte[4];
            using (var stream = File.OpenRead(fullPath))
            {
                if (stream.Read(header, 0, header.Length) < header.Length)
                    return false;
            }

            return (header[0] == 0x00 && header[1] == 0x01 && header[2] == 0x00 && header[3] == 0x00)
                || (header[0] == (byte)'O' && header[1] == (byte)'T' && header[2] == (byte)'T' && header[3] == (byte)'O')
                || (header[0] == (byte)'t' && header[1] == (byte)'r' && header[2] == (byte)'u' && header[3] == (byte)'e');
        }
    }
}
