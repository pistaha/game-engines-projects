using System.Linq;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.UI;
using InputTrackedPoseDriver = UnityEngine.InputSystem.XR.TrackedPoseDriver;

public class VrGrabTestCubeBootstrap : MonoBehaviour
{
    private const string LeftDirectInteractorName = "Left Direct Interactor";
    private const string RightDirectInteractorName = "Right Direct Interactor";

    [Header("Runtime XR Setup")]
    [SerializeField] private bool configureVrSceneOnStart = true;
    [SerializeField] private bool disableLegacyDesktopControllers = true;
    [SerializeField] private bool ensureXrUiEventSystem = true;
    [SerializeField] private bool ensureTrackedDeviceRaycasters = true;
    [SerializeField] private bool configureCraftingVrObjects = true;
    [SerializeField] private bool ensureTeleportation = true;
    [SerializeField] private bool ensurePauseMenu = true;

    [Header("Optional Test Cube")]
    [SerializeField] private bool spawnTestCube = false;
    [SerializeField] private string cubeName = "VR_Grab_Test_Cube";
    [SerializeField] private float spawnDistance = 1.8f;
    [SerializeField] private float spawnHeight = 1.2f;
    [SerializeField] private Vector3 cubeScale = new Vector3(0.25f, 0.25f, 0.25f);

    private int addedComponentCount;
    private int skippedExistingCount;

    private void Awake()
    {
        if (configureVrSceneOnStart)
            ConfigureVrScene();
    }

    private void Start()
    {
        if (!spawnTestCube)
            return;

        if (GameObject.Find(cubeName) != null)
            return;

        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = cubeName;
        cube.transform.position = transform.position + transform.forward * spawnDistance + Vector3.up * spawnHeight;
        cube.transform.localScale = cubeScale;

        Rigidbody rb = cube.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        XRGrabInteractable grabInteractable = cube.AddComponent<XRGrabInteractable>();
        grabInteractable.interactionManager = FindFirstObjectByType<XRInteractionManager>();
        grabInteractable.selectMode = InteractableSelectMode.Single;
        grabInteractable.movementType = XRGrabInteractable.MovementType.Kinematic;
        grabInteractable.trackPosition = true;
        grabInteractable.trackRotation = true;
        grabInteractable.throwOnDetach = true;
        grabInteractable.retainTransformParent = false;
    }

    private void ConfigureVrScene()
    {
        addedComponentCount = 0;
        skippedExistingCount = 0;

        if (disableLegacyDesktopControllers)
            DisableLegacyDesktopControllers();

        if (ensureXrUiEventSystem)
            EnsureEventSystemUsesXrUiInputModule();

        if (ensureTrackedDeviceRaycasters)
            EnsureTrackedDeviceGraphicRaycasters();

        if (configureCraftingVrObjects)
            EnsureCraftingVrObjects();

        if (ensureTeleportation)
            EnsureTeleportationSetup();

        if (ensurePauseMenu)
            EnsurePauseMenuSetup();

        Debug.Log($"VR bootstrap completed. Added {addedComponentCount} missing components, kept {skippedExistingCount} existing components.", this);
    }

    private static void DisableLegacyDesktopControllers()
    {
        FirstPersonCC[] desktopControllers = FindObjectsByType<FirstPersonCC>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < desktopControllers.Length; i++)
            desktopControllers[i].enabled = false;

