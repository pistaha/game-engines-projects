using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CauldronStirZone : MonoBehaviour
{
    [SerializeField] private CauldronReactions cauldron;
    [SerializeField] private int overlapBufferSize = 32;

    private readonly HashSet<StirringSpoon> spoonsCheckedThisTouch = new HashSet<StirringSpoon>();
    private readonly HashSet<StirringSpoon> spoonsInsideThisFrame = new HashSet<StirringSpoon>();
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
        overlapBuffer = new Collider[Mathf.Max(8, overlapBufferSize)];
    }

    private void Update()
    {
        ScanHeldSpoonsInsideZone();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHandleStir(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryHandleStir(other);
    }

    private void OnTriggerExit(Collider other)
    {
        StirringSpoon spoon = other != null ? other.GetComponentInParent<StirringSpoon>() : null;
        if (spoon != null)
            spoonsCheckedThisTouch.Remove(spoon);
    }

    private void TryHandleStir(Collider other)
    {
        EnsureReferences(true);

        if (cauldron == null || other == null)
            return;

        StirringSpoon spoon = other.GetComponentInParent<StirringSpoon>();
        if (spoon == null)
            return;

        if (!ContainsSpoonPoint(spoon))
            return;

        TryHandleStir(spoon);
    }

    public void TryHandleStir(StirringSpoon spoon)
    {
        EnsureReferences(true);

        if (cauldron == null || spoon == null)
            return;

        if (!spoon.IsHeld || !ContainsSpoonPoint(spoon))
            return;

        spoonsCheckedThisTouch.Add(spoon);
        cauldron.TryStir(spoon);
    }

    public bool ContainsSpoonPoint(StirringSpoon spoon)
    {
        if (spoon == null)
            return false;

        Collider[] spoonColliders = spoon.GetComponentsInChildren<Collider>(false);
        for (int i = 0; i < spoonColliders.Length; i++)
        {
            Collider spoonCollider = spoonColliders[i];
            if (spoonCollider != null && spoonCollider.enabled && IsTouchingZone(spoonCollider))
                return true;
        }

        Rigidbody spoonBody = spoon.GetComponent<Rigidbody>();
        Vector3 checkPoint = spoonBody != null ? spoonBody.worldCenterOfMass : spoon.transform.position;
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

    private void ScanHeldSpoonsInsideZone()
    {
        EnsureReferences(false);

        if (cauldron == null || zoneCollider == null || !zoneCollider.enabled)
            return;

        Physics.SyncTransforms();
        spoonsInsideThisFrame.Clear();

        // Ложка в руках двигается скриптом, поэтому обычные события триггера
        // могут прийти не всегда. Дополнительная проверка делает мешание стабильным.
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

            StirringSpoon spoon = hit.GetComponentInParent<StirringSpoon>();
            if (spoon == null || !spoon.IsHeld)
                continue;

            if (!ContainsSpoonPoint(spoon))
                continue;

            spoonsInsideThisFrame.Add(spoon);
            TryHandleStir(spoon);
        }

        spoonsCheckedThisTouch.RemoveWhere(spoon => spoon == null || !spoonsInsideThisFrame.Contains(spoon));
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
}
