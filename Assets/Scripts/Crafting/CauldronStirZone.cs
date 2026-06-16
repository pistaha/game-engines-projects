using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CauldronStirZone : MonoBehaviour
{
    [SerializeField] private CauldronReactions cauldron;
    private readonly Dictionary<StirringSpoon, int> spoonOverlapCounts = new Dictionary<StirringSpoon, int>();

    private void Reset()
    {
        Collider zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null)
            zoneCollider.isTrigger = true;

        if (cauldron == null)
            cauldron = GetComponentInParent<CauldronReactions>();
    }

    private void Awake()
    {
        if (cauldron == null)
            cauldron = GetComponentInParent<CauldronReactions>();
    }

    private void OnTriggerEnter(Collider other)
    {
        StirringSpoon spoon = GetSpoon(other);
        if (spoon == null)
            return;

        // У ложки может быть несколько коллайдеров, поэтому считаем пересечения и не запускаем помешивание повторно.
        if (spoonOverlapCounts.TryGetValue(spoon, out int count))
        {
            spoonOverlapCounts[spoon] = count + 1;
            return;
        }

        spoonOverlapCounts.Add(spoon, 1);
        TryHandleStir(spoon);
    }

    private void OnTriggerExit(Collider other)
    {
        StirringSpoon spoon = GetSpoon(other);
        if (spoon == null)
            return;

        if (!spoonOverlapCounts.TryGetValue(spoon, out int count))
            return;

        count--;
        if (count > 0)
        {
            spoonOverlapCounts[spoon] = count;
            return;
        }

        spoonOverlapCounts.Remove(spoon);
    }

    private void TryHandleStir(StirringSpoon spoon)
    {
        if (cauldron == null || spoon == null)
            return;

        cauldron.TryStir(spoon);
    }

    private static StirringSpoon GetSpoon(Collider other)
    {
        if (other == null)
            return null;

        return other.GetComponentInParent<StirringSpoon>();
    }

    private void OnDisable()
    {
        spoonOverlapCounts.Clear();
    }

    private void OnTriggerStay(Collider other)
    {
        if (other == null)
            return;

        StirringSpoon spoon = GetSpoon(other);
        if (spoon != null && !spoonOverlapCounts.ContainsKey(spoon))
        {
            spoonOverlapCounts.Add(spoon, 1);
            TryHandleStir(spoon);
        }
    }
}
