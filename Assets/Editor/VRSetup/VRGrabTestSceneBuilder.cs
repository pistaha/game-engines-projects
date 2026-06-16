using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Unity.XR.CoreUtils;
using InputTrackedPoseDriver = UnityEngine.InputSystem.XR.TrackedPoseDriver;

public static class VRGrabTestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/VRGrabTestScene.unity";

    [MenuItem("Tools/VR/Create Grab Test Scene")]
    public static void CreateGrabTestScene()
    {
        EnsureScenesFolder();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateLight();
        CreateFloor();

        XRInteractionManager interactionManager = new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
        Camera xrCamera = CreateXrOrigin(interactionManager);
        VRTestSpawner spawner = CreateSpawner(xrCamera.transform);

        CreateWorldSpaceUi(xrCamera.transform, spawner);
        CreateEventSystem();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Debug.Log($"Created VR grab test scene at {ScenePath}.");
    }

    private static void CreateLight()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void CreateFloor()
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "VR Test Floor";
        floor.transform.position = new Vector3(0f, -0.03f, 1.5f);
        floor.transform.localScale = new Vector3(5f, 0.06f, 5f);

        Renderer renderer = floor.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = CreateMaterial("VR Floor Material", new Color(0.28f, 0.30f, 0.32f));
    }

    private static Camera CreateXrOrigin(XRInteractionManager interactionManager)
    {
        GameObject originObject = new GameObject("XR Origin (XR Rig)");
        XROrigin xrOrigin = originObject.AddComponent<XROrigin>();
        xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

        GameObject cameraOffset = new GameObject("Camera Offset");
        cameraOffset.transform.SetParent(originObject.transform, false);

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetParent(cameraOffset.transform, false);
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.nearClipPlane = 0.01f;
        camera.clearFlags = CameraClearFlags.Skybox;
        cameraObject.AddComponent<AudioListener>();

        InputTrackedPoseDriver headPoseDriver = cameraObject.AddComponent<InputTrackedPoseDriver>();
        ConfigureTrackedPoseDriver(headPoseDriver, "Head");

        xrOrigin.Origin = originObject;
        xrOrigin.CameraFloorOffsetObject = cameraOffset;
        xrOrigin.Camera = camera;

        CreateController(cameraOffset.transform, "Left Controller", InteractorHandedness.Left, interactionManager);
        CreateController(cameraOffset.transform, "Right Controller", InteractorHandedness.Right, interactionManager);

        return camera;
    }

    private static void CreateController(Transform parent, string name, InteractorHandedness handedness, XRInteractionManager interactionManager)
    {
        GameObject controller = new GameObject(name);
        controller.transform.SetParent(parent, false);

        InputTrackedPoseDriver poseDriver = controller.AddComponent<InputTrackedPoseDriver>();
        ConfigureTrackedPoseDriver(poseDriver, handedness == InteractorHandedness.Left ? "LeftHand" : "RightHand");

#pragma warning disable 0618
        ActionBasedController actionBasedController = controller.AddComponent<ActionBasedController>();
        ConfigureActionBasedController(actionBasedController, handedness);
#pragma warning restore 0618

        XRRayInteractor rayInteractor = controller.AddComponent<XRRayInteractor>();
        rayInteractor.interactionManager = interactionManager;
        rayInteractor.handedness = handedness;
        rayInteractor.enableUIInteraction = true;
        rayInteractor.rayOriginTransform = controller.transform;
        rayInteractor.maxRaycastDistance = 10f;
        rayInteractor.interactionLayers = -1;
        ConfigureInputInteractor(rayInteractor, handedness);

        LineRenderer lineRenderer = controller.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.widthMultiplier = 0.005f;
        lineRenderer.positionCount = 2;
        lineRenderer.sharedMaterial = CreateMaterial($"{name} Ray Material", handedness == InteractorHandedness.Left ? Color.cyan : Color.yellow);

        XRInteractorLineVisual lineVisual = controller.AddComponent<XRInteractorLineVisual>();
        lineVisual.overrideInteractorLineOrigin = true;
        lineVisual.lineOriginTransform = controller.transform;

        GameObject directObject = new GameObject(handedness == InteractorHandedness.Left ? "Left Direct Interactor" : "Right Direct Interactor");
        directObject.transform.SetParent(controller.transform, false);

        Rigidbody directBody = directObject.AddComponent<Rigidbody>();
        directBody.isKinematic = true;
        directBody.useGravity = false;

        SphereCollider directCollider = directObject.AddComponent<SphereCollider>();
        directCollider.isTrigger = true;
        directCollider.radius = 0.09f;

        XRDirectInteractor directInteractor = directObject.AddComponent<XRDirectInteractor>();
        directInteractor.interactionManager = interactionManager;
        directInteractor.handedness = handedness;
        directInteractor.interactionLayers = -1;
        ConfigureInputInteractor(directInteractor, handedness);
    }

    private static VRTestSpawner CreateSpawner(Transform cameraTransform)
    {
        GameObject spawnerObject = new GameObject("VR Test Spawner");
        VRTestSpawner spawner = spawnerObject.AddComponent<VRTestSpawner>();

        SerializedObject serializedObject = new SerializedObject(spawner);
        serializedObject.FindProperty("spawnOrigin").objectReferenceValue = cameraTransform;
        serializedObject.FindProperty("spawnOnStart").boolValue = true;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        return spawner;
    }

    private static void CreateWorldSpaceUi(Transform cameraTransform, VRTestSpawner spawner)
    {
        Canvas canvas = new GameObject("VR Test World Space Canvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        if (cameraForward.sqrMagnitude < 0.001f)
            cameraForward = Vector3.forward;

        canvas.transform.position = cameraTransform.position + cameraForward * 2.2f + Vector3.up * -0.2f;
        canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - cameraTransform.position, Vector3.up);
        canvas.transform.localScale = Vector3.one * 0.002f;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(560f, 220f);

        canvas.gameObject.AddComponent<GraphicRaycaster>();
        canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();

        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(canvas.transform, false);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.09f, 0.10f, 0.92f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Button button = CreateButton(panel.transform, "Spawn Test Object");
        UnityEventTools.AddPersistentListener(button.onClick, spawner.SpawnRandomObject);
    }

    private static Button CreateButton(Transform parent, string label)
    {
        GameObject buttonObject = new GameObject(label);
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.46f, 0.95f);

        Button button = buttonObject.AddComponent<Button>();
        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(420f, 86f);
        rectTransform.anchoredPosition = Vector2.zero;

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(buttonObject.transform, false);
        Text text = textObject.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 30;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private static void CreateEventSystem()
    {
        EventSystem eventSystem = GetOrCreateSingleEventSystem();

        StandaloneInputModule standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneInputModule != null)
            Object.DestroyImmediate(standaloneInputModule);

        InputSystemUIInputModule inputSystemUiInputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputSystemUiInputModule != null)
            Object.DestroyImmediate(inputSystemUiInputModule);

        XRUIInputModule xrUiInputModule = eventSystem.GetComponent<XRUIInputModule>();
        if (xrUiInputModule == null)
            xrUiInputModule = eventSystem.gameObject.AddComponent<XRUIInputModule>();

        xrUiInputModule.enabled = true;
    }

    private static EventSystem GetOrCreateSingleEventSystem()
    {
        EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        EventSystem primary = eventSystems.FirstOrDefault(eventSystem => eventSystem != null && eventSystem.isActiveAndEnabled)
            ?? eventSystems.FirstOrDefault(eventSystem => eventSystem != null);

        if (primary == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            primary = eventSystemObject.AddComponent<EventSystem>();
        }

        foreach (EventSystem duplicate in eventSystems)
        {
            if (duplicate == null || duplicate == primary)
                continue;

            duplicate.enabled = false;
            DisableInputModules(duplicate.gameObject);
        }

        primary.enabled = true;
        return primary;
    }

    private static void DisableInputModules(GameObject target)
    {
        StandaloneInputModule standaloneInputModule = target.GetComponent<StandaloneInputModule>();
        if (standaloneInputModule != null)
            standaloneInputModule.enabled = false;

        InputSystemUIInputModule inputSystemUiInputModule = target.GetComponent<InputSystemUIInputModule>();
        if (inputSystemUiInputModule != null)
            inputSystemUiInputModule.enabled = false;

        XRUIInputModule xrUiInputModule = target.GetComponent<XRUIInputModule>();
        if (xrUiInputModule != null)
            xrUiInputModule.enabled = false;
    }

    private static void EnsureScenesFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
    }

    private static void ConfigureTrackedPoseDriver(InputTrackedPoseDriver poseDriver, string usage)
    {
        poseDriver.trackingType = InputTrackedPoseDriver.TrackingType.RotationAndPosition;
        poseDriver.updateType = InputTrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        poseDriver.ignoreTrackingState = false;
        poseDriver.positionInput = CreateInputAction("Position", "Vector3", $"<XRController>{{{usage}}}/devicePosition", $"<TrackedDevice>{{{usage}}}/devicePosition");
        poseDriver.rotationInput = CreateInputAction("Rotation", "Quaternion", $"<XRController>{{{usage}}}/deviceRotation", $"<TrackedDevice>{{{usage}}}/deviceRotation");
        poseDriver.trackingStateInput = CreateInputAction("Tracking State", "Integer", $"<XRController>{{{usage}}}/trackingState", $"<TrackedDevice>{{{usage}}}/trackingState");
    }

