using System.Collections;
using Action = System.Action;
using UnityEngine;

public class StormBottleVolleyRecipeEffect : RecipeEffectBase
{
    public static event Action OnStormBottleVolleyFinished;

    [Header("Что появляется из котла")]
    public GameObject bottlePrefab;
    [SerializeField] private IngredientData spawnedBottleIngredient;
    [SerializeField] private CraftStorage storage;

    [Header("Свет")]
    public Color reactionColor = new Color(0.45f, 0.85f, 1f, 1f);
    public float boostedLightIntensity = 4.8f;

    [Header("Появление бутылки")]
    [SerializeField] private float resultLightPulseDuration = 0.85f;

    [Header("Долгое помешивание")]
    [SerializeField] private float requiredStirSeconds = 5f;
    [SerializeField] private ParticleSystem stirringBubbles;
    [SerializeField] private AudioSource stirringAudio;
    [SerializeField] private bool createFallbackBubbles = true;
    [SerializeField] private Vector3 fallbackBubbleOffset = new Vector3(0f, 0.55f, 0f);

    [Header("Позиция готовой бутылки")]
    [SerializeField] private Vector3 fixedResultPosition = new Vector3(-0.26f, 0.8072f, -5.43f);
    [SerializeField] private Vector3 fixedResultEulerAngles = new Vector3(0.196f, 0.027f, 0.562f);
    [SerializeField] private Vector3 fixedResultScale = Vector3.one;

    protected override void PlayMatchedEffect()
    {
        if (storage == null)
            storage = FindFirstObjectByType<CraftStorage>();

        PlayEffectSound();
        StartCoroutine(PlayEffectCoroutine());
    }

    private IEnumerator PlayEffectCoroutine()
    {
        if (bottlePrefab == null)
        {
            Debug.LogWarning("StormBottleVolleyRecipeEffect: не назначен bottlePrefab");
            FinishEffect();
            yield break;
        }

        if (requiredStirSeconds > 0f)
            yield return StartCoroutine(PlayLongStirringRoutine());

        StartCoroutine(PulseLights(reactionColor, boostedLightIntensity, resultLightPulseDuration));

        GameObject bottle = Instantiate(
            bottlePrefab,
            fixedResultPosition,
            Quaternion.Euler(fixedResultEulerAngles)
        );
        SetupCollectableBottle(bottle);

        Rigidbody rb = bottle.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = bottle.AddComponent<Rigidbody>();
        }

        PlaceBottleAtFixedPose(bottle, rb);

        OnStormBottleVolleyFinished?.Invoke();
        FinishEffect();
    }

    private IEnumerator PlayLongStirringRoutine()
    {
        ParticleSystem activeBubbles = stirringBubbles != null ? stirringBubbles : CreateFallbackBubbleParticles();

        if (activeBubbles != null)
        {
            activeBubbles.Clear();
            activeBubbles.Play();
        }

        float originalAudioVolume = 1f;
        if (stirringAudio != null)
        {
            originalAudioVolume = stirringAudio.volume;
            stirringAudio.volume = originalAudioVolume * VRAudioSettings.SfxVolume;
            stirringAudio.Play();
        }

        yield return new WaitForSeconds(requiredStirSeconds);

        if (activeBubbles != null)
            activeBubbles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (stirringAudio != null)
        {
            stirringAudio.Stop();
            stirringAudio.volume = originalAudioVolume;
        }

        if (stirringBubbles == null && activeBubbles != null)
            Destroy(activeBubbles.gameObject, 1.2f);
    }

    private ParticleSystem CreateFallbackBubbleParticles()
    {
        if (!createFallbackBubbles)
            return null;

        GameObject bubbleObject = new GameObject("StormHeartStirringBubbles");
        bubbleObject.transform.position = transform.position + transform.TransformDirection(fallbackBubbleOffset);
        ParticleSystem particles = bubbleObject.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = bubbleObject.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
            renderer.material = CreateTintedMaterial(new Color(0.45f, 0.9f, 1f, 0.85f), 1.4f);

        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.startLifetime = 0.75f;
        main.startSpeed = 0.55f;
        main.startSize = 0.08f;
        main.startColor = new Color(0.55f, 0.95f, 1f, 0.85f);
        main.maxParticles = 80;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 38f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.24f;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = new ParticleSystem.MinMaxCurve(0.2f, 0.65f);

        return particles;
    }

    private void PlaceBottleAtFixedPose(GameObject bottle, Rigidbody rb)
    {
        Transform bottleTransform = bottle.transform;
        Quaternion targetRotation = Quaternion.Euler(fixedResultEulerAngles);
        bottleTransform.SetPositionAndRotation(fixedResultPosition, targetRotation);
        bottleTransform.localScale = fixedResultScale;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.position = fixedResultPosition;
            rb.rotation = targetRotation;
        }

        CollectableItem collectableItem = bottle.GetComponent<CollectableItem>();
        if (collectableItem == null)
            return;

        Transform returnPoint = CreateRuntimeReturnPoint();
        collectableItem.SetCustomReturnPoint(returnPoint);
        collectableItem.MarkCustomReturnPointAsStart();
        collectableItem.MarkCurrentPoseAsStart();
    }

    private Transform CreateRuntimeReturnPoint()
    {
        GameObject returnPointObject = new GameObject("StormHeartBottleReturnPoint");
        returnPointObject.transform.SetPositionAndRotation(fixedResultPosition, Quaternion.Euler(fixedResultEulerAngles));
        returnPointObject.transform.localScale = fixedResultScale;
        return returnPointObject.transform;
    }

    private void SetupCollectableBottle(GameObject bottle)
    {
        if (bottle == null)
            return;

        if (storage == null || spawnedBottleIngredient == null)
            return;

        CollectableItem collectableItem = bottle.GetComponent<CollectableItem>();

        if (collectableItem == null)
            collectableItem = bottle.AddComponent<CollectableItem>();

        collectableItem.ingredient = spawnedBottleIngredient;
        collectableItem.storage = storage;

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
