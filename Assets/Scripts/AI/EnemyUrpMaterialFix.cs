using UnityEngine;

namespace Game.AI
{
    /// <summary>
    /// Converts mecha enemy materials to URP Lit and tints them black + hazard yellow.
    /// </summary>
    public class EnemyUrpMaterialFix : MonoBehaviour
    {
        [SerializeField] private bool applyOnAwake = true;

        [Header("Faction Colors")]
        [SerializeField] private Color yellowTint = new(0.95f, 0.78f, 0.05f);
        [SerializeField] private Color blackTint  = new(0.05f, 0.05f, 0.05f);

        [Header("Surface")]
        [SerializeField, Range(0f, 1f)] private float yellowMetallic = 0.55f;
        [SerializeField, Range(0f, 1f)] private float yellowSmoothness = 0.42f;
        [SerializeField, Range(0f, 1f)] private float blackMetallic = 0.72f;
        [SerializeField, Range(0f, 1f)] private float blackSmoothness = 0.34f;

        private static readonly string[] YellowTokens =
        {
            "CABEZZA", "PECHO", "HOMBRO", "HEAD", "CHEST", "SHOULDER", "ARMOR", "PLATE"
        };

        private static readonly string[] BlackTokens =
        {
            "CUEREPO", "JOINT", "TWIST", "PELVIS", "SPINE", "FRAME", "CORE", "UNDERSUIT"
        };

        private void Awake()
        {
            if (applyOnAwake)
                Apply();
        }

        public void Apply()
        {
            Shader urp = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Universal Render Pipeline/Simple Lit");
            if (urp == null) return;

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = renderer.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < mats.Length; i++)
                {
                    Material styled = StyleMaterial(mats[i], renderer, urp);
                    if (styled != null && styled != mats[i])
                    {
                        mats[i] = styled;
                        changed = true;
                    }
                }

                if (changed)
                    renderer.sharedMaterials = mats;
            }
        }

        private Material StyleMaterial(Material source, Renderer renderer, Shader urpShader)
        {
            if (source == null) return null;

            Material mat = source.shader == urpShader
                ? source
                : ConvertToUrp(source, urpShader);

            string label = $"{mat.name} {renderer.gameObject.name}";
            bool useYellow = ResolveUsesYellow(label);

            Color tint = useYellow ? yellowTint : blackTint;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", tint);
            else if (mat.HasProperty("_Color"))
                mat.color = tint;

            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", useYellow ? yellowMetallic : blackMetallic);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", useYellow ? yellowSmoothness : blackSmoothness);

            return mat;
        }

        private static Material ConvertToUrp(Material source, Shader urpShader)
        {
            var mat = new Material(urpShader) { name = source.name + "_URP" };

            if (source.HasProperty("_BaseMap") && mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
            else if (source.HasProperty("_MainTex") && mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", source.GetTexture("_MainTex"));

            if (source.HasProperty("_BumpMap") && mat.HasProperty("_BumpMap"))
                mat.SetTexture("_BumpMap", source.GetTexture("_BumpMap"));

            if (source.HasProperty("_MetallicGlossMap") && mat.HasProperty("_MetallicGlossMap"))
                mat.SetTexture("_MetallicGlossMap", source.GetTexture("_MetallicGlossMap"));

            return mat;
        }

        private static bool ResolveUsesYellow(string label)
        {
            if (ContainsAny(label, BlackTokens))
                return false;
            if (ContainsAny(label, YellowTokens))
                return true;

            // Mecha has many numbered sub-meshes — alternate for a black/yellow mix on the rest.
            return (label.GetHashCode() & 1) == 0;
        }

        private static bool ContainsAny(string source, string[] tokens)
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
