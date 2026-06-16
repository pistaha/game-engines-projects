using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using XRCommonUsages = UnityEngine.XR.CommonUsages;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRInputDevices = UnityEngine.XR.InputDevices;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class RecipeBookViewer : MonoBehaviour
{
    private static readonly Vector3 FixedWorldBookMenuPosition = new Vector3(-4.66f, 1.52f, -1.26f);
    private static readonly Quaternion FixedWorldBookMenuRotation = Quaternion.Euler(0f, -90f, 0f);
    private const float FixedWorldBookMenuScale = 0.001f;

    [SerializeField] private FirstPersonCC playerController;
    [SerializeField] private ClickCollector clickCollector;
    [SerializeField] private List<Renderer> closedBookRenderers = new List<Renderer>();
    [SerializeField] private Collider clickCollider;
    [SerializeField] private GameObject spreadRoot;
    [SerializeField] private List<GameObject> spreads = new List<GameObject>();
    [SerializeField] private GameObject canvasBookRoot;
    [SerializeField] private GraphicRaycaster canvasRaycaster;
    [SerializeField] private bool useScreenSpaceBookMenu = true;
    [SerializeField] private Vector2 screenBookPanelSize = new Vector2(1280f, 780f);
    [SerializeField] private bool showWorldBookPages = false;

    [SerializeField] private float moveDuration = 0.5f;
    [SerializeField] private float openDistance = 1.08f;
    [SerializeField] private float openHeightOffset = -0.03f;
    [SerializeField] private float openSideOffset;
    [SerializeField] private float openScaleMultiplier = 1.98f;
    [SerializeField] private float openTilt = 1.8f;
    [SerializeField] private bool followCameraWhileOpen = true;
    [SerializeField] private float followCameraSharpness = 18f;
    [SerializeField] private bool lockVrLocomotionWhileOpen = true;
    [SerializeField] private Key keyboardCloseKey = Key.Escape;
    [SerializeField] private Key keyboardAltCloseKey = Key.B;
    [SerializeField] private Key keyboardNextPageKey = Key.RightArrow;
    [SerializeField] private Key keyboardPreviousPageKey = Key.LeftArrow;
    [SerializeField] private float spreadStartScale = 0.36f;
    [SerializeField] private float floatAmplitude = 0.008f;
    [SerializeField] private float floatSpeed = 1.9f;
    [SerializeField] private bool createVrNavigationHotspots = true;
    [SerializeField] private Vector3 nextHotspotLocalPosition = new Vector3(0.23f, 0.03f, 0.02f);
    [SerializeField] private Vector3 previousHotspotLocalPosition = new Vector3(-0.23f, 0.03f, 0.02f);
    [SerializeField] private Vector3 closeHotspotLocalPosition = new Vector3(0f, 0.17f, -0.02f);
    [SerializeField] private Vector3 hotspotSize = new Vector3(0.14f, 0.18f, 0.08f);

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
    private XRSimpleInteractable xrSimpleInteractable;
    private XRSimpleInteractable nextHotspotInteractable;
    private XRSimpleInteractable previousHotspotInteractable;
    private XRSimpleInteractable closeHotspotInteractable;
    private readonly List<Behaviour> disabledLocomotionBehaviours = new List<Behaviour>();
    private Canvas screenBookCanvas;
    private CanvasScaler screenBookCanvasScaler;
    private GraphicRaycaster screenBookRaycaster;
    private TrackedDeviceGraphicRaycaster trackedDeviceRaycaster;
    private bool cursorWasVisible;
    private CursorLockMode previousCursorLockState;
    private bool wasXrSecondaryButtonPressed;
    private bool wasXrPrimaryButtonPressed;
    private bool wasThumbstickPageHeld;

    public bool IsOpenForInteraction => isOpen && !isAnimating;

    private bool UseWorldBookPages()
    {
        if (useScreenSpaceBookMenu)
            return false;

        return showWorldBookPages || canvasBookRoot == null;
    }

    private void Awake()
    {
        // В VR книга рецептов должна быть стабильным UI в мире, а не overlay и не follow-camera панелью.
        useScreenSpaceBookMenu = true;
        followCameraWhileOpen = false;

        EnsureEventSystem();

        if (playerController == null)
            playerController = FindFirstObjectByType<FirstPersonCC>();

        if (clickCollector == null)
            clickCollector = FindFirstObjectByType<ClickCollector>();

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

        EnsureXrInteraction();
        EnsureVrNavigationHotspots();
    }

    private void OnEnable()
    {
        EnsureXrInteraction();
        EnsureVrNavigationHotspots();

        if (xrSimpleInteractable != null)
            xrSimpleInteractable.selectEntered.AddListener(OnXrSelectEntered);

        AddHotspotListeners();
    }

    private void OnDisable()
    {
        if (xrSimpleInteractable != null)
            xrSimpleInteractable.selectEntered.RemoveListener(OnXrSelectEntered);

        RemoveHotspotListeners();
        SetVrLocomotionEnabled(true);
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

        if (WasKeyboardKeyPressed(keyboardCloseKey) ||
            WasKeyboardKeyPressed(keyboardAltCloseKey) ||
            WasControllerClosePressed() ||
            WasXrSecondaryButtonPressed())
        {
            CloseBookFromButton();
        }

        if (WasKeyboardKeyPressed(keyboardNextPageKey))
            ShowNextPage();

        if (WasKeyboardKeyPressed(keyboardPreviousPageKey))
            ShowPreviousPage();

        if (WasXrPrimaryButtonPressed())
            ShowNextPage();

        int thumbstickPageDirection = GetThumbstickPageDirection();
        if (thumbstickPageDirection > 0)
            ShowNextPage();
        else if (thumbstickPageDirection < 0)
            ShowPreviousPage();
    }

    private void LateUpdate()
    {
        // Книга в VR не должна исчезать даже при открытом UI или внешнем изменении renderer.enabled.
        SetClosedBookVisible(true);
    }

    public void Interact()
    {
        if (isOpen || isAnimating)
            return;

        StartCoroutine(OpenBookRoutine());
    }

    private void EnsureXrInteraction()
    {
        if (TryGetComponent<XRGrabInteractable>(out _))
            return;

        xrSimpleInteractable = GetComponent<XRSimpleInteractable>();
        if (xrSimpleInteractable == null)
            xrSimpleInteractable = gameObject.AddComponent<XRSimpleInteractable>();

        if (xrSimpleInteractable.interactionManager == null)
            xrSimpleInteractable.interactionManager = FindFirstObjectByType<XRInteractionManager>();

        xrSimpleInteractable.selectMode = InteractableSelectMode.Single;
    }

    private void OnXrSelectEntered(SelectEnterEventArgs args)
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
        isAnimating = true;
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalScale = transform.localScale;
        previousCursorLockState = Cursor.lockState;
        cursorWasVisible = Cursor.visible;

        if (playerController != null)
            playerController.SetInputEnabled(false);

        if (clickCollector != null)
            clickCollector.enabled = false;

        if (clickCollider != null)
            clickCollider.enabled = false;

        SetVrLocomotionEnabled(false);

        if (useScreenSpaceBookMenu)
        {
            OpenScreenSpaceBookMenu();
            yield break;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            RestoreInteractionAfterFailedOpen();
            yield break;
        }

        if (spreadRoot != null && UseWorldBookPages())
        {
            spreadRoot.SetActive(true);
            spreadRoot.transform.localScale = Vector3.one * spreadStartScale;
        }

        SetNavigationHotspotsActive(true);

        if (canvasBookRoot != null)
            canvasBookRoot.SetActive(true);

        if (canvasRaycaster != null)
            canvasRaycaster.enabled = true;

        if (canvasBookUi != null)
            canvasBookUi.RefreshBook();

        if (UseWorldBookPages())
        {
            currentSpread = 0;
            ShowCurrentSpread();
        }

        CalculateOpenedPose(mainCamera, out Vector3 targetPosition, out Quaternion targetRotation);

        Vector3 targetScale = originalScale * openScaleMultiplier;
        float time = 0f;

        while (time < moveDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / moveDuration);
            float smoothT = t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(originalPosition, targetPosition, smoothT);
            transform.rotation = Quaternion.Slerp(originalRotation, targetRotation, smoothT);
            transform.localScale = Vector3.Lerp(originalScale, targetScale, smoothT);

            if (spreadRoot != null && UseWorldBookPages())
            {
                float spreadT = Mathf.InverseLerp(0.2f, 1f, t);
                float smoothSpreadT = spreadT * spreadT * (3f - 2f * spreadT);
                float spreadScale = Mathf.Lerp(spreadStartScale, 1f, smoothSpreadT);
                spreadRoot.transform.localScale = Vector3.one * spreadScale;
            }

            yield return null;
        }

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

        SetNavigationHotspotsActive(false);

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

        if (clickCollector != null)
            clickCollector.enabled = true;

        if (clickCollider != null)
            clickCollider.enabled = true;

        SetVrLocomotionEnabled(true);

        isOpen = false;
        isAnimating = false;
    }

    private void OpenScreenSpaceBookMenu()
    {
        EnsureScreenSpaceBookMenu();
        ForceRecipeBookWorldMenuPose();

        SetCloseHotspotActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (canvasBookRoot != null)
            canvasBookRoot.SetActive(true);

        if (canvasRaycaster != null)
            canvasRaycaster.enabled = true;

        if (canvasBookUi != null)
            canvasBookUi.RefreshBook();

        openedPosition = originalPosition;
        openedRotation = originalRotation;
        openedScale = originalScale;
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
        // В screen-space режиме книга не проигрывает обратную анимацию, поэтому возвращаем transform вручную.
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        transform.localScale = originalScale;
        openedPosition = originalPosition;
        openedRotation = originalRotation;
        openedScale = originalScale;

        if (playerController != null)
            playerController.SetInputEnabled(true);

        if (clickCollector != null)
            clickCollector.enabled = true;

        if (clickCollider != null)
            clickCollider.enabled = true;

        SetVrLocomotionEnabled(true);

        Cursor.lockState = previousCursorLockState;
        Cursor.visible = cursorWasVisible;

        isOpen = false;
        isAnimating = false;
    }

    private void EnsureScreenSpaceBookMenu()
    {
        if (canvasBookRoot != null && canvasBookUi != null)
        {
            ConfigureWorldBookCanvas(canvasBookRoot);
            ForceRecipeBookWorldMenuPose();
            return;
        }

        GameObject canvasObject = new GameObject(
            "RecipeBook_WorldMenu",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(TrackedDeviceGraphicRaycaster));
        canvasBookRoot = canvasObject;
        canvasObject.SetActive(false);

        ConfigureWorldBookCanvas(canvasObject);
        ForceRecipeBookWorldMenuPose();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        SetCanvasRootRect(canvasRect);

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
        RectTransform titleStripRect = titleStrip.rectTransform;
        titleStripRect.anchorMin = new Vector2(0f, 0.86f);
        titleStripRect.anchorMax = Vector2.one;
        titleStripRect.offsetMin = Vector2.zero;
        titleStripRect.offsetMax = Vector2.zero;

        TMP_Text titleText = CreateText("Title", panelRect, new Vector2(0.1f, 0.875f), new Vector2(0.9f, 0.965f), 46, FontStyles.Bold, TextAlignmentOptions.Center);
        titleText.color = new Color(1f, 0.92f, 0.68f, 1f);

        Image imageFrame = CreateImage("RecipeImageFrame", panelRect, new Color(0.18f, 0.095f, 0.045f, 0.96f));
        AddOutline(imageFrame.gameObject, new Color(0.42f, 0.22f, 0.09f, 1f), new Vector2(2f, -2f));
        RectTransform frameRect = imageFrame.rectTransform;
        frameRect.anchorMin = new Vector2(0.055f, 0.17f);
        frameRect.anchorMax = new Vector2(0.66f, 0.82f);
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;

        Image recipeImage = CreateImage("RecipeImage", frameRect, new Color(1f, 1f, 1f, 0f));
        RectTransform imageRect = recipeImage.rectTransform;
        imageRect.anchorMin = new Vector2(0.04f, 0.04f);
        imageRect.anchorMax = new Vector2(0.96f, 0.96f);
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;
        recipeImage.preserveAspect = true;

        TMP_Text ingredientsHeader = CreateText("IngredientsHeader", panelRect, new Vector2(0.705f, 0.69f), new Vector2(0.94f, 0.77f), 28, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        ingredientsHeader.text = "Ингредиенты";
        ingredientsHeader.color = new Color(0.18f, 0.09f, 0.035f, 1f);

        TMP_Text ingredientsText = CreateText("Ingredients", panelRect, new Vector2(0.705f, 0.21f), new Vector2(0.94f, 0.68f), 27, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        TMP_Text pageText = CreateText("PageCounter", panelRect, new Vector2(0.38f, 0.05f), new Vector2(0.62f, 0.105f), 20, FontStyles.Normal, TextAlignmentOptions.Center);

        Button previousButton = CreateButton("PreviousPageButton", panelRect, "<", new Vector2(0.015f, 0.43f), new Vector2(0.06f, 0.57f));
        Button nextButton = CreateButton("NextPageButton", panelRect, ">", new Vector2(0.94f, 0.43f), new Vector2(0.985f, 0.57f));
        Button closeButton = CreateButton("CloseButton", panelRect, "X", new Vector2(0.94f, 0.885f), new Vector2(0.98f, 0.955f));

        canvasBookUi = panelBackground.gameObject.AddComponent<RecipeBookCanvasUI>();
        canvasBookUi.ConfigureRuntimeReferences(
            FindFirstObjectByType<RecipeChecker>(),
            this,
            panelBackground,
            recipeImage,
            titleText,
            ingredientsText,
            pageText,
            nextButton,
            previousButton,
            closeButton);

        canvasObject.SetActive(false);
    }

    private void ConfigureWorldBookCanvas(GameObject rootObject)
    {
        screenBookCanvas = rootObject.GetComponent<Canvas>();
        if (screenBookCanvas == null)
            screenBookCanvas = rootObject.AddComponent<Canvas>();

        screenBookCanvas.renderMode = RenderMode.WorldSpace;
        screenBookCanvas.worldCamera = Camera.main;
        screenBookCanvas.sortingOrder = 2000;
        screenBookCanvas.pixelPerfect = false;

        screenBookCanvasScaler = rootObject.GetComponent<CanvasScaler>();
        if (screenBookCanvasScaler == null)
            screenBookCanvasScaler = rootObject.AddComponent<CanvasScaler>();

        screenBookCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        screenBookCanvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        screenBookCanvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        screenBookCanvasScaler.matchWidthOrHeight = 0.5f;

        screenBookRaycaster = rootObject.GetComponent<GraphicRaycaster>();
        if (screenBookRaycaster == null)
            screenBookRaycaster = rootObject.AddComponent<GraphicRaycaster>();

        trackedDeviceRaycaster = rootObject.GetComponent<TrackedDeviceGraphicRaycaster>();
        if (trackedDeviceRaycaster == null)
            trackedDeviceRaycaster = rootObject.AddComponent<TrackedDeviceGraphicRaycaster>();

        canvasRaycaster = screenBookRaycaster;
    }

    private void ForceRecipeBookWorldMenuPose()
    {
        if (canvasBookRoot == null)
            return;

        canvasBookRoot.transform.SetParent(null, false);

        RectTransform rectTransform = canvasBookRoot.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            SetCanvasRootRect(rectTransform);
            rectTransform.anchoredPosition3D = FixedWorldBookMenuPosition;
            rectTransform.localRotation = FixedWorldBookMenuRotation;
            rectTransform.localScale = Vector3.one * FixedWorldBookMenuScale;
        }
        else
        {
            canvasBookRoot.transform.localPosition = FixedWorldBookMenuPosition;
            canvasBookRoot.transform.localRotation = FixedWorldBookMenuRotation;
            canvasBookRoot.transform.localScale = Vector3.one * FixedWorldBookMenuScale;
        }

        canvasBookRoot.transform.SetPositionAndRotation(FixedWorldBookMenuPosition, FixedWorldBookMenuRotation);
        canvasBookRoot.transform.localScale = Vector3.one * FixedWorldBookMenuScale;
    }

    private void RestoreInteractionAfterFailedOpen()
    {
        if (playerController != null)
            playerController.SetInputEnabled(true);

        if (clickCollector != null)
            clickCollector.enabled = true;

        if (clickCollider != null)
            clickCollider.enabled = true;

        SetVrLocomotionEnabled(true);
        isAnimating = false;
    }

    private static void SetCanvasRootRect(RectTransform rectTransform)
    {
        if (rectTransform == null)
            return;

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(1920f, 1080f);
        rectTransform.anchoredPosition = Vector2.zero;
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

    private static Image CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return image;
    }

    private static TMP_Text CreateText(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.color = new Color(0.14f, 0.08f, 0.035f, 1f);
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
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

        TMP_Text labelText = CreateText("Label", buttonRect, Vector2.zero, Vector2.one, 34, FontStyles.Bold, TextAlignmentOptions.Center);
        labelText.text = label;
        labelText.color = new Color(0.98f, 0.93f, 0.78f, 1f);
        labelText.raycastTarget = false;

        return button;
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

    private void SetVrLocomotionEnabled(bool enabled)
    {
        if (!lockVrLocomotionWhileOpen)
            return;

        if (enabled)
        {
            for (int i = 0; i < disabledLocomotionBehaviours.Count; i++)
            {
                Behaviour behaviour = disabledLocomotionBehaviours[i];
                if (behaviour != null)
                    behaviour.enabled = true;
            }

            disabledLocomotionBehaviours.Clear();
            return;
        }

        disabledLocomotionBehaviours.Clear();
        // The open book owns the player's attention, so XR movement is paused until it closes.
        Behaviour[] behaviours = FindObjectsByType<Behaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < behaviours.Length; i++)
        {
            Behaviour behaviour = behaviours[i];
            if (behaviour == null || !behaviour.enabled || behaviour.gameObject.scene != gameObject.scene)
                continue;

            if (!IsVrLocomotionBehaviour(behaviour))
                continue;

            behaviour.enabled = false;
            disabledLocomotionBehaviours.Add(behaviour);
        }
    }

    private static bool IsVrLocomotionBehaviour(Behaviour behaviour)
    {
        string typeName = behaviour.GetType().Name;
        return typeName.Contains("MoveProvider")
            || typeName.Contains("TurnProvider")
            || typeName.Contains("TeleportationProvider")
            || typeName.Contains("LocomotionProvider")
            || typeName.Contains("LocomotionMediator");
    }

    private static bool WasKeyboardKeyPressed(Key key)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        KeyControl keyControl = keyboard[key];
        return keyControl != null && keyControl.wasPressedThisFrame;
    }

    private static bool WasControllerClosePressed()
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad != null && gamepad.buttonEast.wasPressedThisFrame)
            return true;

        for (int i = 0; i < InputSystem.devices.Count; i++)
        {
            InputDevice device = InputSystem.devices[i];
            if (device == null || !device.enabled)
                continue;

            if (WasDeviceButtonPressed(device, "secondaryButton") || WasDeviceButtonPressed(device, "buttonEast"))
                return true;
        }

        return false;
    }

    private bool WasXrSecondaryButtonPressed()
    {
        bool isPressed = false;
        List<XRInputDevice> devices = new List<XRInputDevice>();
        XRInputDevices.GetDevices(devices);

        for (int i = 0; i < devices.Count; i++)
        {
            XRInputDevice device = devices[i];
            if (!device.isValid)
                continue;

            if (device.TryGetFeatureValue(XRCommonUsages.secondaryButton, out bool secondaryPressed) && secondaryPressed)
            {
                isPressed = true;
                break;
            }
        }

        bool wasPressedThisFrame = isPressed && !wasXrSecondaryButtonPressed;
        wasXrSecondaryButtonPressed = isPressed;
        return wasPressedThisFrame;
    }

    private bool WasXrPrimaryButtonPressed()
    {
        bool isPressed = false;

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null && gamepad.buttonSouth.isPressed)
            isPressed = true;

        if (!isPressed)
        {
            for (int i = 0; i < InputSystem.devices.Count; i++)
            {
                InputDevice device = InputSystem.devices[i];
                if (device == null || !device.enabled)
                    continue;

                if (IsDeviceButtonPressed(device, "primaryButton") || IsDeviceButtonPressed(device, "buttonSouth"))
                {
                    isPressed = true;
                    break;
                }
            }
        }

        if (!isPressed)
        {
            List<XRInputDevice> devices = new List<XRInputDevice>();
            XRInputDevices.GetDevices(devices);

            for (int i = 0; i < devices.Count; i++)
            {
                XRInputDevice device = devices[i];
                if (!device.isValid)
                    continue;

                if (device.TryGetFeatureValue(XRCommonUsages.primaryButton, out bool primaryPressed) && primaryPressed)
                {
                    isPressed = true;
                    break;
                }
            }
        }

        bool wasPressedThisFrame = isPressed && !wasXrPrimaryButtonPressed;
        wasXrPrimaryButtonPressed = isPressed;
        return wasPressedThisFrame;
    }

    private int GetThumbstickPageDirection()
    {
        const float threshold = 0.65f;
        float horizontal = 0f;

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
            horizontal = gamepad.leftStick.ReadValue().x;

        List<XRInputDevice> devices = new List<XRInputDevice>();
        XRInputDevices.GetDevices(devices);

        for (int i = 0; i < devices.Count; i++)
        {
            XRInputDevice device = devices[i];
            if (!device.isValid)
                continue;

            if (device.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out Vector2 axis) &&
                Mathf.Abs(axis.x) > Mathf.Abs(horizontal))
            {
                horizontal = axis.x;
            }
        }

        bool isHeld = Mathf.Abs(horizontal) >= threshold;
        if (!isHeld)
        {
            wasThumbstickPageHeld = false;
            return 0;
        }

        if (wasThumbstickPageHeld)
            return 0;

        // Одно отклонение стика = одно перелистывание; удержание не пролистывает всю книгу.
        wasThumbstickPageHeld = true;
        return horizontal > 0f ? 1 : -1;
    }

    private static bool WasDeviceButtonPressed(InputDevice device, string controlName)
    {
        ButtonControl control = device.TryGetChildControl<ButtonControl>(controlName);
        return control != null && control.wasPressedThisFrame;
    }

    private static bool IsDeviceButtonPressed(InputDevice device, string controlName)
    {
        ButtonControl control = device.TryGetChildControl<ButtonControl>(controlName);
        return control != null && control.isPressed;
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

    private void ShowPreviousSpread()
    {
        if (spreads.Count == 0)
            return;

        currentSpread--;
        if (currentSpread < 0)
            currentSpread = spreads.Count - 1;

        ShowCurrentSpread();
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

    private void EnsureVrNavigationHotspots()
    {
        if (!createVrNavigationHotspots)
            return;

        nextHotspotInteractable = EnsureHotspot(
            "BookNextHotspot",
            nextHotspotLocalPosition,
            hotspotSize,
            ref nextHotspotInteractable);
        previousHotspotInteractable = EnsureHotspot(
            "BookPreviousHotspot",
            previousHotspotLocalPosition,
            hotspotSize,
            ref previousHotspotInteractable);
        closeHotspotInteractable = EnsureHotspot(
            "BookCloseHotspot",
            closeHotspotLocalPosition,
            hotspotSize,
            ref closeHotspotInteractable);
    }

    private XRSimpleInteractable EnsureHotspot(
        string hotspotName,
        Vector3 localPosition,
        Vector3 colliderSize,
        ref XRSimpleInteractable hotspotInteractable)
    {
        if (hotspotInteractable != null)
        {
            if (hotspotName == "BookCloseHotspot")
                EnsureCloseHotspotVisual(hotspotInteractable.gameObject);

            return hotspotInteractable;
        }

        Transform existing = transform.Find(hotspotName);
        GameObject hotspotObject = existing != null ? existing.gameObject : new GameObject(hotspotName);
        hotspotObject.transform.SetParent(transform, false);
        hotspotObject.transform.localPosition = localPosition;
        hotspotObject.transform.localRotation = Quaternion.identity;
        hotspotObject.transform.localScale = Vector3.one;

        BoxCollider boxCollider = hotspotObject.GetComponent<BoxCollider>();
        if (boxCollider == null)
            boxCollider = hotspotObject.AddComponent<BoxCollider>();
        boxCollider.isTrigger = false;
        boxCollider.size = colliderSize;

        hotspotInteractable = hotspotObject.GetComponent<XRSimpleInteractable>();
        if (hotspotInteractable == null)
            hotspotInteractable = hotspotObject.AddComponent<XRSimpleInteractable>();

        if (hotspotInteractable.interactionManager == null)
            hotspotInteractable.interactionManager = FindFirstObjectByType<XRInteractionManager>();

        hotspotInteractable.selectMode = InteractableSelectMode.Single;

        if (hotspotName == "BookCloseHotspot")
            EnsureCloseHotspotVisual(hotspotObject);

        hotspotObject.SetActive(false);
        return hotspotInteractable;
    }

    private static void EnsureCloseHotspotVisual(GameObject hotspotObject)
    {
        Transform existingLabel = hotspotObject.transform.Find("CloseCross");
        TextMeshPro label = existingLabel != null ? existingLabel.GetComponent<TextMeshPro>() : null;

        if (label == null)
        {
            GameObject labelObject = new GameObject("CloseCross", typeof(TextMeshPro));
            labelObject.transform.SetParent(hotspotObject.transform, false);
            labelObject.transform.localPosition = Vector3.zero;
            labelObject.transform.localRotation = Quaternion.identity;
            labelObject.transform.localScale = Vector3.one * 0.045f;
            label = labelObject.GetComponent<TextMeshPro>();
        }

        label.text = "X";
        label.fontSize = 8f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.08f, 0.04f, 1f);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.rectTransform.sizeDelta = new Vector2(2f, 2f);
    }

    private void AddHotspotListeners()
    {
        if (nextHotspotInteractable != null)
            nextHotspotInteractable.selectEntered.AddListener(OnNextHotspotSelected);
        if (previousHotspotInteractable != null)
            previousHotspotInteractable.selectEntered.AddListener(OnPreviousHotspotSelected);
        if (closeHotspotInteractable != null)
            closeHotspotInteractable.selectEntered.AddListener(OnCloseHotspotSelected);
    }

    private void RemoveHotspotListeners()
    {
        if (nextHotspotInteractable != null)
            nextHotspotInteractable.selectEntered.RemoveListener(OnNextHotspotSelected);
        if (previousHotspotInteractable != null)
            previousHotspotInteractable.selectEntered.RemoveListener(OnPreviousHotspotSelected);
        if (closeHotspotInteractable != null)
            closeHotspotInteractable.selectEntered.RemoveListener(OnCloseHotspotSelected);
    }

    private void OnNextHotspotSelected(SelectEnterEventArgs args)
    {
        if (isOpen && !isAnimating)
            ShowNextPage();
    }

    private void OnPreviousHotspotSelected(SelectEnterEventArgs args)
    {
        if (isOpen && !isAnimating)
            ShowPreviousPage();
    }

    private void OnCloseHotspotSelected(SelectEnterEventArgs args)
    {
        if (isOpen && !isAnimating)
            StartCoroutine(CloseBookRoutine());
    }

    private void SetNavigationHotspotsActive(bool isActive)
    {
        if (nextHotspotInteractable != null)
            nextHotspotInteractable.gameObject.SetActive(isActive);
        if (previousHotspotInteractable != null)
            previousHotspotInteractable.gameObject.SetActive(isActive);
        if (closeHotspotInteractable != null)
            closeHotspotInteractable.gameObject.SetActive(isActive);
    }

    private void SetCloseHotspotActive(bool isActive)
    {
        if (nextHotspotInteractable != null)
            nextHotspotInteractable.gameObject.SetActive(false);
        if (previousHotspotInteractable != null)
            previousHotspotInteractable.gameObject.SetActive(false);
        if (closeHotspotInteractable != null)
            closeHotspotInteractable.gameObject.SetActive(isActive);
    }
}
