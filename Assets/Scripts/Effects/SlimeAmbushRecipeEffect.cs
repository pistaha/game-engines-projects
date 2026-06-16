using System.Collections;
using UnityEngine;

public class SlimeAmbushRecipeEffect : RecipeEffectBase
{
    [Header("Что появляется")]
    public GameObject slimePrefab;

    [Header("Свет")]
    public Color reactionColor = new Color(0.62f, 0.12f, 0.1f, 1f);
    public float boostedLightIntensity = 5.2f;

    [Header("Появление")]
    public float spawnHeight = 0.55f;
    public float leapDuration = 0.35f;
    public float leapArcHeight = 1.7f;
    public float faceDistance = 1.85f;
    public float faceVerticalOffset = -0.45f;
    public float faceHoldDuration = 0.8f;
    public float faceJitter = 0.055f;
    public float faceScaleMultiplier = 0.9f;
    public Vector3 faceRotationOffset = Vector3.zero;

    protected override void PlayMatchedEffect()
    {
        PlayEffectSound();
        StartCoroutine(PlayEffectCoroutine());
    }

    private IEnumerator PlayEffectCoroutine()
    {
        StartCoroutine(PulseLights(reactionColor, boostedLightIntensity, leapDuration + faceHoldDuration));

        Vector3 spawnPosition = transform.position + transform.up * spawnHeight;
        GameObject slime = CreateSlime(spawnPosition);
        PrepareSlimeForScare(slime);

        yield return StartCoroutine(JumpAtPlayerFace(slime));

        if (slime != null)
            Destroy(slime);

        FinishEffect();
    }

    private GameObject CreateSlime(Vector3 spawnPosition)
    {
        if (slimePrefab != null)
        {
            try
            {
                GameObject prefabSlime = Instantiate(slimePrefab, spawnPosition, Random.rotation);
                TintSlime(prefabSlime);
                return prefabSlime;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("SlimeAmbushRecipeEffect: slimePrefab поврежден, создан запасной слизень. " + exception.Message);
            }
        }

        GameObject slime = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        slime.name = "SlimeFallback";
        slime.transform.position = spawnPosition;
        slime.transform.rotation = Random.rotation;
        slime.transform.localScale = new Vector3(0.75f, 0.4f, 0.75f);

        Renderer renderer = slime.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material = CreateTintedMaterial(reactionColor, 1.6f);

        return slime;
    }

    private void PrepareSlimeForScare(GameObject slime)
    {
        if (slime == null)
            return;

        Rigidbody rb = slime.GetComponent<Rigidbody>();
        if (rb == null)
            rb = slime.AddComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true;

        Collider[] colliders = slime.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }
    }

    private IEnumerator JumpAtPlayerFace(GameObject slime)
    {
        if (slime == null)
            yield break;

        Transform target = GetScareTarget();
        Vector3 startPosition = slime.transform.position;
        Vector3 startScale = slime.transform.localScale;
        Vector3 scareScale = startScale * faceScaleMultiplier;
        float elapsed = 0f;

        // Это только визуальный скример: слизень кинематический,
        // летит по дуге и трясётся возле камеры без столкновения с игроком.
        while (elapsed < leapDuration && slime != null)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, leapDuration));
            float fastProgress = 1f - Mathf.Pow(1f - normalized, 3f);

            Vector3 endPosition = GetFacePosition(target);
            Vector3 controlPoint = Vector3.Lerp(startPosition, endPosition, 0.35f) + Vector3.up * leapArcHeight;
            slime.transform.position = QuadraticBezier(startPosition, controlPoint, endPosition, fastProgress);
            slime.transform.localScale = Vector3.Lerp(startScale, scareScale, fastProgress);
            LookAtScareTarget(slime.transform, target);

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < faceHoldDuration && slime != null)
        {
            elapsed += Time.deltaTime;
            float shake = Mathf.Sin(elapsed * 55f);
            Vector3 jitter = (target.right * Mathf.Sin(elapsed * 73f) + target.up * shake) * faceJitter;

            slime.transform.position = GetFacePosition(target) + jitter;
            slime.transform.localScale = scareScale * (1f + Mathf.Abs(shake) * 0.12f);
            LookAtScareTarget(slime.transform, target);

            yield return null;
        }
    }

    private Transform GetScareTarget()
    {
        if (Camera.main != null)
            return Camera.main.transform;

        Camera camera = FindFirstObjectByType<Camera>();
        if (camera != null)
            return camera.transform;

        return transform;
    }

    private Vector3 GetFacePosition(Transform target)
    {
        if (target == null || target == transform)
            return transform.position + transform.forward * 2f + Vector3.up * 1.4f;

        return target.position + target.forward * faceDistance + target.up * faceVerticalOffset;
    }

    private void LookAtScareTarget(Transform slimeTransform, Transform target)
    {
        if (slimeTransform == null || target == null)
            return;

        Vector3 direction = target.position - slimeTransform.position;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        slimeTransform.rotation = Quaternion.LookRotation(direction.normalized) * Quaternion.Euler(faceRotationOffset);
    }

    private Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float inverse = 1f - t;
        return inverse * inverse * a + 2f * inverse * t * b + t * t * c;
    }

    private void TintSlime(GameObject slime)
    {
        if (slime == null)
            return;

        Renderer[] renderers = slime.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
                continue;

            Material[] materials = renderer.materials;

            for (int j = 0; j < materials.Length; j++)
                ApplyMaterialColor(materials[j], reactionColor, 1.6f);
        }
    }
}
