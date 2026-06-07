using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Color shimmer and hover boost for main menu option labels.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MainMenuTextAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private float shimmerSpeed = 4.2f;
        [SerializeField] private float hoverSmoothSpeed = 10f;

        private static readonly Color ShimmerHighlight = new Color(1f, 0.92f, 0.45f, 1f);
        private static readonly Color HoverGlowColor = new Color(1f, 0.72f, 0.18f, 1f);

        private Button _button;
        private float _timer;
        private float _hoverAmount;
        private float _baseSpacing;
        private Color _baseFace = Color.white;
        private Color _baseGlow = Color.white;
        private Color _baseOutline = Color.white;
        private float _baseOutlineWidth;
        private Material _material;
        private bool _hovered;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (label == null) label = GetComponentInChildren<TextMeshProUGUI>();
            CacheBaseState();
        }

        private void Start()
        {
            CacheBaseState();
        }

        private void OnEnable()
        {
            _timer = Random.Range(0f, 8f);
        }

        private void CacheBaseState()
        {
            if (label == null) return;

            label.overflowMode = TextOverflowModes.Overflow;
            label.extraPadding = true;
            label.color = new Color(label.color.r, label.color.g, label.color.b, 1f);

            _baseSpacing = label.characterSpacing;
            _baseOutline = label.outlineColor;
            _baseOutlineWidth = label.outlineWidth;
            _material = label.fontMaterial;

            if (_material == null) return;

            _baseFace = _material.GetColor(ShaderUtilities.ID_FaceColor);
            _baseFace.a = 1f;
            _baseGlow = _material.GetColor(ShaderUtilities.ID_GlowColor);
            _baseGlow.a = 1f;
        }

        private void Update()
        {
            if (label == null) return;

            float targetHover = _button != null && _button.interactable && _hovered ? 1f : 0f;
            _hoverAmount = Mathf.MoveTowards(_hoverAmount, targetHover, Time.unscaledDeltaTime * hoverSmoothSpeed);
            _timer += Time.unscaledDeltaTime;

            float hover = _hoverAmount;
            float idleShimmer = (Mathf.Sin(_timer * shimmerSpeed) + 1f) * 0.5f;
            float shimmerStrength = Mathf.Lerp(0.28f, 0.72f, hover);
            float combinedShimmer = idleShimmer * shimmerStrength;

            label.characterSpacing = _baseSpacing + combinedShimmer * Mathf.Lerp(1.5f, 4f, hover);
            label.outlineWidth = _baseOutlineWidth + combinedShimmer * Mathf.Lerp(0.06f, 0.2f, hover);
            label.outlineColor = Color.Lerp(_baseOutline, ShimmerHighlight, combinedShimmer * Mathf.Lerp(0.35f, 0.85f, hover));

            if (_material == null)
            {
                _material = label.fontMaterial;
                if (_material == null) return;
            }

            Color face = Color.Lerp(_baseFace, Color.white, combinedShimmer * Mathf.Lerp(0.25f, 0.55f, hover));
            Color glow = Color.Lerp(_baseGlow, HoverGlowColor, combinedShimmer * Mathf.Lerp(0.4f, 0.9f, hover));
            face.a = 1f;
            glow.a = 1f;
            _material.SetColor(ShaderUtilities.ID_FaceColor, face);
            _material.SetColor(ShaderUtilities.ID_GlowColor, glow);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_button != null && !_button.interactable) return;
            _hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
        }
    }
}
