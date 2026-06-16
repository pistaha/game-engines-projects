using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 0414

public class PoseidonWaterfallRecipeEffect : MonoBehaviour
{
    [Header("Какой рецепт запускает эффект")]
    public RecipeData targetRecipe;

    [Header("Звук эффекта")]
    [SerializeField] private AudioClip[] effectSounds;
    [SerializeField] private float effectSoundVolume = 1f;
    [SerializeField] private float effectSoundMinPitch = 0.97f;
    [SerializeField] private float effectSoundMaxPitch = 1.03f;

    [Header("Эффекты котла")]
    public ParticleSystem boilingParticles;
    public AudioSource boilingAudio;
    public Light cauldronLight;

    [Header("Настройки бурления")]
    public float boilDuration = 5f;
    public float boostedLightIntensity = 4f;
    public float lightPulseSpeed = 7f;
    public float rangeBoostMultiplier = 1.15f;
    public float boostedParticleSpeed = 1.25f;

    [Header("Бутылка")]
    public GameObject bottlePrefab;
    public Transform bottleSpawnPoint;
    [SerializeField] private Transform bottleReturnPoint;
    [SerializeField] private IngredientData spawnedBottleIngredient;
    [SerializeField] private CraftStorage storage;

    [Header("Выброс бутылки")]
    public float riseHeight = 1.2f;
    public float riseDuration = 0.7f;
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float fallDistance = 1.6f;
    public float fallDuration = 1.8f;
    public AnimationCurve fallCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public Vector3 rotationPerSecond = new Vector3(0f, 60f, 0f);
    public float sideArc = 0.28f;
    public float apexPause = 0.2f;
    public float finalBounceHeight = 0.14f;
    public float finalBounceDuration = 0.2f;
    public float scalePunch = 0.08f;

    [Header("Свечение бутылки")]
    public float bottleLightIntensity = 2f;

    [Header("Удаление")]
    public bool destroyAfterEffect = false;
    public float destroyDelay = 1f;

    private readonly Color waterColor = new Color(0.33f, 0.83f, 1f, 0.82f);
    private float defaultLightIntensity;
    private float defaultLightRange;
    private float defaultParticleSpeed = 1f;
    private bool isPlayingEffect;

    private void Awake()
    {
        if (storage == null)
            storage = FindFirstObjectByType<CraftStorage>();

        if (cauldronLight != null)
        {
            defaultLightIntensity = cauldronLight.intensity;
            defaultLightRange = cauldronLight.range;
        }

        if (boilingParticles != null)
            defaultParticleSpeed = boilingParticles.main.simulationSpeed;
    }

    private void OnEnable()
    {
        RecipeChecker.OnRecipeSuccess += OnRecipeSuccess;
    }

    private void OnDisable()
    {
        RecipeChecker.OnRecipeSuccess -= OnRecipeSuccess;
        ResetCauldron();
    }

    private void OnRecipeSuccess(RecipeData recipe)
    {
        if (isPlayingEffect || recipe == null || recipe != targetRecipe)
            return;

        StartCoroutine(PlayEffectRoutine());
    }

    private IEnumerator PlayEffectRoutine()
    {
        isPlayingEffect = true;

        PlayEffectSound();
        StartWaterEffect();
        yield return StartCoroutine(PlayWaterRoutine());
        StopWaterEffect();

        isPlayingEffect = false;
    }

    private void StartWaterEffect()
    {
        if (boilingParticles != null)
        {
            ParticleSystem.MainModule main = boilingParticles.main;
            main.simulationSpeed = boostedParticleSpeed;
            main.startColor = new ParticleSystem.MinMaxGradient(waterColor);
            boilingParticles.Clear();
            boilingParticles.Play();
        }

    }

