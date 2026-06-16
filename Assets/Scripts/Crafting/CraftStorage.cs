using System.Collections.Generic;
using UnityEngine;

public class CraftStorage : MonoBehaviour
{
    public event System.Action OnStorageChanged;

    [SerializeField] private List<IngredientData> collectedIngredients = new List<IngredientData>();
    [SerializeField] private List<CollectableItem> collectedItems = new List<CollectableItem>();

    public void AddIngredient(IngredientData ingredient)
    {
        if (ingredient == null)
            return;

        // В одном списке лежат данные ингредиентов для проверки рецепта.
        // В другом списке лежат физические бутылки, которые потом возвращаются на стол.
        collectedIngredients.Add(ingredient);
        OnStorageChanged?.Invoke();
    }

    public void RegisterCollectedItem(CollectableItem item)
    {
        if (item == null || collectedItems.Contains(item))
            return;

        collectedItems.Add(item);
    }

    public List<IngredientData> GetIngredients()
    {
        return new List<IngredientData>(collectedIngredients);
    }

    public void ReturnAllCollectedItems()
    {
        // После успешного или неправильного рецепта бутылки возвращаются,
        // а сам результат показывают отдельные эффекты.
        for (int i = 0; i < collectedItems.Count; i++)
        {
            if (collectedItems[i] == null)
                continue;

            collectedItems[i].ReturnToStart();
        }

        collectedItems.Clear();
        RemoveNullIngredients();
        OnStorageChanged?.Invoke();
    }

    public void ClearIngredients()
    {
        collectedIngredients.Clear();
        collectedItems.RemoveAll(item => item == null);
        OnStorageChanged?.Invoke();
    }

    public bool HasIngredients()
    {
        RemoveNullIngredients();
        return collectedIngredients.Count > 0;
    }

    private void RemoveNullIngredients()
    {
        collectedIngredients.RemoveAll(ingredient => ingredient == null);
    }
}
