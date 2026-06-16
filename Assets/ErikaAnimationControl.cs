using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ErikaAnimationControl : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private RecipeData stormHeartRecipe;
    [SerializeField] private string saluteTrigger = "Salute";
    [SerializeField] private string takingPunchTrigger = "TakingPunch";
    [SerializeField] private bool triggerPunchOnStormEffectFinished = true;
    [SerializeField] private Collider saluteActivator;

    private bool punchPlayed;
    private XRSimpleInteractable saluteInteractable;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        EnsureSaluteInteraction();
    }

    private void OnEnable()
    {
        RecipeChecker.OnRecipeSuccess += OnRecipeSuccess;
        StormBottleVolleyRecipeEffect.OnStormBottleVolleyFinished += OnStormBottleVolleyFinished;

        EnsureSaluteInteraction();
        if (saluteInteractable != null)
            saluteInteractable.selectEntered.AddListener(OnSaluteSelected);
    }

    private void OnDisable()
    {
        RecipeChecker.OnRecipeSuccess -= OnRecipeSuccess;
        StormBottleVolleyRecipeEffect.OnStormBottleVolleyFinished -= OnStormBottleVolleyFinished;

        if (saluteInteractable != null)
            saluteInteractable.selectEntered.RemoveListener(OnSaluteSelected);
    }

    private void OnRecipeSuccess(RecipeData recipe)
    {
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

    private void SetTrigger(string triggerName)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName))
            return;

        animator.ResetTrigger(triggerName);
        animator.SetTrigger(triggerName);
    }

    private void EnsureSaluteInteraction()
    {
        if (saluteActivator == null)
            saluteActivator = GetComponent<Collider>();

        if (saluteActivator == null)
            return;

        saluteInteractable = saluteActivator.GetComponent<XRSimpleInteractable>();
        if (saluteInteractable == null)
            saluteInteractable = saluteActivator.gameObject.AddComponent<XRSimpleInteractable>();

        if (saluteInteractable.interactionManager == null)
            saluteInteractable.interactionManager = FindFirstObjectByType<XRInteractionManager>();

        saluteInteractable.selectMode = InteractableSelectMode.Single;
    }

    private void OnSaluteSelected(SelectEnterEventArgs args)
    {
        PlaySalute();
    }
}