    private IEnumerator PlayWaterRoutine()
    {
        float duration = Mathf.Min(boilDuration, 3.2f);
        Vector3 origin = transform.position + transform.up * 0.35f;
        GameObject waterColumn = CreateWaterColumn(origin);
        List<GameObject> droplets = CreateWaterDroplets();
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float normalized = Mathf.Clamp01(timer / duration);
            float pulse = (Mathf.Sin(timer * lightPulseSpeed) + 1f) * 0.5f;

            if (cauldronLight != null)
            {
                cauldronLight.color = Color.Lerp(Color.white, waterColor, 0.8f);
                cauldronLight.intensity = Mathf.Lerp(defaultLightIntensity, boostedLightIntensity, pulse);
                cauldronLight.range = Mathf.Lerp(defaultLightRange, defaultLightRange * rangeBoostMultiplier, pulse);
            }

            if (waterColumn != null)
            {
                float height = Mathf.Lerp(0.9f, 1.45f, Mathf.Sin(normalized * Mathf.PI));
                float width = Mathf.Lerp(0.18f, 0.24f, pulse);
                waterColumn.transform.position = origin + transform.up * (height * 0.5f - 0.1f);
                waterColumn.transform.localScale = new Vector3(width, height, width);
            }

            for (int i = 0; i < droplets.Count; i++)
            {
                GameObject droplet = droplets[i];
                if (droplet == null)
                    continue;

                float orbitAngle = timer * 3.4f + i * 0.78f;
                float radius = 0.16f + Mathf.Sin(timer * 2.5f + i) * 0.03f;
                float height = 0.2f + Mathf.Abs(Mathf.Sin(timer * 4.2f + i * 0.55f)) * 1.05f;
                Vector3 offset = new Vector3(Mathf.Cos(orbitAngle), 0f, Mathf.Sin(orbitAngle)) * radius;

                droplet.transform.position = origin + offset + transform.up * height;
                droplet.transform.localScale = Vector3.one * (0.08f + Mathf.Sin(timer * 7f + i) * 0.01f);
            }

            yield return null;
        }

        if (waterColumn != null)
            Destroy(waterColumn);

