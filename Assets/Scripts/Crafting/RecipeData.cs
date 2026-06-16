using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Craft/Recipe")]
public class RecipeData : ScriptableObject
{
    // Данные рецепта. Новый рецепт можно добавить ассетом без переписывания проверяющего скрипта.
    [Header("Название результата")]
    public string resultName;

    [TextArea(2, 5)]
    public string description;

    [Header("Готовая картинка страницы книги")]
    public Sprite bookPageSprite;
    public float bookPageScale = 1f;
    public Vector2 bookPageOffset = Vector2.zero;

    [Header("Ингредиенты рецепта")]
    public List<IngredientData> ingredients = new List<IngredientData>();

    [Header("Проверка")]
    public bool ingredientOrderMatters;
    // Если значение больше нуля, рецепт засчитывается только после долгого перемешивания.
    public float requiredStirSeconds;

    [Header("Данные успешной реакции")]
    public Color successLiquidColor = Color.green;
    public AudioClip successSound;
}
