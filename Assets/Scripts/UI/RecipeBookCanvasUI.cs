using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class RecipeBookUiEntry
{
    public RecipeData recipe;

    [TextArea(2, 5)]
    public string effectDescription;

    public Sprite recipeIcon;
}

public class RecipeBookCanvasUI : MonoBehaviour
{
    // Отвечает за содержимое книги: страницы, название, картинку, ингредиенты и описание.
    // Открытие и закрытие самой книги находится в отдельном скрипте книги.
    [SerializeField] private RecipeChecker recipeChecker;
    [SerializeField] private List<RecipeBookUiEntry> recipeEntries = new List<RecipeBookUiEntry>();

    [SerializeField] private Image bookBackground;
    [SerializeField] private Image recipeIconImage;
    [SerializeField] private Image[] ingredientImages;
    [SerializeField] private Text titleText;
    [SerializeField] private Text ingredientsText;
    [SerializeField] private Text effectText;
    [SerializeField] private Text pageText;
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
        Image[] runtimeIngredientImages,
        Text runtimeTitleText,
        Text runtimeIngredientsText,
        Text runtimeEffectText,
        Text runtimePageText,
        Button runtimeNextButton,
        Button runtimePreviousButton,
        Button runtimeCloseButton)
    {
        recipeChecker = runtimeRecipeChecker;
        bookViewer = runtimeBookViewer;
        bookBackground = runtimeBookBackground;
        recipeIconImage = runtimeRecipeIconImage;
        ingredientImages = runtimeIngredientImages;
        titleText = runtimeTitleText;
        ingredientsText = runtimeIngredientsText;
        effectText = runtimeEffectText;
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

    public void RefreshBookFromFirstPage()
    {
        currentIndex = 0;
        RefreshBook();
    }

    public void ShowNextRecipe()
    {
        if (runtimeEntries.Count == 0)
            return;

        if (currentIndex >= runtimeEntries.Count - 1)
            return;

        currentIndex++;
        ShowRecipe(runtimeEntries[currentIndex]);
        UpdateButtons();
    }

    public void ShowPreviousRecipe()
    {
        if (runtimeEntries.Count == 0)
            return;

        if (currentIndex <= 0)
            return;

        currentIndex--;
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

        if (runtimeEntries.Count == 0 && recipeChecker != null && recipeChecker.recipes != null)
        {
            for (int i = 0; i < recipeChecker.recipes.Count; i++)
            {
                RecipeData recipe = recipeChecker.recipes[i];

                if (recipe == null)
                    continue;

                RecipeBookUiEntry generatedEntry = new RecipeBookUiEntry();
                generatedEntry.recipe = recipe;
                generatedEntry.effectDescription = BuildDefaultDescription(recipe);
                runtimeEntries.Add(generatedEntry);
            }
        }

        runtimeEntries.Sort(CompareEntriesByRecipeName);
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
            titleText.text = recipe.resultName;

        if (ingredientsText != null)
            ingredientsText.text = BuildIngredientText(recipe);

        if (effectText != null)
            effectText.text = BuildRecipeDescription(recipe);

        if (pageText != null)
            pageText.text = "Страница " + (currentIndex + 1) + " / " + runtimeEntries.Count;

        if (recipeIconImage != null)
        {
            Sprite displaySprite = entry.recipeIcon != null ? entry.recipeIcon : recipe.bookPageSprite;
            recipeIconImage.sprite = displaySprite;
            recipeIconImage.enabled = displaySprite != null;
            recipeIconImage.color = Color.white;
            recipeIconImage.raycastTarget = false;
        }

        UpdateIngredientImages(recipe);
    }

    private void UpdateIngredientImages(RecipeData recipe)
    {
        if (ingredientImages == null || ingredientImages.Length == 0)
            return;

        for (int i = 0; i < ingredientImages.Length; i++)
        {
            Image image = ingredientImages[i];

            if (image == null)
                continue;

            if (recipe != null && recipe.ingredients != null && i < recipe.ingredients.Count && recipe.ingredients[i] != null)
            {
                image.sprite = recipe.ingredients[i].icon;
                image.enabled = recipe.ingredients[i].icon != null;
                image.color = Color.white;
            }
            else
            {
                image.sprite = null;
                image.enabled = false;
            }
        }
    }

    private void ShowEmptyState()
    {
        if (bookBackground != null)
            bookBackground.color = new Color(0.98f, 0.96f, 0.91f, 0.98f);

        if (titleText != null)
            titleText.text = emptyBookTitle;

        if (ingredientsText != null)
            ingredientsText.text = emptyBookText;

        if (effectText != null)
            effectText.text = "Назначь минимум 4 рецепта в инспекторе или через RecipeChecker.";

        if (pageText != null)
            pageText.text = "Страница 0 / 0";

        if (recipeIconImage != null)
        {
            recipeIconImage.sprite = null;
            recipeIconImage.enabled = false;
        }

        if (ingredientImages != null)
        {
            for (int i = 0; i < ingredientImages.Length; i++)
            {
                if (ingredientImages[i] == null)
                    continue;

                ingredientImages[i].sprite = null;
                ingredientImages[i].enabled = false;
            }
        }

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        if (nextButton != null)
            nextButton.interactable = currentIndex < runtimeEntries.Count - 1;

        if (previousButton != null)
            previousButton.interactable = currentIndex > 0;
    }

    private static int CompareEntriesByRecipeName(RecipeBookUiEntry left, RecipeBookUiEntry right)
    {
        string leftName = left != null && left.recipe != null ? left.recipe.name : string.Empty;
        string rightName = right != null && right.recipe != null ? right.recipe.name : string.Empty;
        return CompareNatural(leftName, rightName);
    }

    private static int CompareNatural(string left, string right)
    {
        int leftIndex = 0;
        int rightIndex = 0;

        while (leftIndex < left.Length && rightIndex < right.Length)
        {
            char leftChar = left[leftIndex];
            char rightChar = right[rightIndex];

            if (char.IsDigit(leftChar) && char.IsDigit(rightChar))
            {
                int leftNumber = ReadNumber(left, ref leftIndex);
                int rightNumber = ReadNumber(right, ref rightIndex);
                int numberCompare = leftNumber.CompareTo(rightNumber);
                if (numberCompare != 0)
                    return numberCompare;

                continue;
            }

            int charCompare = char.ToUpperInvariant(leftChar).CompareTo(char.ToUpperInvariant(rightChar));
            if (charCompare != 0)
                return charCompare;

            leftIndex++;
            rightIndex++;
        }

        return left.Length.CompareTo(right.Length);
    }

    private static int ReadNumber(string text, ref int index)
    {
        int value = 0;
        while (index < text.Length && char.IsDigit(text[index]))
        {
            value = value * 10 + (text[index] - '0');
            index++;
        }

        return value;
    }

    private string BuildIngredientText(RecipeData recipe)
    {
        if (recipe == null || recipe.ingredients == null || recipe.ingredients.Count == 0)
            return "Ингредиенты не указаны";

        string text = string.Empty;

        for (int i = 0; i < recipe.ingredients.Count; i++)
        {
            IngredientData ingredient = recipe.ingredients[i];

            if (ingredient == null)
                continue;

            text += "- " + ingredient.ingredientName.Trim();

            if (i < recipe.ingredients.Count - 1)
                text += "\n";
        }

        return text;
    }

    private string BuildDefaultDescription(RecipeData recipe)
    {
        return string.Empty;
    }

    private string BuildRecipeDescription(RecipeData recipe)
    {
        if (recipe != null && !string.IsNullOrWhiteSpace(recipe.description))
            return recipe.description;

        return BuildDefaultDescription(recipe);
    }
}
