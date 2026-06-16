using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CauldronIngredientZone : MonoBehaviour
{
    [SerializeField] private CauldronReactions cauldron;
    [SerializeField] private int overlapBufferSize = 64;
    [SerializeField] private bool forceFrontZoneShape = false;
    [SerializeField] private Vector3 frontLocalPosition = new Vector3(0f, 0.28f, 0.72f);
    [SerializeField] private Vector3 frontLocalScale = new Vector3(1.1f, 1.7f, 0.55f);
    [SerializeField] private Vector3 frontColliderSize = new Vector3(0.65f, 0.42f, 0.5f);

    private Collider zoneCollider;
    private Collider[] overlapBuffer;

    private void Reset()
    {
        zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null)
            zoneCollider.isTrigger = true;

        if (cauldron == null)
            cauldron = GetComponentInParent<CauldronReactions>();
    }

    private void Awake()
    {
        EnsureReferences(true);
        ApplyFrontZoneShape();
        overlapBuffer = new Collider[Mathf.Max(8, overlapBufferSize)];
    }

    private void Update()
    {
        AbsorbIngredientsInsideZone();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryAddIngredient(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryAddIngredient(other);
    }

    public void TryAddIngredient(Collider other)
    {
        EnsureReferences(true);

        if (cauldron == null || other == null)
            return;

        CollectableItem item = other.GetComponentInParent<CollectableItem>();
        if (item == null || !ContainsIngredientPoint(item))
            return;

        cauldron.TryAddIngredient(item, true);
    }

    public void TryAddIngredient(CollectableItem item)
    {
        EnsureReferences(true);

        if (cauldron == null || item == null)
            return;

        if (!ContainsIngredientPoint(item))
            return;

        cauldron.TryAddIngredient(item, true);
    }

    public bool ContainsIngredientPoint(CollectableItem item)
    {
        if (item == null)
            return false;

        // Ингредиент засчитывается только при реальном пересечении коллайдера
        // бутылки с триггер-зоной котла, а не просто рядом с котлом.
        Collider[] itemColliders = item.GetComponentsInChildren<Collider>(false);
        for (int i = 0; i < itemColliders.Length; i++)
        {
            Collider itemCollider = itemColliders[i];
            if (itemCollider != null && itemCollider.enabled && IsTouchingZone(itemCollider))
                return true;
        }

        Rigidbody itemBody = item.GetComponent<Rigidbody>();
        Vector3 checkPoint = itemBody != null ? itemBody.worldCenterOfMass : item.transform.position;
        return ContainsPoint(checkPoint);
    }

    public bool ContainsPoint(Vector3 worldPoint)
    {
        EnsureReferences(false);

        if (zoneCollider == null || !zoneCollider.enabled)
            return false;

        Vector3 closestPoint = zoneCollider.ClosestPoint(worldPoint);
        return (closestPoint - worldPoint).sqrMagnitude <= 0.0001f;
    }

    private bool IsTouchingZone(Collider other)
    {
        if (zoneCollider == null || other == null || other == zoneCollider)
            return false;

        return Physics.ComputePenetration(
            zoneCollider,
            zoneCollider.transform.position,
            zoneCollider.transform.rotation,
            other,
            other.transform.position,
            other.transform.rotation,
            out _,
            out _
        );
    }

    private void AbsorbIngredientsInsideZone()
    {
        EnsureReferences(false);

        if (cauldron == null || zoneCollider == null || !zoneCollider.enabled)
            return;

        Physics.SyncTransforms();

        // Предметы в руках двигаются скриптом, поэтому события триггера могут
        // пропуститься. Поэтому зона дополнительно сканируется каждый кадр.
        Bounds bounds = zoneCollider.bounds;
        int count = Physics.OverlapBoxNonAlloc(
            bounds.center,
            bounds.extents,
            overlapBuffer,
            Quaternion.identity,
            ~0,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < count; i++)
        {
            Collider hit = overlapBuffer[i];
            if (hit == null || hit == zoneCollider)
                continue;

            CollectableItem item = hit.GetComponentInParent<CollectableItem>();
            if (item == null || item.IsAddedToCauldron)
                continue;

            if (!ContainsIngredientPoint(item))
                continue;

            cauldron.TryAddIngredient(item, true);
        }
    }

    private void EnsureReferences(bool allowSceneSearch)
    {
        if (zoneCollider == null)
            zoneCollider = GetComponent<Collider>();

        if (zoneCollider != null)
            zoneCollider.isTrigger = true;

        if (cauldron == null)
            cauldron = GetComponentInParent<CauldronReactions>();

        if (allowSceneSearch && cauldron == null)
            cauldron = FindFirstObjectByType<CauldronReactions>();
    }

    private void ApplyFrontZoneShape()
    {
        if (!forceFrontZoneShape)
            return;

        transform.localPosition = frontLocalPosition;
        transform.localRotation = Quaternion.identity;
        transform.localScale = frontLocalScale;

        if (zoneCollider is BoxCollider boxCollider)
        {
            boxCollider.center = Vector3.zero;
            boxCollider.size = frontColliderSize;
        }
    }
}
