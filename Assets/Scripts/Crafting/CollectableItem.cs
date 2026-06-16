using UnityEngine;

public class CollectableItem : PickupItem
{
    [Header("Данные бутылки")]
    public IngredientData ingredient;
    public CraftStorage storage;

    [Header("Звук подбора")]
    [SerializeField] private float pickupVolume = 0.8f;
    [SerializeField] private float pickupMinPitch = 0.96f;
    [SerializeField] private float pickupMaxPitch = 1.04f;

    private bool addedToCauldron = false;
    private bool desktopHeld = false;
    private bool ignoreCauldronUntilHeld = false;
    private static AudioClip[] cachedPickupClips;

    private Renderer[] renderers;
    private Collider[] colliders;
    private Rigidbody rb;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Transform customReturnPoint;
    private bool hasCustomReturnPose;
    private Vector3 customReturnPosition;
    private Quaternion customReturnRotation;

    protected override void Awake()
    {
        base.Awake();
        RefreshCachedComponents();
        ConfigureRigidbody();
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    public void ReturnToStart()
    {
        RefreshCachedComponents();
        desktopHeld = false;

        if (customReturnPoint != null)
        {
            startPosition = customReturnPoint.position;
            startRotation = customReturnPoint.rotation;
        }
        else if (hasCustomReturnPose)
        {
            startPosition = customReturnPosition;
            startRotation = customReturnRotation;
        }

        transform.position = startPosition;
        transform.rotation = startRotation;

        RestoreVisiblePhysics(startPosition, startRotation);

        SetItemState(true);

        addedToCauldron = false;
        ignoreCauldronUntilHeld = false;
    }

    public IngredientData Ingredient => ingredient;

    public bool IsAddedToCauldron => addedToCauldron;

    public bool IsHeld => IsHeldByPlayer || desktopHeld;

    public override string InteractionPrompt => IsHeldByPlayer ? "ПКМ - отпустить" : "ПКМ - взять";

    public void SetDesktopHeld(bool value)
    {
        desktopHeld = value;
        if (value)
            ignoreCauldronUntilHeld = false;
    }

    public bool CanBeAddedToCauldron(bool requireHeld = true)
    {
        RefreshCachedComponents();
        return ingredient != null && !addedToCauldron && !ignoreCauldronUntilHeld && (!requireHeld || IsHeld);
    }

    public override void PickUp(PlayerInteraction playerInteraction)
    {
        ignoreCauldronUntilHeld = false;
        base.PickUp(playerInteraction);
    }

    public bool TryMarkAddedToCauldron(bool requireHeld = true)
    {
        if (!CanBeAddedToCauldron(requireHeld))
            return false;

        addedToCauldron = true;
        return true;
    }

    public void AddToCauldron(bool requireHeld = true)
    {
        if (!TryMarkAddedToCauldron(requireHeld))
            return;

        // При добавлении в котёл бутылка скрывается сразу.
        // Хранилище крафта запоминает её, чтобы потом вернуть на стол.
        desktopHeld = false;
        ForceReleaseWithoutPhysicsRestore();
        PlayPickupSound();
        HideItem();
    }

    public void SetCustomReturnPoint(Transform returnPoint)
    {
        customReturnPoint = returnPoint;
    }

    public void SetCustomReturnPose(Vector3 position, Quaternion rotation)
    {
        hasCustomReturnPose = true;
        customReturnPosition = position;
        customReturnRotation = rotation;
    }

    private void HideItem()
    {
        RefreshCachedComponents();

        SetItemState(false);

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    private void SetItemState(bool value)
    {
        RefreshCachedComponents();

        foreach (Renderer r in renderers)
        {
            if (r != null)
                r.enabled = value;
        }

        foreach (Collider c in colliders)
        {
            if (c != null)
                c.enabled = value;
        }
    }

    private void RestoreVisiblePhysics(Vector3 position, Quaternion rotation)
    {
        if (rb == null)
            return;

        // Во время удержания у бутылки меняются физические настройки.
        // При возврате на стол их нужно сбросить, иначе предмет может зависнуть.
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.detectCollisions = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = position;
        rb.rotation = rotation;
        rb.WakeUp();
    }

    private void RefreshCachedComponents()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        rb = GetComponent<Rigidbody>();
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

    private void PlayPickupSound()
    {
        AudioClip clip = GetRandomPickupClip();
        if (clip == null)
            return;

        GameObject audioObject = new GameObject("PickupSound");
        audioObject.transform.position = transform.position;

        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = pickupVolume * DesktopPauseMenu.SfxVolume;
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
}
