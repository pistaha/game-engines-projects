using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Craft/Recipe")]
public class RecipeData : ScriptableObject
{
    [Header("Название результата")]
    public string resultName;

    [Header("Примечание")]
    [TextArea(2, 5)]
    public string note;

    [Header("Готовая картинка страницы книги")]
    public Sprite bookPageSprite;
    public float bookPageScale = 1f;
    public Vector2 bookPageOffset = Vector2.zero;

    [Header("Ингредиенты рецепта")]
    public List<IngredientData> ingredients = new List<IngredientData>();
}
