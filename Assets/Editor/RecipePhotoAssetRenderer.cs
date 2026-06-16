using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class RecipePhotoAssetRenderer
{
    private const string OutputFolder = "/tmp/recipe_tablet_assets";
    private const string IngredientOutputFolder = "/tmp/recipe_ingredient_assets";

    [MenuItem("Tools/Recipe Tablet/Render Photo Assets")]
    public static void RenderPhotoAssets()
    {
        Directory.CreateDirectory(OutputFolder);

        var renderJobs = new List<RenderJob>
        {
            new RenderJob("main_potion", "Assets/StylizedMagicPotion/Prefabs/Prefab4K/Bottle10_4K.prefab", new Vector3(18f, -28f, 0f), 1.15f),
            new RenderJob("toxic_potion", "Assets/StylizedMagicPotion/Prefabs/Prefab4K/Bottle3_4K.prefab", new Vector3(18f, -30f, 0f), 1.15f),
            new RenderJob("storm_potion", "Assets/StylizedMagicPotion/Prefabs/Prefab4K/Bottle8_4K.prefab", new Vector3(18f, -30f, 0f), 1.12f),
            new RenderJob("forest_potion", "Assets/StylizedMagicPotion/Prefabs/Prefab4K/Bottle5_4K.prefab", new Vector3(18f, -26f, 0f), 1.12f),
            new RenderJob("green_slime", "Assets/Prefabs/CraftedResults/SLIME-GREEN.prefab", new Vector3(14f, -40f, 0f), 1.05f),
            new RenderJob("red_slime", "Assets/PACK_ENEMY_LOWPOLY/SLIME/PREFAB/SLIME-RED.prefab", new Vector3(14f, -40f, 0f), 1.05f),
            new RenderJob("fairy_bottle", "Assets/Prefabs/CraftedResults/2,6,8 Фея в тумане Variant.prefab", new Vector3(18f, -30f, 0f), 1.2f),
        };

        foreach (var job in renderJobs)
        {
            RenderPrefab(job);
        }

        AssetDatabase.Refresh();
        Debug.Log($"Recipe photo assets rendered to {OutputFolder}");
    }

    [MenuItem("Tools/Recipe Tablet/Render Ingredient Assets")]
    public static void RenderIngredientAssets()
    {
        Directory.CreateDirectory(IngredientOutputFolder);

        var renderJobs = new List<RenderJob>
        {
            new RenderJob("ingredient_1", "Assets/StylizedMagicPotion/Prefabs/Prefab2K/Bottle1_2K.prefab", new Vector3(20f, -28f, 0f), 1.08f),
            new RenderJob("ingredient_2", "Assets/StylizedMagicPotion/Prefabs/Prefab2K/Bottle2_2K.prefab", new Vector3(20f, -28f, 0f), 1.08f),
            new RenderJob("ingredient_3", "Assets/StylizedMagicPotion/Prefabs/Prefab2K/Bottle3_2K.prefab", new Vector3(20f, -28f, 0f), 1.08f),
            new RenderJob("ingredient_4", "Assets/StylizedMagicPotion/Prefabs/Prefab2K/Bottle4_2K.prefab", new Vector3(18f, -28f, 0f), 1.08f),
            new RenderJob("ingredient_5", "Assets/StylizedMagicPotion/Prefabs/Prefab2K/Bottle5_2K.prefab", new Vector3(18f, -28f, 0f), 1.08f),
            new RenderJob("ingredient_6", "Assets/StylizedMagicPotion/Prefabs/Prefab2K/Bottle6_2K.prefab", new Vector3(18f, -28f, 0f), 1.08f),
            new RenderJob("ingredient_7", "Assets/StylizedMagicPotion/Prefabs/Prefab2K/Bottle7_2K.prefab", new Vector3(18f, -28f, 0f), 1.08f),
            new RenderJob("ingredient_8", "Assets/StylizedMagicPotion/Prefabs/Prefab2K/Bottle8_2K.prefab", new Vector3(18f, -28f, 0f), 1.08f),
            new RenderJob("ingredient_9", "Assets/StylizedMagicPotion/Prefabs/Prefab2K/Bottle9_2K.prefab", new Vector3(18f, -28f, 0f), 1.08f),
            new RenderJob("ingredient_10", "Assets/StylizedMagicPotion/Prefabs/Prefab2K/Bottle10_2K.prefab", new Vector3(18f, -28f, 0f), 1.08f),
            new RenderJob("ingredient_11", "Assets/Prefabs/CraftedResults/2,6,8 Фея в тумане Variant.prefab", new Vector3(18f, -30f, 0f), 1.2f),
        };

        foreach (var job in renderJobs)
        {
            RenderPrefab(job, IngredientOutputFolder);
        }

        AssetDatabase.Refresh();
        Debug.Log($"Recipe ingredient assets rendered to {IngredientOutputFolder}");
    }

    private static void RenderPrefab(RenderJob job)
    {
        RenderPrefab(job, OutputFolder);
    }

    private static void RenderPrefab(RenderJob job, string outputFolder)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(job.AssetPath);
        if (prefab == null)
        {
            throw new FileNotFoundException($"Could not load prefab at {job.AssetPath}");
        }

        var stageRoot = new GameObject($"Preview_{job.OutputName}");
        try
        {
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException($"Could not instantiate prefab at {job.AssetPath}");
            }

            instance.transform.SetParent(stageRoot.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(job.RotationEuler);

            var lightA = CreateLight("KeyLight", stageRoot.transform, new Vector3(35f, -35f, 0f), 1.55f);
            var lightB = CreateLight("FillLight", stageRoot.transform, new Vector3(340f, 40f, 0f), 0.65f);
            var lightC = CreateLight("RimLight", stageRoot.transform, new Vector3(160f, 0f, 0f), 0.85f);
            _ = lightA;
            _ = lightB;
            _ = lightC;

            var bounds = CalculateBounds(instance);
            var focus = bounds.center + new Vector3(0f, bounds.extents.y * 0.08f, 0f);

            var cameraObject = new GameObject("PreviewCamera");
            cameraObject.transform.SetParent(stageRoot.transform, false);

            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 100f;
            camera.fieldOfView = 26f;

            var distance = Mathf.Max(bounds.extents.y, bounds.extents.x) * 4.3f * job.DistanceScale;
            camera.transform.position = focus + new Vector3(0f, bounds.extents.y * 0.18f, -distance);
            camera.transform.LookAt(focus);

            var outputPath = Path.Combine(outputFolder, job.OutputName + ".png");
            RenderCameraToPng(camera, 768, 768, outputPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(stageRoot);
        }
    }

    private static Light CreateLight(string name, Transform parent, Vector3 rotationEuler, float intensity)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localRotation = Quaternion.Euler(rotationEuler);

        var light = gameObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = intensity;
        light.color = Color.white;
        light.shadows = LightShadows.None;
        return light;
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
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

    private static void RenderCameraToPng(Camera camera, int width, int height, string outputPath)
    {
        var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        try
        {
            camera.targetTexture = renderTexture;
            camera.Render();

            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = renderTexture;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                try
                {
                    texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    texture.Apply(false, false);
                    File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }
        finally
        {
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(renderTexture);
        }
    }

    private readonly struct RenderJob
    {
        public RenderJob(string outputName, string assetPath, Vector3 rotationEuler, float distanceScale)
        {
            OutputName = outputName;
            AssetPath = assetPath;
            RotationEuler = rotationEuler;
            DistanceScale = distanceScale;
        }

        public string OutputName { get; }
        public string AssetPath { get; }
        public Vector3 RotationEuler { get; }
        public float DistanceScale { get; }
    }
}
