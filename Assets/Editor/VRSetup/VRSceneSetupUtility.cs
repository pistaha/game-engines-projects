using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.UI;
using InputTrackedPoseDriver = UnityEngine.InputSystem.XR.TrackedPoseDriver;

public static class VRSceneSetupUtility
{
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string MenuPath = "Tools/VR/Setup SampleScene VR Objects";
    private const string RightControllerName = "Right Controller";
    private const string RightControllerNameWithTrailingSpace = "Right Controller ";
    private const string LeftControllerName = "Left Controller";
    private const string CameraOffsetName = "Camera Offset";
    private const string MainCameraName = "Main Camera";
    private const string PlayerName = "Player";
    private const string SpoonName = "Spoon_1";
    private const string SpoonPrefabPath = "Assets/FantasyInHouseProps_Free/Prefabs/Spoons/Spoon_1.prefab";
    private const string CauldronName = "Cotel";
    private const string IngredientZoneName = "IngredientZone";
    private const string StirZoneName = "StirZone";
    private const string LeftDirectInteractorName = "Left Direct Interactor";
    private const string RightDirectInteractorName = "Right Direct Interactor";

    private static readonly List<string> logMessages = new();

    [MenuItem(MenuPath)]
    public static void SetupSampleSceneVrObjects()
    {
        logMessages.Clear();

        try
        {
            var scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
            bool changed = false;

            var context = new SceneContext(scene);
            context.Resolve();

            if (!context.IsValid)
            {
                FlushLog();
                Debug.LogError("VR setup aborted. Required scene objects are missing.");
                return;
            }

            changed |= EnsureEventSystem(context);
            changed |= EnsureControllerSetup(context.LeftController, context.InteractionManager, InteractorHandedness.Left);
            changed |= EnsureControllerSetup(context.RightController, context.InteractionManager, InteractorHandedness.Right);
            changed |= EnsureCauldronSetup(context);
            changed |= EnsureSpoonSetup(context);
            changed |= EnsureIngredientSetup(context);
            changed |= EnsureBootstrapFallback(context.Player);

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                Log("SampleScene configured in memory. Review Console and Inspector, then save the scene manually if there are no errors.");
            }
            else
            {
                Log("SampleScene already satisfied VR setup requirements. No save was performed.");
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"VR setup failed: {exception}");
            throw;
        }
        finally
        {
            FlushLog();
        }
    }

    private static bool EnsureEventSystem(SceneContext context)
    {
        bool changed = false;
        EventSystem eventSystem = GetOrCreateSingleEventSystem(context.Scene, ref changed);

        StandaloneInputModule standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneInputModule != null && standaloneInputModule.enabled)
        {
            standaloneInputModule.enabled = false;
            changed = true;
            Log("Disabled StandaloneInputModule on EventSystem.");
        }

        InputSystemUIInputModule inputSystemUiInputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputSystemUiInputModule != null && inputSystemUiInputModule.enabled)
        {
            inputSystemUiInputModule.enabled = false;
            changed = true;
            Log("Disabled InputSystemUIInputModule on EventSystem.");
        }

        XRUIInputModule xrUiInputModule = EnsureComponent<XRUIInputModule>(eventSystem.gameObject, ref changed);
        if (!xrUiInputModule.enabled)
        {
            xrUiInputModule.enabled = true;
            changed = true;
            Log("Enabled XRUIInputModule on EventSystem.");
        }

        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>()
            .Where(canvas => canvas.gameObject.scene == context.Scene)
            .ToArray();

        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                continue;

            EnsureComponent<TrackedDeviceGraphicRaycaster>(canvas.gameObject, ref changed);
        }

        return changed;
    }

    private static EventSystem GetOrCreateSingleEventSystem(Scene scene, ref bool changed)
    {
        EventSystem[] eventSystems = Resources.FindObjectsOfTypeAll<EventSystem>()
            .Where(eventSystem => eventSystem != null && eventSystem.gameObject.scene == scene)
            .ToArray();

        EventSystem primary = eventSystems.FirstOrDefault(eventSystem => eventSystem.isActiveAndEnabled)
            ?? eventSystems.FirstOrDefault();

        if (primary == null)
        {
            var eventSystemObject = new GameObject("EventSystem");
            primary = eventSystemObject.AddComponent<EventSystem>();
            changed = true;
            Log("Created EventSystem.");
        }

        foreach (EventSystem duplicate in eventSystems)
        {
            if (duplicate == null || duplicate == primary)
                continue;

            if (duplicate.enabled)
            {
                duplicate.enabled = false;
                changed = true;
                Log($"Disabled duplicate EventSystem on '{duplicate.name}'.");
            }

            changed |= DisableInputModules(duplicate.gameObject);
        }

        if (!primary.enabled)
        {
            primary.enabled = true;
            changed = true;
            Log($"Enabled primary EventSystem on '{primary.name}'.");
        }

        return primary;
    }

    private static bool DisableInputModules(GameObject target)
    {
        bool changed = false;

        StandaloneInputModule standaloneInputModule = target.GetComponent<StandaloneInputModule>();
        if (standaloneInputModule != null && standaloneInputModule.enabled)
        {
            standaloneInputModule.enabled = false;
            changed = true;
        }

        InputSystemUIInputModule inputSystemUiInputModule = target.GetComponent<InputSystemUIInputModule>();
        if (inputSystemUiInputModule != null && inputSystemUiInputModule.enabled)
        {
            inputSystemUiInputModule.enabled = false;
            changed = true;
        }

        XRUIInputModule xrUiInputModule = target.GetComponent<XRUIInputModule>();
        if (xrUiInputModule != null && xrUiInputModule.enabled)
        {
            xrUiInputModule.enabled = false;
            changed = true;
        }

        return changed;
    }

    private static bool EnsureControllerSetup(GameObject controller, XRInteractionManager interactionManager, InteractorHandedness handedness)
    {
        bool changed = false;
        if (controller == null)
            return false;

        changed |= EnsureTrackedPoseDriver(controller, handedness);
        changed |= EnsureRayInteractorSetup(controller, interactionManager, handedness);
        changed |= EnsureDirectInteractorChild(controller, interactionManager, handedness);

        return changed;
    }

    private static bool EnsureDirectInteractorChild(GameObject controller, XRInteractionManager interactionManager, InteractorHandedness handedness)
    {
        bool changed = false;
        if (controller == null)
            return false;

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
                Undo.RegisterCreatedObjectUndo(directObject, $"Create {childName}");
                directObject.transform.SetParent(controller.transform, false);
                changed = true;
                Log($"Created '{childName}' under '{controller.name}'.");
            }

            directInteractor = directObject.GetComponent<XRDirectInteractor>();
            if (directInteractor == null)
            {
                if (directObject.GetComponent<XRBaseInteractor>() != null)
                {
                    Log($"Skipped XRDirectInteractor on '{directObject.name}' because it already has another XRBaseInteractor.");
                    return changed;
                }

                directInteractor = Undo.AddComponent<XRDirectInteractor>(directObject);
                changed = true;
                Log($"Added XRDirectInteractor to '{directObject.name}'.");
            }
        }

        if (directObject.transform.localPosition != Vector3.zero)
        {
            directObject.transform.localPosition = Vector3.zero;
            changed = true;
        }

        if (directObject.transform.localRotation != Quaternion.identity)
        {
            directObject.transform.localRotation = Quaternion.identity;
            changed = true;
        }

        if (directObject.transform.localScale != Vector3.one)
        {
            directObject.transform.localScale = Vector3.one;
            changed = true;
        }

        if (directInteractor.interactionManager != interactionManager)
        {
            directInteractor.interactionManager = interactionManager;
            changed = true;
            Log($"Assigned XR Interaction Manager to direct interactor on '{directObject.name}'.");
        }

        if (directInteractor.handedness != handedness)
        {
            directInteractor.handedness = handedness;
            changed = true;
        }

        changed |= EnsureInteractionLayerMask(directInteractor, directObject.name, "direct interactor");

        var rigidbody = directObject.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = Undo.AddComponent<Rigidbody>(directObject);
            changed = true;
            Log($"Added Rigidbody to '{directObject.name}' for direct grab.");
        }

        if (!rigidbody.isKinematic)
        {
            rigidbody.isKinematic = true;
            changed = true;
        }

        if (rigidbody.useGravity)
        {
            rigidbody.useGravity = false;
            changed = true;
        }

        var sphereCollider = directObject.GetComponents<SphereCollider>().FirstOrDefault(c => c.isTrigger);
        if (sphereCollider == null)
        {
            sphereCollider = Undo.AddComponent<SphereCollider>(directObject);
            changed = true;
            Log($"Added trigger SphereCollider to '{directObject.name}' for near-hand grab.");
        }

        if (!sphereCollider.enabled)
        {
            sphereCollider.enabled = true;
            changed = true;
        }

        if (!sphereCollider.isTrigger)
        {
            sphereCollider.isTrigger = true;
            changed = true;
        }

        if (!Mathf.Approximately(sphereCollider.radius, 0.09f))
        {
            sphereCollider.radius = 0.09f;
            changed = true;
        }

        if (sphereCollider.center != Vector3.zero)
        {
            sphereCollider.center = Vector3.zero;
            changed = true;
        }

        return changed;
    }

    private static bool EnsureTrackedPoseDriver(GameObject controller, InteractorHandedness handedness)
    {
        bool changed = false;

        var trackedPoseDriver = controller.GetComponent<InputTrackedPoseDriver>();
        if (trackedPoseDriver == null)
        {
            trackedPoseDriver = Undo.AddComponent<InputTrackedPoseDriver>(controller);
            changed = true;
            Log($"Added TrackedPoseDriver to '{controller.name}'.");
        }

        if (trackedPoseDriver.trackingType != InputTrackedPoseDriver.TrackingType.RotationAndPosition)
        {
            trackedPoseDriver.trackingType = InputTrackedPoseDriver.TrackingType.RotationAndPosition;
            changed = true;
        }

        if (trackedPoseDriver.updateType != InputTrackedPoseDriver.UpdateType.UpdateAndBeforeRender)
        {
            trackedPoseDriver.updateType = InputTrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            changed = true;
        }

        if (trackedPoseDriver.ignoreTrackingState)
        {
            trackedPoseDriver.ignoreTrackingState = false;
            changed = true;
        }

        changed |= EnsureTrackedAction(
            trackedPoseDriver.positionInput,
            "Position",
            "Vector3",
            GetHandBindings(handedness, "devicePosition"),
            updatedProperty => trackedPoseDriver.positionInput = updatedProperty);
        changed |= EnsureTrackedAction(
            trackedPoseDriver.rotationInput,
            "Rotation",
            "Quaternion",
            GetHandBindings(handedness, "deviceRotation"),
            updatedProperty => trackedPoseDriver.rotationInput = updatedProperty);
        changed |= EnsureTrackedAction(
            trackedPoseDriver.trackingStateInput,
            "Tracking State",
            "Integer",
            GetHandBindings(handedness, "trackingState"),
            updatedProperty => trackedPoseDriver.trackingStateInput = updatedProperty);

        return changed;
    }

    private static bool EnsureRayInteractorSetup(GameObject controller, XRInteractionManager interactionManager, InteractorHandedness handedness)
    {
        bool changed = false;

        var rayInteractor = controller.GetComponent<XRRayInteractor>();
        if (rayInteractor == null)
        {
            rayInteractor = Undo.AddComponent<XRRayInteractor>(controller);
            changed = true;
            Log($"Added XRRayInteractor to '{controller.name}'.");
        }

        if (rayInteractor.interactionManager != interactionManager)
        {
            rayInteractor.interactionManager = interactionManager;
            changed = true;
        }

        if (rayInteractor.handedness != handedness)
        {
            rayInteractor.handedness = handedness;
            changed = true;
        }

        if (rayInteractor.maxRaycastDistance < 10f)
        {
            rayInteractor.maxRaycastDistance = 10f;
            changed = true;
        }

        if (rayInteractor.rayOriginTransform == null)
        {
            rayInteractor.rayOriginTransform = controller.transform;
            changed = true;
        }

        if (!rayInteractor.enableUIInteraction)
        {
            rayInteractor.enableUIInteraction = true;
            changed = true;
        }

        changed |= EnsureInteractionLayerMask(rayInteractor, controller.name, "ray interactor");

        var lineRenderer = controller.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = Undo.AddComponent<LineRenderer>(controller);
            changed = true;
            Log($"Added LineRenderer to '{controller.name}'.");
        }

        if (!lineRenderer.enabled)
        {
            lineRenderer.enabled = true;
            changed = true;
        }

        if (!lineRenderer.useWorldSpace)
        {
            lineRenderer.useWorldSpace = true;
            changed = true;
        }

        if (lineRenderer.positionCount < 2)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, Vector3.forward);
            changed = true;
        }

        if (lineRenderer.widthMultiplier <= 0f)
        {
            lineRenderer.widthMultiplier = 0.005f;
            changed = true;
        }

        var lineVisual = controller.GetComponent<XRInteractorLineVisual>();
        if (lineVisual == null)
        {
            lineVisual = Undo.AddComponent<XRInteractorLineVisual>(controller);
            changed = true;
            Log($"Added XRInteractorLineVisual to '{controller.name}'.");
        }

        if (!lineVisual.enabled)
        {
            lineVisual.enabled = true;
            changed = true;
        }

        if (!lineVisual.overrideInteractorLineOrigin)
        {
            lineVisual.overrideInteractorLineOrigin = true;
            changed = true;
        }

        if (lineVisual.lineOriginTransform == null)
        {
            lineVisual.lineOriginTransform = controller.transform;
            changed = true;
        }

        return changed;
    }

    private static bool EnsureCauldronSetup(SceneContext context)
    {
        bool changed = false;
        var cauldron = context.Cauldron;

        var storage = EnsureComponent<CraftStorage>(cauldron, ref changed);
        var recipeChecker = EnsureComponent<RecipeChecker>(cauldron, ref changed);
        var reactions = EnsureComponent<CauldronReactions>(cauldron, ref changed);

        changed |= AssignObjectReference(reactions, "storage", storage);
        changed |= AssignObjectReference(reactions, "recipeChecker", recipeChecker);
        changed |= AssignObjectReference(recipeChecker, "storage", storage);

        var ingredientZone = EnsureZone<CauldronIngredientZone>(cauldron.transform, IngredientZoneName, new Vector3(0f, 0.24f, 0f), new Vector3(0.55f, 0.35f, 0.55f), ref changed);
        var stirZone = EnsureZone<CauldronStirZone>(cauldron.transform, StirZoneName, new Vector3(0f, 0.42f, 0f), new Vector3(0.3f, 0.22f, 0.3f), ref changed);

        changed |= AssignObjectReference(ingredientZone, "cauldron", reactions);
        changed |= AssignObjectReference(stirZone, "cauldron", reactions);

        return changed;
    }

    private static bool EnsureSpoonSetup(SceneContext context)
    {
        bool changed = false;
        if (context.Spoon == null)
        {
            context.Spoon = CreateSpoonIfMissing(context.Cauldron);
            changed |= context.Spoon != null;
        }

        if (context.Spoon == null)
        {
            Log("Spoon_1 could not be found or created for SampleScene.");
            return changed;
        }

        var storage = context.Cauldron != null ? context.Cauldron.GetComponent<CraftStorage>() : context.CraftStorage;
        changed |= EnsurePickupObject(context.Spoon, isSpoon: true, context.InteractionManager, storage);
        return changed;
    }

    private static bool EnsureIngredientSetup(SceneContext context)
    {
        bool changed = false;
        var storage = context.Cauldron != null ? context.Cauldron.GetComponent<CraftStorage>() : context.CraftStorage;
        foreach (var item in context.CollectableItems)
            changed |= EnsurePickupObject(item.gameObject, isSpoon: false, context.InteractionManager, storage);

        return changed;
    }

    private static GameObject CreateSpoonIfMissing(GameObject cauldron)
    {
        GameObject spoonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpoonPrefabPath);
        if (spoonPrefab == null)
        {
            Log($"Spoon prefab not found at '{SpoonPrefabPath}'.");
            return null;
        }

        GameObject spoonInstance = PrefabUtility.InstantiatePrefab(spoonPrefab) as GameObject;
        if (spoonInstance == null)
            return null;

        spoonInstance.name = SpoonName;

        Transform targetTransform = spoonInstance.transform;
        if (cauldron != null)
        {
            Bounds bounds = new Bounds(cauldron.transform.position, Vector3.one);
            Renderer cauldronRenderer = cauldron.GetComponentInChildren<Renderer>();
            if (cauldronRenderer != null)
                bounds = cauldronRenderer.bounds;

            targetTransform.position = bounds.center + cauldron.transform.right * 0.55f + Vector3.up * 0.25f;
            targetTransform.rotation = Quaternion.Euler(0f, 35f, 90f);
        }

        Log("Instantiated missing Spoon_1 prefab into SampleScene.");
        return spoonInstance;
    }

    private static bool EnsurePickupObject(GameObject target, bool isSpoon, XRInteractionManager interactionManager, CraftStorage storage)
    {
        bool changed = false;
        if (target == null)
            return false;

        if (!target.activeSelf)
        {
            target.SetActive(true);
            changed = true;
        }

        GameObjectUtility.SetStaticEditorFlags(target, 0);

        var rigidbody = target.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = Undo.AddComponent<Rigidbody>(target);
            changed = true;
            Log($"Added Rigidbody to '{target.name}'.");
        }

        changed |= ConfigureRigidbody(rigidbody);

        Collider primaryCollider = FindPrimaryCollider(target);
        if (primaryCollider == null)
        {
            primaryCollider = Undo.AddComponent<BoxCollider>(target);
            changed = true;
            Log($"Added BoxCollider to '{target.name}'.");
        }

        if (!primaryCollider.enabled)
        {
            primaryCollider.enabled = true;
            changed = true;
        }

        if (primaryCollider.isTrigger)
        {
            primaryCollider.isTrigger = false;
            changed = true;
        }

        var grabInteractable = target.GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = Undo.AddComponent<XRGrabInteractable>(target);
            changed = true;
            Log($"Added XRGrabInteractable to '{target.name}'.");
        }

        changed |= ConfigureGrabInteractable(grabInteractable, interactionManager);

        if (isSpoon)
        {
            var stirringSpoon = EnsureComponent<StirringSpoon>(target, ref changed);
            changed |= AssignObjectReference(stirringSpoon, "grabInteractable", grabInteractable);
        }
        else
        {
            var collectableItem = target.GetComponent<CollectableItem>();
            if (collectableItem != null)
                changed |= AssignObjectReference(collectableItem, "storage", storage);
        }

        var simpleInteractable = target.GetComponent<XRSimpleInteractable>();
        if (simpleInteractable != null && simpleInteractable.enabled)
        {
            simpleInteractable.enabled = false;
            changed = true;
        }

        return changed;
    }

    private static bool EnsureBootstrapFallback(GameObject player)
    {
        bool changed = false;
        var bootstrap = player != null ? player.GetComponent<VrGrabTestCubeBootstrap>() : null;
        if (bootstrap == null)
            return false;

        changed |= AssignBool(bootstrap, "configureVrSceneOnStart", true);
        changed |= AssignBool(bootstrap, "disableLegacyDesktopControllers", true);
        changed |= AssignBool(bootstrap, "ensureXrUiEventSystem", true);
        changed |= AssignBool(bootstrap, "ensureTrackedDeviceRaycasters", true);
        changed |= AssignBool(bootstrap, "configureCraftingVrObjects", true);
        changed |= AssignBool(bootstrap, "spawnTestCube", false);
        return changed;
    }

    private static T EnsureZone<T>(Transform parent, string zoneName, Vector3 localCenter, Vector3 size, ref bool changed) where T : Component
    {
        Transform existing = parent.Find(zoneName);
        GameObject zoneObject;
        if (existing == null)
        {
            zoneObject = new GameObject(zoneName);
            Undo.RegisterCreatedObjectUndo(zoneObject, $"Create {zoneName}");
            zoneObject.transform.SetParent(parent, false);
            changed = true;
            Log($"Created zone '{zoneName}'.");
        }
        else
        {
            zoneObject = existing.gameObject;
        }

        zoneObject.transform.localPosition = localCenter;
        zoneObject.transform.localRotation = Quaternion.identity;
        zoneObject.transform.localScale = Vector3.one;

        var boxCollider = zoneObject.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = Undo.AddComponent<BoxCollider>(zoneObject);
            changed = true;
        }

        if (!boxCollider.isTrigger)
        {
            boxCollider.isTrigger = true;
            changed = true;
        }

        if (boxCollider.size != size)
        {
            boxCollider.size = size;
            changed = true;
        }

        if (boxCollider.center != Vector3.zero)
        {
            boxCollider.center = Vector3.zero;
            changed = true;
        }

        return EnsureComponent<T>(zoneObject, ref changed);
    }

    private static bool ConfigureRigidbody(Rigidbody rigidbody)
    {
        bool changed = false;
        if (!rigidbody.useGravity)
        {
            rigidbody.useGravity = true;
            changed = true;
        }

        if (rigidbody.isKinematic)
        {
            rigidbody.isKinematic = false;
            changed = true;
        }

        if (rigidbody.interpolation != RigidbodyInterpolation.Interpolate)
        {
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            changed = true;
        }

        if (rigidbody.collisionDetectionMode != CollisionDetectionMode.ContinuousDynamic)
        {
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            changed = true;
        }

        if (Mathf.Approximately(rigidbody.mass, 0f))
        {
            rigidbody.mass = 1f;
            changed = true;
        }

        return changed;
    }

    private static bool ConfigureGrabInteractable(XRGrabInteractable grabInteractable, XRInteractionManager interactionManager)
    {
        bool changed = false;
        if (grabInteractable.interactionManager != interactionManager)
        {
            grabInteractable.interactionManager = interactionManager;
            changed = true;
        }

        if (grabInteractable.selectMode != InteractableSelectMode.Single)
        {
            grabInteractable.selectMode = InteractableSelectMode.Single;
            changed = true;
        }

        if (grabInteractable.movementType != XRGrabInteractable.MovementType.VelocityTracking)
        {
            grabInteractable.movementType = XRGrabInteractable.MovementType.VelocityTracking;
            changed = true;
        }

        if (!grabInteractable.trackPosition)
        {
            grabInteractable.trackPosition = true;
            changed = true;
        }

        if (!grabInteractable.trackRotation)
        {
            grabInteractable.trackRotation = true;
            changed = true;
        }

        if (!grabInteractable.throwOnDetach)
        {
            grabInteractable.throwOnDetach = true;
            changed = true;
        }

        if (grabInteractable.retainTransformParent)
        {
            grabInteractable.retainTransformParent = false;
            changed = true;
        }

        return changed;
    }

    private static Collider FindPrimaryCollider(GameObject target)
    {
        Collider collider = target.GetComponents<Collider>().FirstOrDefault(c => c.enabled && !c.isTrigger);
        if (collider != null)
            return collider;

        return target.GetComponentsInChildren<Collider>(true).FirstOrDefault(c => c.enabled && !c.isTrigger);
    }

    private static T EnsureComponent<T>(GameObject target, ref bool changed) where T : Component
    {
        var component = target.GetComponent<T>();
        if (component != null)
        {
            Log($"Kept existing {typeof(T).Name} on '{target.name}'.");
            return component;
        }

        component = Undo.AddComponent<T>(target);
        changed = true;
        Log($"Added {typeof(T).Name} to '{target.name}'.");
        return component;
    }

    private static bool EnsureTrackedAction(InputActionProperty property, string actionName, string expectedControlType, string[] bindings, Action<InputActionProperty> assignProperty)
    {
        var action = property.action;
        if (action != null && HasAllBindings(action, bindings))
            return false;

        action = new InputAction(actionName, expectedControlType: expectedControlType);
        foreach (string binding in bindings)
            action.AddBinding(binding);

        assignProperty(new InputActionProperty(action));
        return true;
    }

    private static bool HasAllBindings(InputAction action, string[] bindings)
    {
        if (action == null)
            return false;

        foreach (string bindingPath in bindings)
        {
            bool found = false;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].path == bindingPath)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                return false;
        }

        return true;
    }

    private static string[] GetHandBindings(InteractorHandedness handedness, string controlSuffix)
    {
        string hand = handedness == InteractorHandedness.Left ? "LeftHand" : "RightHand";
        return new[]
        {
            $"<XRController>{{{hand}}}/{controlSuffix}",
            $"<TrackedDevice>{{{hand}}}/{controlSuffix}",
        };
    }

    private static bool EnsureInteractionLayerMask(XRBaseInteractor interactor, string objectName, string label)
    {
        SerializedObject serializedObject = new SerializedObject(interactor);
        SerializedProperty bitsProperty = serializedObject.FindProperty("m_InteractionLayers.m_Bits");
        if (bitsProperty == null || bitsProperty.intValue != 0)
            return false;

        bitsProperty.intValue = -1;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        Log($"Filled empty interaction layer mask on {label} for '{objectName}'.");
        return true;
    }

    private static bool AssignObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        if (target == null)
            return false;

        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || property.objectReferenceValue == value)
            return false;

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        Log($"Assigned {propertyName} on '{target.name}'.");
        return true;
    }

    private static bool AssignBool(UnityEngine.Object target, string propertyName, bool value)
    {
        if (target == null)
            return false;

        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || property.boolValue == value)
            return false;

        property.boolValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    private static void Log(string message)
    {
        logMessages.Add(message);
    }

    private static void FlushLog()
    {
        foreach (string message in logMessages)
            Debug.Log($"[VR Setup] {message}");

        logMessages.Clear();
    }

    private sealed class SceneContext
    {
        public Scene Scene { get; }
        public GameObject Player { get; private set; }
        public GameObject CameraOffset { get; private set; }
        public Camera MainCamera { get; private set; }
        public GameObject LeftController { get; private set; }
        public GameObject RightController { get; private set; }
        public XRInteractionManager InteractionManager { get; private set; }
        public GameObject Cauldron { get; private set; }
        public GameObject Spoon { get; set; }
        public CraftStorage CraftStorage { get; private set; }
        public CollectableItem[] CollectableItems { get; private set; } = Array.Empty<CollectableItem>();

        public bool IsValid =>
            Player != null &&
            CameraOffset != null &&
            MainCamera != null &&
            LeftController != null &&
            RightController != null &&
            InteractionManager != null &&
            Cauldron != null;

        public SceneContext(Scene scene)
        {
            Scene = scene;
        }

        public void Resolve()
        {
            GameObject[] roots = Scene.GetRootGameObjects();
            Player = FindByName(roots, PlayerName);
            CameraOffset = FindByName(roots, CameraOffsetName);
            LeftController = FindByName(roots, LeftControllerName);
            RightController = FindByName(roots, RightControllerName) ?? FindByName(roots, RightControllerNameWithTrailingSpace);
            Cauldron = FindByName(roots, CauldronName) ?? FindByComponent<CauldronReactions>(roots)?.gameObject;
            Spoon = FindByName(roots, SpoonName);
            MainCamera = FindByName(roots, MainCameraName)?.GetComponent<Camera>();
            InteractionManager = FindByName(roots, "XR Interaction Manager")?.GetComponent<XRInteractionManager>()
                ?? UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>();
            CraftStorage = Cauldron != null ? Cauldron.GetComponent<CraftStorage>() : null;
            CollectableItems = UnityEngine.Object.FindObjectsByType<CollectableItem>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(item => item != null && item.gameObject.scene == Scene)
                .ToArray();

            if (RightController != null && RightController.name != RightControllerName)
            {
                RightController.name = RightControllerName;
                Log("Normalized right controller name.");
            }

            if (Spoon == null)
                Spoon = FindByName(roots, "Spoon_1_final");

            if (Player == null)
                Log("Player not found.");
            if (CameraOffset == null)
                Log("Camera Offset not found.");
            if (MainCamera == null)
                Log("Main Camera not found.");
            if (LeftController == null)
                Log("Left Controller not found.");
            if (RightController == null)
                Log("Right Controller not found.");
            if (InteractionManager == null)
                Log("XR Interaction Manager not found.");
            if (Cauldron == null)
                Log("Cotel not found.");
        }

        private static GameObject FindByName(IEnumerable<GameObject> roots, string targetName)
        {
            foreach (GameObject root in roots)
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform transform in transforms)
                {
                    if (transform.name == targetName)
                        return transform.gameObject;
                }
            }

            return null;
        }

        private static T FindByComponent<T>(IEnumerable<GameObject> roots) where T : Component
        {
            foreach (GameObject root in roots)
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }

            return null;
        }
    }
}
