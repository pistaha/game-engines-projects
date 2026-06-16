using System.Collections;
using UnityEngine;

#pragma warning disable 0414

public class CauldronFailEffect : MonoBehaviour
{
    [Header("Что красим")]
    public Renderer cauldronRenderer;
    public Light cauldronLight;

    [Header("Звук эффекта")]
    [SerializeField] private AudioClip[] effectSounds;
    [SerializeField] private float effectSoundVolume = 1f;
    [SerializeField] private float effectSoundMinPitch = 0.97f;
    [SerializeField] private float effectSoundMaxPitch = 1.03f;

    [Header("Цвет ошибки")]
    public Color failColor = Color.red;

    [Header("Сколько секунд горит")]
    public float failDuration = 1f;
    public int flashCount = 3;
    public float emissionBoost = 1.8f;
    public float lightBoost = 2.5f;

    private Material[] runtimeMaterials;
    private Color[] originalColor;
    private Color[] originalBaseColor;
    private Color[] originalEmissionColor;
    private bool[] hasColor;
    private bool[] hasBaseColor;
    private bool[] hasEmissionColor;
    private bool[] originalEmissionEnabled;
    private bool hasOriginalMaterials;
    private Color originalLightColor;
    private float originalLightIntensity;
    private float originalLightRange;
    private bool hasOriginalLight;
    private Coroutine activeFlash;

    private void Awake()
    {
        ResolveVisuals();

        if (cauldronRenderer != null)
        {
            EnsureRuntimeMaterials();
            CaptureOriginalMaterials();
        }

        CaptureOriginalLight();
    }

    private void Reset()
    {
        ResolveVisuals();
    }

    private void OnValidate()
    {
        ResolveVisuals();
    }

    private void OnEnable()
    {
        RecipeChecker.OnRecipeFail += PlayFailEffect;
    }

    private void OnDisable()
    {
        RecipeChecker.OnRecipeFail -= PlayFailEffect;
        StopActiveFlash();
        RestoreOriginalVisuals();
    }

    private void PlayFailEffect()
    {
        StartFlashEffect(failColor, failDuration, flashCount);
    }

    private void StartFlashEffect(Color targetColor, float duration, int flashes)
    {
        ResolveVisuals();

        if (cauldronRenderer == null && cauldronLight == null)
            return;

        if (cauldronRenderer != null)
            EnsureRuntimeMaterials();

        if (cauldronRenderer != null && !HasOriginalDataForRuntime())
            CaptureOriginalMaterials();

        if (cauldronLight != null && !hasOriginalLight)
            CaptureOriginalLight();

        StopActiveFlash();
        RestoreOriginalVisuals();

        PlayEffectSound();
        activeFlash = StartCoroutine(FlashRoutine(targetColor, duration, flashes));
    }

    private void StopActiveFlash()
    {
        if (activeFlash == null)
            return;

        StopCoroutine(activeFlash);
        activeFlash = null;
    }

    private void EnsureRuntimeMaterials()
    {
        if (runtimeMaterials != null && runtimeMaterials.Length > 0)
            return;

        runtimeMaterials = cauldronRenderer.materials;
    }

    private void CaptureOriginalMaterials()
    {
        if (runtimeMaterials == null || runtimeMaterials.Length == 0)
            return;

        int length = runtimeMaterials.Length;
        originalColor = new Color[length];
        originalBaseColor = new Color[length];
        originalEmissionColor = new Color[length];
        hasColor = new bool[length];
        hasBaseColor = new bool[length];
        hasEmissionColor = new bool[length];
        originalEmissionEnabled = new bool[length];

        for (int i = 0; i < length; i++)
        {
            Material material = runtimeMaterials[i];
            if (material == null)
                continue;

            hasColor[i] = material.HasProperty("_Color");
            hasBaseColor[i] = material.HasProperty("_BaseColor");
            hasEmissionColor[i] = material.HasProperty("_EmissionColor");
            originalEmissionEnabled[i] = material.IsKeywordEnabled("_EMISSION");

            if (hasColor[i])
                originalColor[i] = material.GetColor("_Color");

            if (hasBaseColor[i])
                originalBaseColor[i] = material.GetColor("_BaseColor");

            if (hasEmissionColor[i])
                originalEmissionColor[i] = material.GetColor("_EmissionColor");
        }

        hasOriginalMaterials = true;
    }

