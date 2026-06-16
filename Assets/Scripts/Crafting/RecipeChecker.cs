using System;
using System.Collections.Generic;
using UnityEngine;

public class RecipeChecker : MonoBehaviour
{
    public static event Action<RecipeData> OnRecipeSuccess;
    public static event Action OnRecipeFail;

    public CraftStorage storage;
    public List<RecipeData> recipes = new List<RecipeData>();
    public Transform ejectPoint;
    public float ejectUpwardForce = 3f;
    public float ejectForwardForce = 4f;
    public float ejectSpreadForce = 0.6f;

    private CauldronReactions cachedCauldronReactions;

    private void Awake()
    {
        if (storage == null)
            storage = GetComponent<CraftStorage>();

        cachedCauldronReactions = GetComponent<CauldronReactions>();
    }

    public bool HasIngredientsToCheck()
    {
        return storage != null && storage.HasIngredients();
    }

    public bool CheckRecipe()
    {
        if (storage == null)
            return false;

        List<IngredientData> currentIngredients = storage.GetIngredients();
        if (currentIngredients.Count == 0)
            return false;

        if (recipes == null || recipes.Count == 0)
        {
            Debug.LogWarning("RecipeChecker has no recipes assigned.", this);
            storage.ReturnAllCollectedItems();
            storage.ClearIngredients();
            cachedCauldronReactions?.ResetCauldronState();
            OnRecipeFail?.Invoke();
            return false;
        }

        RecipeData matchedRecipe = FindMatchingRecipe(currentIngredients);
        if (matchedRecipe != null)
        {
            // После успешной проверки котел очищается, а конкретный визуальный эффект слушает OnRecipeSuccess.
            storage.ReturnAllCollectedItems();
            storage.ClearIngredients();
            cachedCauldronReactions?.ResetCauldronState();
            OnRecipeSuccess?.Invoke(matchedRecipe);
            return true;
        }

        storage.ReturnAllCollectedItems();
        storage.ClearIngredients();
        cachedCauldronReactions?.ResetCauldronState();
        OnRecipeFail?.Invoke();
        return false;
    }

    public RecipeData FindMatchingRecipeForIngredients(List<IngredientData> currentIngredients)
    {
        if (currentIngredients == null || currentIngredients.Count == 0)
            return null;

        return FindMatchingRecipe(currentIngredients);
    }

    private RecipeData FindMatchingRecipe(List<IngredientData> currentIngredients)
    {
        for (int i = 0; i < recipes.Count; i++)
        {
            RecipeData recipe = recipes[i];

            if (recipe == null)
                continue;

            if (recipe.ingredients == null)
                continue;

            if (IsSameRecipe(currentIngredients, recipe.ingredients))
                return recipe;
        }

        return null;
    }

    private bool IsSameRecipe(List<IngredientData> currentIngredients, List<IngredientData> recipeIngredients)
    {
        if (currentIngredients.Count != recipeIngredients.Count)
            return false;

        // Порядок добавления бутылок не важен: каждый найденный ингредиент удаляется из временного списка рецепта.
        List<IngredientData> tempRecipe = new List<IngredientData>(recipeIngredients);

        for (int i = 0; i < currentIngredients.Count; i++)
        {
            IngredientData currentIngredient = currentIngredients[i];

            if (currentIngredient == null)
                return false;

            int foundIndex = tempRecipe.FindIndex(x => x == currentIngredient);

            if (foundIndex == -1)
                return false;

            tempRecipe.RemoveAt(foundIndex);
        }

        return tempRecipe.Count == 0;
    }
}
