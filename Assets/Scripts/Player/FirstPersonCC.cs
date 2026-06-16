using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonCC : MonoBehaviour
{
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private InputActionAsset inputActions;
    private bool inputEnabled = true;

    private void Awake()
    {
        enabled = false;
    }

    private void OnEnable()
    {
        SetActionState(true);
    }

    private void OnDisable()
    {
        SetActionState(false);
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        SetActionState(enabled);
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
}
