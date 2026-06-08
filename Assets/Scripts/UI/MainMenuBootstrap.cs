using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Builds the main menu Canvas at runtime so the scene can stay minimal.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class MainMenuBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            if (MenuAlreadyBuilt()) return;

            EnsureEventSystem();
            EnsureMenuCamera();
            BuildUi();
            EnsureWarAmbience();
            EnsureMenuMusic();
        }

        private static bool MenuAlreadyBuilt()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;

            Transform menu = canvas.transform.Find("MainMenu");
            if (menu == null) return false;

            return menu.GetComponentsInChildren<Button>(true).Length >= 3;
        }

        private static void EnsureWarAmbience()
        {
            if (FindAnyObjectByType<MainMenuWarAmbience>() != null) return;
            var ambience = new GameObject("MainMenuWarAmbience");
            ambience.AddComponent<MainMenuWarAmbience>();
        }

        private static void EnsureMenuMusic()
        {
            if (FindAnyObjectByType<MainMenuMusic>() != null) return;
            var music = new GameObject("MainMenuMusic");
            music.AddComponent<MainMenuMusic>();
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        private static void EnsureMenuCamera()
        {
            if (Camera.main != null) return;

            var camGo = new GameObject("MainMenuCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = RenderSettings.skybox != null
                ? CameraClearFlags.Skybox
                : CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.015f, 0.04f);
            cam.depth = -100;
            cam.cullingMask = 0;
            cam.farClipPlane = 5000f;
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
        }

        private void BuildUi()
        {
            Texture[] backgrounds = MainMenuBackgrounds.LoadSlideshow();
            if (backgrounds == null || backgrounds.Length == 0)
                backgrounds = new Texture[] { Texture2D.blackTexture };

            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var bgRoot = CreateUiObject("Background", canvasGo.transform);
            Stretch(bgRoot);

            Texture first = backgrounds[0];
            Texture second = backgrounds.Length > 1 ? backgrounds[1] : backgrounds[0];
            var layerA = CreateRawImage("LayerA", bgRoot.transform, first);
            var layerB = CreateRawImage("LayerB", bgRoot.transform, second);
            layerB.color = new Color(1f, 1f, 1f, 0f);

            var bgAnim = bgRoot.AddComponent<MainMenuBackground>();
            bgAnim.Init(layerA, layerB, backgrounds);

            var vignette = CreateUiObject("Vignette", canvasGo.transform);
            Stretch(vignette);
            var vignetteImg = vignette.AddComponent<Image>();
            vignetteImg.color = new Color(0f, 0f, 0f, 0.35f);
            vignetteImg.raycastTarget = false;

            var menuRoot = CreateUiObject("MainMenu", canvasGo.transform);
            var menuRt = menuRoot.GetComponent<RectTransform>();
            menuRt.anchorMin = new Vector2(0f, 0.5f);
            menuRt.anchorMax = new Vector2(0f, 0.5f);
            menuRt.pivot = new Vector2(0f, 0.5f);
            menuRt.anchoredPosition = new Vector2(120f, -40f);
            menuRt.sizeDelta = new Vector2(520f, 420f);

            var layout = menuRoot.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = 40f;
            layout.padding = new RectOffset(0, 0, 8, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var controller = menuRoot.AddComponent<MainMenuController>();

            CreateMenuButton(menuRoot.transform, "Start Prologue").onClick.AddListener(controller.StartPrologue);
            CreateMenuButton(menuRoot.transform, "Web Version").onClick.AddListener(controller.Options);
            CreateMenuButton(menuRoot.transform, "Credits").onClick.AddListener(controller.Credits);
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, worldPositionStays: false);
            return go;
        }

        private static void Stretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static RawImage CreateRawImage(string name, Transform parent, Texture texture)
        {
            var go = CreateUiObject(name, parent);
            Stretch(go);
            var raw = go.AddComponent<RawImage>();
            raw.texture = texture;
            raw.raycastTarget = false;
            return raw;
        }

        private static Button CreateMenuButton(Transform parent, string label)
        {
            var go = CreateUiObject(label.Replace(" ", "") + "Button", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500f, 72f);

            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.minHeight = 72f;
            layoutElement.preferredHeight = 72f;

            var image = go.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.45f);

            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.08f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.22f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.38f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var textGo = CreateUiObject("Text", go.transform);
            Stretch(textGo);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            MainMenuTypography.Apply(tmp);

            go.AddComponent<MainMenuButtonGlow>();
            go.AddComponent<MainMenuTextAnimator>();

            return button;
        }
    }
}
