using UnityEngine;

namespace Game.AI
{
    /// <summary>
    /// Swaps Alteruna Armature materials to URP body/arm/leg materials so enemies
    /// do not render pink in a URP project.
    /// </summary>
    public class EnemyMaterialStyler : MonoBehaviour
    {
        [SerializeField] private Material bodyMaterial;
        [SerializeField] private Material armsMaterial;
        [SerializeField] private Material legsMaterial;
        [SerializeField] private bool applyOnAwake = true;

        private void Awake()
        {
            if (applyOnAwake)
                Apply();
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

            if (Contains(label, "Legs"))
                return legsMaterial != null ? legsMaterial : bodyMaterial;
            if (Contains(label, "Arms"))
                return armsMaterial != null ? armsMaterial : bodyMaterial;

            return bodyMaterial != null ? bodyMaterial : armsMaterial;
        }

        private static bool Contains(string source, string token) =>
            source.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
