using UnityEngine;
using UnityEngine.InputSystem;

public class DesktopGrabber : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private float grabDistance = 4f;
    [SerializeField] private float holdDistance = 1.25f;
    [SerializeField] private float holdHeightOffset = -0.15f;
    [SerializeField] private float followSpeed = 24f;
    [SerializeField] private LayerMask grabMask = ~0;

    private Rigidbody heldBody;
    private CollectableItem heldCollectable;
    private StirringSpoon heldSpoon;
    private bool previousKinematic;
    private bool previousUseGravity;
    private CollisionDetectionMode previousCollisionDetectionMode;
    private CauldronIngredientZone[] ingredientZones;
    private CauldronStirZone[] stirZones;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerInteraction == null)
            playerInteraction = GetComponent<PlayerInteraction>();

        RefreshZones();
    }

    private void OnDisable()
    {
        ReleaseHeld();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse != null && mouse.rightButton.wasPressedThisFrame)
            ToggleHeldObject();

        UpdateHeldPose();
        CheckHeldObjectAgainstCauldronZones();
    }

    private void ToggleHeldObject()
    {
        // На десктопе захват работает как переключатель:
        // один ПКМ берёт предмет, следующий ПКМ отпускает.
        if (heldBody != null)
        {
            ReleaseHeld();
            return;
        }

        if (TryOpenBookUnderReticle())
            return;

        TryGrab();
    }

    private bool TryOpenBookUnderReticle()
    {
        Camera rayCamera = playerCamera != null ? playerCamera : Camera.main;
        if (rayCamera == null)
            return false;

        Ray ray = new Ray(rayCamera.transform.position, rayCamera.transform.forward);
        // Берём все попадания луча, чтобы книга открывалась даже тогда,
        // когда перед ней находится декоративный коллайдер.
        RaycastHit[] hits = Physics.RaycastAll(ray, grabDistance, grabMask, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            RecipeBookViewer book = hits[i].collider.GetComponentInParent<RecipeBookViewer>();
            if (book == null || !book.CanInteract(playerInteraction))
                continue;

            book.Interact(playerInteraction);
            return true;
        }

        return false;
    }

    private void TryGrab()
    {
        if (heldBody != null)
            return;

        if (playerInteraction != null && playerInteraction.HeldItem != null)
            return;

        if (!FindGrabbableUnderReticle(out Rigidbody body, out CollectableItem collectable, out StirringSpoon spoon, out _))
            return;

        if (collectable == null && spoon == null)
            return;

        if (body == null)
            return;

        heldBody = body;
        heldCollectable = collectable;
        heldSpoon = spoon;
        previousKinematic = heldBody.isKinematic;
        previousUseGravity = heldBody.useGravity;
        previousCollisionDetectionMode = heldBody.collisionDetectionMode;

        // Пока предмет в руках, мы двигаем его скриптом перед камерой.
        // Кинематический режим нужен, чтобы физика не спорила с этим движением.
        heldBody.isKinematic = false;
        heldBody.linearVelocity = Vector3.zero;
        heldBody.angularVelocity = Vector3.zero;
        heldBody.useGravity = false;
        heldBody.isKinematic = true;
        heldBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        heldCollectable?.SetDesktopHeld(true);
        heldSpoon?.SetDesktopHeld(true);
        RefreshZones();
    }

    private bool FindGrabbableUnderReticle(
        out Rigidbody body,
        out CollectableItem collectable,
        out StirringSpoon spoon,
        out RaycastHit hit)
    {
        body = null;
        collectable = null;
        spoon = null;
        hit = default;

        Camera rayCamera = playerCamera != null ? playerCamera : Camera.main;
        if (rayCamera == null)
            return false;

        Ray ray = new Ray(rayCamera.transform.position, rayCamera.transform.forward);
        if (!Physics.Raycast(ray, out hit, grabDistance, grabMask, QueryTriggerInteraction.Ignore))
            return false;

        collectable = hit.collider.GetComponentInParent<CollectableItem>();
        spoon = hit.collider.GetComponentInParent<StirringSpoon>();
        if (collectable == null && spoon == null)
            return false;

        body = hit.collider.GetComponentInParent<Rigidbody>();
        return body != null;
    }

    private void UpdateHeldPose()
    {
        if (heldBody == null)
            return;

        if (heldCollectable != null && heldCollectable.IsAddedToCauldron)
        {
            ReleaseHeld();
            return;
        }

        Camera rayCamera = playerCamera != null ? playerCamera : Camera.main;
        if (rayCamera == null)
            return;

        Transform cameraTransform = rayCamera.transform;
        Vector3 targetPosition =
            cameraTransform.position
            + cameraTransform.forward * holdDistance
            + cameraTransform.up * holdHeightOffset;

        Transform heldTransform = heldBody.transform;
        heldTransform.position = Vector3.Lerp(
            heldTransform.position,
            targetPosition,
            1f - Mathf.Exp(-followSpeed * Time.deltaTime)
        );
    }

    private void CheckHeldObjectAgainstCauldronZones()
    {
        if (heldBody == null)
            return;

        Physics.SyncTransforms();

        // Предмет должен реально коснуться триггер-зоны котла.
        // Так ингредиенты и ложка не срабатывают заранее на расстоянии.
        if (heldCollectable != null && TryFeedHeldCollectableToIngredientZone())
            return;

        if (heldSpoon != null)
            TryStirWithHeldSpoonInStirZone();
    }

    private bool TryFeedHeldCollectableToIngredientZone()
    {
        if (heldCollectable == null || heldCollectable.IsAddedToCauldron)
            return false;

        if (ingredientZones == null || ingredientZones.Length == 0)
            RefreshZones();

        for (int i = 0; i < ingredientZones.Length; i++)
        {
            CauldronIngredientZone zone = ingredientZones[i];
            if (zone == null || !zone.isActiveAndEnabled)
                continue;

            if (!zone.ContainsIngredientPoint(heldCollectable))
                continue;

            zone.TryAddIngredient(heldCollectable);
            if (heldCollectable.IsAddedToCauldron)
            {
                ReleaseHeld();
                return true;
            }
        }

        return false;
    }

    private void TryStirWithHeldSpoonInStirZone()
    {
        if (heldSpoon == null)
            return;

        if (stirZones == null || stirZones.Length == 0)
            RefreshZones();

        for (int i = 0; i < stirZones.Length; i++)
        {
            CauldronStirZone zone = stirZones[i];
            if (zone == null || !zone.isActiveAndEnabled)
                continue;

            if (zone.ContainsSpoonPoint(heldSpoon))
                zone.TryHandleStir(heldSpoon);
        }
    }

    private void RefreshZones()
    {
        ingredientZones = FindObjectsByType<CauldronIngredientZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        stirZones = FindObjectsByType<CauldronStirZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
    }

    private void ReleaseHeld()
    {
        if (heldBody == null)
            return;

        heldCollectable?.SetDesktopHeld(false);
        heldSpoon?.SetDesktopHeld(false);

        if (heldCollectable == null || !heldCollectable.IsAddedToCauldron)
        {
            heldBody.isKinematic = previousKinematic;
            heldBody.useGravity = previousUseGravity;
            heldBody.collisionDetectionMode = previousCollisionDetectionMode;
            heldBody.WakeUp();
        }

        heldBody = null;
        heldCollectable = null;
        heldSpoon = null;
    }
}
