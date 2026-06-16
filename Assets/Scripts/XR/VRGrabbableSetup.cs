using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Adds the minimum physics and XR Interaction Toolkit components required
/// for a scene object to be grabbed, released, and thrown in VR.
/// </summary>
[DisallowMultipleComponent]
public class VRGrabbableSetup : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] private float mass = 1f;
    [SerializeField] private bool useGravity = true;
    [SerializeField] private CollisionDetectionMode collisionDetection = CollisionDetectionMode.ContinuousDynamic;
    [SerializeField] private RigidbodyInterpolation interpolation = RigidbodyInterpolation.Interpolate;

    [Header("Grab")]
    [SerializeField] private XRGrabInteractable.MovementType movementType = XRGrabInteractable.MovementType.VelocityTracking;
    [SerializeField] private bool throwOnDetach = true;
    [SerializeField] private float throwVelocityScale = 1.5f;
    [SerializeField] private float throwAngularVelocityScale = 1f;
    [SerializeField] private bool configureOnAwake = true;

    public Rigidbody Rigidbody { get; private set; }
    public Collider Collider { get; private set; }
    public XRGrabInteractable GrabInteractable { get; private set; }

    private void Reset()
    {
        Configure();
    }

    private void Awake()
    {
        if (configureOnAwake)
            Configure();
    }

    [ContextMenu("Configure VR Grabbable")]
    public void Configure()
    {
        Rigidbody = GetComponent<Rigidbody>();
        if (Rigidbody == null)
            Rigidbody = gameObject.AddComponent<Rigidbody>();

        Rigidbody.mass = Mathf.Max(0.01f, mass);
        Rigidbody.useGravity = useGravity;
        Rigidbody.isKinematic = false;
        Rigidbody.interpolation = interpolation;
        Rigidbody.collisionDetectionMode = collisionDetection;

        Collider = FindUsableCollider();
        if (Collider == null)
            Collider = AddBestCollider();

        Collider.enabled = true;
        Collider.isTrigger = false;

        GrabInteractable = GetComponent<XRGrabInteractable>();
        if (GrabInteractable == null)
            GrabInteractable = gameObject.AddComponent<XRGrabInteractable>();

        GrabInteractable.selectMode = InteractableSelectMode.Single;
        GrabInteractable.movementType = movementType;
        GrabInteractable.trackPosition = true;
        GrabInteractable.trackRotation = true;
        GrabInteractable.throwOnDetach = throwOnDetach;
        GrabInteractable.throwVelocityScale = throwVelocityScale;
        GrabInteractable.throwAngularVelocityScale = throwAngularVelocityScale;
        GrabInteractable.retainTransformParent = false;

        if (GrabInteractable.interactionManager == null)
            GrabInteractable.interactionManager = FindFirstObjectByType<XRInteractionManager>();
    }

    private Collider FindUsableCollider()
    {
        Collider[] colliders = GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && colliders[i].enabled && !colliders[i].isTrigger)
                return colliders[i];
        }

        return null;
    }

    private Collider AddBestCollider()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = true;
            return meshCollider;
        }

        return gameObject.AddComponent<BoxCollider>();
    }
}
