using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Hover boost for menu button glow, outline, and background.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MainMenuButtonGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Image buttonImage;
        [SerializeField] private float pulseSpeed = 1.8f;
        [SerializeField] private float hoverSmoothSpeed = 10f;
        [SerializeField] private float hoverScale = 1.05f;

        private Button _button;
        private RectTransform _labelRect;
        private Material _glowMaterial;
        private float _baseGlowPower;
        private float _baseGlowOuter;
        private float _baseGlowInner;
        private Color _baseButtonColor;
        private Vector3 _baseLabelScale;
        private bool _hovered;
        private float _hoverAmount;
        private float _pulsePhase;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (label == null) label = GetComponentInChildren<TextMeshProUGUI>();
            if (buttonImage == null) buttonImage = GetComponent<Image>();

            if (label != null)
            {
                _labelRect = label.rectTransform;
                _baseLabelScale = _labelRect != null ? _labelRect.localScale : Vector3.one;
                CacheMaterialState();
            }

            if (buttonImage != null)
                _baseButtonColor = buttonImage.color;
        }

        private void OnEnable()
        {
            _pulsePhase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (label == null || _labelRect == null) return;

            float targetHover = _button != null && _button.interactable && _hovered ? 1f : 0f;
            _hoverAmount = Mathf.MoveTowards(_hoverAmount, targetHover, Time.unscaledDeltaTime * hoverSmoothSpeed);

            _pulsePhase += Time.unscaledDeltaTime * pulseSpeed;
            float pulse = (Mathf.Sin(_pulsePhase) + 1f) * 0.5f;
            float glowMix = pulse * Mathf.Lerp(0.35f, 0.15f, _hoverAmount) + _hoverAmount;

            if (_glowMaterial != null)
            {
                float hoverBoost = _hoverAmount * (0.65f + pulse * 0.35f);
                _glowMaterial.SetFloat(ShaderUtilities.ID_GlowPower, _baseGlowPower + glowMix * 0.2f + hoverBoost * 0.35f);
                _glowMaterial.SetFloat(ShaderUtilities.ID_GlowOuter, _baseGlowOuter + glowMix * 0.16f + hoverBoost * 0.32f);
                _glowMaterial.SetFloat(ShaderUtilities.ID_GlowInner, _baseGlowInner + glowMix * 0.04f + hoverBoost * 0.08f);
            }

            _labelRect.localScale = _baseLabelScale * Mathf.Lerp(1f, hoverScale, _hoverAmount);

            if (buttonImage != null)
            {
                Color glowBg = new Color(1f, 0.5f, 0.1f, Mathf.Lerp(0.1f, 0.32f, _hoverAmount));
                buttonImage.color = Color.Lerp(_baseButtonColor, glowBg, Mathf.Lerp(0.25f, 0.75f, _hoverAmount));
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_button.interactable) return;
            _hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
        }

        private void CacheMaterialState()
        {
            _glowMaterial = label.fontMaterial;
            if (_glowMaterial == null) return;

            _baseGlowPower = _glowMaterial.HasProperty(ShaderUtilities.ID_GlowPower)
                ? _glowMaterial.GetFloat(ShaderUtilities.ID_GlowPower)
                : 0.9f;
            _baseGlowOuter = _glowMaterial.HasProperty(ShaderUtilities.ID_GlowOuter)
                ? _glowMaterial.GetFloat(ShaderUtilities.ID_GlowOuter)
                : 0.5f;
            _baseGlowInner = _glowMaterial.HasProperty(ShaderUtilities.ID_GlowInner)
                ? _glowMaterial.GetFloat(ShaderUtilities.ID_GlowInner)
                : 0.1f;
        }
    }
}
