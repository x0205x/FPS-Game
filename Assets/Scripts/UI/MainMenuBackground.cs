using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Cycles fullscreen menu backgrounds with a slow crossfade and Ken Burns zoom
    /// so still frames from the provided GIFs feel cinematic.
    /// </summary>
    public class MainMenuBackground : MonoBehaviour
    {
        [SerializeField] private RawImage layerA;
        [SerializeField] private RawImage layerB;
        [SerializeField] private Texture[] backgrounds;
        [SerializeField, Min(1f)] private float holdSeconds = 7f;
        [SerializeField, Min(0.1f)] private float fadeSeconds = 1.75f;
        [SerializeField] private float zoomFrom = 1f;
        [SerializeField] private float zoomTo = 1.08f;

        private int _index;
        private bool _showA = true;
        private float _phaseTimer;
        private float _zoomT;

        public void Init(RawImage a, RawImage b, Texture[] textures)
        {
            layerA = a;
            layerB = b;
            backgrounds = textures;
        }

        private void Awake()
        {
            if (backgrounds == null || backgrounds.Length == 0) return;
            if (layerA == null || layerB == null) return;

            Stretch(layerA);
            Stretch(layerB);

            layerA.texture = backgrounds[0];
            layerB.texture = backgrounds.Length > 1 ? backgrounds[1] : backgrounds[0];
            layerA.color = Color.white;
            layerB.color = new Color(1f, 1f, 1f, 0f);
            layerA.rectTransform.localScale = Vector3.one * zoomFrom;
            layerB.rectTransform.localScale = Vector3.one * zoomFrom;

            ApplyCoverFit(layerA, layerA.texture);
            ApplyCoverFit(layerB, layerB.texture);
        }

        private void Update()
        {
            if (backgrounds == null || backgrounds.Length == 0 || layerA == null || layerB == null)
                return;

            if (backgrounds.Length == 1)
            {
                layerA.texture = backgrounds[0];
                ApplyCoverFit(layerA, backgrounds[0]);
                layerA.color = Color.white;
                layerB.color = new Color(1f, 1f, 1f, 0f);
                return;
            }

            float hold = Mathf.Max(0.1f, holdSeconds);
            float fade = Mathf.Max(0.05f, fadeSeconds);
            float cycle = hold + fade;

            _phaseTimer += Time.unscaledDeltaTime;
            if (_phaseTimer >= cycle)
            {
                _phaseTimer -= cycle;
                _index = (_index + 1) % backgrounds.Length;
                _showA = !_showA;
                _zoomT = 0f;
            }

            RawImage front = _showA ? layerA : layerB;
            RawImage back  = _showA ? layerB : layerA;

            int nextIndex = (_index + 1) % backgrounds.Length;
            front.texture = backgrounds[_index];
            back.texture  = backgrounds[nextIndex];

            float alpha = _phaseTimer <= hold
                ? 0f
                : Mathf.InverseLerp(hold, cycle, _phaseTimer);

            front.color = Color.white;
            back.color  = new Color(1f, 1f, 1f, alpha);

            ApplyCoverFit(front, front.texture);
            ApplyCoverFit(back, back.texture);

            _zoomT = Mathf.Clamp01(_zoomT + Time.unscaledDeltaTime / hold);
            float scale = Mathf.Lerp(zoomFrom, zoomTo, _zoomT);
            front.rectTransform.localScale = Vector3.one * scale;
            back.rectTransform.localScale  = Vector3.one * zoomFrom;
        }

        private static void Stretch(RawImage image)
        {
            RectTransform rt = image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>Crops portrait/landscape textures to fill the screen without stretching.</summary>
        private static void ApplyCoverFit(RawImage image, Texture texture)
        {
            if (image == null || texture == null || texture.height <= 0) return;

            float texAspect = texture.width / (float)texture.height;
            float screenAspect = Screen.width / (float)Mathf.Max(1, Screen.height);
            Rect uv = new Rect(0f, 0f, 1f, 1f);

            if (texAspect > screenAspect)
            {
                float scale = screenAspect / texAspect;
                uv.x = (1f - scale) * 0.5f;
                uv.width = scale;
            }
            else
            {
                float scale = texAspect / screenAspect;
                uv.y = (1f - scale) * 0.5f;
                uv.height = scale;
            }

            image.uvRect = uv;
        }
    }
}
