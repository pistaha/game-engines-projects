using UnityEngine;

public class ClickCollector : MonoBehaviour
{
    // Обрабатывает попадание луча по объекту.
    // Сейчас нужен для открытия книги с клавиатуры, даже если перед ней есть лишний коллайдер.
    public bool TryHandleColliderHit(Collider hitCollider)
    {
        if (hitCollider == null)
            return false;

        RecipeBookViewer book = hitCollider.GetComponentInParent<RecipeBookViewer>();
        if (book != null)
        {
            book.Interact();
            return true;
        }

        return false;
    }
}
