using UnityEngine;

public class ClickCollector : MonoBehaviour
{
    public bool TryHandleColliderHit(Collider hitCollider)
    {
        if (hitCollider == null)
            return false;

        CollectableItem item = hitCollider.GetComponentInParent<CollectableItem>();
        if (item != null && item.Collect())
            return true;

        RecipeBookViewer book = hitCollider.GetComponentInParent<RecipeBookViewer>();
        if (book != null)
        {
            book.Interact();
            return true;
        }

        return false;
    }
}
