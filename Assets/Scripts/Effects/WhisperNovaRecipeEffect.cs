using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhisperNovaRecipeEffect : RecipeEffectBase
{
    [Header("Свет и цвет")]
    public Color reactionColor = new Color(0.62f, 0.95f, 1f, 1f);
    public float boostedLightIntensity = 4.9f;

    [Header("Туман комнаты")]
    public int fogCloudCount = 7;
    public float spawnHeight = 0.45f;
    public float launchDuration = 1.6f;
    public float fogDuration = 15f;
    public float initialRadius = 0.16f;
    public float roomRadius = 1.9f;
    public float fogHeight = 0.9f;
    public float cloudBaseScale = 0.55f;
    public float cloudEndScale = 1.45f;
    public float driftStrength = 0f;
    public float swirlSpeed = 0f;
    public float pulseSpeed = 0f;
    public float alphaStrength = 0.04f;

    protected override void PlayMatchedEffect()
    {
        PlayEffectSound();
        StartCoroutine(PlayEffectCoroutine());
    }

    private IEnumerator PlayEffectCoroutine()
    {
        float totalDuration = launchDuration + fogDuration;
        StartCoroutine(PulseLights(reactionColor, boostedLightIntensity * 0.25f, totalDuration));

        Vector3 origin = transform.position + transform.up * spawnHeight;
        List<GameObject> clouds = new List<GameObject>();
        List<Vector3> baseOffsets = new List<Vector3>();

        for (int i = 0; i < fogCloudCount; i++)
        {
            GameObject cloud = CreateFogCloud(origin, "WhisperFogCloud", cloudBaseScale, alphaStrength * 0.2f);

            float angle = (360f / Mathf.Max(1, fogCloudCount)) * i;
            float radius = Mathf.Lerp(roomRadius * 0.25f, roomRadius, Random.value);
            float radians = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * radius;
            offset.y = Random.Range(0.05f, fogHeight);

            clouds.Add(cloud);
            baseOffsets.Add(offset);
        }

        float elapsed = 0f;
        while (elapsed < launchDuration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, launchDuration));
            float eased = 1f - Mathf.Pow(1f - normalized, 3f);

            for (int i = 0; i < clouds.Count; i++)
            {
                GameObject cloud = clouds[i];

                if (cloud == null)
                    continue;

                Vector3 targetOffset = baseOffsets[i];
                targetOffset.x *= Mathf.Lerp(0.2f, 1f, eased);
                targetOffset.z *= Mathf.Lerp(0.2f, 1f, eased);
                targetOffset.y *= Mathf.Lerp(0.15f, 1f, eased);

                cloud.transform.position = origin + Vector3.Lerp(Vector3.zero, targetOffset, eased);
                cloud.transform.localScale = Vector3.one * Mathf.Lerp(cloudBaseScale, cloudEndScale, eased);
                UpdateFogMaterial(cloud, alphaStrength * Mathf.Lerp(0.2f, 1f, eased));
            }

            yield return null;
        }

        elapsed = 0f;
        while (elapsed < fogDuration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, fogDuration));
            float fade = 1f;

            if (normalized > 0.82f)
                fade = 1f - ((normalized - 0.82f) / 0.18f);

            for (int i = 0; i < clouds.Count; i++)
            {
                GameObject cloud = clouds[i];

                if (cloud == null)
                    continue;

                Vector3 baseOffset = baseOffsets[i];
                cloud.transform.position = origin + baseOffset;
                cloud.transform.localScale = Vector3.one * cloudEndScale;
                UpdateFogMaterial(cloud, alphaStrength * fade);
            }

            yield return null;
        }

        for (int i = 0; i < clouds.Count; i++)
        {
            if (clouds[i] != null)
                Destroy(clouds[i]);
        }

        FinishEffect();
    }

    private GameObject CreateFogCloud(Vector3 position, string objectName, float scale, float alpha)
    {
        GameObject cloud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cloud.name = objectName;
        cloud.transform.position = position;
        cloud.transform.localScale = Vector3.one * scale;

        Collider collider = cloud.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        Renderer renderer = cloud.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Material material = CreateTintedMaterial(reactionColor, 1.25f);
            SetFogMaterial(material, alpha);
            renderer.material = material;
        }

        return cloud;
    }

    private void SetFogMaterial(Material material, float alpha)
    {
        if (material == null)
            return;

        Color fogColor = reactionColor;
        fogColor.a = alpha;

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);

        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);

        if (material.HasProperty("_AlphaClip"))
            material.SetFloat("_AlphaClip", 0f);

        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", fogColor);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", fogColor);

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", reactionColor * 0.55f);
        }
    }

    private void UpdateFogMaterial(GameObject cloud, float alpha)
    {
        if (cloud == null)
            return;

        Renderer renderer = cloud.GetComponent<Renderer>();
        if (renderer == null || renderer.material == null)
            return;

        SetFogMaterial(renderer.material, alpha);
    }
}
