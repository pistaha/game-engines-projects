using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RecipeBookBuilder
{
    private const string ScenePath = "Assets/LowPolyDungeonsLite_Demo.unity";
    private const string IngredientPhotoSourceFolder = "/tmp/recipe_ingredient_assets";
    private const string IngredientPhotoAssetFolder = "Assets/RecipeBook/IngredientPhotos";
    private const string MaterialFolder = "Assets/RecipeBook/Materials";

    [MenuItem("Tools/Recipe Book/Rebuild Interactive Book")]
    public static void RebuildInteractiveBook()
    {
        EnsureIngredientPhotos();
        OpenScene();
        RemoveRecipeTablet();

        RecipeChecker recipeChecker = Object.FindFirstObjectByType<RecipeChecker>();
        if (recipeChecker == null)
            throw new FileNotFoundException("RecipeChecker not found in scene.");

        List<RecipeData> recipes = GetSceneRecipes(recipeChecker);
        if (recipes.Count == 0)
            throw new FileNotFoundException("No valid recipes were found in RecipeChecker.");

        GameObject book = GameObject.Find("Book_09");
        if (book == null)
            throw new FileNotFoundException("Book_09 was not found in the scene.");

        BuildInteractiveBook(book, recipes);
        SaveScene();

        Debug.Log($"Recipe book rebuilt with {recipes.Count} existing recipes.");
    }

    private static void EnsureIngredientPhotos()
    {
        EnsureAssetFolder("Assets/RecipeBook");
        EnsureAssetFolder(IngredientPhotoAssetFolder);
        EnsureAssetFolder(MaterialFolder);

        Directory.CreateDirectory(IngredientPhotoSourceFolder);
        Directory.CreateDirectory(GetFullPath(IngredientPhotoAssetFolder));
        Directory.CreateDirectory(GetFullPath(MaterialFolder));

        RecipePhotoAssetRenderer.RenderIngredientAssets();

        for (int i = 1; i <= 11; i++)
        {
            string fileName = $"ingredient_{i}.png";
            string sourcePath = Path.Combine(IngredientPhotoSourceFolder, fileName);
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException($"Ingredient preview not found: {sourcePath}");

            string destinationPath = Path.Combine(GetFullPath(IngredientPhotoAssetFolder), fileName);
            File.WriteAllBytes(destinationPath, File.ReadAllBytes(sourcePath));
        }

        AssetDatabase.Refresh();

        for (int i = 1; i <= 11; i++)
        {
            string assetPath = $"{IngredientPhotoAssetFolder}/ingredient_{i}.png";
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }
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
        AssetDatabase.Refresh();
    }

    private static List<RecipeData> GetSceneRecipes(RecipeChecker recipeChecker)
    {
        List<RecipeData> recipes = new List<RecipeData>();

        for (int i = 0; i < recipeChecker.recipes.Count; i++)
        {
            RecipeData recipe = recipeChecker.recipes[i];
            if (recipe == null)
                continue;

            if (recipe.ingredients == null || recipe.ingredients.Count == 0)
                continue;

            if (recipe.bookPageSprite == null)
                continue;

            recipes.Add(recipe);
        }

        return recipes;
    }

    private static void RemoveRecipeTablet()
    {
        GameObject tablet = GameObject.Find("RecipeTablet");
        if (tablet != null)
            Object.DestroyImmediate(tablet);
    }

    private static void BuildInteractiveBook(GameObject book, List<RecipeData> recipes)
    {
        GameObjectUtility.SetStaticEditorFlags(book, 0);

        CleanupOldBookObjects(book.transform);

        Transform spreadRoot = new GameObject("RecipeBook_SpreadRoot").transform;
        spreadRoot.SetParent(book.transform, false);
        spreadRoot.localPosition = new Vector3(0f, 0.055f, -0.11f);
        spreadRoot.localRotation = Quaternion.identity;
        spreadRoot.localScale = Vector3.one;

        Material coverMaterial = GetOrCreateColorMaterial(
            $"{MaterialFolder}/RecipeBookCover.mat",
            new Color(0.29f, 0.17f, 0.11f, 1f)
        );

        Material seamMaterial = GetOrCreateColorMaterial(
            $"{MaterialFolder}/RecipeBookSeam.mat",
            new Color(0.18f, 0.11f, 0.08f, 1f)
        );

        Material pageMaterial = GetOrCreateColorMaterial(
            $"{MaterialFolder}/RecipeBookPage.mat",
            new Color(0.99f, 0.98f, 0.93f, 1f)
        );

        CreateCube("RecipeBook_Back", spreadRoot, new Vector3(0f, 0f, -0.03f), new Vector3(1.38f, 0.92f, 0.07f), coverMaterial);
        CreateCube("RecipeBook_Spine", spreadRoot, new Vector3(0f, 0f, -0.005f), new Vector3(0.078f, 0.86f, 0.04f), seamMaterial);
        CreateQuad("RecipeBook_LeftPage", spreadRoot, new Vector3(-0.315f, 0f, 0.005f), new Vector3(0.655f, 0.845f, 1f), Quaternion.Euler(0f, 0f, 1.3f), pageMaterial);
        CreateQuad("RecipeBook_RightPage", spreadRoot, new Vector3(0.315f, 0f, 0.005f), new Vector3(0.655f, 0.845f, 1f), Quaternion.Euler(0f, 0f, -1.3f), pageMaterial);

        List<GameObject> spreadObjects = new List<GameObject>();
        for (int i = 0; i < recipes.Count; i += 2)
        {
            GameObject spread = new GameObject($"RecipeSpread_{i / 2 + 1}");
            spread.transform.SetParent(spreadRoot, false);
            spread.transform.localPosition = Vector3.zero;
            spread.transform.localRotation = Quaternion.identity;
            spread.transform.localScale = Vector3.one;

            RectTransform spreadCanvas = CreateSpreadCanvas(spread.transform);
            BuildRecipePageUi(spreadCanvas, recipes[i], new Vector2(-300f, 0f));

            if (i + 1 < recipes.Count)
                BuildRecipePageUi(spreadCanvas, recipes[i + 1], new Vector2(300f, 0f));

            CreateSpreadHint(spreadCanvas);

            spread.SetActive(i == 0);
            spreadObjects.Add(spread);
        }

        spreadRoot.gameObject.SetActive(false);
        ConfigureViewer(book, spreadRoot.gameObject, spreadObjects);
    }

    private static void CleanupOldBookObjects(Transform book)
    {
        List<GameObject> toDelete = new List<GameObject>();

        for (int i = 0; i < book.childCount; i++)
        {
            Transform child = book.GetChild(i);
            if (child.name.StartsWith("RecipeBook_") || child.name.StartsWith("RecipeSpread_"))
                toDelete.Add(child.gameObject);
        }

        for (int i = 0; i < toDelete.Count; i++)
        {
            Object.DestroyImmediate(toDelete[i]);
        }
    }

    private static void ConfigureViewer(GameObject book, GameObject spreadRoot, List<GameObject> spreads)
    {
        RecipeBookViewer viewer = book.GetComponent<RecipeBookViewer>();
        if (viewer == null)
            viewer = book.AddComponent<RecipeBookViewer>();

        FirstPersonCC playerController = Object.FindFirstObjectByType<FirstPersonCC>();
        ClickCollector clickCollector = Object.FindFirstObjectByType<ClickCollector>();
        Collider clickCollider = book.GetComponent<Collider>();
        Renderer renderer = book.GetComponent<Renderer>();

        SerializedObject serializedObject = new SerializedObject(viewer);
        serializedObject.FindProperty("playerController").objectReferenceValue = playerController;
        serializedObject.FindProperty("clickCollector").objectReferenceValue = clickCollector;
        serializedObject.FindProperty("clickCollider").objectReferenceValue = clickCollider;
        serializedObject.FindProperty("spreadRoot").objectReferenceValue = spreadRoot;
        serializedObject.FindProperty("canvasBookRoot").objectReferenceValue = null;
        serializedObject.FindProperty("canvasRaycaster").objectReferenceValue = null;
        serializedObject.FindProperty("useScreenSpaceBookMenu").boolValue = true;
        serializedObject.FindProperty("showWorldBookPages").boolValue = false;
        serializedObject.FindProperty("moveDuration").floatValue = 0.5f;
        serializedObject.FindProperty("openDistance").floatValue = 1.08f;
        serializedObject.FindProperty("openHeightOffset").floatValue = -0.03f;
        serializedObject.FindProperty("openScaleMultiplier").floatValue = 1.98f;
        serializedObject.FindProperty("openTilt").floatValue = 1.8f;
        serializedObject.FindProperty("spreadStartScale").floatValue = 0.36f;
        serializedObject.FindProperty("floatAmplitude").floatValue = 0.008f;
        serializedObject.FindProperty("floatSpeed").floatValue = 1.9f;

        SerializedProperty rendererList = serializedObject.FindProperty("closedBookRenderers");
        rendererList.ClearArray();
        rendererList.arraySize = 1;
        rendererList.GetArrayElementAtIndex(0).objectReferenceValue = renderer;

        SerializedProperty spreadList = serializedObject.FindProperty("spreads");
        spreadList.ClearArray();
        spreadList.arraySize = spreads.Count;
        for (int i = 0; i < spreads.Count; i++)
        {
            spreadList.GetArrayElementAtIndex(i).objectReferenceValue = spreads[i];
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildRecipePage(Transform spread, RecipeData recipe, float pageCenterX)
    {
        CreateText(
            $"Title_{recipe.name}",
            spread,
            new Vector3(pageCenterX, 0.285f, 0.02f),
            WrapTitle(recipe.resultName),
            76,
            0.021f,
            FontStyle.Bold,
            new Color(0.18f, 0.11f, 0.07f, 1f)
        );

        List<IngredientData> ingredients = recipe.ingredients;
        int count = ingredients.Count;
        int columns = count <= 3 ? count : count <= 6 ? 3 : 5;
        int rows = Mathf.CeilToInt(count / (float)columns);
        float iconSize = count <= 3 ? 0.165f : count <= 6 ? 0.145f : 0.095f;
        float xSpacing = iconSize * 1.18f;
        float ySpacing = iconSize * 1.28f;
        float totalWidth = (columns - 1) * xSpacing;
        float totalHeight = (rows - 1) * ySpacing;
        float startY = count <= 3 ? -0.04f : -0.025f;

        for (int i = 0; i < count; i++)
        {
            int row = i / columns;
            int column = i % columns;

            float x = pageCenterX - totalWidth * 0.5f + column * xSpacing;
            float y = startY + totalHeight * 0.5f - row * ySpacing;

            IngredientData ingredient = ingredients[i];
            string ingredientId = ingredient != null ? ingredient.name.Trim() : "?";

            CreateQuad(
                $"Ingredient_{recipe.name}_{i + 1}",
                spread,
                new Vector3(x, y, 0.02f),
                new Vector3(iconSize, iconSize, 1f),
                Quaternion.identity,
                GetIngredientMaterial(ingredientId)
            );

            if (count <= 4 && row == 0 && i < count - 1)
            {
                CreateText(
                    $"Plus_{recipe.name}_{i + 1}",
                    spread,
                    new Vector3(x + xSpacing * 0.5f, y, 0.03f),
                    "+",
                    80,
                    0.027f,
                    FontStyle.Bold,
                    new Color(0.27f, 0.16f, 0.1f, 1f)
                );
            }
        }
    }

    private static void AddInstructionText(Transform spreadRoot)
    {
        CreateText(
            "RecipeBook_Hint",
            spreadRoot,
            new Vector3(0f, -0.4f, 0.015f),
            "A / D - листать   ESC - закрыть",
            42,
            0.02f,
            FontStyle.Normal,
            new Color(0.28f, 0.18f, 0.12f, 1f)
        );
    }

    private static RectTransform CreateSpreadCanvas(Transform parent)
    {
        GameObject canvasObject = new GameObject(
            "SpreadCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        canvasObject.transform.SetParent(parent, false);
        canvasObject.transform.localPosition = new Vector3(0f, 0f, 0.02f);
        canvasObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        canvasObject.transform.localScale = Vector3.one * 0.001f;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 20f;

        GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
        raycaster.enabled = false;

        RectTransform rectTransform = canvasObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(1340f, 940f);
        return rectTransform;
    }

    private static void BuildRecipePageUi(RectTransform canvas, RecipeData recipe, Vector2 anchoredPosition)
    {
        RectTransform page = CreateUiPanel(
            canvas,
            $"Page_{recipe.name}",
            anchoredPosition,
            new Vector2(440f, 560f),
            new Color(0.99f, 0.98f, 0.93f, 0.98f)
        );

        page.localRotation = Quaternion.Euler(0f, 0f, anchoredPosition.x < 0f ? 1.2f : -1.2f);

        if (recipe.bookPageSprite != null)
        {
            float pageScale = recipe.bookPageScale <= 0f ? 1f : recipe.bookPageScale;
            CreateUiSpriteImage(
                page,
                $"PageArtwork_{recipe.name}",
                recipe.bookPageOffset,
                new Vector2(430f * pageScale, 550f * pageScale),
                recipe.bookPageSprite
            );
            return;
        }

        CreateUiText(
            page,
            $"ResultLabel_{recipe.name}",
            new Vector2(0f, 210f),
            new Vector2(300f, 30f),
            "Результат",
            19,
            FontStyle.Bold,
            new Color(0.35f, 0.23f, 0.15f, 1f)
        );

        CreateUiText(
            page,
            $"ResultName_{recipe.name}",
            new Vector2(0f, 160f),
            new Vector2(350f, 76f),
            WrapTitle(recipe.resultName),
            26,
            FontStyle.Bold,
            new Color(0.16f, 0.1f, 0.06f, 1f)
        );

        CreateUiText(
            page,
            $"IngredientLabel_{recipe.name}",
            new Vector2(0f, 108f),
            new Vector2(290f, 26f),
            "Ингредиенты",
            17,
            FontStyle.Normal,
            new Color(0.4f, 0.27f, 0.18f, 1f)
        );

        List<IngredientData> ingredients = recipe.ingredients;
        int count = ingredients.Count;
        int columns = count <= 2 ? count : count <= 4 ? 2 : count <= 6 ? 3 : 5;
        int rows = Mathf.CeilToInt(count / (float)columns);
        float iconSize = count <= 2 ? 176f : count == 3 ? 146f : count == 4 ? 132f : count <= 6 ? 102f : 58f;
        float xSpacing = count <= 2 ? iconSize + 24f : count <= 4 ? iconSize + 22f : count <= 6 ? iconSize + 18f : iconSize + 10f;
        float ySpacing = count <= 2 ? iconSize + 34f : count <= 4 ? iconSize + 28f : count <= 6 ? iconSize + 20f : iconSize + 12f;
        float totalHeight = (rows - 1) * ySpacing;
        float startY = count <= 2 ? -70f : count <= 4 ? -82f : count <= 6 ? -92f : -90f;

        for (int i = 0; i < count; i++)
        {
            int row = i / columns;
            int column = i % columns;
            int itemsInRow = Mathf.Min(columns, count - row * columns);

            float rowWidth = (itemsInRow - 1) * xSpacing;
            float x = -rowWidth * 0.5f + column * xSpacing;
            float y = startY + totalHeight * 0.5f - row * ySpacing;

            IngredientData ingredient = ingredients[i];
            string ingredientId = ingredient != null ? ingredient.name.Trim() : "?";

            RectTransform frame = CreateUiPanel(
                page,
                $"IngredientFrame_{recipe.name}_{i + 1}",
                new Vector2(x, y),
                new Vector2(iconSize + 14f, iconSize + 14f),
                new Color(0.93f, 0.88f, 0.78f, 0.95f)
            );

            CreateUiRawImage(
                frame,
                $"Ingredient_{recipe.name}_{i + 1}",
                Vector2.zero,
                new Vector2(iconSize, iconSize),
                GetIngredientTexture(ingredientId)
            );

            if (count <= 2 && i < count - 1)
            {
                CreateUiText(
                    page,
                    $"Plus_{recipe.name}_{i + 1}",
                    new Vector2(x + xSpacing * 0.5f, y),
                    new Vector2(34f, 34f),
                    "+",
                    28,
                    FontStyle.Bold,
                    new Color(0.39f, 0.23f, 0.14f, 1f)
                );
            }
        }
    }

    private static RectTransform CreateUiPanel(
        RectTransform parent,
        string name,
        Vector2 anchoredPosition,
        Vector2 size,
        Color color
    )
    {
        GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Image image = panelObject.GetComponent<Image>();
        image.color = color;
        return rectTransform;
    }

    private static void CreateUiText(
        RectTransform parent,
        string name,
        Vector2 anchoredPosition,
        Vector2 size,
        string text,
        int fontSize,
        FontStyle fontStyle,
        Color color
    )
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI uiText = textObject.GetComponent<TextMeshProUGUI>();
        uiText.font = TMP_Settings.defaultFontAsset != null
            ? TMP_Settings.defaultFontAsset
            : Resources.Load<TMP_FontAsset>("LiberationSans SDF");
        uiText.text = text;
        uiText.fontSize = fontSize;
        uiText.fontStyle = fontStyle == FontStyle.Bold ? FontStyles.Bold : FontStyles.Normal;
        uiText.alignment = TextAlignmentOptions.Center;
        uiText.textWrappingMode = TextWrappingModes.Normal;
        uiText.overflowMode = TextOverflowModes.Overflow;
        uiText.color = color;
    }

    private static void CreateUiRawImage(
        RectTransform parent,
        string name,
        Vector2 anchoredPosition,
        Vector2 size,
        Texture texture
    )
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        imageObject.transform.SetParent(parent, false);

        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        RawImage rawImage = imageObject.GetComponent<RawImage>();
        rawImage.texture = texture;
        rawImage.color = Color.white;
    }

    private static void CreateUiSpriteImage(
        RectTransform parent,
        string name,
        Vector2 anchoredPosition,
        Vector2 size,
        Sprite sprite
    )
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private static void CreateSpreadHint(RectTransform canvas)
    {
        CreateUiText(
            canvas,
            "RecipeBookHint",
            new Vector2(0f, -402f),
            new Vector2(520f, 32f),
            "A / D - листать   ESC - закрыть",
            24,
            FontStyle.Normal,
            new Color(0.34f, 0.23f, 0.16f, 1f)
        );
    }

    private static GameObject CreateQuad(string name, Transform parent, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = position;
        quad.transform.localRotation = rotation;
        quad.transform.localScale = scale;

        Collider collider = quad.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        return quad;
    }

    private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localRotation = Quaternion.identity;
        cube.transform.localScale = scale;

        Collider collider = cube.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        MeshRenderer renderer = cube.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        return cube;
    }

    private static void CreateText(
        string name,
        Transform parent,
        Vector3 position,
        string content,
        int fontSize,
        float characterSize,
        FontStyle fontStyle,
        Color color
    )
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        textObject.transform.localPosition = position;
        textObject.transform.localRotation = Quaternion.identity;
        textObject.transform.localScale = Vector3.one;

        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = content;
        textMesh.fontSize = fontSize;
        textMesh.characterSize = characterSize;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = color;
        textMesh.fontStyle = fontStyle;
    }

    private static Material GetIngredientMaterial(string ingredientId)
    {
        string materialPath = $"{MaterialFolder}/Ingredient_{ingredientId}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material != null)
            return material;

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{IngredientPhotoAssetFolder}/ingredient_{ingredientId}.png");
        if (texture == null)
            return GetOrCreateColorMaterial($"{MaterialFolder}/MissingIngredient.mat", new Color(0.8f, 0.2f, 0.2f, 1f));

        Shader shader = FindShader("Sprites/Default", "Unlit/Transparent", "Universal Render Pipeline/Unlit");
        material = new Material(shader);
        material.name = $"Ingredient_{ingredientId}";
        material.mainTexture = texture;
        material.SetTexture("_MainTex", texture);

        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);

        AssetDatabase.CreateAsset(material, materialPath);
        return material;
    }

    private static Texture2D GetIngredientTexture(string ingredientId)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{IngredientPhotoAssetFolder}/ingredient_{ingredientId}.png");
        if (texture != null)
            return texture;

        return Texture2D.whiteTexture;
    }

    private static Material GetOrCreateColorMaterial(string assetPath, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        Shader shader = FindShader("Universal Render Pipeline/Unlit", "Sprites/Default", "Standard");
        if (material == null)
        {
            material = new Material(shader);
            material.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(material, assetPath);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", color * 0.15f);

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 0f);

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0f);

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.02f);

        EditorUtility.SetDirty(material);
        return material;
    }

    private static Shader FindShader(params string[] shaderNames)
    {
        for (int i = 0; i < shaderNames.Length; i++)
        {
            Shader shader = Shader.Find(shaderNames[i]);
            if (shader != null)
                return shader;
        }

        throw new FileNotFoundException("Could not find a shader for the recipe book.");
    }

    private static string WrapTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "Без названия";

        string[] words = title.Split(' ');
        if (words.Length < 2)
            return title;

        if (title.Length <= 15)
            return title;

        int middle = Mathf.CeilToInt(words.Length * 0.5f);
        string firstLine = string.Join(" ", words, 0, middle);
        string secondLine = string.Join(" ", words, middle, words.Length - middle);
        return firstLine + "\n" + secondLine;
    }

    private static string GetFullPath(string assetPath)
    {
        string projectPath = Directory.GetParent(Application.dataPath).FullName;
        if (!assetPath.StartsWith("Assets/"))
            return assetPath;

        return Path.Combine(projectPath, assetPath);
    }

    private static void EnsureAssetFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
            return;

        int lastSlash = assetPath.LastIndexOf('/');
        if (lastSlash <= 0)
            return;

        string parentFolder = assetPath.Substring(0, lastSlash);
        string folderName = assetPath.Substring(lastSlash + 1);

        EnsureAssetFolder(parentFolder);
        AssetDatabase.CreateFolder(parentFolder, folderName);
    }
}
