using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DesktopPauseMenu : MonoBehaviour
{
    // Меню паузы создаётся во время запуска: пауза, продолжить, громкость музыки и звуков.
    public static float MusicVolume { get; private set; } = 1f;
    public static float SfxVolume { get; private set; } = 1f;

    private const string MusicVolumeKey = "DesktopMusicVolume";
    private const string SfxVolumeKey = "DesktopSfxVolume";

    private readonly Dictionary<AudioSource, float> baseVolumes = new Dictionary<AudioSource, float>();

    private Canvas canvas;
    private GameObject overlayRoot;
    private Button pauseButton;
    private Slider musicSlider;
    private Slider sfxSlider;
    private FirstPersonCC playerController;
    private PlayerInteraction playerInteraction;
    private DesktopGrabber desktopGrabber;
    private float previousTimeScale = 1f;
    private bool wasCursorVisible;
    private CursorLockMode previousCursorLockMode;
    private bool isPaused;

    private void Awake()
    {
        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);

        BuildUi();
        ResolvePlayerReferences();
        ApplyAudioVolumes();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame)
            SetPaused(!isPaused);
    }

    public void SetPaused(bool paused)
    {
        if (isPaused == paused)
            return;

        isPaused = paused;
        ResolvePlayerReferences();

        if (paused)
        {
            previousTimeScale = Time.timeScale;
            wasCursorVisible = Cursor.visible;
            previousCursorLockMode = Cursor.lockState;
            Time.timeScale = 0f;

            if (playerController != null)
                playerController.SetInputEnabled(false);

            if (playerInteraction != null)
                playerInteraction.enabled = false;

            if (desktopGrabber != null)
                desktopGrabber.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;

            if (playerController != null)
                playerController.SetInputEnabled(true);

            if (playerInteraction != null)
                playerInteraction.enabled = true;

            if (desktopGrabber != null)
                desktopGrabber.enabled = true;

            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = wasCursorVisible;
        }

        if (overlayRoot != null)
            overlayRoot.SetActive(paused);

        if (pauseButton != null)
            pauseButton.gameObject.SetActive(!paused);
    }

    private void BuildUi()
    {
        GameObject canvasObject = new GameObject("DesktopPauseCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        StretchToParent(canvasRect);

        pauseButton = CreatePauseIconButton(canvasRect);
        pauseButton.onClick.AddListener(() => SetPaused(true));

        Image overlayImage = CreateImage("PauseOverlay", canvasRect, new Color(0f, 0f, 0f, 0.58f));
        overlayRoot = overlayImage.gameObject;
        StretchToParent(overlayImage.rectTransform);

        Image panel = CreateImage("Panel", overlayImage.rectTransform, new Color(0.12f, 0.10f, 0.09f, 0.97f));
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(560f, 420f);

        Text title = CreateText("Title", panelRect, "Пауза", 42, FontStyle.Bold, TextAnchor.MiddleCenter);
        SetRect(title.rectTransform, new Vector2(0.1f, 0.76f), new Vector2(0.9f, 0.95f), Vector2.zero, Vector2.zero);

        Text musicLabel = CreateText("MusicLabel", panelRect, "Музыка", 26, FontStyle.Bold, TextAnchor.MiddleLeft);
        SetRect(musicLabel.rectTransform, new Vector2(0.12f, 0.58f), new Vector2(0.42f, 0.68f), Vector2.zero, Vector2.zero);

        musicSlider = CreateSlider("MusicSlider", panelRect, new Vector2(0.43f, 0.59f), new Vector2(0.88f, 0.67f), MusicVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);

        Text sfxLabel = CreateText("SfxLabel", panelRect, "Звуки", 26, FontStyle.Bold, TextAnchor.MiddleLeft);
        SetRect(sfxLabel.rectTransform, new Vector2(0.12f, 0.42f), new Vector2(0.42f, 0.52f), Vector2.zero, Vector2.zero);

        sfxSlider = CreateSlider("SfxSlider", panelRect, new Vector2(0.43f, 0.43f), new Vector2(0.88f, 0.51f), SfxVolume);
        sfxSlider.onValueChanged.AddListener(SetSfxVolume);

        Button continueButton = CreateButton("ContinueButton", panelRect, "Продолжить", new Vector2(0.5f, 0.16f), new Vector2(0.5f, 0.16f), Vector2.zero, new Vector2(260f, 64f));
        continueButton.onClick.AddListener(() => SetPaused(false));

        overlayRoot.SetActive(false);
    }

    private void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        ApplyAudioVolumes();
    }

    private void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
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

            float multiplier = GetSourceMultiplier(source);
            source.volume = baseVolumes[source] * multiplier;
        }
    }

    private static float GetSourceMultiplier(AudioSource source)
    {
        AudioMixerGroup group = source.outputAudioMixerGroup;
        if (group == null)
            return SfxVolume;

        string groupName = group.name;
        if (groupName == "Music")
            return MusicVolume;

        if (groupName == "SFX")
            return SfxVolume;

        return 1f;
    }

    private void ResolvePlayerReferences()
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<FirstPersonCC>();

        if (playerInteraction == null)
            playerInteraction = FindFirstObjectByType<PlayerInteraction>();

        if (desktopGrabber == null)
            desktopGrabber = FindFirstObjectByType<DesktopGrabber>();
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