    private bool HasOriginalDataForRuntime()
    {
        return hasOriginalMaterials
            && runtimeMaterials != null
            && originalColor != null
            && originalColor.Length == runtimeMaterials.Length;
    }

    private IEnumerator FlashRoutine(Color targetColor, float duration, int flashes)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float flash = (Mathf.Sin(normalized * Mathf.PI * flashes * 2f) + 1f) * 0.5f;
            float blend = Mathf.Lerp(flash, 1f, normalized * 0.35f);

            if (runtimeMaterials != null)
            {
                for (int i = 0; i < runtimeMaterials.Length; i++)
                    ApplyFailColor(runtimeMaterials[i], i, blend, targetColor);
            }

            ApplyFailLight(blend, targetColor);

            yield return null;
        }

        RestoreOriginalVisuals();
        activeFlash = null;
    }

    private void ApplyFailColor(Material material, int index, float blend, Color targetColor)
    {
        if (material == null)
            return;

        if (hasColor[index])
            material.SetColor("_Color", Color.Lerp(originalColor[index], targetColor, blend));

        if (hasBaseColor[index])
            material.SetColor("_BaseColor", Color.Lerp(originalBaseColor[index], targetColor, blend));

        if (hasEmissionColor[index])
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.Lerp(originalEmissionColor[index], targetColor * emissionBoost, blend));
        }
    }

    private void RestoreOriginalMaterials()
    {
        if (!hasOriginalMaterials || runtimeMaterials == null || runtimeMaterials.Length == 0)
            return;

        for (int i = 0; i < runtimeMaterials.Length; i++)
        {
            Material material = runtimeMaterials[i];

            if (material == null)
                continue;

            if (hasColor[i])
                material.SetColor("_Color", originalColor[i]);

            if (hasBaseColor[i])
                material.SetColor("_BaseColor", originalBaseColor[i]);

            if (hasEmissionColor[i])
            {
                material.SetColor("_EmissionColor", originalEmissionColor[i]);

                if (originalEmissionEnabled[i])
                    material.EnableKeyword("_EMISSION");
                else
                    material.DisableKeyword("_EMISSION");
            }
        }
    }

    private void CaptureOriginalLight()
    {
        if (cauldronLight == null)
            return;

        originalLightColor = cauldronLight.color;
        originalLightIntensity = cauldronLight.intensity;
        originalLightRange = cauldronLight.range;
        hasOriginalLight = true;
    }

    private void ApplyFailLight(float blend, Color targetColor)
    {
        if (cauldronLight == null || !hasOriginalLight)
            return;

        cauldronLight.color = Color.Lerp(originalLightColor, targetColor, blend);
        cauldronLight.intensity = Mathf.Lerp(originalLightIntensity, Mathf.Max(originalLightIntensity * lightBoost, lightBoost), blend);
        cauldronLight.range = Mathf.Lerp(originalLightRange, Mathf.Max(originalLightRange, 3f), blend);
    }

    private void RestoreOriginalVisuals()
    {
        RestoreOriginalMaterials();

        if (cauldronLight == null || !hasOriginalLight)
            return;

        cauldronLight.color = originalLightColor;
        cauldronLight.intensity = originalLightIntensity;
        cauldronLight.range = originalLightRange;
    }

    private void PlayEffectSound()
    {
        // Звук подбора бутылок обрабатывается отдельно, здесь он не нужен.
    }

    private void ResolveVisuals()
    {
        if (cauldronRenderer == null)
            cauldronRenderer = GetComponentInChildren<Renderer>();

        if (cauldronLight == null)
            cauldronLight = GetComponentInChildren<Light>();
    }
}
