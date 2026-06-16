using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonCC : MonoBehaviour
{
    [SerializeField] private Transform cameraHolder = null;
    [SerializeField] private InputActionAsset inputActions = null;
    [Header("Desktop Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float sprintSpeed = 5.2f;
    [SerializeField] private float jumpHeight = 1.1f;
    [SerializeField] private float gravity = -18f;
    [SerializeField] private float mouseSensitivity = 0.11f;
    [SerializeField] private float keyboardLookDegreesPerSecond = 140f;
    [Header("Desktop Interaction")]
    [SerializeField] private float interactDistance = 4f;
    [SerializeField] private LayerMask interactMask = ~0;
    [SerializeField] private Key interactKey = Key.E;
    [SerializeField] private Key fallbackInteractKey = Key.F;

    private bool inputEnabled = true;
    private CharacterController characterController;
    private ClickCollector clickCollector;
    private Camera playerCamera;
    private float verticalVelocity;
    private float cameraPitch;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        clickCollector = GetComponent<ClickCollector>();
        if (clickCollector == null)
            clickCollector = gameObject.AddComponent<ClickCollector>();

        if (cameraHolder == null && Camera.main != null)
            cameraHolder = Camera.main.transform;

        if (cameraHolder != null)
        {
            playerCamera = cameraHolder.GetComponent<Camera>();
        }

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void OnEnable()
    {
        SetActionState(true);
        ApplyCursorState();
    }

    private void OnDisable()
    {
        SetActionState(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (!inputEnabled)
            return;

        HandleLook();
        HandleKeyboardLook();
        HandleMove();
        HandleInteraction();
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        SetActionState(enabled);
        ApplyCursorState();
    }

    private void SetActionState(bool enabled)
    {
        if (inputActions == null)
            return;

        if (enabled && inputEnabled)
            inputActions.Enable();
        else
            inputActions.Disable();
    }

    private void HandleLook()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || cameraHolder == null)
            return;

        Vector2 mouseDelta = mouse.delta.ReadValue() * mouseSensitivity;
        transform.Rotate(Vector3.up * mouseDelta.x);

        cameraPitch = Mathf.Clamp(cameraPitch - mouseDelta.y, -82f, 82f);
        cameraHolder.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void HandleKeyboardLook()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        // Клавиши Q и E дополнительно поворачивают игрока с клавиатуры.
        // Основной обзор остаётся на мыши.
        float direction = 0f;
        if (keyboard.qKey.isPressed)
            direction -= 1f;
        if (keyboard.eKey.isPressed)
            direction += 1f;

        if (Mathf.Approximately(direction, 0f))
            return;

        transform.Rotate(Vector3.up * (direction * keyboardLookDegreesPerSecond * Time.deltaTime));
    }

    private void HandleMove()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        Vector2 input = Vector2.zero;
        if (keyboard.wKey.isPressed)
            input.y += 1f;
        if (keyboard.sKey.isPressed)
            input.y -= 1f;
        if (keyboard.dKey.isPressed)
            input.x += 1f;
        if (keyboard.aKey.isPressed)
            input.x -= 1f;

        input = Vector2.ClampMagnitude(input, 1f);

        bool grounded = characterController.isGrounded;
        if (grounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        if (grounded && keyboard.spaceKey.wasPressedThisFrame)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * Time.deltaTime;

        float speed = keyboard.leftShiftKey.isPressed ? sprintSpeed : moveSpeed;
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        move *= speed;
        move.y = verticalVelocity;

        characterController.Move(move * Time.deltaTime);
    }

    private void HandleInteraction()
    {
        Keyboard keyboard = Keyboard.current;
        bool interactPressed = keyboard != null
            && (WasPressed(keyboard[interactKey])
                || WasPressed(keyboard[fallbackInteractKey])
                || keyboard.eKey.wasPressedThisFrame
                || keyboard.fKey.wasPressedThisFrame);

        if (!interactPressed || clickCollector == null)
            return;

        Camera rayCamera = playerCamera != null ? playerCamera : Camera.main;
        if (rayCamera == null)
            return;

        Ray ray = new Ray(rayCamera.transform.position, rayCamera.transform.forward);
        // Проверяем все попадания луча, потому что перед книгой или предметом
        // может стоять декоративный коллайдер.
        RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance, interactMask, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            if (clickCollector.TryHandleColliderHit(hits[i].collider))
                return;
        }
    }

    private static bool WasPressed(KeyControl keyControl)
    {
        return keyControl != null && keyControl.wasPressedThisFrame;
    }

    private void ApplyCursorState()
    {
        Cursor.lockState = inputEnabled ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !inputEnabled;
    }

}
