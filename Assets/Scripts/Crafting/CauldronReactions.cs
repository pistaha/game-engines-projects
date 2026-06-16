using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CauldronReactions : MonoBehaviour
{
    public enum CauldronGameplayState
    {
        WaitingForIngredients,
        ReadyToMix,
        CheckingRecipe,
        SuccessReaction,
        FailureReaction,
        Resetting
    }

    [SerializeField] private CraftStorage storage;
    [SerializeField] private RecipeChecker recipeChecker;
    [SerializeField] private float stirCooldown = 1f;
    [SerializeField] private float successReactionLockDuration = 2f;
    [SerializeField] private float failureReactionLockDuration = 1.2f;
    [SerializeField] private float resetLockDuration = 0.1f;

    private readonly HashSet<int> acceptedInstanceIds = new HashSet<int>();
    private float nextAllowedStirTime;
    private Coroutine stateLockRoutine;

    public CauldronGameplayState State { get; private set; } = CauldronGameplayState.WaitingForIngredients;
    public bool IsBusy => State == CauldronGameplayState.CheckingRecipe
        || State == CauldronGameplayState.SuccessReaction
        || State == CauldronGameplayState.FailureReaction
        || State == CauldronGameplayState.Resetting;

    public bool CanAcceptIngredients => State == CauldronGameplayState.WaitingForIngredients
        || State == CauldronGameplayState.ReadyToMix;

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

        if (Time.time < nextAllowedStirTime)
            return;

        if (IsBusy)
            return;

        nextAllowedStirTime = Time.time + stirCooldown;

        if (storage == null || !storage.HasIngredients())
            return;

        if (recipeChecker == null)
        {
            Debug.LogWarning("RecipeChecker is not assigned on cauldron.", this);
            return;
        }

        State = CauldronGameplayState.CheckingRecipe;
        bool success = recipeChecker.CheckRecipe();
        StartReactionLock(success);
    }

    public void TryStir(GameObject stirrerObject)
    {
        if (stirrerObject == null)
            return;

        StirringSpoon spoon = stirrerObject.GetComponentInParent<StirringSpoon>();
        if (spoon != null)
            TryStir(spoon);
    }

    public void TryAddIngredient(Collider other)
    {
        if (other == null || storage == null)
            return;

        if (!CanAcceptIngredients)
            return;

        CollectableItem item = other.GetComponentInParent<CollectableItem>();
        if (item == null || !item.CanBeAddedToCauldron())
            return;

        int instanceId = item.gameObject.GetInstanceID();
        if (acceptedInstanceIds.Contains(instanceId))
            return;

        IngredientData ingredient = item.Ingredient;
        if (ingredient == null)
            return;

        if (!item.TryPlaceInCauldron(storage))
            return;

        acceptedInstanceIds.Add(instanceId);
        State = CauldronGameplayState.ReadyToMix;
        Debug.Log($"Ingredient added to cauldron: {ingredient.ingredientName}", item);
    }

    public void ResetCauldronState()
    {
        acceptedInstanceIds.Clear();
        nextAllowedStirTime = 0f;

        if (State == CauldronGameplayState.SuccessReaction || State == CauldronGameplayState.FailureReaction)
            return;

        State = storage != null && storage.HasIngredients()
            ? CauldronGameplayState.ReadyToMix
            : CauldronGameplayState.WaitingForIngredients;
    }

    private void StartReactionLock(bool success)
    {
        if (stateLockRoutine != null)
            StopCoroutine(stateLockRoutine);

        State = success ? CauldronGameplayState.SuccessReaction : CauldronGameplayState.FailureReaction;
        float reactionDuration = success ? successReactionLockDuration : failureReactionLockDuration;
        stateLockRoutine = StartCoroutine(UnlockAfterReaction(Mathf.Max(0f, reactionDuration)));
    }

    private IEnumerator UnlockAfterReaction(float reactionDuration)
    {
        if (reactionDuration > 0f)
            yield return new WaitForSeconds(reactionDuration);

        State = CauldronGameplayState.Resetting;

        if (resetLockDuration > 0f)
            yield return new WaitForSeconds(resetLockDuration);

        stateLockRoutine = null;
        ResetCauldronState();
    }
}
