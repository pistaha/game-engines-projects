using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class RecipeBookUiEntry
{
    public RecipeData recipe;

    public Sprite recipeIcon;
}

public class RecipeBookCanvasUI : MonoBehaviour
{
    [SerializeField] private RecipeChecker recipeChecker;
    [SerializeField] private List<RecipeBookUiEntry> recipeEntries = new List<RecipeBookUiEntry>();

    [SerializeField] private Image bookBackground;
    [SerializeField] private Image recipeIconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text ingredientsText;
    [SerializeField] private TMP_Text pageText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private RecipeBookViewer bookViewer;

    [SerializeField] private Color defaultRecipeColor = new Color(0.16f, 0.1f, 0.06f, 1f);
    [SerializeField] private string emptyBookTitle = "Книга рецептов";
    [SerializeField] private string emptyBookText = "Рецепты не назначены";

    private readonly List<RecipeBookUiEntry> runtimeEntries = new List<RecipeBookUiEntry>();
    private int currentIndex;

    public void ConfigureRuntimeReferences(
        RecipeChecker runtimeRecipeChecker,
        RecipeBookViewer runtimeBookViewer,
        Image runtimeBookBackground,
        Image runtimeRecipeIconImage,
        TMP_Text runtimeTitleText,
        TMP_Text runtimeIngredientsText,
        TMP_Text runtimePageText,
        Button runtimeNextButton,
        Button runtimePreviousButton,
        Button runtimeCloseButton)
    {
        recipeChecker = runtimeRecipeChecker;
        bookViewer = runtimeBookViewer;
        bookBackground = runtimeBookBackground;
        recipeIconImage = runtimeRecipeIconImage;
        titleText = runtimeTitleText;
        ingredientsText = runtimeIngredientsText;
        pageText = runtimePageText;

        if (nextButton != null)
            nextButton.onClick.RemoveListener(ShowNextRecipe);

        if (previousButton != null)
            previousButton.onClick.RemoveListener(ShowPreviousRecipe);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseBook);

        nextButton = runtimeNextButton;
        previousButton = runtimePreviousButton;
        closeButton = runtimeCloseButton;

        if (nextButton != null)
            nextButton.onClick.AddListener(ShowNextRecipe);

