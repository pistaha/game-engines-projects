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

        collectedIngredients.Add(ingredient);
        OnStorageChanged?.Invoke();
    }

    public void RegisterCollectedItem(CollectableItem item)
    {
        if (item == null || collectedItems.Contains(item))
            return;

        collectedItems.Add(item);
    }

    public bool ContainsCollectedItem(CollectableItem item)
    {
        return item != null && collectedItems.Contains(item);
    }

    public List<IngredientData> GetIngredients()
    {
        return new List<IngredientData>(collectedIngredients);
    }

    public int GetIngredientCount()
    {
        return collectedIngredients.Count;
    }

    public void ReturnAllCollectedItems()
    {
        // При проверке рецепта бутылки возвращаются на исходные позиции вместо окончательного удаления из сцены.
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

    public void EjectAllCollectedItems(Transform ejectPoint, float upwardForce, float forwardForce, float spreadForce)
    {
        ReturnAllCollectedItems();
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
