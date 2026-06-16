using System;
using System.Collections.Generic;
using UnityEngine;

public class RecipeChecker : MonoBehaviour
{
    public static event Action<RecipeData> OnRecipeSuccess;
    public static event Action OnRecipeFail;

    public CraftStorage storage;
    public RecipeData activeRecipe;
    public List<RecipeData> recipes = new List<RecipeData>();

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
        {
            Debug.LogWarning("RecipeChecker: CraftStorage is missing.", this);
            return false;
        }

        List<IngredientData> currentIngredients = storage.GetIngredients();
        if (currentIngredients.Count == 0)
            return false;

        // Сам рецепт проверяется только после действия перемешивания.
        // Зона котла лишь собирает ингредиенты, но не решает успех/ошибку.
        RecipeData matchedRecipe = FindMatchingRecipe(currentIngredients);
        if (matchedRecipe != null)
        {
            // После проверки возвращаем физические бутылки на стол
            // и очищаем список ингредиентов для следующей попытки.
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

    public RecipeData GetMatchingRecipeForCurrentIngredients()
    {
        if (storage == null)
            return null;

        List<IngredientData> currentIngredients = storage.GetIngredients();
        if (currentIngredients.Count == 0)
            return null;

        return FindMatchingRecipe(currentIngredients);
    }

    private RecipeData FindMatchingRecipe(List<IngredientData> currentIngredients)
    {
        if (activeRecipe != null)
            return IsSameRecipe(currentIngredients, activeRecipe) ? activeRecipe : null;

        for (int i = 0; i < recipes.Count; i++)
        {
            RecipeData recipe = recipes[i];

            if (recipe == null)
                continue;

            if (IsSameRecipe(currentIngredients, recipe))
                return recipe;
        }

        return null;
    }

    private bool IsSameRecipe(List<IngredientData> currentIngredients, RecipeData recipe)
    {
        if (recipe == null || recipe.ingredients == null)
            return false;

        List<IngredientData> recipeIngredients = recipe.ingredients;
        if (currentIngredients.Count != recipeIngredients.Count)
            return false;

        if (recipe.ingredientOrderMatters)
            return IsSameRecipeInOrder(currentIngredients, recipeIngredients);

        // Если порядок не важен, сравниваем набор ингредиентов.
        // Копия списка нужна, чтобы корректно считать повторяющиеся ингредиенты.
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

    private bool IsSameRecipeInOrder(List<IngredientData> currentIngredients, List<IngredientData> recipeIngredients)
    {
        for (int i = 0; i < currentIngredients.Count; i++)
        {
            if (currentIngredients[i] == null || currentIngredients[i] != recipeIngredients[i])
                return false;
        }

        return true;
    }
}