        if (previousButton != null)
            previousButton.onClick.AddListener(ShowPreviousRecipe);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseBook);

        RefreshBook();
    }

    private void Awake()
    {
        if (recipeChecker == null)
            recipeChecker = FindFirstObjectByType<RecipeChecker>();

        if (bookViewer == null)
            bookViewer = GetComponentInParent<RecipeBookViewer>();

        if (nextButton != null)
            nextButton.onClick.AddListener(ShowNextRecipe);

        if (previousButton != null)
            previousButton.onClick.AddListener(ShowPreviousRecipe);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseBook);

        RebuildRuntimeEntries();
        RefreshBook();
    }

    private void OnEnable()
    {
        RefreshBook();
    }

    private void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(ShowNextRecipe);

        if (previousButton != null)
            previousButton.onClick.RemoveListener(ShowPreviousRecipe);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseBook);
    }

    [ContextMenu("Обновить книгу")]
    public void RefreshBook()
    {
        RebuildRuntimeEntries();

        if (runtimeEntries.Count == 0)
        {
            ShowEmptyState();
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, runtimeEntries.Count - 1);
        ShowRecipe(runtimeEntries[currentIndex]);
        UpdateButtons();
    }

    public void ShowNextRecipe()
    {
        if (runtimeEntries.Count == 0)
            return;

        currentIndex++;

        if (currentIndex >= runtimeEntries.Count)
            currentIndex = 0;

        ShowRecipe(runtimeEntries[currentIndex]);
        UpdateButtons();
    }

    public void ShowPreviousRecipe()
    {
        if (runtimeEntries.Count == 0)
            return;

        currentIndex--;

        if (currentIndex < 0)
            currentIndex = runtimeEntries.Count - 1;

        ShowRecipe(runtimeEntries[currentIndex]);
        UpdateButtons();
    }

    public void ShowRecipeByIndex(int index)
    {
        if (runtimeEntries.Count == 0)
            return;

        currentIndex = Mathf.Clamp(index, 0, runtimeEntries.Count - 1);
        ShowRecipe(runtimeEntries[currentIndex]);
        UpdateButtons();
    }

    private void CloseBook()
    {
        if (bookViewer != null)
            bookViewer.CloseBookFromButton();
    }

    private void RebuildRuntimeEntries()
    {
        runtimeEntries.Clear();

        for (int i = 0; i < recipeEntries.Count; i++)
        {
            RecipeBookUiEntry entry = recipeEntries[i];

            if (entry == null || entry.recipe == null)
                continue;

            runtimeEntries.Add(entry);
        }

        if (runtimeEntries.Count > 0)
            return;

        if (recipeChecker == null || recipeChecker.recipes == null)
            return;

        for (int i = 0; i < recipeChecker.recipes.Count; i++)
        {
            RecipeData recipe = recipeChecker.recipes[i];

            if (recipe == null)
                continue;

            RecipeBookUiEntry generatedEntry = new RecipeBookUiEntry();
            generatedEntry.recipe = recipe;
            runtimeEntries.Add(generatedEntry);
        }
    }

    private void ShowRecipe(RecipeBookUiEntry entry)
    {
        if (entry == null || entry.recipe == null)
        {
            ShowEmptyState();
            return;
        }

        RecipeData recipe = entry.recipe;

        if (bookBackground != null)
            bookBackground.color = new Color(0.98f, 0.96f, 0.91f, 0.98f);

        if (titleText != null)
        {
            titleText.text = recipe.resultName;
            titleText.color = defaultRecipeColor;
        }

        if (ingredientsText != null)
            ingredientsText.text = BuildIngredientText(recipe);

        if (pageText != null)
            pageText.text = "Страница " + (currentIndex + 1) + " / " + runtimeEntries.Count;

        if (recipeIconImage != null)
        {
            Sprite displaySprite = GetRecipeDisplaySprite(entry, recipe);
            recipeIconImage.sprite = displaySprite;
            recipeIconImage.enabled = displaySprite != null;
            recipeIconImage.color = Color.white;
            recipeIconImage.raycastTarget = false;
        }
    }

    private static Sprite GetRecipeDisplaySprite(RecipeBookUiEntry entry, RecipeData recipe)
    {
        // Приоритет изображения: ручная иконка страницы, затем sprite рецепта, затем fallback на иконку ингредиента.
        if (entry != null && entry.recipeIcon != null)
            return entry.recipeIcon;

        if (recipe == null)
            return null;

        if (recipe.bookPageSprite != null)
            return recipe.bookPageSprite;

        if (recipe.ingredients == null)
            return null;

        for (int i = 0; i < recipe.ingredients.Count; i++)
        {
            IngredientData ingredient = recipe.ingredients[i];
            if (ingredient != null && ingredient.icon != null)
                return ingredient.icon;
        }

        return null;
    }

    private void ShowEmptyState()
    {
        if (bookBackground != null)
            bookBackground.color = new Color(0.98f, 0.96f, 0.91f, 0.98f);

        if (titleText != null)
        {
            titleText.text = emptyBookTitle;
            titleText.color = defaultRecipeColor;
        }

        if (ingredientsText != null)
            ingredientsText.text = emptyBookText;

        if (pageText != null)
            pageText.text = "Страница 0 / 0";

        if (recipeIconImage != null)
        {
            recipeIconImage.sprite = null;
            recipeIconImage.enabled = false;
        }

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        bool canSwitch = runtimeEntries.Count > 1;

        if (nextButton != null)
            nextButton.interactable = canSwitch;

        if (previousButton != null)
            previousButton.interactable = canSwitch;
    }

    private string BuildIngredientText(RecipeData recipe)
    {
        if (recipe == null || recipe.ingredients == null || recipe.ingredients.Count == 0)
            return "Ингредиенты не указаны";

        string text = "Ингредиенты:\n";

        for (int i = 0; i < recipe.ingredients.Count; i++)
        {
            IngredientData ingredient = recipe.ingredients[i];

            if (ingredient == null)
                continue;

            text += "• " + ingredient.ingredientName;

            if (i < recipe.ingredients.Count - 1)
                text += "\n";
        }

        if (!string.IsNullOrWhiteSpace(recipe.note))
            text += "\n\n" + recipe.note;

        return text;
    }

}
