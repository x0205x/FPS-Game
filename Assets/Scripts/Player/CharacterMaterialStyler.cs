using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Swaps Spartan FBX materials to green armor, black undersuit, and dark green helmet.
    /// Matches by imported material names (Spartan_Chest_Mat, Spartan_Helmet_Mat, etc.).
    /// </summary>
    public class CharacterMaterialStyler : MonoBehaviour
    {
        [SerializeField] private Material armorGreen;
        [SerializeField] private Material undersuitBlack;
        [SerializeField] private Material helmetDarkGreen;
        [SerializeField] private bool applyOnAwake = true;

        private static readonly string[] HelmetTokens =
        {
            "Spartan_Helmet", "Spartan_Ear", "Helmet"
        };

        private static readonly string[] BlackTokens =
        {
            "Spartan_Undersuit", "Undersuit"
        };

        private static readonly string[] GreenTokens =
        {
            "Spartan_Chest", "Spartan_Arms", "Spartan_Legs", "Spartan_Shoulders", "Armour"
        };

        private void Awake()
        {
            if (applyOnAwake) Apply();
        }

        public void Apply()
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = renderer.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < mats.Length; i++)
                {
                    Material replacement = Resolve(mats[i], renderer);
                    if (replacement != null && mats[i] != replacement)
                    {
                        mats[i] = replacement;
                        changed = true;
                    }
                }

                if (changed)
                    renderer.sharedMaterials = mats;
            }
        }

        private Material Resolve(Material current, Renderer renderer)
        {
            string label = $"{current?.name} {renderer.gameObject.name}";

            if (Matches(label, HelmetTokens))
                return helmetDarkGreen != null ? helmetDarkGreen : current;
            if (Matches(label, BlackTokens))
                return undersuitBlack != null ? undersuitBlack : current;
            if (Matches(label, GreenTokens))
                return armorGreen != null ? armorGreen : current;

            if (renderer.gameObject.name.IndexOf("Helmet", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return helmetDarkGreen != null ? helmetDarkGreen : current;

            return armorGreen != null ? armorGreen : current;
        }

        private static bool Matches(string source, string[] tokens)
        {
            foreach (string token in tokens)
            {
                if (source.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }
    }
}
