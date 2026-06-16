using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CauldronIngredientZone : MonoBehaviour
{
    [SerializeField] private CauldronReactions cauldron;

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
        TryHandleIngredient(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryHandleIngredient(other);
    }

    private void TryHandleIngredient(Collider other)
    {
        if (cauldron == null)
            return;

        // Зона только делегирует попадание объекта котлу; фильтрация бутылок живет в CauldronReactions.
        cauldron.TryAddIngredient(other);
    }
}
