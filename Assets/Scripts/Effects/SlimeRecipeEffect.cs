using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

#pragma warning disable 0414

public class SlimeRecipeEffect : MonoBehaviour
{
    [Header("Какой рецепт должен создать слизня")]
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

    [Header("Настройки реакции")]
    public float boilDuration = 5f;
    public float boostedLightIntensity = 4f;
    public float lightPulseSpeed = 6.5f;
    public float rangeBoostMultiplier = 1.12f;
    public float boostedParticleSpeed = 1.25f;

    [Header("Спавн слизня")]
    public GameObject slimePrefab;
    public Transform slimeSpawnPoint;
    public Transform crawlTargetPoint;
    public float visibleSpawnLift = 0.12f;
    public bool spawnInFrontOfPlayerCamera = true;
    public float cameraSpawnDistance = 1.6f;
    public float cameraSpawnVerticalOffset = -0.45f;
    public float minimumVisibleSize = 0.75f;
    public float playerSpawnDistance = 1.4f;
    public Vector3 cameraLocalSpawnPosition = new Vector3(0f, -0.45f, 1.35f);

    [Header("Выброс слизня")]
    public float jumpUpForce = 2.5f;
    public float jumpForwardForce = 1.5f;
    public float waitAfterSpawnBeforeCrawl = 0f;
    public bool useSpawnImpulse = false;

    [Header("Движение слизня")]
    public float crawlSpeed = 1.2f;
    public float rotationSpeed = 8f;
    public float groundRayDistance = 5f;
    public LayerMask groundMask = ~0;
    public float crawlBobHeight = 0f;
    public float crawlBobSpeed = 8f;

    [Header("Исчезновение")]
    public float visibleHoldDuration = 8f;
    public float sinkDistance = 2f;
    public float sinkSpeed = 1.5f;
    public float destroyDelayAfterSink = 0.1f;

    [Header("Animator эффекта")]
    public RuntimeAnimatorController slimeVisibilityController;
    public string activeParameterName = "isActive";
    public float showAnimationDuration = 0.15f;
    public float hideAnimationDuration = 0.15f;

    private float defaultLightIntensity;
    private float defaultLightRange;
    private float defaultParticleSpeed = 1f;
    private bool isPlayingEffect;
    private const string SlimePrefabResourcePath = "Effects/SLIME-GREEN";
    private const string SlimePrefabAssetPath = "Assets/Prefabs/CraftedResults/SLIME-GREEN.prefab";

    private void Awake()
    {
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
        StartBoiling();
        yield return StartCoroutine(BoilRoutine());
        StopBoiling();

        GameObject resolvedSlimePrefab = ResolveSlimePrefab();

        if (resolvedSlimePrefab != null)
        {
            slimePrefab = resolvedSlimePrefab;
            yield return StartCoroutine(SpawnAndMoveSlime());
        }
        else
        {
            Debug.LogWarning($"SlimeRecipeEffect: не назначен slimePrefab и не удалось загрузить {SlimePrefabResourcePath}. Создан временный fallback-слизень.");
            yield return StartCoroutine(SpawnAndMoveFallbackSlime());
        }

        isPlayingEffect = false;
    }

    private GameObject ResolveSlimePrefab()
    {
        if (slimePrefab != null)
            return slimePrefab;

        GameObject resourcePrefab = Resources.Load<GameObject>(SlimePrefabResourcePath);
        if (resourcePrefab != null)
            return resourcePrefab;

#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<GameObject>(SlimePrefabAssetPath);
#else
        return null;
#endif
    }

    private void StartBoiling()
    {
        if (boilingParticles != null)
        {
            ParticleSystem.MainModule main = boilingParticles.main;
            main.simulationSpeed = boostedParticleSpeed;
            boilingParticles.Clear();
            boilingParticles.Play();
        }

    }

    private IEnumerator BoilRoutine()
    {
        float timer = 0f;

        while (timer < boilDuration)
        {
            timer += Time.deltaTime;

            if (cauldronLight != null)
            {
                float pulse = (Mathf.Sin(timer * lightPulseSpeed) + 1f) * 0.5f;
                cauldronLight.intensity = Mathf.Lerp(defaultLightIntensity, boostedLightIntensity, pulse);
                cauldronLight.range = Mathf.Lerp(defaultLightRange, defaultLightRange * rangeBoostMultiplier, pulse);
            }

            yield return null;
        }
    }

    private void StopBoiling()
    {
        if (boilingParticles != null)
        {
            ParticleSystem.MainModule main = boilingParticles.main;
            main.simulationSpeed = defaultParticleSpeed;
            boilingParticles.Stop();
        }

        ResetCauldron();
    }

    private IEnumerator SpawnAndMoveSlime()
    {
        GameObject slime = Instantiate(slimePrefab);
        AttachSlimeToPlayerCamera(slime);

        Rigidbody rb = slime.GetComponent<Rigidbody>();

        if (rb == null)
            rb = slime.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = true;

        yield return new WaitForSeconds(waitAfterSpawnBeforeCrawl);

        if (slime == null)
            yield break;

        Animator animator = FindSlimeAnimator(slime);
        SetSlimeActive(animator, true);
        EnsureSlimeVisible(slime);

        if (showAnimationDuration > 0f)
            yield return new WaitForSeconds(showAnimationDuration);

        EnsureSlimeVisible(slime);

        if (visibleHoldDuration > 0f)
            yield return new WaitForSeconds(visibleHoldDuration);

        SetSlimeActive(animator, false);

        if (hideAnimationDuration > 0f)
            yield return new WaitForSeconds(hideAnimationDuration);

        if (slime != null)
            Destroy(slime);
    }

