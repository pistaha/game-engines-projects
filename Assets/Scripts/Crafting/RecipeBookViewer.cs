using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

public class RecipeBookViewer : MonoBehaviour, IInteractable
{
    [SerializeField] private FirstPersonCC playerController = null;
    [SerializeField] private PlayerInteraction playerInteraction = null;
    [SerializeField] private DesktopGrabber desktopGrabber = null;
    [SerializeField] private ClickCollector clickCollector = null;
    [SerializeField] private RecipeChecker recipeChecker = null;
    [SerializeField] private List<Renderer> closedBookRenderers = new List<Renderer>();
    [SerializeField] private Collider clickCollider = null;
    [SerializeField] private GameObject spreadRoot = null;
    [SerializeField] private List<GameObject> spreads = new List<GameObject>();
    [SerializeField] private GameObject canvasBookRoot = null;
    [SerializeField] private GraphicRaycaster canvasRaycaster = null;
    [SerializeField] private bool useScreenSpaceBookMenu = true;
    [SerializeField] private Vector2 screenBookPanelSize = new Vector2(1280f, 780f);
    [SerializeField] private bool showWorldBookPages = false;

    [SerializeField] private float moveDuration = 0.5f;
    [SerializeField] private float openDistance = 1.08f;
    [SerializeField] private float openHeightOffset = -0.03f;
    [SerializeField] private float openSideOffset = 0f;
    [SerializeField] private float openScaleMultiplier = 1.98f;
    [SerializeField] private float openTilt = 1.8f;
    [SerializeField] private bool followCameraWhileOpen = true;
    [SerializeField] private float followCameraSharpness = 18f;
    [SerializeField] private Key keyboardCloseKey = Key.Escape;
    [SerializeField] private Key keyboardAltCloseKey = Key.B;
    [SerializeField] private Key keyboardNextPageKey = Key.RightArrow;
    [SerializeField] private Key keyboardPreviousPageKey = Key.LeftArrow;
    [SerializeField] private float spreadStartScale = 0.36f;
    [SerializeField] private float floatAmplitude = 0.008f;
    [SerializeField] private float floatSpeed = 1.9f;

    private bool isOpen;
    private bool isAnimating;
    private int currentSpread;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private Vector3 openedPosition;
    private Quaternion openedRotation;
    private Vector3 openedScale;
    private RecipeBookCanvasUI canvasBookUi;
    private Canvas screenBookCanvas;
    private CanvasScaler screenBookCanvasScaler;
    private GraphicRaycaster screenBookRaycaster;
    private bool cursorWasVisible;
    private CursorLockMode previousCursorLockState;
    private bool desktopGrabberWasEnabled;

    public bool IsOpenForInteraction => isOpen && !isAnimating;

    public string InteractionPrompt => isOpen ? "Esc - закрыть книгу" : "E / ПКМ - открыть книгу";

    private bool UseWorldBookPages()
    {
        if (useScreenSpaceBookMenu)
            return false;

        return showWorldBookPages || canvasBookRoot == null;
    }

    private void Awake()
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<FirstPersonCC>();

        if (playerInteraction == null)
            playerInteraction = FindFirstObjectByType<PlayerInteraction>();

        if (desktopGrabber == null)
            desktopGrabber = FindFirstObjectByType<DesktopGrabber>();

        if (clickCollector == null)
            clickCollector = FindFirstObjectByType<ClickCollector>();

        if (recipeChecker == null)
            recipeChecker = FindFirstObjectByType<RecipeChecker>();

        if (clickCollider == null)
            clickCollider = GetComponent<Collider>();

