using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Loads ordered main menu slideshow textures from Resources.
    /// </summary>
    public static class MainMenuBackgrounds
    {
        private const string ResourcesFolder = "UI/MainMenu/Backgrounds";

        public static Texture[] LoadSlideshow()
        {
            var textures = new List<Texture>();
            Texture2D[] loaded = Resources.LoadAll<Texture2D>(ResourcesFolder);
            if (loaded == null || loaded.Length == 0)
                return LoadLegacyFallback();

            Array.Sort(loaded, (a, b) => string.CompareOrdinal(a.name, b.name));
            foreach (Texture2D tex in loaded)
            {
                if (tex != null)
                    textures.Add(tex);
            }

            return textures.Count > 0 ? textures.ToArray() : LoadLegacyFallback();
        }

        private static Texture[] LoadLegacyFallback()
        {
            var battle = Resources.Load<Texture2D>("UI/MainMenu/background_battle");
            var marine = Resources.Load<Texture2D>("UI/MainMenu/background_marine");
            if (battle != null && marine != null) return new Texture[] { battle, marine };
            if (battle != null) return new Texture[] { battle };
            if (marine != null) return new Texture[] { marine };
            return new Texture[] { Texture2D.blackTexture };
        }
    }
}
