using System.Collections;
using UnityEngine;

#pragma warning disable 0414

public abstract class RecipeEffectBase : MonoBehaviour
{
    [Header("Какой рецепт запускает эффект")]
    public RecipeData targetRecipe;

    [Header("Защита от повторного запуска")]
    [SerializeField] private bool ignoreWhilePlaying = true;

    [Header("Звук эффекта")]
    [SerializeField] private AudioClip[] effectSounds;
    [SerializeField] private float effectSoundVolume = 1f;
    [SerializeField] private float effectSoundMinPitch = 0.97f;
    [SerializeField] private float effectSoundMaxPitch = 1.03f;

    private bool isPlaying;

    protected virtual void OnEnable()
    {
        RecipeChecker.OnRecipeSuccess += HandleRecipeSuccess;
    }

    protected virtual void OnDisable()
    {
        RecipeChecker.OnRecipeSuccess -= HandleRecipeSuccess;
    }

    protected virtual void HandleRecipeSuccess(RecipeData recipe)
    {
        if (recipe == null || targetRecipe == null || recipe != targetRecipe)
            return;

        if (ignoreWhilePlaying && isPlaying)
            return;

        isPlaying = true;
        PlayMatchedEffect();
    }

    protected abstract void PlayMatchedEffect();

    protected void FinishEffect()
    {
        isPlaying = false;
    }

    protected void PlayEffectSound()
    {
        // Звук подбора бутылок обрабатывается отдельно, здесь он не нужен.
    }

    protected Light[] GetCauldronLights()
    {
        return GetComponentsInChildren<Light>(true);
    }

    protected IEnumerator PulseLights(Color targetColor, float boostedIntensity, float duration)
    {
        Light[] lights = GetCauldronLights();

        if (lights == null || lights.Length == 0)
            yield break;

        Color[] startColors = new Color[lights.Length];
        float[] startIntensities = new float[lights.Length];

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null)
                continue;

            startColors[i] = lights[i].color;
            startIntensities[i] = lights[i].intensity;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float blend = Mathf.Sin(normalized * Mathf.PI);

            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] == null)
                    continue;

                lights[i].color = Color.Lerp(startColors[i], targetColor, blend);
                lights[i].intensity = Mathf.Lerp(startIntensities[i], boostedIntensity, blend);
            }

            yield return null;
        }

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null)
                continue;

            lights[i].color = startColors[i];
            lights[i].intensity = startIntensities[i];
        }
    }

    protected Material CreateTintedMaterial(Color color, float emissionMultiplier = 1.8f)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            shader = Shader.Find("Standard");

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        ApplyMaterialColor(material, color, emissionMultiplier);
        return material;
    }

    protected void ApplyMaterialColor(Material material, Color color, float emissionMultiplier = 1.8f)
    {
        if (material == null)
            return;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emissionMultiplier);
        }
    }
}
