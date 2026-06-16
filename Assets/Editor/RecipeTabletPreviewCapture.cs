using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RecipeTabletPreviewCapture
{
    private const string ScenePath = "Assets/LowPolyDungeonsLite_Demo.unity";
    private const string TabletMaterialPath = "Assets/RecipeTablet/RecipeTabletBoard.mat";
    private const string ClosePreviewPath = "/tmp/recipe_tablet_close.png";
    private const string ContextPreviewPath = "/tmp/recipe_tablet_context.png";

    [MenuItem("Tools/Recipe Tablet/Rebuild And Capture")]
    public static void RebuildAndCapture()
    {
        OpenScene();
        var tablet = BuildTablet();
        SaveScene();

        CaptureClosePreview(tablet);
        CaptureContextPreview(tablet);

        Debug.Log($"Recipe tablet rebuilt. Close preview: {ClosePreviewPath}. Context preview: {ContextPreviewPath}");
    }

    public static void CaptureOnly()
    {
        OpenScene();

        var tablet = GameObject.Find("RecipeTablet");
        if (tablet == null)
        {
            throw new FileNotFoundException("RecipeTablet object was not found in the scene.");
        }

        CaptureClosePreview(tablet);
        CaptureContextPreview(tablet);
    }

    private static void OpenScene()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    private static void SaveScene()
    {
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
    }

    private static GameObject BuildTablet()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(TabletMaterialPath);
        if (material == null)
        {
            throw new FileNotFoundException($"Recipe tablet material was not found at {TabletMaterialPath}.");
        }

        CleanupExistingTabletObjects(material);

        var tablet = GameObject.CreatePrimitive(PrimitiveType.Quad);
        tablet.name = "RecipeTablet";
        var cauldron = GameObject.Find("Cotel");
        var cauldronPosition = cauldron != null ? cauldron.transform.position : new Vector3(-2.07f, -0.016f, -5.141f);
        var boardPosition = cauldronPosition + new Vector3(1.75f, 1.35f, 0.95f);
        var readableSideTarget = cauldronPosition + Vector3.up * 0.85f;
        var awayFromReadableSide = (boardPosition - readableSideTarget).normalized;

        tablet.transform.position = boardPosition;
        tablet.transform.rotation = Quaternion.LookRotation(awayFromReadableSide, Vector3.up) * Quaternion.Euler(4f, 0f, 0f);
        tablet.transform.localScale = new Vector3(1.55f, 2.5f, 1f);

        var renderer = tablet.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        return tablet;
    }

    private static void CaptureClosePreview(GameObject tablet)
    {
        var bounds = CalculateBounds(tablet);
        var focusPoint = bounds.center;
        var cameraPosition = GetFramedCameraPosition(tablet.transform.forward, bounds, 28f, 1500f / 2000f, 0.8f) + Vector3.up * 0.05f;
        RenderPreview(cameraPosition, focusPoint, 28f, 1500, 2000, ClosePreviewPath);
    }

    private static void CaptureContextPreview(GameObject tablet)
    {
        var bounds = CalculateBounds(tablet);
        var focusPoint = bounds.center + Vector3.down * 0.15f;
        var cameraPosition =
            GetFramedCameraPosition(tablet.transform.forward, bounds, 44f, 1600f / 1000f, 2.6f)
            + tablet.transform.right * 1.35f
            + Vector3.up * 0.55f;
        RenderPreview(cameraPosition, focusPoint, 44f, 1600, 1000, ContextPreviewPath);
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(root.transform.position, Vector3.one);
        }

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private static void CleanupExistingTabletObjects(Material material)
    {
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
        {
            if (root.name == "RecipeTablet")
            {
                Object.DestroyImmediate(root);
                continue;
            }

            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterial == material)
                {
                    Object.DestroyImmediate(root);
                    break;
                }
            }
        }
    }

    private static Vector3 GetFramedCameraPosition(Vector3 forward, Bounds bounds, float fieldOfView, float aspect, float extraDistance)
    {
        var halfVerticalFov = fieldOfView * 0.5f * Mathf.Deg2Rad;
        var halfHorizontalFov = Mathf.Atan(Mathf.Tan(halfVerticalFov) * aspect);

        var verticalDistance = bounds.extents.y / Mathf.Tan(halfVerticalFov);
        var horizontalDistance = bounds.extents.x / Mathf.Tan(halfHorizontalFov);
        var distance = Mathf.Max(verticalDistance, horizontalDistance) + extraDistance;

        return bounds.center - forward.normalized * distance;
    }

    private static void RenderPreview(Vector3 position, Vector3 target, float fieldOfView, int width, int height, string outputPath)
    {
        var previewRoot = new GameObject("RecipeTabletPreviewCamera");
        try
        {
            var camera = previewRoot.AddComponent<Camera>();
            camera.transform.position = position;
            camera.transform.LookAt(target);
            camera.fieldOfView = fieldOfView;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.06f, 0.05f, 1f);
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 100f;
            camera.allowHDR = false;
            camera.allowMSAA = true;

            var renderTexture = new RenderTexture(width, height, 24);
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();

                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                try
                {
                    RenderTexture.active = renderTexture;
                    texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    texture.Apply(false, false);

                    File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                }
                finally
                {
                    RenderTexture.active = null;
                    Object.DestroyImmediate(texture);
                }
            }
            finally
            {
                camera.targetTexture = null;
                Object.DestroyImmediate(renderTexture);
            }
        }
        finally
        {
            Object.DestroyImmediate(previewRoot);
        }
    }
}
