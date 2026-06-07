using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Creates URP Lit materials for the Spartan character and applies them via
    /// <see cref="CharacterMaterialStyler"/>.
    /// </summary>
    public static class SetupSpartanMaterials
    {
        public const string MaterialsFolder = "Assets/Art/Materials/Characters";
        public const string ArmorGreenPath  = MaterialsFolder + "/M_Spartan_Armor_Green.mat";
        public const string UndersuitBlackPath = MaterialsFolder + "/M_Spartan_Undersuit_Black.mat";
        public const string HelmetDarkGreenPath = MaterialsFolder + "/M_Spartan_Helmet_DarkGreen.mat";

        [MenuItem("Tools/Game/Setup Spartan Character Materials")]
        public static void SetupFromMenu()
        {
            EnsureMaterials(out Material green, out Material black, out Material helmet);
            AssetDatabase.SaveAssets();
            Debug.Log("[SetupSpartanMaterials] Created/updated Spartan materials in " + MaterialsFolder);
        }

        [MenuItem("Tools/Game/Apply Spartan Colors To Selection")]
        public static void ApplyToSelection()
        {
            EnsureMaterials(out Material green, out Material black, out Material helmet);

            int count = 0;
            foreach (GameObject go in Selection.gameObjects)
            {
                var styler = go.GetComponent<Game.Player.CharacterMaterialStyler>();
                if (styler == null)
                    styler = go.AddComponent<Game.Player.CharacterMaterialStyler>();

                WireStyler(styler, green, black, helmet);
                styler.Apply();
                count++;
            }

            Debug.Log($"[SetupSpartanMaterials] Applied colors to {count} object(s).");
        }

        public static void EnsureMaterials(out Material armorGreen, out Material undersuitBlack, out Material helmetDarkGreen)
        {
            EnsureFolder(MaterialsFolder);

            armorGreen = LoadOrCreate(
                ArmorGreenPath,
                new Color(0.10f, 0.36f, 0.12f),
                metallic: 0.55f,
                smoothness: 0.40f);

            undersuitBlack = LoadOrCreate(
                UndersuitBlackPath,
                new Color(0.03f, 0.03f, 0.03f),
                metallic: 0.10f,
                smoothness: 0.28f);

            helmetDarkGreen = LoadOrCreate(
                HelmetDarkGreenPath,
                new Color(0.04f, 0.18f, 0.06f),
                metallic: 0.55f,
                smoothness: 0.42f);

            AssetDatabase.SaveAssets();
        }

        public static void WireStyler(
            Game.Player.CharacterMaterialStyler styler,
            Material armorGreen,
            Material undersuitBlack,
            Material helmetDarkGreen)
        {
            if (styler == null) return;

            var so = new SerializedObject(styler);
            so.FindProperty("armorGreen").objectReferenceValue       = armorGreen;
            so.FindProperty("undersuitBlack").objectReferenceValue   = undersuitBlack;
            so.FindProperty("helmetDarkGreen").objectReferenceValue  = helmetDarkGreen;
            so.FindProperty("applyOnAwake").boolValue              = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material LoadOrCreate(string assetPath, Color baseColor, float metallic, float smoothness)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader)
                {
                    name = Path.GetFileNameWithoutExtension(assetPath)
                };
                AssetDatabase.CreateAsset(mat, assetPath);
            }

            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
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
