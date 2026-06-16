using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 0414

public class MainPotionEffect : MonoBehaviour
{
    [Header("Какой рецепт запускает эффект")]
    public RecipeData targetRecipe;

    [Header("Звук эффекта")]
    [SerializeField] private AudioClip[] effectSounds;
    [SerializeField] private float effectSoundVolume = 1f;
    [SerializeField] private float effectSoundMinPitch = 0.97f;
    [SerializeField] private float effectSoundMaxPitch = 1.03f;

    [Header("Базовые ссылки сцены")]
    public ParticleSystem successParticles;
    public Light cauldronLight;
    public AudioSource successAudio;
    public Renderer cauldronRenderer;

    [Header("Заряд котла")]
    public float chargeDuration = 1.35f;
    public float burnDuration = 6f;
    public float boostedIntensity = 5f;
    public float effectDuration = 2f;
    public int pulseCount = 3;
    public float rangeBoostMultiplier = 1.2f;
    public int burstParticles = 18;
    public float boostedSimulationSpeed = 1.35f;

    [Header("Огонь вокруг котла")]
    public int fireEmitterCount = 4;
    public float fireRadius = 1.2f;
    public float fireHeightOffset = 0.2f;
    public float fireLightIntensity = 6f;
    public Color fireColor = new Color(1f, 0.34f, 0.06f, 1f);
    public float cauldronEmissionBoost = 2.2f;

    private readonly List<GameObject> spawnedFireObjects = new List<GameObject>();

    private float defaultLightIntensity;
    private float defaultLightRange;
    private float defaultParticleSpeed = 1f;
    private Color defaultLightColor = Color.white;

    private Material[] originalSharedMaterials;
    private Material[] cauldronMaterials;
    private Color[] startColor;
    private Color[] startBaseColor;
    private Color[] startEmissionColor;
    private bool[] hasColor;
    private bool[] hasBaseColor;
    private bool[] hasEmission;

    private Coroutine activeRoutine;

    private void Awake()
    {
        AutoAssignSceneReferences();
        CacheDefaults();
    }

    private void OnEnable()
    {
        RecipeChecker.OnRecipeSuccess += PlayEffect;
    }

    private void OnDisable()
    {
        RecipeChecker.OnRecipeSuccess -= PlayEffect;
        StopEffect();
    }

    private void PlayEffect(RecipeData recipe)
    {
        if (recipe == null || targetRecipe == null || recipe != targetRecipe)
            return;

        StopEffect();
        activeRoutine = StartCoroutine(EffectRoutine());
    }

    private IEnumerator EffectRoutine()
    {
        PlaySuccessParticles();

        yield return StartCoroutine(ChargeCauldron());

        SpawnFireEffects();

        float timer = 0f;

        while (timer < burnDuration)
        {
            timer += Time.deltaTime;
            float pulse = 0.65f + Mathf.Sin(timer * 8f) * 0.35f;
            ApplyCauldronVisuals(pulse);
            yield return null;
        }

        yield return new WaitForSeconds(effectDuration);

        StopEffect();
    }