#pragma warning disable 0618
    private static void ConfigureActionBasedController(ActionBasedController controller, InteractorHandedness handedness)
    {
        string usage = handedness == InteractorHandedness.Left ? "LeftHand" : "RightHand";
        controller.positionAction = CreateInputAction("Position", "Vector3", $"<XRController>{{{usage}}}/devicePosition", $"<TrackedDevice>{{{usage}}}/devicePosition");
        controller.rotationAction = CreateInputAction("Rotation", "Quaternion", $"<XRController>{{{usage}}}/deviceRotation", $"<TrackedDevice>{{{usage}}}/deviceRotation");
        controller.trackingStateAction = CreateInputAction("Tracking State", "Integer", $"<XRController>{{{usage}}}/trackingState", $"<TrackedDevice>{{{usage}}}/trackingState");
        controller.selectAction = CreateButtonAction("Select", $"<XRController>{{{usage}}}/gripPressed", $"<TrackedDevice>{{{usage}}}/gripPressed");
        controller.selectActionValue = CreateInputAction("Select Value", "Axis", $"<XRController>{{{usage}}}/grip", $"<TrackedDevice>{{{usage}}}/grip");
        controller.activateAction = CreateButtonAction("Activate", $"<XRController>{{{usage}}}/triggerPressed", $"<TrackedDevice>{{{usage}}}/triggerPressed");
        controller.activateActionValue = CreateInputAction("Activate Value", "Axis", $"<XRController>{{{usage}}}/trigger", $"<TrackedDevice>{{{usage}}}/trigger");
        controller.uiPressAction = CreateButtonAction("UI Press", $"<XRController>{{{usage}}}/triggerPressed", $"<TrackedDevice>{{{usage}}}/triggerPressed");
        controller.uiPressActionValue = CreateInputAction("UI Press Value", "Axis", $"<XRController>{{{usage}}}/trigger", $"<TrackedDevice>{{{usage}}}/trigger");
    }
