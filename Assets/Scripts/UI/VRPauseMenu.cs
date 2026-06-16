using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class VRPauseMenu : MonoBehaviour
{
    private readonly Dictionary<AudioSource, float> baseVolumes = new Dictionary<AudioSource, float>();

    private Canvas canvas;
    private GameObject overlayRoot;
    private Button pauseButton;
    private Slider musicSlider;
    private Slider sfxSlider;
    private FirstPersonCC playerController;
    private ClickCollector clickCollector;
    private float previousTimeScale = 1f;
    private bool wasCursorVisible;
    private CursorLockMode previousCursorLockMode;
    private bool isPaused;
    private InputAction menuAction;
    private InputAction closePauseAction;

    private void Awake()
    {
        EnsureEventSystem();
        BuildUi();
        ResolveSceneReferences();
        CreateMenuAction();
        VRAudioSettings.ApplySceneAudioVolumes();
    }

    private void OnEnable()
    {
        menuAction?.Enable();
        closePauseAction?.Enable();
    }

    private void OnDisable()
    {
        menuAction?.Disable();
        closePauseAction?.Disable();
        Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame))
            SetPaused(!isPaused);

        if (isPaused && closePauseAction != null && closePauseAction.WasPressedThisFrame())
        {
            SetPaused(false);
            return;
        }

        if (menuAction != null && menuAction.WasPressedThisFrame())
            SetPaused(!isPaused);
    }

    public void SetPaused(bool paused)
    {
        if (isPaused == paused)
            return;

        isPaused = paused;
        ResolveSceneReferences();

        if (paused)
        {
            previousTimeScale = Time.timeScale;
            wasCursorVisible = Cursor.visible;
            previousCursorLockMode = Cursor.lockState;
            Time.timeScale = 0f;

            if (playerController != null)
                playerController.SetInputEnabled(false);

            if (clickCollector != null)
                clickCollector.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;

            if (playerController != null)
                playerController.SetInputEnabled(true);

            if (clickCollector != null)
                clickCollector.enabled = true;

            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = wasCursorVisible;
        }

        if (overlayRoot != null)
            overlayRoot.SetActive(paused);

        if (pauseButton != null)
            pauseButton.gameObject.SetActive(!paused);
    }

    private void RestartScene()
    {
        Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    private void BuildUi()
    {
        GameObject canvasObject = new GameObject(
            "VRPauseCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 910;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        StretchToParent(canvasRect);

        pauseButton = CreatePauseIconButton(canvasRect);
        pauseButton.onClick.AddListener(() => SetPaused(true));

        overlayRoot = new GameObject("PauseOverlay", typeof(RectTransform));
        overlayRoot.transform.SetParent(canvasRect, false);
        RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
        StretchToParent(overlayRect);

        Image dim = CreateImage("Dim", overlayRect, new Color(0f, 0f, 0f, 0.58f));
        StretchToParent(dim.rectTransform);

        Image panel = CreateImage("Panel", overlayRect, new Color(0.12f, 0.10f, 0.09f, 0.97f));
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(560f, 500f);

        Text title = CreateText("Title", panelRect, "Пауза", 42, FontStyle.Bold, TextAnchor.MiddleCenter);
        SetRect(title.rectTransform, new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.95f), Vector2.zero, Vector2.zero);

        Text musicLabel = CreateText("MusicLabel", panelRect, "Музыка", 26, FontStyle.Bold, TextAnchor.MiddleLeft);
        SetRect(musicLabel.rectTransform, new Vector2(0.12f, 0.61f), new Vector2(0.42f, 0.70f), Vector2.zero, Vector2.zero);

        musicSlider = CreateSlider("MusicSlider", panelRect, new Vector2(0.43f, 0.62f), new Vector2(0.88f, 0.70f), VRAudioSettings.MusicVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);

        Text sfxLabel = CreateText("SfxLabel", panelRect, "Звуки", 26, FontStyle.Bold, TextAnchor.MiddleLeft);
        SetRect(sfxLabel.rectTransform, new Vector2(0.12f, 0.47f), new Vector2(0.42f, 0.56f), Vector2.zero, Vector2.zero);

        sfxSlider = CreateSlider("SfxSlider", panelRect, new Vector2(0.43f, 0.48f), new Vector2(0.88f, 0.56f), VRAudioSettings.SfxVolume);
        sfxSlider.onValueChanged.AddListener(SetSfxVolume);

        Button continueButton = CreateButton("ContinueButton", panelRect, "Продолжить", new Vector2(0.5f, 0.30f), new Vector2(0.5f, 0.30f), Vector2.zero, new Vector2(280f, 60f));
        continueButton.onClick.AddListener(() => SetPaused(false));

        Button restartButton = CreateButton("RestartButton", panelRect, "Заново", new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), Vector2.zero, new Vector2(280f, 60f));
        restartButton.onClick.AddListener(RestartScene);

        overlayRoot.SetActive(false);
    }

    private void SetMusicVolume(float value)
    {
        VRAudioSettings.SaveMusicVolume(value);
        ApplyAudioVolumes();
    }

    private void SetSfxVolume(float value)
    {
        VRAudioSettings.SaveSfxVolume(value);
        ApplyAudioVolumes();
    }

    private void ApplyAudioVolumes()
    {
        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source == null)
                continue;

            if (!baseVolumes.ContainsKey(source))
                baseVolumes.Add(source, source.volume);

            source.volume = baseVolumes[source] * GetSourceMultiplier(source);
        }
    }

    private static float GetSourceMultiplier(AudioSource source)
    {
        AudioMixerGroup group = source.outputAudioMixerGroup;
        if (group == null)
            return VRAudioSettings.SfxVolume;

        if (group.name == "Music")
            return VRAudioSettings.MusicVolume;

        if (group.name == "SFX")
            return VRAudioSettings.SfxVolume;

        return 1f;
    }

    private void ResolveSceneReferences()
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<FirstPersonCC>();

        if (clickCollector == null)
            clickCollector = FindFirstObjectByType<ClickCollector>();
    }

    private void CreateMenuAction()
    {
        menuAction = new InputAction("VR Pause", InputActionType.Button);
        menuAction.AddBinding("<XRController>{LeftHand}/menuButton");
        menuAction.AddBinding("<XRController>{RightHand}/menuButton");
        menuAction.AddBinding("<XRController>{RightHand}/primaryButton");
        menuAction.AddBinding("<OculusTouchController>{RightHand}/primaryButton");

        closePauseAction = new InputAction("Close VR Pause", InputActionType.Button);
        closePauseAction.AddBinding("<XRController>{RightHand}/secondaryButton");
        closePauseAction.AddBinding("<OculusTouchController>{RightHand}/secondaryButton");
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        if (eventSystem.GetComponent<XRUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<XRUIInputModule>();
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(string name, Transform parent, string text, int fontSize, FontStyle fontStyle, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text label = textObject.GetComponent<Text>();
        label.font = GetDefaultUiFont();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private static Button CreateButton(string name, Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        Image image = CreateImage(name, parent, new Color(0.32f, 0.18f, 0.08f, 0.96f));
        Button button = image.gameObject.AddComponent<Button>();
        RectTransform rect = image.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text label = CreateText("Label", rect, text, 24, FontStyle.Bold, TextAnchor.MiddleCenter);
        StretchToParent(label.rectTransform);
        return button;
    }

    private static Button CreatePauseIconButton(Transform parent)
    {
        Image image = CreateImage("PauseButton", parent, new Color(0.16f, 0.10f, 0.07f, 0.94f));
        Button button = image.gameObject.AddComponent<Button>();

        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-54f, -46f);
        rect.sizeDelta = new Vector2(64f, 56f);

        Image leftBar = CreateImage("PauseBarLeft", rect, new Color(1f, 0.9f, 0.7f, 1f));
        RectTransform leftRect = leftBar.rectTransform;
        leftRect.anchorMin = new Vector2(0.34f, 0.24f);
        leftRect.anchorMax = new Vector2(0.44f, 0.76f);
        leftRect.offsetMin = Vector2.zero;
        leftRect.offsetMax = Vector2.zero;
        leftBar.raycastTarget = false;

        Image rightBar = CreateImage("PauseBarRight", rect, new Color(1f, 0.9f, 0.7f, 1f));
        RectTransform rightRect = rightBar.rectTransform;
        rightRect.anchorMin = new Vector2(0.56f, 0.24f);
        rightRect.anchorMax = new Vector2(0.66f, 0.76f);
        rightRect.offsetMin = Vector2.zero;
        rightRect.offsetMax = Vector2.zero;
        rightBar.raycastTarget = false;

        return button;
    }

    private static Slider CreateSlider(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, float value)
    {
        GameObject sliderObject = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        SetRect(sliderRect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);

        Image background = CreateImage("Background", sliderRect, new Color(0.22f, 0.18f, 0.14f, 1f));
        StretchToParent(background.rectTransform);

        Image fill = CreateImage("Fill", sliderRect, new Color(0.89f, 0.54f, 0.22f, 1f));
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(6f, 6f);
        fillRect.offsetMax = new Vector2(-6f, -6f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderRect, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        StretchToParent(fillAreaRect);
        fill.transform.SetParent(fillAreaRect, false);

        Image handle = CreateImage("Handle", sliderRect, new Color(1f, 0.88f, 0.62f, 1f));
        RectTransform handleRect = handle.rectTransform;
        handleRect.sizeDelta = new Vector2(28f, 42f);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = Mathf.Clamp01(value);
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle;
        return slider;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void StretchToParent(RectTransform rect)
    {
        SetRect(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private static Font GetDefaultUiFont()
    {
        Font font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Helvetica", "Liberation Sans" }, 18);
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return font;
    }
}