    private void AttachSlimeToPlayerCamera(GameObject slime)
    {
        Camera playerCamera = GetPlayerCamera();
        if (slime == null)
            return;

        if (playerCamera == null)
        {
            GetVisibleSpawnPose(out Vector3 spawnPosition, out Quaternion spawnRotation);
            slime.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            return;
        }

        Transform cameraTransform = playerCamera.transform;
        slime.transform.SetParent(cameraTransform, false);
        slime.transform.localPosition = cameraLocalSpawnPosition;
        slime.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        slime.transform.localScale = Vector3.one;
    }

    private void GetVisibleSpawnPose(out Vector3 spawnPosition, out Quaternion spawnRotation)
    {
        FirstPersonCC player = FindFirstObjectByType<FirstPersonCC>();
        if (player != null)
        {
            Transform playerTransform = player.transform;
            Vector3 forward = Vector3.ProjectOnPlane(playerTransform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;

            spawnPosition = playerTransform.position + forward * playerSpawnDistance + Vector3.up * visibleSpawnLift;
            spawnRotation = Quaternion.LookRotation(-forward, Vector3.up);
            return;
        }

        Camera playerCamera = GetPlayerCamera();
        if (spawnInFrontOfPlayerCamera && playerCamera != null)
        {
            Transform cameraTransform = playerCamera.transform;
            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f)
                forward = cameraTransform.forward.normalized;

            spawnPosition = cameraTransform.position + forward * cameraSpawnDistance + Vector3.up * cameraSpawnVerticalOffset;
            spawnRotation = Quaternion.LookRotation(-forward, Vector3.up);
            return;
        }

        Transform spawnTransform = slimeSpawnPoint != null ? slimeSpawnPoint : transform;
        spawnPosition = spawnTransform.position + Vector3.up * visibleSpawnLift;
        spawnRotation = spawnTransform.rotation;
    }

    private Camera GetPlayerCamera()
    {
        if (Camera.main != null)
            return Camera.main;

        foreach (Camera camera in Camera.allCameras)
        {
            if (camera != null && camera.isActiveAndEnabled)
                return camera;
        }

        return null;
    }

    private IEnumerator SpawnAndMoveFallbackSlime()
    {
        Transform spawnTransform = slimeSpawnPoint != null ? slimeSpawnPoint : transform;
        GameObject slime = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        slime.name = "SLIME-GREEN-Fallback";
        slime.transform.SetPositionAndRotation(spawnTransform.position, spawnTransform.rotation);
        slime.transform.localScale = new Vector3(0.8f, 0.35f, 0.8f);

        Renderer renderer = slime.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = new Color(0.2f, 0.9f, 0.25f, 1f);

        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(SinkSlime(slime));
    }

    private void SetSlimeActive(Animator animator, bool isActive)
    {
        if (animator == null || string.IsNullOrEmpty(activeParameterName))
            return;

        animator.SetBool(activeParameterName, isActive);
        animator.Update(0f);
    }

    private void EnsureSlimeVisible(GameObject slime)
    {
        if (slime == null)
            return;

        Transform armature = slime.transform.Find("Armature");
        if (armature != null)
        {
            armature.localScale = Vector3.one;
            armature.localPosition = Vector3.zero;
        }

        Renderer[] renderers = slime.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
            renderer.enabled = true;

        ScalePrefabToVisibleSize(slime, renderers);
    }

    private float GetRendererBoundsSize(Renderer[] renderers)
    {
        Bounds bounds = new Bounds();
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds ? bounds.size.magnitude : 0f;
    }

    private void ScalePrefabToVisibleSize(GameObject slime, Renderer[] renderers)
    {
        float currentSize = GetRendererBoundsSize(renderers);
        if (currentSize <= 0f || currentSize >= minimumVisibleSize)
            return;

        float scaleMultiplier = minimumVisibleSize / currentSize;
        slime.transform.localScale *= scaleMultiplier;
    }

    private Animator FindSlimeAnimator(GameObject slime)
    {
        if (slime == null)
            return null;

        Animator[] animators = slime.GetComponents<Animator>();

        foreach (Animator animator in animators)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == activeParameterName)
                    return animator;
            }
        }

        Animator fallbackAnimator = animators.Length > 0 ? animators[0] : slime.AddComponent<Animator>();

        if (slimeVisibilityController != null)
            fallbackAnimator.runtimeAnimatorController = slimeVisibilityController;

        return fallbackAnimator;
    }

    private IEnumerator SinkSlime(GameObject slime)
    {
        if (slime == null)
            yield break;

        Vector3 startPosition = slime.transform.position;
        Vector3 endPosition = startPosition + Vector3.down * sinkDistance;

        while (slime != null && Vector3.Distance(slime.transform.position, endPosition) > 0.02f)
        {
            slime.transform.position = Vector3.MoveTowards(slime.transform.position, endPosition, sinkSpeed * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(destroyDelayAfterSink);

        if (slime != null)
            Destroy(slime);
    }

    private bool TryFindGroundY(Vector3 worldPosition, out float groundY)
    {
        Vector3 start = worldPosition + Vector3.up * 2f;

        if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, groundRayDistance, groundMask))
        {
            groundY = hit.point.y;
            return true;
        }

        groundY = worldPosition.y;
        return false;
    }

    private void ResetCauldron()
    {
        if (cauldronLight == null)
            return;

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

        GameObject audioObject = new GameObject($"{name}_SlimeSound");
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
}
