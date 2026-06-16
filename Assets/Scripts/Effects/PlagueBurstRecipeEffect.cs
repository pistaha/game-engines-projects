using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlagueBurstRecipeEffect : RecipeEffectBase
{
    [Header("Свет и цвет")]
    public Color reactionColor = new Color(0.57f, 1f, 0.25f, 1f);
    public float boostedLightIntensity = 5f;

    [Header("Ядовитые сгустки")]
    public int projectileCount = 7;
    public float spawnHeight = 0.45f;
    public float projectileScale = 0.16f;
    public float upwardForce = 2.2f;
    public float outwardForce = 2.7f;
    public float projectileLifetime = 2.8f;

    protected override void PlayMatchedEffect()
    {
        PlayEffectSound();
        StartCoroutine(PlayEffectCoroutine());
    }

    private IEnumerator PlayEffectCoroutine()
    {
        StartCoroutine(PulseLights(reactionColor, boostedLightIntensity, projectileLifetime));

        Vector3 origin = transform.position + transform.up * spawnHeight;
        List<GameObject> projectiles = new List<GameObject>();

        for (int i = 0; i < projectileCount; i++)
        {
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "PlagueBlob";
            projectile.transform.position = origin;
            projectile.transform.localScale = Vector3.one * projectileScale;

            Collider collider = projectile.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            Renderer renderer = projectile.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.material = CreateTintedMaterial(reactionColor, 2.5f);
            }

            Rigidbody rb = projectile.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.mass = 0.15f;
            rb.linearDamping = 0.15f;
            rb.angularDamping = 0.05f;

            Vector3 spreadDirection = Quaternion.Euler(0f, (360f / projectileCount) * i + Random.Range(-18f, 18f), 0f) * transform.forward;
            Vector3 force = transform.up * upwardForce + spreadDirection.normalized * outwardForce;

            rb.AddForce(force, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 1.8f, ForceMode.Impulse);

            projectiles.Add(projectile);
        }

        yield return new WaitForSeconds(projectileLifetime);

        for (int i = 0; i < projectiles.Count; i++)
        {
            if (projectiles[i] != null)
                Destroy(projectiles[i]);
        }

        FinishEffect();
    }
}
