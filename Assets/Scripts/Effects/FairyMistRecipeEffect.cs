using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 0414

public class FairyMistRecipeEffect : MonoBehaviour
{
    [Header("Какой рецепт запускает эффект")]
    public RecipeData targetRecipe;

    [Header("Звук")]
    [SerializeField] private AudioClip fairySound;
    [SerializeField] private float soundVolume = 1f;
    [SerializeField] private float soundMinPitch = 0.97f;
    [SerializeField] private float soundMaxPitch = 1.03f;

    [Header("Простые феи")]
    [SerializeField] private int fairyCount = 12;
    [SerializeField] private float fairyDuration = 9.2f;
    [SerializeField] private float fairySpawnHeight = 0.45f;
    [SerializeField] private float fairyRoomRadius = 3.8f;
    [SerializeField] private float fairyRiseHeight = 1.9f;
    [SerializeField] private float fairyScale = 0.09f;
    [SerializeField] private float burstDuration = 0.28f;
    [SerializeField] private float burstDistance = 0.42f;
    [SerializeField] private float burstScaleMultiplier = 2.8f;
    [SerializeField] private Color[] fairyColors =
    {
        new Color(1f, 0.78f, 0.95f, 0.95f),
        new Color(0.73f, 1f, 0.95f, 0.95f),
        new Color(0.78f, 0.86f, 1f, 0.95f),
        new Color(1f, 0.92f, 0.7f, 0.95f),
    };

    private bool isPlayingEffect;

    private void OnEnable()
    {
        RecipeChecker.OnRecipeSuccess += OnRecipeSuccess;
    }

    private void OnDisable()
    {
        RecipeChecker.OnRecipeSuccess -= OnRecipeSuccess;
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

        PlayFairySound();
        yield return StartCoroutine(PlayFairyRoutine());

        isPlayingEffect = false;
    }

    private IEnumerator PlayFairyRoutine()
    {
        float duration = Mathf.Max(2f, fairyDuration);

        Vector3 origin = transform.position + transform.up * fairySpawnHeight;
        List<GameObject> fairies = new List<GameObject>();
        List<Vector3> targets = new List<Vector3>();
        List<Color> colors = new List<Color>();

        for (int i = 0; i < fairyCount; i++)
        {
            GameObject fairy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fairy.name = "SimpleFairy";
            fairy.transform.localScale = Vector3.one * fairyScale;

            Collider collider = fairy.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            Renderer renderer = fairy.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                Color fairyTint = GetFairyColor(i);
                colors.Add(fairyTint);
                renderer.material = CreateFairyMaterial(fairyTint);

                Light fairyLight = fairy.AddComponent<Light>();
                fairyLight.type = LightType.Point;
                fairyLight.range = 1.6f;
                fairyLight.intensity = 0.85f;
                fairyLight.color = fairyTint;
            }
            else
            {
                colors.Add(Color.white);
            }

            fairies.Add(fairy);

            Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(fairyRoomRadius * 0.45f, fairyRoomRadius);
            Vector3 target = origin + new Vector3(circle.x, Random.Range(0.7f, fairyRiseHeight), circle.y);
            targets.Add(target);
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < fairies.Count; i++)
            {
                GameObject fairy = fairies[i];
                if (fairy == null)
                    continue;

                Vector3 target = targets[i];
                Vector3 basePosition = Vector3.Lerp(origin, target, normalized);
                float driftX = Mathf.Sin(elapsed * 2.4f + i * 0.7f) * 0.18f;
                float driftY = Mathf.Sin(elapsed * 4.1f + i) * 0.08f;
                float driftZ = Mathf.Cos(elapsed * 2.8f + i * 0.5f) * 0.18f;

                fairy.transform.position = basePosition + new Vector3(driftX, driftY, driftZ);
                fairy.transform.localScale = Vector3.one * (fairyScale + Mathf.Sin(elapsed * 8f + i) * 0.018f);

                Light fairyLight = fairy.GetComponent<Light>();
                if (fairyLight != null)
                {
                    fairyLight.color = colors[i];
                    fairyLight.intensity = 0.75f + Mathf.Sin(elapsed * 7f + i) * 0.18f;
                }
            }

            yield return null;
        }

        yield return StartCoroutine(PlayBurstRoutine(fairies, colors));
    }

    private Material CreateFairyMaterial(Color color)
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 2.2f);
        }

        return material;
    }

    private Color GetFairyColor(int index)
    {
        if (fairyColors == null || fairyColors.Length == 0)
            return Color.white;

        return fairyColors[index % fairyColors.Length];
    }

    private void PlayFairySound()
    {
        if (fairySound == null)
            return;

        GameObject audioObject = new GameObject($"{name}_FairySound");
        audioObject.transform.position = transform.position;
        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.clip = fairySound;
        audioSource.volume = soundVolume * VRAudioSettings.SfxVolume;
        audioSource.pitch = Random.Range(soundMinPitch, soundMaxPitch);
        audioSource.spatialBlend = 1f;
        audioSource.maxDistance = 12f;
        audioSource.Play();

        Destroy(audioObject, fairySound.length / Mathf.Max(0.01f, Mathf.Abs(audioSource.pitch)) + 0.1f);
    }

    private IEnumerator PlayBurstRoutine(List<GameObject> fairies, List<Color> colors)
    {
        float elapsed = 0f;
        List<Vector3> startPositions = new List<Vector3>(fairies.Count);
        List<Vector3> burstDirections = new List<Vector3>(fairies.Count);

        for (int i = 0; i < fairies.Count; i++)
        {
            GameObject fairy = fairies[i];
            startPositions.Add(fairy != null ? fairy.transform.position : Vector3.zero);

            Vector3 direction = Random.onUnitSphere;
            if (direction.y < 0f)
                direction.y *= -0.35f;

            burstDirections.Add(direction.normalized);
        }

        while (elapsed < burstDuration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, burstDuration));
            float eased = 1f - Mathf.Pow(1f - normalized, 3f);

            for (int i = 0; i < fairies.Count; i++)
            {
                GameObject fairy = fairies[i];
                if (fairy == null)
                    continue;

                fairy.transform.position = startPositions[i] + burstDirections[i] * burstDistance * eased;
                fairy.transform.localScale = Vector3.one * Mathf.Lerp(fairyScale, fairyScale * burstScaleMultiplier, eased);

                Light fairyLight = fairy.GetComponent<Light>();
                if (fairyLight != null)
                {
                    fairyLight.color = colors[i];
                    fairyLight.intensity = Mathf.Lerp(0.9f, 2.3f, eased);
                    fairyLight.range = Mathf.Lerp(1.6f, 2.2f, eased);
                }
            }

            yield return null;
        }

        for (int i = 0; i < fairies.Count; i++)
        {
            if (fairies[i] != null)
                Destroy(fairies[i]);
        }
    }
}
