using System.Collections;
using Action = System.Action;
using UnityEngine;

public class StormBottleVolleyRecipeEffect : RecipeEffectBase
{
    // Событие можно использовать другим скриптам, чтобы узнать: бутылка уже появилась.
    public static event Action OnStormBottleVolleyFinished;

    [Header("Что появляется из котла")]
    public GameObject bottlePrefab;
    [SerializeField] private IngredientData spawnedBottleIngredient = null;
    [SerializeField] private CraftStorage storage = null;

    [Header("Свет")]
    public Color reactionColor = new Color(0.45f, 0.85f, 1f, 1f);
    public float boostedLightIntensity = 4.8f;

    [Header("Появление бутылки")]
    public float spawnHeight = 0.55f;
    public float riseHeight = 1.35f;
    public float riseDuration = 0.9f;
    public float hoverDuration = 1.2f;
    public float hoverRadius = 0.22f;
    public float hoverSpinSpeed = 190f;
    public float hoverBobHeight = 0.12f;
    public float releaseUpwardForce = 2.6f;
    public float releaseForwardForce = 1.4f;
    public float torqueForce = 1.8f;
    [SerializeField] private Vector3 spawnedBottleReturnPosition = new Vector3(-0.26f, 0.80727f, -5.43f);
    [SerializeField] private Vector3 spawnedBottleReturnEuler = new Vector3(0.196f, 0.027f, 0.562f);

    protected override void PlayMatchedEffect()
    {
        // Этот метод вызывается базовым классом, когда RecipeChecker нашёл нужный рецепт.
        if (storage == null)
            storage = FindFirstObjectByType<CraftStorage>();

        PlayEffectSound();
        // Корутина нужна, потому что появление бутылки занимает несколько секунд.
        StartCoroutine(PlayEffectCoroutine());
    }

    private IEnumerator PlayEffectCoroutine()
    {
        // Без prefab бутылки эффект не сможет ничего создать.
        if (bottlePrefab == null)
        {
            Debug.LogWarning("StormBottleVolleyRecipeEffect: не назначен bottlePrefab");
            FinishEffect();
            yield break;
        }

        StartCoroutine(PulseLights(reactionColor, boostedLightIntensity, riseDuration + hoverDuration));

        // Создаём бутылку чуть выше котла.
        Vector3 origin = transform.position + transform.up * spawnHeight;
        GameObject bottle = Instantiate(
            bottlePrefab,
            origin,
            bottlePrefab.transform.rotation
        );
        SetupCollectableBottle(bottle);

        // На время красивой анимации выключаем обычную физику.
        // Пока бутылка летит вверх, её двигает не физика, а код ниже.
        Rigidbody rb = bottle.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = bottle.AddComponent<Rigidbody>();
        }

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true;

        // Коллайдеры временно выключаются, чтобы бутылка не зацепилась за котёл при появлении.
        Collider[] colliders = bottle.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }

        Transform bottleTransform = bottle.transform;
        Vector3 startPosition = bottleTransform.position;
        Vector3 peakPosition = startPosition + transform.up * riseHeight;
        float elapsed = 0f;

        // Первая часть анимации: бутылка поднимается вверх по спирали и вращается.
        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / riseDuration);
            float spiral = normalized * Mathf.PI * 2.2f;
            Vector3 swirl = new Vector3(Mathf.Cos(spiral), 0f, Mathf.Sin(spiral)) * hoverRadius * normalized;

            bottleTransform.position = Vector3.Lerp(startPosition, peakPosition, normalized) + swirl;
            bottleTransform.Rotate(Vector3.up, hoverSpinSpeed * Time.deltaTime, Space.Self);
            yield return null;
        }

        elapsed = 0f;
        // Вторая часть анимации: бутылка немного висит над котлом, кружится и покачивается.
        while (elapsed < hoverDuration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / hoverDuration);
            float orbit = elapsed * hoverSpinSpeed * Mathf.Deg2Rad;
            Vector3 swirl = new Vector3(Mathf.Cos(orbit), 0f, Mathf.Sin(orbit)) * hoverRadius;
            Vector3 bob = transform.up * (Mathf.Sin(normalized * Mathf.PI * 4f) * hoverBobHeight);

            bottleTransform.position = peakPosition + swirl + bob;
            bottleTransform.Rotate(Vector3.up, hoverSpinSpeed * 1.1f * Time.deltaTime, Space.Self);
            yield return null;
        }

        // После визуальной части возвращаем настоящую физику.
        // Дальше бутылка уже падает как обычный предмет.
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Включаем коллайдеры обратно, чтобы бутылку можно было брать и чтобы она сталкивалась с миром.
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = true;
        }

        // Даём бутылке толчок и вращение, чтобы она эффектно вылетела из котла.
        rb.AddForce(transform.up * releaseUpwardForce + transform.forward * releaseForwardForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * torqueForce, ForceMode.Impulse);

        OnStormBottleVolleyFinished?.Invoke();
        FinishEffect();
    }

    private void SetupCollectableBottle(GameObject bottle)
    {
        // Настраиваем созданную бутылку как обычный ингредиент, который игрок сможет подобрать.
        if (bottle == null)
            return;

        if (storage == null || spawnedBottleIngredient == null)
            return;

        CollectableItem collectableItem = bottle.GetComponent<CollectableItem>();

        if (collectableItem == null)
            collectableItem = bottle.AddComponent<CollectableItem>();

        // Назначаем, каким ингредиентом станет новая бутылка и куда она вернётся после использования.
        collectableItem.ingredient = spawnedBottleIngredient;
        collectableItem.storage = storage;
        collectableItem.SetCustomReturnPose(
            spawnedBottleReturnPosition,
            Quaternion.Euler(spawnedBottleReturnEuler));

        EnsureBottleHasCollider(bottle);
    }

    private void EnsureBottleHasCollider(GameObject bottle)
    {
        // Если у prefab уже есть коллайдер, ничего добавлять не нужно.
        Collider[] colliders = bottle.GetComponentsInChildren<Collider>(true);

        if (colliders.Length > 0)
            return;

        Renderer[] renderers = bottle.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
            return;

        // Если коллайдера нет, считаем размер по видимой модели и создаём BoxCollider.
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
