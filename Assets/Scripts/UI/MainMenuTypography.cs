using TMPro;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Applies cinematic TMP styling (wide caps + ember glow) to main menu labels.
    /// </summary>
    public static class MainMenuTypography
    {
        private static TMP_FontAsset _menuFont;

        public static void Apply(TextMeshProUGUI label)
        {
            if (label == null) return;

            TMP_FontAsset font = ResolveFont();
            if (!IsFontUsable(font))
                font = TMP_Settings.defaultFontAsset;

            if (font != null)
                label.font = font;
            label.text = label.text.ToUpperInvariant();
            label.fontSize = 40f;
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 6f;
            label.wordSpacing = 4f;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = new Color(1f, 0.94f, 0.82f, 1f);
            label.margin = new Vector4(28f, 0f, 0f, 0f);
            label.raycastTarget = false;

            if (font != null && font.material != null)
            {
                Material glow = CreateGlowMaterial(font);
                if (glow != null)
                {
                    label.fontMaterial = glow;
                    label.fontMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0.12f);
                }
            }

            label.outlineWidth = 0.22f;
            label.outlineColor = new Color(0.75f, 0.28f, 0.04f, 1f);
        }

        private static TMP_FontAsset ResolveFont()
        {
            if (IsFontUsable(_menuFont)) return _menuFont;

            _menuFont = Resources.Load<TMP_FontAsset>("UI/MainMenu/MenuFont");
            if (IsFontUsable(_menuFont)) return _menuFont;

            _menuFont = TMP_Settings.defaultFontAsset;
            return _menuFont;
        }

        private static bool IsFontUsable(TMP_FontAsset font)
        {
            if (font == null) return false;
            if (font.atlasTextures == null || font.atlasTextures.Length == 0) return false;
            return font.atlasTextures[0] != null;
        }

        private static Material CreateGlowMaterial(TMP_FontAsset font)
        {
            if (font == null || font.material == null) return null;

            Material glow = new Material(font.material);
            if (glow.HasProperty(ShaderUtilities.ID_GlowColor))
                glow.EnableKeyword(ShaderUtilities.Keyword_Glow);
            glow.SetColor(ShaderUtilities.ID_GlowColor, new Color(1f, 0.62f, 0.14f, 1f));
            glow.SetFloat(ShaderUtilities.ID_GlowPower, 0.95f);
            glow.SetFloat(ShaderUtilities.ID_GlowOuter, 0.58f);
            glow.SetFloat(ShaderUtilities.ID_GlowInner, 0.12f);
            glow.SetColor(ShaderUtilities.ID_FaceColor, new Color(1f, 0.96f, 0.82f, 1f));
            return glow;
        }
    }
}