        for (int i = 0; i < droplets.Count; i++)
        {
            if (droplets[i] != null)
                Destroy(droplets[i]);
        }
    }

    private void StopWaterEffect()
    {
        if (boilingParticles != null)
        {
            ParticleSystem.MainModule main = boilingParticles.main;
            main.simulationSpeed = defaultParticleSpeed;
            boilingParticles.Stop();
        }

        ResetCauldron();
    }

    private GameObject CreateWaterColumn(Vector3 origin)
    {
        GameObject column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        column.name = "PoseidonWaterColumn";
        column.transform.position = origin;

        Collider collider = column.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        Renderer renderer = column.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.material = CreateWaterMaterial();
        }

        return column;
    }

    private List<GameObject> CreateWaterDroplets()
    {
        List<GameObject> droplets = new List<GameObject>();

        for (int i = 0; i < 8; i++)
        {
            GameObject droplet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            droplet.name = "PoseidonDroplet";
            droplet.transform.localScale = Vector3.one * 0.08f;

            Collider collider = droplet.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            Renderer renderer = droplet.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.material = CreateWaterMaterial();
            }

            droplets.Add(droplet);
        }

        return droplets;
    }

    private Material CreateWaterMaterial()
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);

        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", waterColor);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", waterColor);

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", waterColor * 1.8f);
        }

        return material;
    }

    private IEnumerator SpawnBottleRoutine()
    {
        GameObject bottle = Instantiate(bottlePrefab, bottleSpawnPoint.position, bottleSpawnPoint.rotation);
        SetupCollectableBottle(bottle);

        Transform bottleTransform = bottle.transform;
        Vector3 startPosition = bottleTransform.position;
        Vector3 topPosition = startPosition + bottleSpawnPoint.up * riseHeight;
        Vector3 endPosition = topPosition + Vector3.down * fallDistance;
        Vector3 sideOffset = bottleSpawnPoint.right * sideArc;
        Vector3 startScale = bottleTransform.localScale;

        Rigidbody rb = bottle.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Light bottleLight = bottle.GetComponentInChildren<Light>();
        if (bottleLight != null)
        {
            bottleLight.enabled = true;
            bottleLight.intensity = bottleLightIntensity;
            bottleLight.color = waterColor;
        }

        yield return StartCoroutine(MoveBottle(bottleTransform, startPosition, topPosition, riseDuration, riseCurve, sideOffset, startScale));
        yield return new WaitForSeconds(apexPause);
        yield return StartCoroutine(MoveBottle(bottleTransform, topPosition, endPosition, fallDuration, fallCurve, sideOffset, startScale));

        if (finalBounceHeight > 0f && finalBounceDuration > 0f)
            yield return StartCoroutine(PlayFinalBounce(bottleTransform, endPosition));

        bottleTransform.position = endPosition;
        bottleTransform.localScale = startScale;

        if (bottleLight != null)
            bottleLight.enabled = false;

        if (rb != null)
            rb.isKinematic = false;

        if (destroyAfterEffect)
        {
            yield return new WaitForSeconds(destroyDelay);

            if (bottle != null)
                Destroy(bottle);
        }
    }

    private IEnumerator MoveBottle(
        Transform bottleTransform,
        Vector3 startPosition,
        Vector3 endPosition,
        float duration,
        AnimationCurve curve,
        Vector3 sideOffset,
        Vector3 startScale)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / duration);
            float curveValue = curve == null ? progress : curve.Evaluate(progress);
            float side = Mathf.Sin(progress * Mathf.PI) * sideArc;

            bottleTransform.position = Vector3.Lerp(startPosition, endPosition, curveValue) + sideOffset.normalized * side;
            bottleTransform.Rotate(rotationPerSecond * Time.deltaTime, Space.Self);
            bottleTransform.localScale = startScale * (1f + Mathf.Sin(progress * Mathf.PI) * scalePunch);

            yield return null;
        }
    }

    private IEnumerator PlayFinalBounce(Transform bottleTransform, Vector3 endPosition)
    {
        float timer = 0f;
        Vector3 top = endPosition + Vector3.up * finalBounceHeight;

        while (timer < finalBounceDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / finalBounceDuration);
            bottleTransform.position = Vector3.Lerp(endPosition, top, Mathf.Sin(progress * Mathf.PI));
            yield return null;
        }
    }

    private void ResetCauldron()
    {
        if (cauldronLight == null)
            return;

        cauldronLight.color = Color.white;
        cauldronLight.intensity = defaultLightIntensity;
        cauldronLight.range = defaultLightRange;
    }

    private void PlayEffectSound()
    {
        if (effectSounds == null || effectSounds.Length == 0)
            return;

        AudioClip clip = effectSounds[Random.Range(0, effectSounds.Length)];
        if (clip == null)
            return;

        GameObject audioObject = new GameObject($"{name}_PoseidonSound");
        audioObject.transform.position = transform.position;
        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = effectSoundVolume * VRAudioSettings.SfxVolume;
        audioSource.pitch = Random.Range(effectSoundMinPitch, effectSoundMaxPitch);
        audioSource.spatialBlend = 1f;
        audioSource.maxDistance = 12f;
        audioSource.Play();

        Destroy(audioObject, clip.length / Mathf.Max(0.01f, Mathf.Abs(audioSource.pitch)) + 0.1f);
    }

    private void SetupCollectableBottle(GameObject bottle)
    {
        if (bottle == null || storage == null || spawnedBottleIngredient == null)
            return;

        CollectableItem collectableItem = bottle.GetComponent<CollectableItem>();
        if (collectableItem == null)
            collectableItem = bottle.AddComponent<CollectableItem>();

        collectableItem.ingredient = spawnedBottleIngredient;
        collectableItem.storage = storage;
        collectableItem.SetCustomReturnPoint(bottleReturnPoint);

        EnsureBottleHasCollider(bottle);
    }

    private void EnsureBottleHasCollider(GameObject bottle)
    {
        Collider[] colliders = bottle.GetComponentsInChildren<Collider>(true);
        if (colliders.Length > 0)
            return;

        Renderer[] renderers = bottle.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        BoxCollider boxCollider = bottle.AddComponent<BoxCollider>();
        boxCollider.center = bottle.transform.InverseTransformPoint(bounds.center);

        Vector3 size = bounds.size;
        Vector3 scale = bottle.transform.lossyScale;

        if (Mathf.Abs(scale.x) > 0.0001f) size.x /= scale.x;
        if (Mathf.Abs(scale.y) > 0.0001f) size.y /= scale.y;
        if (Mathf.Abs(scale.z) > 0.0001f) size.z /= scale.z;

        boxCollider.size = size;
    }
}