        if (closedBookRenderers.Count == 0)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
                closedBookRenderers.Add(renderer);
        }

        if (spreadRoot != null)
            spreadRoot.SetActive(false);

        if (canvasBookRoot != null)
            canvasBookRoot.SetActive(false);

        if (canvasRaycaster != null)
            canvasRaycaster.enabled = false;

        canvasBookUi = GetComponentInChildren<RecipeBookCanvasUI>(true);

        if (useScreenSpaceBookMenu)
            EnsureScreenSpaceBookMenu();
    }

    private void Update()
    {
        if (isOpen && !isAnimating && !useScreenSpaceBookMenu)
        {
            Camera mainCamera = Camera.main;
            if (followCameraWhileOpen && mainCamera != null)
            {
                CalculateOpenedPose(mainCamera, out Vector3 targetPosition, out Quaternion targetRotation);
                float followT = GetSharpFollowT(followCameraSharpness, Time.unscaledDeltaTime);
                openedPosition = Vector3.Lerp(openedPosition, targetPosition, followT);
                openedRotation = Quaternion.Slerp(openedRotation, targetRotation, followT);
            }

            float hoverOffset = Mathf.Sin(Time.unscaledTime * floatSpeed) * floatAmplitude;
            Vector3 hoverDirection = mainCamera != null ? mainCamera.transform.up : transform.up;
            transform.position = openedPosition + hoverDirection * hoverOffset;
            transform.rotation = openedRotation;
            transform.localScale = openedScale;
        }

        if (!isOpen || isAnimating)
            return;

        if (WasKeyboardKeyPressed(keyboardCloseKey) || WasKeyboardKeyPressed(keyboardAltCloseKey))
            CloseBookFromButton();

        if (WasKeyboardKeyPressed(keyboardNextPageKey))
            ShowNextPage();

        if (WasKeyboardKeyPressed(keyboardPreviousPageKey))
            ShowPreviousPage();
    }

    public void Interact()
    {
        if (isOpen || isAnimating)
            return;

        StartCoroutine(OpenBookRoutine());
    }

    public bool CanInteract(PlayerInteraction interaction)
    {
        return !isOpen && !isAnimating;
    }

    public void Interact(PlayerInteraction interaction)
    {
        Interact();
    }

    public void CloseBookFromButton()
    {
        if (!isOpen || isAnimating)
            return;

        StartCoroutine(CloseBookRoutine());
    }

    private IEnumerator OpenBookRoutine()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            yield break;

        isAnimating = true;
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalScale = transform.localScale;
        previousCursorLockState = Cursor.lockState;
        cursorWasVisible = Cursor.visible;

        if (playerController != null)
            playerController.SetInputEnabled(false);

        if (playerInteraction != null)
            playerInteraction.enabled = false;

        if (desktopGrabber == null)
            desktopGrabber = FindFirstObjectByType<DesktopGrabber>();

        desktopGrabberWasEnabled = desktopGrabber != null && desktopGrabber.enabled;
        if (desktopGrabber != null)
            desktopGrabber.enabled = false;

        if (clickCollector != null)
            clickCollector.enabled = false;

        if (clickCollider != null)
            clickCollider.enabled = false;

        if (useScreenSpaceBookMenu)
        {
            OpenScreenSpaceBookMenu();
            yield break;
        }

        if (spreadRoot != null && UseWorldBookPages())
        {
            spreadRoot.SetActive(true);
            spreadRoot.transform.localScale = Vector3.one * spreadStartScale;
        }

        if (canvasBookRoot != null)
            canvasBookRoot.SetActive(true);

        if (canvasRaycaster != null)
            canvasRaycaster.enabled = true;

        if (canvasBookUi != null)
            canvasBookUi.RefreshBookFromFirstPage();

        if (UseWorldBookPages())
        {
            currentSpread = 0;
            ShowCurrentSpread();
        }

        CalculateOpenedPose(mainCamera, out Vector3 targetPosition, out Quaternion targetRotation);

        Vector3 targetScale = originalScale * openScaleMultiplier;
        float time = 0f;
        bool bookHidden = false;

        while (time < moveDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / moveDuration);
            float smoothT = t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(originalPosition, targetPosition, smoothT);
            transform.rotation = Quaternion.Slerp(originalRotation, targetRotation, smoothT);
            transform.localScale = Vector3.Lerp(originalScale, targetScale, smoothT);

            if (!bookHidden && t >= 0.35f)
            {
                SetClosedBookVisible(false);
                bookHidden = true;
            }

            if (spreadRoot != null && UseWorldBookPages())
            {
                float spreadT = Mathf.InverseLerp(0.2f, 1f, t);
                float smoothSpreadT = spreadT * spreadT * (3f - 2f * spreadT);
                float spreadScale = Mathf.Lerp(spreadStartScale, 1f, smoothSpreadT);
                spreadRoot.transform.localScale = Vector3.one * spreadScale;
            }

            yield return null;
        }

        SetClosedBookVisible(false);

        if (spreadRoot != null && UseWorldBookPages())
            spreadRoot.transform.localScale = Vector3.one;

        openedPosition = targetPosition;
        openedRotation = targetRotation;
        openedScale = targetScale;
        isOpen = true;
        isAnimating = false;
    }

    private IEnumerator CloseBookRoutine()
    {
        isAnimating = true;

        if (useScreenSpaceBookMenu)
        {
            CloseScreenSpaceBookMenu();
            yield break;
        }

        if (spreadRoot != null && UseWorldBookPages())
            spreadRoot.transform.localScale = Vector3.one;

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        Vector3 startScale = transform.localScale;
        float time = 0f;
        bool bookShown = false;

        while (time < moveDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / moveDuration);
            float smoothT = t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(startPosition, originalPosition, smoothT);
            transform.rotation = Quaternion.Slerp(startRotation, originalRotation, smoothT);
            transform.localScale = Vector3.Lerp(startScale, originalScale, smoothT);

            if (!bookShown && t >= 0.5f)
            {
                SetClosedBookVisible(true);
                bookShown = true;
            }

            if (spreadRoot != null && UseWorldBookPages())
            {
                float spreadScale = Mathf.Lerp(1f, spreadStartScale, smoothT);
                spreadRoot.transform.localScale = Vector3.one * spreadScale;
            }

            yield return null;
        }

        if (spreadRoot != null)
        {
            spreadRoot.transform.localScale = Vector3.one;
            spreadRoot.SetActive(false);
        }

        if (canvasBookRoot != null)
            canvasBookRoot.SetActive(false);

        if (canvasRaycaster != null)
            canvasRaycaster.enabled = false;

        SetClosedBookVisible(true);
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        transform.localScale = originalScale;

        if (playerController != null)
            playerController.SetInputEnabled(true);

        if (playerInteraction != null)
            playerInteraction.enabled = true;

        if (desktopGrabber != null)
            desktopGrabber.enabled = desktopGrabberWasEnabled;

        if (clickCollector != null)
            clickCollector.enabled = true;

        if (clickCollider != null)
            clickCollider.enabled = true;

        isOpen = false;
        isAnimating = false;
    }

    private void OpenScreenSpaceBookMenu()
    {
        EnsureScreenSpaceBookMenu();

        // Игровая книга открывается как экранное меню.
        // Пока она открыта, движение/захват отключены, а курсор разблокирован.
        SetClosedBookVisible(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (canvasBookRoot != null)
            canvasBookRoot.SetActive(true);

        if (canvasRaycaster != null)
            canvasRaycaster.enabled = true;

        if (canvasBookUi != null)
            canvasBookUi.RefreshBookFromFirstPage();

        isOpen = true;
        isAnimating = false;
    }

    private void CloseScreenSpaceBookMenu()
    {
        if (canvasBookRoot != null)
            canvasBookRoot.SetActive(false);

        if (canvasRaycaster != null)
            canvasRaycaster.enabled = false;

        SetClosedBookVisible(true);

        if (playerController != null)
            playerController.SetInputEnabled(true);

        if (playerInteraction != null)
            playerInteraction.enabled = true;

        if (desktopGrabber != null)
            desktopGrabber.enabled = desktopGrabberWasEnabled;

        if (clickCollector != null)
            clickCollector.enabled = true;

        if (clickCollider != null)
            clickCollider.enabled = true;

        Cursor.lockState = previousCursorLockState;
        Cursor.visible = cursorWasVisible;

        isOpen = false;
        isAnimating = false;
    }

    private void EnsureScreenSpaceBookMenu()
    {
        if (canvasBookUi == null)
            canvasBookUi = GetComponentInChildren<RecipeBookCanvasUI>(true);

        if (canvasBookRoot != null && canvasBookUi != null)
        {
            ConfigureScreenSpaceCanvas(canvasBookRoot);
            return;
        }

        GameObject rootObject = new GameObject("RecipeBook_ScreenSpaceMenu", typeof(RectTransform));
        rootObject.transform.SetParent(transform, false);
        canvasBookRoot = rootObject;
        ConfigureScreenSpaceCanvas(rootObject);

        RectTransform canvasRect = rootObject.GetComponent<RectTransform>();
        StretchToParent(canvasRect);

        Image dimBackground = CreateImage("DimBackground", canvasRect, new Color(0f, 0f, 0f, 0.5f));
        StretchToParent(dimBackground.rectTransform);

        Image panelBackground = CreateImage("RecipeBookPanel", canvasRect, new Color(0.96f, 0.91f, 0.78f, 0.99f));
        AddOutline(panelBackground.gameObject, new Color(0.2f, 0.105f, 0.045f, 1f), new Vector2(4f, -4f));
        AddShadow(panelBackground.gameObject, new Color(0f, 0f, 0f, 0.34f), new Vector2(0f, -10f));

        RectTransform panelRect = panelBackground.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = screenBookPanelSize;

        Image titleStrip = CreateImage("TitleStrip", panelRect, new Color(0.29f, 0.14f, 0.06f, 0.96f));
        titleStrip.rectTransform.anchorMin = new Vector2(0f, 0.86f);
        titleStrip.rectTransform.anchorMax = new Vector2(1f, 1f);
        titleStrip.rectTransform.offsetMin = Vector2.zero;
        titleStrip.rectTransform.offsetMax = Vector2.zero;

        Text titleText = CreateText("Title", panelRect, new Vector2(0.1f, 0.875f), new Vector2(0.9f, 0.965f), 46f, FontStyle.Bold, TextAnchor.MiddleCenter);
        titleText.color = new Color(1f, 0.92f, 0.68f, 1f);

        Image imageFrame = CreateImage("RecipeImageFrame", panelRect, new Color(0.18f, 0.095f, 0.045f, 0.96f));
        AddOutline(imageFrame.gameObject, new Color(0.42f, 0.22f, 0.09f, 1f), new Vector2(2f, -2f));
        imageFrame.rectTransform.anchorMin = new Vector2(0.055f, 0.17f);
        imageFrame.rectTransform.anchorMax = new Vector2(0.66f, 0.82f);
        imageFrame.rectTransform.offsetMin = Vector2.zero;
        imageFrame.rectTransform.offsetMax = Vector2.zero;

        Image recipeImage = CreateImage("RecipeImage", imageFrame.rectTransform, new Color(1f, 1f, 1f, 0f));
        recipeImage.rectTransform.anchorMin = new Vector2(0.04f, 0.04f);
        recipeImage.rectTransform.anchorMax = new Vector2(0.96f, 0.96f);
        recipeImage.rectTransform.offsetMin = Vector2.zero;
        recipeImage.rectTransform.offsetMax = Vector2.zero;
        recipeImage.preserveAspect = true;

        Text ingredientsHeader = CreateText("IngredientsHeader", panelRect, new Vector2(0.705f, 0.69f), new Vector2(0.94f, 0.77f), 28f, FontStyle.Bold, TextAnchor.MiddleLeft);
        ingredientsHeader.text = "Ингредиенты";

        Text ingredientsText = CreateText("Ingredients", panelRect, new Vector2(0.705f, 0.36f), new Vector2(0.94f, 0.68f), 27f, FontStyle.Normal, TextAnchor.UpperLeft);
        Text effectText = CreateText("Effect", panelRect, new Vector2(0.705f, 0.21f), new Vector2(0.94f, 0.34f), 20f, FontStyle.Normal, TextAnchor.UpperLeft);
        Text pageText = CreateText("PageCounter", panelRect, new Vector2(0.38f, 0.05f), new Vector2(0.62f, 0.105f), 20f, FontStyle.Normal, TextAnchor.MiddleCenter);

        Image[] ingredientSlots = CreateIngredientSlots(panelRect);
        Button previousButton = CreateButton("PreviousPageButton", panelRect, "<", new Vector2(0.015f, 0.43f), new Vector2(0.06f, 0.57f));
        Button nextButton = CreateButton("NextPageButton", panelRect, ">", new Vector2(0.94f, 0.43f), new Vector2(0.985f, 0.57f));
        Button closeButton = CreateButton("CloseButton", panelRect, "X", new Vector2(0.94f, 0.885f), new Vector2(0.98f, 0.955f));

        canvasBookUi = panelBackground.gameObject.AddComponent<RecipeBookCanvasUI>();
        canvasBookUi.ConfigureRuntimeReferences(
            recipeChecker,
            this,
            panelBackground,
            recipeImage,
            ingredientSlots,
            titleText,
            ingredientsText,
            effectText,
            pageText,
            nextButton,
            previousButton,
            closeButton);

        canvasBookRoot.SetActive(false);
    }

    private void ConfigureScreenSpaceCanvas(GameObject rootObject)
    {
        screenBookCanvas = rootObject.GetComponent<Canvas>();
        if (screenBookCanvas == null)
            screenBookCanvas = rootObject.AddComponent<Canvas>();

        screenBookCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        screenBookCanvas.sortingOrder = 2000;
        screenBookCanvas.pixelPerfect = true;

        screenBookCanvasScaler = rootObject.GetComponent<CanvasScaler>();
        if (screenBookCanvasScaler == null)
            screenBookCanvasScaler = rootObject.AddComponent<CanvasScaler>();

        screenBookCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        screenBookCanvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        screenBookCanvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        screenBookCanvasScaler.matchWidthOrHeight = 0.5f;

        screenBookRaycaster = rootObject.GetComponent<GraphicRaycaster>();
        if (screenBookRaycaster == null)
            screenBookRaycaster = rootObject.AddComponent<GraphicRaycaster>();

        canvasRaycaster = screenBookRaycaster;
    }

    private static Image CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return image;
    }

    private static Text CreateText(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float fontSize,
        FontStyle fontStyle,
        TextAnchor alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.font = GetDefaultUiFont();
        text.color = new Color(0.14f, 0.08f, 0.035f, 1f);
        text.fontSize = Mathf.RoundToInt(fontSize);
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = false;
        return text;
    }

    private static Button CreateButton(string objectName, Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax)
    {
        Image buttonImage = CreateImage(objectName, parent, new Color(0.22f, 0.12f, 0.055f, 0.92f));
        AddOutline(buttonImage.gameObject, new Color(0.53f, 0.31f, 0.13f, 1f), new Vector2(1.5f, -1.5f));
        RectTransform buttonRect = buttonImage.rectTransform;
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        Button button = buttonImage.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = buttonImage.color;
        colors.highlightedColor = new Color(0.34f, 0.2f, 0.09f, 1f);
        colors.pressedColor = new Color(0.12f, 0.07f, 0.035f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text labelText = CreateText(
            "Label",
            buttonRect,
            Vector2.zero,
            Vector2.one,
            34,
            FontStyle.Bold,
            TextAnchor.MiddleCenter);
        labelText.text = label;
        labelText.color = new Color(0.98f, 0.93f, 0.78f, 1f);
        labelText.raycastTarget = false;

        return button;
    }

    private static Font GetDefaultUiFont()
    {
        Font font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Helvetica", "Liberation Sans" }, 18);
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return font;
    }

    private static Image[] CreateIngredientSlots(Transform parent)
    {
        const int slotCount = 5;
        Image[] slots = new Image[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            Image slot = CreateImage("IngredientSlot_" + (i + 1), parent, new Color(1f, 0.97f, 0.88f, 0.95f));
            RectTransform rect = slot.rectTransform;
            float start = 0.705f + i * 0.047f;
            rect.anchorMin = new Vector2(start, 0.24f);
            rect.anchorMax = new Vector2(start + 0.04f, 0.305f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            slot.preserveAspect = true;
            AddOutline(slot.gameObject, new Color(0.38f, 0.2f, 0.08f, 0.75f), new Vector2(1f, -1f));
            slots[i] = slot;
        }

        return slots;
    }

    private static void AddOutline(GameObject target, Color color, Vector2 distance)
    {
        Outline outline = target.GetComponent<Outline>();
        if (outline == null)
            outline = target.AddComponent<Outline>();

        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }

    private static void AddShadow(GameObject target, Color color, Vector2 distance)
    {
        Shadow shadow = target.GetComponent<Shadow>();
        if (shadow == null)
            shadow = target.AddComponent<Shadow>();

        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void CalculateOpenedPose(Camera mainCamera, out Vector3 targetPosition, out Quaternion targetRotation)
    {
        Transform cameraTransform = mainCamera.transform;
        targetPosition =
            cameraTransform.position
            + cameraTransform.forward * openDistance
            + cameraTransform.up * openHeightOffset
            + cameraTransform.right * openSideOffset;

        targetRotation =
            Quaternion.LookRotation(-cameraTransform.forward, cameraTransform.up)
            * Quaternion.Euler(openTilt, 0f, 0f);
    }

    private static float GetSharpFollowT(float sharpness, float deltaTime)
    {
        if (sharpness <= 0f)
            return 1f;

        return 1f - Mathf.Exp(-sharpness * deltaTime);
    }

    private static bool WasKeyboardKeyPressed(Key key)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        KeyControl keyControl = keyboard[key];
        return keyControl != null && keyControl.wasPressedThisFrame;
    }

    private void ShowNextSpread()
    {
        if (spreads.Count == 0)
            return;

        currentSpread++;
        if (currentSpread >= spreads.Count)
            currentSpread = 0;

        ShowCurrentSpread();
    }

    private void ShowNextPage()
    {
        if (UseWorldBookPages())
        {
            ShowNextSpread();
            return;
        }

        if (canvasBookUi != null)
            canvasBookUi.ShowNextRecipe();
    }

    private void ShowPreviousPage()
    {
        if (UseWorldBookPages())
        {
            ShowPreviousSpread();
            return;
        }

        if (canvasBookUi != null)
            canvasBookUi.ShowPreviousRecipe();
    }

    private void ShowPreviousSpread()
    {
        if (spreads.Count == 0)
            return;

        currentSpread--;
        if (currentSpread < 0)
            currentSpread = spreads.Count - 1;

        ShowCurrentSpread();
    }

    private void ShowCurrentSpread()
    {
        if (!UseWorldBookPages())
            return;

        for (int i = 0; i < spreads.Count; i++)
        {
            if (spreads[i] != null)
                spreads[i].SetActive(i == currentSpread);
        }
    }

    private void SetClosedBookVisible(bool isVisible)
    {
        for (int i = 0; i < closedBookRenderers.Count; i++)
        {
            if (closedBookRenderers[i] != null)
                closedBookRenderers[i].enabled = isVisible;
        }
    }

}
