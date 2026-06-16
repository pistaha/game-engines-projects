using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    // Клавиатурное взаимодействие игрока: ищет цель под прицелом и запускает её действие.
    // Захват предметов на ПКМ в десктопной версии в основном делает отдельный скрипт захвата.
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform holdPoint;
    [SerializeField] private float interactDistance = 4f;
    [SerializeField] private float holdDistance = 1.25f;
    [SerializeField] private float holdHeightOffset = -0.15f;
    [SerializeField] private LayerMask interactMask = ~0;
    [SerializeField] private Key interactKey = Key.E;
    [SerializeField] private Key fallbackInteractKey = Key.F;
    [SerializeField] private bool allowLeftMouseInteract = false;
    [SerializeField] private InteractionPromptUI promptUI;

    private PickupItem heldItem;
    private IInteractable currentTarget;

    public Transform HoldPoint => holdPoint;
    public PickupItem HeldItem => heldItem;

    public void SetLeftMouseInteractEnabled(bool enabled)
    {
        allowLeftMouseInteract = enabled;
    }

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (holdPoint == null)
            holdPoint = CreateHoldPoint();

        if (promptUI == null)
            promptUI = GetComponent<InteractionPromptUI>();

        if (promptUI == null)
            promptUI = gameObject.AddComponent<InteractionPromptUI>();
    }

    private void Update()
    {
        UpdateHoldPointPose();
        UpdateHeldItem();
        UpdateCurrentTarget();
        UpdatePrompt();
        HandleInteractInput();
    }

    public void SetHeldItem(PickupItem item)
    {
        if (heldItem != null && heldItem != item)
            heldItem.Drop();

        heldItem = item;
    }

    public void ClearHeldItem(PickupItem item)
    {
        if (heldItem == item)
            heldItem = null;
    }

    private Transform CreateHoldPoint()
    {
        GameObject holdPointObject = new GameObject("Desktop Hold Point");
        holdPointObject.transform.SetParent(transform, false);
        return holdPointObject.transform;
    }

    private void UpdateHoldPointPose()
    {
        Camera rayCamera = playerCamera != null ? playerCamera : Camera.main;
        if (rayCamera == null || holdPoint == null)
            return;

        Transform cameraTransform = rayCamera.transform;
        holdPoint.position =
            cameraTransform.position
            + cameraTransform.forward * holdDistance
            + cameraTransform.up * holdHeightOffset;
        holdPoint.rotation = cameraTransform.rotation;
    }

    private void UpdateHeldItem()
    {
        if (heldItem != null)
            heldItem.TickHeld(Time.deltaTime);
    }

    private void UpdateCurrentTarget()
    {
        currentTarget = null;

        if (heldItem != null)
        {
            currentTarget = heldItem;
            return;
        }

        Camera rayCamera = playerCamera != null ? playerCamera : Camera.main;
        if (rayCamera == null)
            return;

        Ray ray = new Ray(rayCamera.transform.position, rayCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance, interactMask, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            currentTarget = hits[i].collider.GetComponentInParent<IInteractable>();
            if (currentTarget == null || currentTarget is PickupItem)
                continue;

            if (currentTarget.CanInteract(this))
                return;
        }

        currentTarget = null;
    }

    private void UpdatePrompt()
    {
        if (promptUI == null)
            return;

        promptUI.SetPrompt(currentTarget?.InteractionPrompt, currentTarget != null);
    }

    private void HandleInteractInput()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        bool keyPressed = keyboard != null
            && (keyboard[interactKey].wasPressedThisFrame
                || keyboard[fallbackInteractKey].wasPressedThisFrame
                || keyboard.eKey.wasPressedThisFrame
                || keyboard.fKey.wasPressedThisFrame);
        bool mousePressed = allowLeftMouseInteract && mouse != null && mouse.leftButton.wasPressedThisFrame;

        if (heldItem != null)
            keyPressed = false;

        if (!keyPressed && !mousePressed)
            return;

        currentTarget?.Interact(this);
    }

}
