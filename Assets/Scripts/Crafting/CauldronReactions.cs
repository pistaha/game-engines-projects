using System.Collections.Generic;
using UnityEngine;

public class CauldronReactions : MonoBehaviour
{
    [SerializeField] private CraftStorage storage;
    [SerializeField] private RecipeChecker recipeChecker;
    [SerializeField] private float stirCooldown = 1f;
    [SerializeField] private ParticleSystem longStirParticles;

    private readonly HashSet<int> acceptedInstanceIds = new HashSet<int>();
    private float nextAllowedStirTime;
    private RecipeData activeLongStirRecipe;
    private float longStirTime;
    private int lastLongStirFrame = -1;

    private void Awake()
    {
        if (storage == null)
            storage = GetComponent<CraftStorage>();

        if (recipeChecker == null)
            recipeChecker = GetComponent<RecipeChecker>();
    }

    private void Reset()
    {
        if (storage == null)
            storage = GetComponent<CraftStorage>();

        if (recipeChecker == null)
            recipeChecker = GetComponent<RecipeChecker>();
    }

    private void OnValidate()
    {
        if (storage == null)
            storage = GetComponent<CraftStorage>();

        if (recipeChecker == null)
            recipeChecker = GetComponent<RecipeChecker>();
    }

    public void TryStir(StirringSpoon spoon)
    {
        if (spoon == null)
            return;

        if (!spoon.IsHeld)
            return;

        if (storage == null || !storage.HasIngredients())
            return;

        // Рецепт проверяется только во время перемешивания.
        // До этого котёл просто накапливает добавленные ингредиенты.
        if (recipeChecker == null)
        {
            Debug.LogWarning("RecipeChecker is not assigned on cauldron.", this);
            return;
        }

        RecipeData matchedRecipe = recipeChecker.GetMatchingRecipeForCurrentIngredients();
        if (matchedRecipe != null && matchedRecipe.requiredStirSeconds > 0f)
        {
            HandleLongStirRecipe(matchedRecipe);
            return;
        }

        if (Time.time < nextAllowedStirTime)
            return;

        nextAllowedStirTime = Time.time + stirCooldown;
        StopLongStirParticles();
        activeLongStirRecipe = null;
        longStirTime = 0f;
        recipeChecker.CheckRecipe();
        acceptedInstanceIds.Clear();
    }

    public void TryAddIngredient(CollectableItem item, bool requireHeld)
    {
        if (item == null)
            return;

        EnsureRuntimeReferences();

        if (storage == null)
        {
            Debug.LogWarning("CauldronReactions: CraftStorage is missing, ingredient cannot be stored.", this);
            return;
        }

        if (!item.CanBeAddedToCauldron(requireHeld))
            return;

        int instanceId = item.gameObject.GetInstanceID();
        if (acceptedInstanceIds.Contains(instanceId))
            return;

        IngredientData ingredient = item.Ingredient;
        if (ingredient == null)
            return;

        // Одна бутылка может задеть несколько коллайдеров.
        // Запоминаем идентификатор объекта, чтобы один предмет добавился только один раз.
        acceptedInstanceIds.Add(instanceId);
        storage.AddIngredient(ingredient);
        storage.RegisterCollectedItem(item);
        item.AddToCauldron(requireHeld);
    }

    public void ResetCauldronState()
    {
        acceptedInstanceIds.Clear();
        nextAllowedStirTime = 0f;
        activeLongStirRecipe = null;
        longStirTime = 0f;
        lastLongStirFrame = -1;
        StopLongStirParticles();
    }

    private void EnsureRuntimeReferences()
    {
        if (storage == null)
            storage = GetComponent<CraftStorage>();

        if (recipeChecker == null)
            recipeChecker = GetComponent<RecipeChecker>();
    }

    private void HandleLongStirRecipe(RecipeData recipe)
    {
        if (activeLongStirRecipe != recipe)
        {
            activeLongStirRecipe = recipe;
            longStirTime = 0f;
        }

        if (lastLongStirFrame != Time.frameCount)
        {
            longStirTime += Time.deltaTime;
            lastLongStirFrame = Time.frameCount;
        }
        StartLongStirParticles();

        if (longStirTime < recipe.requiredStirSeconds)
            return;

        StopLongStirParticles();
        activeLongStirRecipe = null;
        longStirTime = 0f;
        nextAllowedStirTime = Time.time + stirCooldown;
        recipeChecker.CheckRecipe();
        acceptedInstanceIds.Clear();
    }

    private void StartLongStirParticles()
    {
        if (longStirParticles == null)
            longStirParticles = GetComponentInChildren<ParticleSystem>(true);

        if (longStirParticles == null || longStirParticles.isPlaying)
            return;

        longStirParticles.Clear();
        longStirParticles.Play();
    }

    private void StopLongStirParticles()
    {
        if (longStirParticles == null)
            return;

        longStirParticles.Stop();
    }
}
