using UnityEngine;

/// <summary>
/// Spawns simple colored physics objects in front of the XR camera for grab testing.
/// Can be called from a world-space UI Button.
/// </summary>
public class VRTestSpawner : MonoBehaviour
{
    [SerializeField] private Transform spawnOrigin;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private float spawnDistance = 1.6f;
    [SerializeField] private float spawnHeightOffset = -0.25f;
    [SerializeField] private float spacing = 0.45f;
    [SerializeField] private Vector3 defaultScale = new Vector3(0.25f, 0.25f, 0.25f);

    private int spawnedCount;

    private static readonly Color[] Colors =
    {
        new Color(0.95f, 0.22f, 0.22f),
        new Color(0.20f, 0.55f, 1.00f),
        new Color(0.20f, 0.85f, 0.38f),
        new Color(1.00f, 0.72f, 0.20f),
        new Color(0.85f, 0.35f, 1.00f),
    };

    private void Start()
    {
        if (spawnOnStart)
            SpawnDefaultSet();
    }

    [ContextMenu("Spawn Default Set")]
    public void SpawnDefaultSet()
    {
        SpawnPrimitive(PrimitiveType.Cube, "VR Test Cube", -spacing, 0);
        SpawnPrimitive(PrimitiveType.Sphere, "VR Test Sphere", 0f, 1);
        SpawnPrimitive(PrimitiveType.Capsule, "VR Test Capsule", spacing, 2);
    }

    public void SpawnSingleTestObject()
    {
        SpawnRandomObject();
    }

    public void SpawnRandomObject()
    {
        PrimitiveType type = spawnedCount % 3 == 0
            ? PrimitiveType.Cube
            : spawnedCount % 3 == 1
                ? PrimitiveType.Sphere
                : PrimitiveType.Capsule;

        float xOffset = ((spawnedCount % 5) - 2) * spacing;
        SpawnPrimitive(type, $"VR Spawned Object {spawnedCount + 1}", xOffset, spawnedCount);
    }

    private GameObject SpawnPrimitive(PrimitiveType primitiveType, string objectName, float xOffset, int colorIndex)
    {
        Transform origin = ResolveSpawnOrigin();
        Vector3 forward = Vector3.ProjectOnPlane(origin.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        Vector3 right = Vector3.ProjectOnPlane(origin.right, Vector3.up).normalized;
        Vector3 position = origin.position + forward * spawnDistance + right * xOffset + Vector3.up * spawnHeightOffset;

        GameObject spawned = GameObject.CreatePrimitive(primitiveType);
        spawned.name = objectName;
        spawned.transform.SetPositionAndRotation(position, Quaternion.identity);
        spawned.transform.localScale = defaultScale;

        Renderer renderer = spawned.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material = CreateMaterial(Colors[colorIndex % Colors.Length]);

        VRGrabbableSetup grabbableSetup = spawned.AddComponent<VRGrabbableSetup>();
        grabbableSetup.Configure();

        spawnedCount++;
        return spawned;
    }

    private Transform ResolveSpawnOrigin()
    {
        if (spawnOrigin != null)
            return spawnOrigin;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            return mainCamera.transform;

        return transform;
    }

    private static Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.color = color;
        return material;
    }
}