#pragma warning restore 0618

    private static void ConfigureInputInteractor(XRBaseInputInteractor interactor, InteractorHandedness handedness)
    {
        ConfigureButtonReader(interactor.selectInput, "Select", handedness, "gripPressed", "grip");
        ConfigureButtonReader(interactor.activateInput, "Activate", handedness, "triggerPressed", "trigger");

        if (interactor is XRRayInteractor rayInteractor)
            ConfigureButtonReader(rayInteractor.uiPressInput, "UI Press", handedness, "triggerPressed", "trigger");
    }

    private static void ConfigureButtonReader(XRInputButtonReader reader, string actionName, InteractorHandedness handedness, string pressedControl, string valueControl)
    {
        if (reader == null)
            return;

        string usage = handedness == InteractorHandedness.Left ? "LeftHand" : "RightHand";
        reader.inputActionPerformed = CreateButtonAction(actionName, $"<XRController>{{{usage}}}/{pressedControl}", $"<TrackedDevice>{{{usage}}}/{pressedControl}").action;
        reader.inputActionValue = CreateInputAction($"{actionName} Value", "Axis", $"<XRController>{{{usage}}}/{valueControl}", $"<TrackedDevice>{{{usage}}}/{valueControl}").action;
        reader.inputSourceMode = XRInputButtonReader.InputSourceMode.InputAction;
        reader.EnableDirectActionIfModeUsed();
    }

    private static InputActionProperty CreateButtonAction(string actionName, params string[] bindings)
    {
        InputAction action = new InputAction(actionName, InputActionType.Button);
        foreach (string binding in bindings.Where(binding => !string.IsNullOrWhiteSpace(binding)))
            action.AddBinding(binding);

        return new InputActionProperty(action);
    }

    private static InputActionProperty CreateInputAction(string actionName, string expectedControlType, params string[] bindings)
    {
        InputAction action = new InputAction(actionName, expectedControlType: expectedControlType);
        foreach (string binding in bindings.Where(binding => !string.IsNullOrWhiteSpace(binding)))
            action.AddBinding(binding);

        return new InputActionProperty(action);
    }

    private static Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader)
        {
            name = name,
            color = color,
        };

        return material;
    }
}