    private IEnumerator ChargeCauldron()
    {
        float timer = 0f;

        while (timer < chargeDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / chargeDuration);
            float pulse = (Mathf.Sin(progress * Mathf.PI * pulseCount) + 1f) * 0.5f;
            float blend = Mathf.Lerp(progress * 0.5f, 1f, pulse);

            ApplyCauldronVisuals(blend);
            yield return null;
        }
    }

    private void ApplyCauldronVisuals(float blend)
    {
        if (cauldronLight != null)
        {
            cauldronLight.color = Color.Lerp(defaultLightColor, fireColor, blend);
            cauldronLight.intensity = Mathf.Lerp(defaultLightIntensity, Mathf.Max(boostedIntensity, fireLightIntensity), blend);
            cauldronLight.range = Mathf.Lerp(defaultLightRange, defaultLightRange * rangeBoostMultiplier, blend);
        }

        if (cauldronMaterials == null)
            return;

        if (cauldronRenderer != null && originalSharedMaterials != null && originalSharedMaterials.Length > 0)
            cauldronRenderer.sharedMaterials = originalSharedMaterials;

        for (int i = 0; i < cauldronMaterials.Length; i++)
        {
            Material material = cauldronMaterials[i];

            if (material == null)
                continue;

            Color targetColor = Color.Lerp(new Color(0.3f, 0.08f, 0.02f), fireColor, blend);

            if (hasColor[i])
                material.SetColor("_Color", Color.Lerp(startColor[i], targetColor, blend));

            if (hasBaseColor[i])
                material.SetColor("_BaseColor", Color.Lerp(startBaseColor[i], targetColor, blend));

            if (hasEmission[i])
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.Lerp(startEmissionColor[i], targetColor * cauldronEmissionBoost, blend));
            }
        }
    }

    private void PlaySuccessParticles()
    {
        if (successParticles == null)
            return;

        ParticleSystem.MainModule main = successParticles.main;
        main.simulationSpeed = boostedSimulationSpeed;

        successParticles.Clear();
        successParticles.Play();
        successParticles.Emit(burstParticles);
    }

    private void StopSuccessParticles()
    {
        if (successParticles == null)
            return;

        ParticleSystem.MainModule main = successParticles.main;
        main.simulationSpeed = defaultParticleSpeed;

        successParticles.Stop();
        successParticles.Clear();
    }

    private void PlaySuccessAudio()
    {
        // Звук подбора бутылок обрабатывается отдельно, здесь он не нужен.
    }

    private void SpawnFireEffects()
    {
        ClearFireEffects();

        Vector3 center = GetEffectOrigin();

        for (int i = 0; i < fireEmitterCount; i++)
        {
            float angle = Mathf.PI * 2f * i / Mathf.Max(1, fireEmitterCount);
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * fireRadius;

            GameObject fireObject = new GameObject("MainPotionFire_" + i);
            fireObject.transform.position = center + offset + Vector3.up * fireHeightOffset;

            ParticleSystem fireParticles = fireObject.AddComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = fireObject.GetComponent<ParticleSystemRenderer>();

            if (renderer != null)
                renderer.material = GetParticleMaterial();

            ParticleSystem.MainModule main = fireParticles.main;
            main.loop = true;
            main.startLifetime = 0.8f;
            main.startSpeed = 0.7f;
            main.startSize = 0.8f;
            main.startColor = new Color(1f, 0.55f, 0.15f, 0.9f);
            main.maxParticles = 60;

            ParticleSystem.EmissionModule emission = fireParticles.emission;
            emission.rateOverTime = 24f;

            ParticleSystem.ShapeModule shape = fireParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 0.15f;
            shape.angle = 12f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = fireParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.95f, 0.75f), 0f),
                    new GradientColorKey(new Color(1f, 0.4f, 0.05f), 0.5f),
                    new GradientColorKey(new Color(0.25f, 0.05f, 0.02f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.9f, 0.15f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            fireParticles.Play();
            spawnedFireObjects.Add(fireObject);
        }
    }

    private void ClearFireEffects()
    {
        for (int i = 0; i < spawnedFireObjects.Count; i++)
        {
            if (spawnedFireObjects[i] != null)
                Destroy(spawnedFireObjects[i]);
        }

        spawnedFireObjects.Clear();
    }

    private void StopEffect()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        StopSuccessParticles();
        ClearFireEffects();
        RestoreCauldronState();
    }

    private void RestoreCauldronState()
    {
        if (cauldronLight != null)
        {
            cauldronLight.color = defaultLightColor;
            cauldronLight.intensity = defaultLightIntensity;
            cauldronLight.range = defaultLightRange;
        }

        if (cauldronRenderer != null && originalSharedMaterials != null && originalSharedMaterials.Length > 0)
        {
            cauldronRenderer.sharedMaterials = originalSharedMaterials;
            cauldronMaterials = cauldronRenderer.materials;
        }

        if (cauldronMaterials == null)
            return;

        for (int i = 0; i < cauldronMaterials.Length; i++)
        {
            Material material = cauldronMaterials[i];

            if (material == null)
                continue;

            if (hasColor[i])
                material.SetColor("_Color", startColor[i]);

            if (hasBaseColor[i])
                material.SetColor("_BaseColor", startBaseColor[i]);

            if (hasEmission[i])
                material.SetColor("_EmissionColor", startEmissionColor[i]);
        }
    }

    private void CacheDefaults()
    {
        if (cauldronLight != null)
        {
            defaultLightIntensity = cauldronLight.intensity;
            defaultLightRange = cauldronLight.range;
            defaultLightColor = cauldronLight.color;
        }

        if (successParticles != null)
            defaultParticleSpeed = successParticles.main.simulationSpeed;

        if (cauldronRenderer == null)
            return;

        originalSharedMaterials = cauldronRenderer.sharedMaterials;
        cauldronMaterials = cauldronRenderer.materials;

        if (cauldronMaterials == null || cauldronMaterials.Length == 0)
            return;

        startColor = new Color[cauldronMaterials.Length];
        startBaseColor = new Color[cauldronMaterials.Length];
        startEmissionColor = new Color[cauldronMaterials.Length];
        hasColor = new bool[cauldronMaterials.Length];
        hasBaseColor = new bool[cauldronMaterials.Length];
        hasEmission = new bool[cauldronMaterials.Length];

        for (int i = 0; i < cauldronMaterials.Length; i++)
        {
            Material material = cauldronMaterials[i];

            if (material == null)
                continue;

            hasColor[i] = material.HasProperty("_Color");
            hasBaseColor[i] = material.HasProperty("_BaseColor");
            hasEmission[i] = material.HasProperty("_EmissionColor");

            if (hasColor[i])
                startColor[i] = material.GetColor("_Color");

            if (hasBaseColor[i])
                startBaseColor[i] = material.GetColor("_BaseColor");

            if (hasEmission[i])
                startEmissionColor[i] = material.GetColor("_EmissionColor");
        }
    }

    private void PlayEffectSound()
    {
        // Звук подбора бутылок обрабатывается отдельно, здесь он не нужен.
    }

    private Vector3 GetEffectOrigin()
    {
        if (cauldronLight != null)
            return cauldronLight.transform.position;

        return transform.position + Vector3.up * 0.7f;
    }

    private Material GetParticleMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (shader == null)
            return null;

        Material material = new Material(shader);
        material.color = Color.white;
        return material;
    }

    private void AutoAssignSceneReferences()
    {
        if (cauldronRenderer == null)
            cauldronRenderer = GetComponent<Renderer>();

        if (cauldronRenderer == null)
            cauldronRenderer = GetComponentInChildren<Renderer>();

        if (cauldronLight == null)
            cauldronLight = GetComponentInChildren<Light>();
    }
}
