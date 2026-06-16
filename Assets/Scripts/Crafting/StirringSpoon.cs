using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class StirringSpoon : MonoBehaviour
{
    [SerializeField] private XRGrabInteractable grabInteractable;

    public XRGrabInteractable GrabInteractable => grabInteractable;

    public bool IsHeld => grabInteractable != null && grabInteractable.isSelected;

    private void Awake()
    {
        if (grabInteractable == null)
            grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnValidate()
    {
        if (grabInteractable == null)
            grabInteractable = GetComponent<XRGrabInteractable>();
    }
}
