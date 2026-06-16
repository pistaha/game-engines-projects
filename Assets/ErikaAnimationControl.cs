using UnityEngine;
using UnityEngine.InputSystem;

public class ErikaAnimationControl : MonoBehaviour
{
    [SerializeField] private Animator animator = null;
    [SerializeField] private RecipeData stormHeartRecipe = null;
    [SerializeField] private RecipeData recipeMovementRecipe = null;
    [SerializeField] private Key keyboardMovementKey = Key.T;
    [SerializeField] private string saluteTrigger = "Salute";
    [SerializeField] private string takingPunchTrigger = "TakingPunch";
    [SerializeField] private string keyboardMovementTrigger = "Salute";
    [SerializeField] private string recipeMovementTrigger = "TakingPunch";
    [SerializeField] private bool triggerMovementOnRecipeSuccess = true;
    [SerializeField] private bool triggerPunchOnStormEffectFinished = true;

    private bool punchPlayed;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (recipeMovementRecipe == null)
            recipeMovementRecipe = stormHeartRecipe;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[keyboardMovementKey].wasPressedThisFrame)
            PlayKeyboardMovement();
    }

    private void OnEnable()
    {
        RecipeChecker.OnRecipeSuccess += OnRecipeSuccess;
        StormBottleVolleyRecipeEffect.OnStormBottleVolleyFinished += OnStormBottleVolleyFinished;
    }

    private void OnDisable()
    {
        RecipeChecker.OnRecipeSuccess -= OnRecipeSuccess;
        StormBottleVolleyRecipeEffect.OnStormBottleVolleyFinished -= OnStormBottleVolleyFinished;
    }

    private void OnRecipeSuccess(RecipeData recipe)
    {
        if (triggerMovementOnRecipeSuccess && IsRecipeMovementRecipe(recipe))
        {
            PlayRecipeMovement();
            return;
        }

        if (triggerPunchOnStormEffectFinished)
            return;

        if (stormHeartRecipe != null && recipe == stormHeartRecipe)
            PlayTakingPunch();
    }

    private void OnStormBottleVolleyFinished()
    {
        if (!triggerPunchOnStormEffectFinished)
            return;

        PlayTakingPunch();
    }

    private void PlayTakingPunch()
    {
        if (punchPlayed)
            return;

        punchPlayed = true;
        SetTrigger(takingPunchTrigger);
    }

    public void PlaySalute()
    {
        SetTrigger(saluteTrigger);
    }

    public void PlayKeyboardMovement()
    {
        SetTrigger(keyboardMovementTrigger);
    }

    public void PlayRecipeMovement()
    {
        SetTrigger(recipeMovementTrigger);
    }

    private bool IsRecipeMovementRecipe(RecipeData recipe)
    {
        if (recipe == null)
            return false;

        if (recipeMovementRecipe != null)
            return recipe == recipeMovementRecipe;

        return stormHeartRecipe != null && recipe == stormHeartRecipe;
    }

    private void SetTrigger(string triggerName)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName))
            return;

        animator.ResetTrigger(triggerName);
        animator.SetTrigger(triggerName);
    }

}