        ClickCollector[] clickCollectors = FindObjectsByType<ClickCollector>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < clickCollectors.Length; i++)
            clickCollectors[i].enabled = false;
    }

    private static void EnsureEventSystemUsesXrUiInputModule()
    {
        EventSystem eventSystem = GetOrCreateSingleEventSystem();

        StandaloneInputModule standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneInputModule != null)
            standaloneInputModule.enabled = false;

        InputSystemUIInputModule inputSystemUiModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputSystemUiModule != null)
            inputSystemUiModule.enabled = false;

        XRUIInputModule xrUiInputModule = eventSystem.GetComponent<XRUIInputModule>();
        if (xrUiInputModule == null)
            xrUiInputModule = eventSystem.gameObject.AddComponent<XRUIInputModule>();

        xrUiInputModule.enabled = true;
    }

    private static EventSystem GetOrCreateSingleEventSystem()
    {
        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        EventSystem primary = EventSystem.current;

        if (primary == null)
            primary = eventSystems.FirstOrDefault(eventSystem => eventSystem != null && eventSystem.isActiveAndEnabled);

        if (primary == null)
            primary = eventSystems.FirstOrDefault(eventSystem => eventSystem != null);

        if (primary == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            primary = eventSystemObject.AddComponent<EventSystem>();
        }

        for (int i = 0; i < eventSystems.Length; i++)
        {
            EventSystem duplicate = eventSystems[i];
            if (duplicate == null || duplicate == primary)
                continue;

            duplicate.enabled = false;

            StandaloneInputModule standaloneInputModule = duplicate.GetComponent<StandaloneInputModule>();
            if (standaloneInputModule != null)
                standaloneInputModule.enabled = false;

            InputSystemUIInputModule inputSystemUiInputModule = duplicate.GetComponent<InputSystemUIInputModule>();
            if (inputSystemUiInputModule != null)
                inputSystemUiInputModule.enabled = false;

            XRUIInputModule xrUiInputModule = duplicate.GetComponent<XRUIInputModule>();
            if (xrUiInputModule != null)
                xrUiInputModule.enabled = false;
        }

        primary.enabled = true;
        return primary;
    }

    private static void EnsureTrackedDeviceGraphicRaycasters()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null)
                continue;

            GraphicRaycaster graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
            if (graphicRaycaster == null)
                continue;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                Debug.LogWarning($"Canvas '{canvas.name}' is still Screen Space Overlay. Convert it to World Space for VR.", canvas);
                continue;
            }

            if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        }
    }

    private void EnsureCraftingVrObjects()
    {
        EnsureDirectGrabControllers();
        EnsureCauldronSetup();
        EnsureSpoonSetup();
        EnsureIngredientSetup();
    }

    private void EnsureDirectGrabControllers()
    {
        EnsureControllerInteractorSetup(GameObject.Find("Left Controller"), InteractorHandedness.Left);
        EnsureControllerInteractorSetup(GameObject.Find("Right Controller") ?? GameObject.Find("Right Controller "), InteractorHandedness.Right);
    }

    private void EnsureControllerInteractorSetup(GameObject controller, InteractorHandedness handedness)
    {
        if (controller == null)
            return;

        EnsureTrackedPoseDriver(controller, handedness);
        EnsureRayInteractor(controller, handedness);
        EnsureDirectInteractorChild(controller, handedness);
        EnsureInteractorInputs(controller, handedness);
    }

    private void EnsureCauldronSetup()
    {
        CauldronReactions cauldron = FindFirstObjectByType<CauldronReactions>();
        if (cauldron == null)
            return;

        CraftStorage storage = cauldron.GetComponent<CraftStorage>();
        RecipeChecker recipeChecker = cauldron.GetComponent<RecipeChecker>();
        if (storage == null || recipeChecker == null)
        {
            Debug.LogWarning("VR bootstrap skipped partial cauldron setup because CraftStorage or RecipeChecker is missing.", cauldron);
            return;
        }

        Renderer cauldronRenderer = cauldron.GetComponentInChildren<Renderer>();
        Bounds bounds = cauldronRenderer != null
            ? cauldronRenderer.bounds
            : new Bounds(cauldron.transform.position, new Vector3(0.8f, 1f, 0.8f));

        Vector3 ingredientCenterWorld = bounds.center + Vector3.up * bounds.extents.y * 0.25f;
        Vector3 stirCenterWorld = bounds.center + Vector3.up * bounds.extents.y * 0.55f;

        Vector3 ingredientSize = new Vector3(
            Mathf.Max(0.35f, bounds.size.x * 0.55f),
            Mathf.Max(0.25f, bounds.size.y * 0.45f),
            Mathf.Max(0.35f, bounds.size.z * 0.55f));

        Vector3 stirSize = new Vector3(
            Mathf.Max(0.2f, bounds.size.x * 0.35f),
            Mathf.Max(0.22f, bounds.size.y * 0.3f),
            Mathf.Max(0.2f, bounds.size.z * 0.35f));

        EnsureZone<CauldronIngredientZone>(cauldron.transform, "IngredientZone", ingredientCenterWorld, ingredientSize, cauldron);
        EnsureZone<CauldronStirZone>(cauldron.transform, "StirZone", stirCenterWorld, stirSize, cauldron);
    }

    private void EnsureSpoonSetup()
    {
        GameObject spoon = GameObject.Find("Spoon_1") ?? GameObject.Find("Spoon_1_final");
        if (spoon == null)
            return;

        Rigidbody rb = spoon.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = spoon.AddComponent<Rigidbody>();
            addedComponentCount++;
            rb.mass = 1f;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            Debug.Log("VR bootstrap added missing Rigidbody to spoon.", spoon);
        }
        else
        {
            skippedExistingCount++;
        }

        BoxCollider boxCollider = spoon.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = spoon.AddComponent<BoxCollider>();
            boxCollider.isTrigger = false;
            addedComponentCount++;
            Debug.Log("VR bootstrap added missing BoxCollider to spoon.", spoon);
        }
        else
        {
            skippedExistingCount++;
        }

        EnsureGrabInteractable(spoon, rb);

        StirringSpoon stirringSpoon = spoon.GetComponent<StirringSpoon>();
        if (stirringSpoon == null)
        {
            spoon.AddComponent<StirringSpoon>();
            addedComponentCount++;
            Debug.Log("VR bootstrap added missing StirringSpoon to spoon.", spoon);
        }
        else
        {
            skippedExistingCount++;
        }

        XRSimpleInteractable simpleInteractable = spoon.GetComponent<XRSimpleInteractable>();
        if (simpleInteractable != null)
            simpleInteractable.enabled = false;
    }

    private void EnsureIngredientSetup()
    {
        CollectableItem[] collectableItems = FindObjectsByType<CollectableItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < collectableItems.Length; i++)
        {
            CollectableItem item = collectableItems[i];
            if (item == null)
                continue;

            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = item.gameObject.AddComponent<Rigidbody>();
                rb.mass = 1f;
                rb.useGravity = true;
                rb.isKinematic = false;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                addedComponentCount++;
                Debug.Log($"VR bootstrap added missing Rigidbody to ingredient '{item.name}'.", item);
            }
            else
            {
                skippedExistingCount++;
            }

            Collider collider = item.GetComponent<Collider>();
            if (collider == null)
            {
                collider = item.gameObject.AddComponent<BoxCollider>();
                collider.isTrigger = false;
                addedComponentCount++;
                Debug.Log($"VR bootstrap added missing Collider to ingredient '{item.name}'.", item);
            }
            else
            {
                skippedExistingCount++;
            }

            EnsureGrabInteractable(item.gameObject, rb);

            XRSimpleInteractable simpleInteractable = item.GetComponent<XRSimpleInteractable>();
            if (simpleInteractable != null)
                simpleInteractable.enabled = false;
        }
    }

    private void EnsureTeleportationSetup()
    {
        XROrigin origin = FindFirstObjectByType<XROrigin>();
        GameObject locomotionRoot = origin != null ? origin.gameObject : gameObject;

        LocomotionMediator mediator = locomotionRoot.GetComponent<LocomotionMediator>();
        if (mediator == null)
        {
            mediator = locomotionRoot.AddComponent<LocomotionMediator>();
            addedComponentCount++;
        }
        else
        {
            skippedExistingCount++;
        }

        XRBodyTransformer bodyTransformer = locomotionRoot.GetComponent<XRBodyTransformer>();
        if (bodyTransformer != null && origin != null && bodyTransformer.xrOrigin == null)
            bodyTransformer.xrOrigin = origin;

        TeleportationProvider provider = locomotionRoot.GetComponent<TeleportationProvider>();
        if (provider == null)
        {
            provider = locomotionRoot.AddComponent<TeleportationProvider>();
            addedComponentCount++;
        }
        else
        {
            skippedExistingCount++;
        }

        provider.mediator = mediator;
        provider.delayTime = 0f;

        TeleportationArea area = FindFirstObjectByType<TeleportationArea>(FindObjectsInactive.Include);
        if (area == null)
        {
            GameObject areaObject = new GameObject("Runtime Teleportation Area");
            areaObject.transform.SetPositionAndRotation(GetTeleportAreaCenter(), Quaternion.identity);

            BoxCollider collider = areaObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(12f, 0.05f, 12f);
            collider.center = Vector3.zero;
            collider.isTrigger = false;

            area = areaObject.AddComponent<TeleportationArea>();
            addedComponentCount += 2;
        }
        else
        {
            skippedExistingCount++;
        }

        XRInteractionManager interactionManager = FindFirstObjectByType<XRInteractionManager>();
        if (interactionManager != null)
            area.interactionManager = interactionManager;

        area.teleportationProvider = provider;
        area.teleportTrigger = BaseTeleportationInteractable.TeleportTrigger.OnSelectExited;
        area.matchOrientation = MatchOrientation.WorldSpaceUp;
        area.filterSelectionByHitNormal = true;
        area.interactionLayers = -1;
    }

    private static Vector3 GetTeleportAreaCenter()
    {
        CauldronReactions cauldron = FindFirstObjectByType<CauldronReactions>();
        if (cauldron != null)
            return new Vector3(cauldron.transform.position.x, 0f, cauldron.transform.position.z);

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            return new Vector3(mainCamera.transform.position.x, 0f, mainCamera.transform.position.z);

        return Vector3.zero;
    }

    private void EnsurePauseMenuSetup()
    {
        if (FindFirstObjectByType<VRPauseMenu>(FindObjectsInactive.Include) != null)
        {
            skippedExistingCount++;
            return;
        }

        Camera mainCamera = Camera.main;
        GameObject host = mainCamera != null ? mainCamera.gameObject : gameObject;
        host.AddComponent<VRPauseMenu>();
        addedComponentCount++;
    }

    private void EnsureDirectInteractorChild(GameObject controller, InteractorHandedness handedness)
    {
        if (controller == null)
            return;

        XRInteractionManager interactionManager = FindFirstObjectByType<XRInteractionManager>();
        if (interactionManager == null)
        {
            Debug.LogWarning($"VR bootstrap skipped direct interactor setup for '{controller.name}' because XR Interaction Manager was not found.", controller);
            return;
        }

        string childName = handedness == InteractorHandedness.Left ? LeftDirectInteractorName : RightDirectInteractorName;
        XRDirectInteractor directInteractor = controller.GetComponentsInChildren<XRDirectInteractor>(true)
            .FirstOrDefault(interactor => interactor != null && interactor.gameObject != controller);
        GameObject directObject;

        if (directInteractor != null)
        {
            directObject = directInteractor.gameObject;
        }
        else
        {
            Transform existingChild = controller.transform.Find(childName);
            if (existingChild != null)
            {
                directObject = existingChild.gameObject;
            }
            else
            {
                directObject = new GameObject(childName);
                directObject.transform.SetParent(controller.transform, false);
                addedComponentCount++;
                Debug.Log($"VR bootstrap created '{childName}' under '{controller.name}'.", controller);
            }

            directInteractor = directObject.GetComponent<XRDirectInteractor>();
            if (directInteractor == null)
            {
                if (directObject.GetComponent<XRBaseInteractor>() != null)
                {
                    Debug.LogWarning($"VR bootstrap skipped XRDirectInteractor on '{directObject.name}' because it already has another XRBaseInteractor.", directObject);
                    return;
                }

                directInteractor = directObject.AddComponent<XRDirectInteractor>();
                if (directInteractor == null)
                {
                    Debug.LogWarning($"VR bootstrap could not add XRDirectInteractor to '{directObject.name}'.", directObject);
                    return;
                }

                addedComponentCount++;
                Debug.Log($"VR bootstrap added XRDirectInteractor to '{directObject.name}'.", directObject);
            }
            else
            {
                skippedExistingCount++;
            }
        }

        directObject = directInteractor.gameObject;
        directObject.transform.localPosition = Vector3.zero;
        directObject.transform.localRotation = Quaternion.identity;
        directObject.transform.localScale = Vector3.one;

        if (directInteractor.interactionManager == null || directInteractor.interactionManager != interactionManager)
            directInteractor.interactionManager = interactionManager;

        if (directInteractor.handedness != handedness)
            directInteractor.handedness = handedness;

        Rigidbody rb = directObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = directObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            addedComponentCount++;
        }
        else
        {
            skippedExistingCount++;
        }

        rb.isKinematic = true;
        rb.useGravity = false;

        SphereCollider triggerCollider = null;
        SphereCollider[] sphereColliders = directObject.GetComponents<SphereCollider>();
        for (int i = 0; i < sphereColliders.Length; i++)
        {
            if (sphereColliders[i].isTrigger)
            {
                triggerCollider = sphereColliders[i];
                break;
            }
        }

        if (triggerCollider == null)
        {
            triggerCollider = directObject.AddComponent<SphereCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = 0.09f;
            addedComponentCount++;
        }
        else
        {
            skippedExistingCount++;
        }

        triggerCollider.enabled = true;
        triggerCollider.isTrigger = true;
        triggerCollider.radius = 0.09f;
        triggerCollider.center = Vector3.zero;

        EnsureInteractionLayerMask(directInteractor);
    }

    private void EnsureTrackedPoseDriver(GameObject controller, InteractorHandedness handedness)
    {
        InputTrackedPoseDriver trackedPoseDriver = controller.GetComponent<InputTrackedPoseDriver>();
        if (trackedPoseDriver == null)
        {
            trackedPoseDriver = controller.AddComponent<InputTrackedPoseDriver>();
            addedComponentCount++;
            Debug.Log($"VR bootstrap added TrackedPoseDriver to '{controller.name}'.", controller);
        }
        else
        {
            skippedExistingCount++;
        }

        trackedPoseDriver.trackingType = InputTrackedPoseDriver.TrackingType.RotationAndPosition;
        trackedPoseDriver.updateType = InputTrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        trackedPoseDriver.ignoreTrackingState = false;
        trackedPoseDriver.positionInput = EnsureTrackedAction(trackedPoseDriver.positionInput, "Position", "Vector3", handedness, "devicePosition");
        trackedPoseDriver.rotationInput = EnsureTrackedAction(trackedPoseDriver.rotationInput, "Rotation", "Quaternion", handedness, "deviceRotation");
        trackedPoseDriver.trackingStateInput = EnsureTrackedAction(trackedPoseDriver.trackingStateInput, "Tracking State", "Integer", handedness, "trackingState");
    }

    private void EnsureRayInteractor(GameObject controller, InteractorHandedness handedness)
    {
        XRInteractionManager interactionManager = FindFirstObjectByType<XRInteractionManager>();
        if (interactionManager == null)
        {
            Debug.LogWarning($"VR bootstrap skipped ray interactor setup for '{controller.name}' because XR Interaction Manager was not found.", controller);
            return;
        }

        XRRayInteractor rayInteractor = controller.GetComponentsInChildren<XRRayInteractor>(true)
            .FirstOrDefault();
        if (rayInteractor == null)
        {
            rayInteractor = controller.AddComponent<XRRayInteractor>();
            addedComponentCount++;
            Debug.Log($"VR bootstrap added XRRayInteractor to '{controller.name}'.", controller);
        }
        else
        {
            skippedExistingCount++;
        }

        rayInteractor.interactionManager = interactionManager;
        rayInteractor.handedness = handedness;
        rayInteractor.enableUIInteraction = true;
        if (rayInteractor.maxRaycastDistance < 10f)
            rayInteractor.maxRaycastDistance = 10f;
        if (rayInteractor.rayOriginTransform == null)
            rayInteractor.rayOriginTransform = rayInteractor.transform;

        EnsureInteractionLayerMask(rayInteractor);

        GameObject rayObject = rayInteractor.gameObject;

        LineRenderer lineRenderer = rayObject.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = rayObject.AddComponent<LineRenderer>();
            addedComponentCount++;
        }
        else
        {
            skippedExistingCount++;
        }

        lineRenderer.enabled = true;
        lineRenderer.useWorldSpace = true;
        if (lineRenderer.positionCount < 2)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, Vector3.forward);
        }

        if (lineRenderer.widthMultiplier <= 0f)
            lineRenderer.widthMultiplier = 0.005f;

        XRInteractorLineVisual lineVisual = rayObject.GetComponent<XRInteractorLineVisual>();
        if (lineVisual == null)
        {
            lineVisual = rayObject.AddComponent<XRInteractorLineVisual>();
            addedComponentCount++;
        }
        else
        {
            skippedExistingCount++;
        }

        lineVisual.enabled = true;
        lineVisual.overrideInteractorLineOrigin = true;
        if (lineVisual.lineOriginTransform == null)
            lineVisual.lineOriginTransform = rayInteractor.rayOriginTransform;
    }

    private static InputActionProperty CreateTrackedAction(string actionName, string expectedControlType, InteractorHandedness handedness, string controlSuffix)
    {
        string hand = handedness == InteractorHandedness.Left ? "LeftHand" : "RightHand";
        InputAction action = new InputAction(actionName, expectedControlType: expectedControlType);
        action.AddBinding($"<XRController>{{{hand}}}/{controlSuffix}");
        action.AddBinding($"<TrackedDevice>{{{hand}}}/{controlSuffix}");
        return new InputActionProperty(action);
    }

    private static InputActionProperty EnsureTrackedAction(InputActionProperty existingProperty, string actionName, string expectedControlType, InteractorHandedness handedness, string controlSuffix)
    {
        InputAction existingAction = existingProperty.action;
        string hand = handedness == InteractorHandedness.Left ? "LeftHand" : "RightHand";
        string xrControllerBinding = $"<XRController>{{{hand}}}/{controlSuffix}";
        string trackedDeviceBinding = $"<TrackedDevice>{{{hand}}}/{controlSuffix}";

        if (existingAction != null && HasBinding(existingAction, xrControllerBinding) && HasBinding(existingAction, trackedDeviceBinding))
            return existingProperty;

        return CreateTrackedAction(actionName, expectedControlType, handedness, controlSuffix);
    }

    private static bool HasBinding(InputAction action, string bindingPath)
    {
        if (action == null)
            return false;

        for (int i = 0; i < action.bindings.Count; i++)
        {
            if (action.bindings[i].path == bindingPath)
                return true;
        }

        return false;
    }

    private static void EnsureInteractionLayerMask(XRBaseInteractor interactor)
    {
        if (interactor.interactionLayers.value != 0)
            return;

        interactor.interactionLayers = -1;
    }

    private void EnsureInteractorInputs(GameObject controller, InteractorHandedness handedness)
    {
        XRBaseInputInteractor[] inputInteractors = controller.GetComponentsInChildren<XRBaseInputInteractor>(true);
        for (int i = 0; i < inputInteractors.Length; i++)
        {
            XRBaseInputInteractor interactor = inputInteractors[i];
            if (interactor == null)
                continue;

            ConfigureButtonReader(interactor.selectInput, "Select", handedness, "gripPressed", "grip");
            ConfigureButtonReader(interactor.activateInput, "Activate", handedness, "triggerPressed", "trigger");

            if (interactor is XRRayInteractor rayInteractor)
                ConfigureButtonReader(rayInteractor.uiPressInput, "UI Press", handedness, "triggerPressed", "trigger");

            if (interactor is NearFarInteractor nearFarInteractor)
                ConfigureButtonReader(nearFarInteractor.uiPressInput, "UI Press", handedness, "triggerPressed", "trigger");
        }
    }

    private static void ConfigureButtonReader(
        XRInputButtonReader reader,
        string actionName,
        InteractorHandedness handedness,
        string pressedControlSuffix,
        string valueControlSuffix)
    {
        if (reader == null)
            return;

        string hand = handedness == InteractorHandedness.Left ? "LeftHand" : "RightHand";
        string xrPressedBinding = $"<XRController>{{{hand}}}/{pressedControlSuffix}";
        string trackedPressedBinding = $"<TrackedDevice>{{{hand}}}/{pressedControlSuffix}";
        string xrValueBinding = $"<XRController>{{{hand}}}/{valueControlSuffix}";
        string trackedValueBinding = $"<TrackedDevice>{{{hand}}}/{valueControlSuffix}";

        InputAction performedAction = reader.inputActionPerformed;
        if (performedAction == null || !HasBinding(performedAction, xrPressedBinding) || !HasBinding(performedAction, trackedPressedBinding))
        {
            performedAction = new InputAction(actionName, InputActionType.Button);
            performedAction.AddBinding(xrPressedBinding);
            performedAction.AddBinding(trackedPressedBinding);
            reader.inputActionPerformed = performedAction;
        }

        InputAction valueAction = reader.inputActionValue;
        if (valueAction == null || !HasBinding(valueAction, xrValueBinding) || !HasBinding(valueAction, trackedValueBinding))
        {
            valueAction = new InputAction($"{actionName} Value", InputActionType.Value, expectedControlType: "Axis");
            valueAction.AddBinding(xrValueBinding);
            valueAction.AddBinding(trackedValueBinding);
            reader.inputActionValue = valueAction;
        }

        reader.inputSourceMode = XRInputButtonReader.InputSourceMode.InputAction;
        reader.EnableDirectActionIfModeUsed();
    }

    private XRGrabInteractable EnsureGrabInteractable(GameObject target, Rigidbody rb)
    {
        XRGrabInteractable grabInteractable = target.GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = target.AddComponent<XRGrabInteractable>();
            grabInteractable.interactionManager = FindFirstObjectByType<XRInteractionManager>();
            grabInteractable.selectMode = InteractableSelectMode.Single;
            grabInteractable.movementType = XRGrabInteractable.MovementType.VelocityTracking;
            grabInteractable.trackPosition = true;
            grabInteractable.trackRotation = true;
            grabInteractable.throwOnDetach = true;
            grabInteractable.retainTransformParent = false;
            addedComponentCount++;
            Debug.Log($"VR bootstrap added missing XRGrabInteractable to '{target.name}'.", target);
        }
        else
        {
            skippedExistingCount++;
        }

        if (rb != null && Mathf.Approximately(rb.mass, 0f))
            rb.mass = 1f;

        return grabInteractable;
    }

    private void EnsureZone<T>(Transform parent, string zoneName, Vector3 worldCenter, Vector3 zoneSize, CauldronReactions cauldron) where T : Component
    {
        Transform zoneTransform = parent.Find(zoneName);
        if (zoneTransform == null)
        {
            GameObject zoneObject = new GameObject(zoneName);
            zoneTransform = zoneObject.transform;
            zoneTransform.SetParent(parent, false);
            zoneTransform.position = worldCenter;
            zoneTransform.rotation = parent.rotation;
            zoneTransform.localScale = Vector3.one;
            addedComponentCount++;
            Debug.Log($"VR bootstrap created missing zone '{zoneName}'.", parent);
        }
        else
        {
            skippedExistingCount++;
        }

        BoxCollider boxCollider = zoneTransform.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = zoneTransform.gameObject.AddComponent<BoxCollider>();
            addedComponentCount++;
        }
        else
        {
            skippedExistingCount++;
        }

        boxCollider.size = zoneSize;
        boxCollider.center = Vector3.zero;
        if (!boxCollider.isTrigger)
            boxCollider.isTrigger = true;

        T zoneComponent = zoneTransform.GetComponent<T>();
        if (zoneComponent == null)
        {
            zoneComponent = zoneTransform.gameObject.AddComponent<T>();
            addedComponentCount++;
        }
        else
        {
            skippedExistingCount++;
        }

        if (zoneComponent is CauldronIngredientZone ingredientZone && cauldron != null)
            AssignCauldronReference(ingredientZone, cauldron);

        if (zoneComponent is CauldronStirZone stirZone && cauldron != null)
            AssignCauldronReference(stirZone, cauldron);
    }

    private static void AssignCauldronReference(Component zoneComponent, CauldronReactions cauldron)
    {
        var field = zoneComponent.GetType().GetField("cauldron", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (field == null || ReferenceEquals(field.GetValue(zoneComponent), cauldron))
            return;

        field.SetValue(zoneComponent, cauldron);
    }
}
