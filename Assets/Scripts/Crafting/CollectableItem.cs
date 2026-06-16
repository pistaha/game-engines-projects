using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CollectableItem : MonoBehaviour
{
    [Header("Данные бутылки")]
    public IngredientData ingredient;
    public CraftStorage storage;

    [Header("Звук подбора")]
    [SerializeField] private float pickupVolume = 0.8f;
    [SerializeField] private float pickupMinPitch = 0.96f;
    [SerializeField] private float pickupMaxPitch = 1.04f;

    private bool isCollected = false;
    private bool addedToCauldron = false;
    private static AudioClip[] cachedPickupClips;

    private Renderer[] renderers;
    private Collider[] colliders;
    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Transform startParent;
    private Transform customReturnPoint;
    private bool startActiveSelf;
    private bool hasCapturedReturnState;
    private bool startRigidbodyUseGravity;
    private bool startRigidbodyIsKinematic;
    private float startRigidbodyMass = 1f;
    private CollisionDetectionMode startCollisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    private RigidbodyInterpolation startRigidbodyInterpolation = RigidbodyInterpolation.Interpolate;
    private bool startGrabInteractableEnabled;
    private ColliderState[] startColliderStates;

    private void Awake()
    {
        RefreshCachedComponents();
        CaptureReturnState();
        ConfigureRigidbody();
    }

    public bool Collect()
    {
        if (isCollected) return false;

        RefreshCachedComponents();

        if (ingredient == null)
        {
            Debug.LogWarning("У бутылки не назначен ingredient: " + gameObject.name);
            return false;
        }

        if (storage == null)
        {
            Debug.LogWarning("У бутылки не назначен CraftStorage: " + gameObject.name);
            return false;
        }

        isCollected = true;
        PlayPickupSound();

        storage.AddIngredient(ingredient);
        storage.RegisterCollectedItem(this);

        // В котле бутылка исчезает визуально, но объект сохраняется, чтобы его можно было вернуть после проверки рецепта.
        HideItem();
        return true;
    }

    public void ReturnToStart()
    {
        RefreshCachedComponents();
        ForceReleaseIfGrabbed();

        if (customReturnPoint != null)
        {
            startPosition = customReturnPoint.position;
            startRotation = customReturnPoint.rotation;
        }

        transform.SetParent(startParent, true);
        gameObject.SetActive(startActiveSelf);

        if (rb != null)
        {
            rb.isKinematic = true;
            ResetRigidbodyVelocity();
            rb.position = startPosition;
            rb.rotation = startRotation;
        }

        transform.position = startPosition;
        transform.rotation = startRotation;

        RestoreColliderStates();
        SetRenderersEnabled(true);

        if (rb != null)
        {
            rb.mass = Mathf.Max(0.01f, startRigidbodyMass);
            rb.useGravity = startRigidbodyUseGravity;
            rb.collisionDetectionMode = startCollisionDetectionMode;
            rb.interpolation = startRigidbodyInterpolation;
            rb.isKinematic = startRigidbodyIsKinematic;
        }

        if (grabInteractable != null)
            grabInteractable.enabled = startGrabInteractableEnabled;

        isCollected = false;
        addedToCauldron = false;
    }

    public IngredientData Ingredient => ingredient;

    public bool IsAddedToCauldron => addedToCauldron;

    public bool CanBeAddedToCauldron()
    {
        return ingredient != null && !addedToCauldron;
    }

    public bool TryMarkAddedToCauldron()
    {
        if (!CanBeAddedToCauldron())
            return false;

        addedToCauldron = true;
        return true;
    }

    public bool TryPlaceInCauldron(CraftStorage cauldronStorage)
    {
        if (cauldronStorage == null)
            return false;

        RefreshCachedComponents();

        if (!TryMarkAddedToCauldron())
            return false;

        if (storage == null)
            storage = cauldronStorage;

        isCollected = true;
        PlayPickupSound();

        cauldronStorage.AddIngredient(ingredient);
        cauldronStorage.RegisterCollectedItem(this);

        HideItem(false);
        return true;
    }

    public void MarkCurrentPoseAsStart()
    {
        CaptureReturnState();
    }

    public void SetCustomReturnPoint(Transform returnPoint)
    {
        customReturnPoint = returnPoint;
    }

    public void MarkCustomReturnPointAsStart()
    {
        if (customReturnPoint == null)
            return;

        startPosition = customReturnPoint.position;
        startRotation = customReturnPoint.rotation;
    }

    private void HideItem(bool resetCauldronFlag = true)
    {
        RefreshCachedComponents();

        ForceReleaseIfGrabbed();
        SetRenderersEnabled(false);
        SetCollidersEnabled(false);

        if (grabInteractable != null)
            grabInteractable.enabled = false;

        if (rb != null)
        {
            ResetRigidbodyVelocity();
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        if (resetCauldronFlag)
            addedToCauldron = false;
    }

    private void SetRenderersEnabled(bool value)
    {
        RefreshCachedComponents();

        foreach (Renderer r in renderers)
        {
            if (r != null)
                r.enabled = value;
        }
    }

    private void SetCollidersEnabled(bool value)
    {
        RefreshCachedComponents();

        foreach (Collider c in colliders)
        {
            if (c != null)
                c.enabled = value;
        }
    }

    private void RefreshCachedComponents()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    public void CaptureReturnState()
    {
        RefreshCachedComponents();

        startParent = transform.parent;
        startPosition = transform.position;
        startRotation = transform.rotation;
        startActiveSelf = gameObject.activeSelf;

        if (rb != null)
        {
            startRigidbodyUseGravity = rb.useGravity;
            startRigidbodyIsKinematic = rb.isKinematic;
            startRigidbodyMass = rb.mass;
            startCollisionDetectionMode = rb.collisionDetectionMode;
            startRigidbodyInterpolation = rb.interpolation;
        }

        startGrabInteractableEnabled = grabInteractable == null || grabInteractable.enabled;
        CaptureColliderStates();
        hasCapturedReturnState = true;
    }

    private void CaptureColliderStates()
    {
        if (colliders == null)
        {
            startColliderStates = null;
            return;
        }

        startColliderStates = new ColliderState[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            startColliderStates[i] = new ColliderState
            {
                collider = collider,
                enabled = collider != null && collider.enabled,
                isTrigger = collider != null && collider.isTrigger
            };
        }
    }

    private void RestoreColliderStates()
    {
        RefreshCachedComponents();

        if (!hasCapturedReturnState || startColliderStates == null || startColliderStates.Length == 0)
        {
            SetCollidersEnabled(true);
            return;
        }

        for (int i = 0; i < startColliderStates.Length; i++)
        {
            ColliderState state = startColliderStates[i];
            if (state.collider == null)
                continue;

            state.collider.enabled = state.enabled;
            state.collider.isTrigger = state.isTrigger;
        }
    }

    private void ConfigureRigidbody()
    {
        if (rb == null)
            return;

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void ResetRigidbodyVelocity()
    {
        if (rb == null)
            return;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void ForceReleaseIfGrabbed()
    {
        if (grabInteractable == null || !grabInteractable.isSelected)
            return;

        var interactionManager = grabInteractable.interactionManager;
        if (interactionManager == null)
            return;

        var interactors = grabInteractable.interactorsSelecting;
        for (int i = interactors.Count - 1; i >= 0; i--)
        {
            var interactor = interactors[i];
            if (interactor != null)
                interactionManager.SelectExit(interactor, grabInteractable);
        }
    }

    private void PlayPickupSound()
    {
        AudioClip clip = GetRandomPickupClip();
        if (clip == null)
            return;

        GameObject audioObject = new GameObject("PickupSound");
        audioObject.transform.position = transform.position;

        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = pickupVolume;
        audioSource.pitch = Random.Range(pickupMinPitch, pickupMaxPitch);
        audioSource.spatialBlend = 0f;
        audioSource.Play();

        Destroy(audioObject, clip.length / Mathf.Max(0.1f, audioSource.pitch) + 0.1f);
    }

    private static AudioClip GetRandomPickupClip()
    {
        if (cachedPickupClips == null || cachedPickupClips.Length == 0)
            cachedPickupClips = Resources.LoadAll<AudioClip>("PickupSounds");

        if (cachedPickupClips == null || cachedPickupClips.Length == 0)
            return null;

        return cachedPickupClips[Random.Range(0, cachedPickupClips.Length)];
    }

    private struct ColliderState
    {
        public Collider collider;
        public bool enabled;
        public bool isTrigger;
    }
}
