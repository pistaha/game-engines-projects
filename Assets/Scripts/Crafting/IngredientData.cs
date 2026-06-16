using UnityEngine;

[CreateAssetMenu(fileName = "Ingredient", menuName = "Craft/Ingredient")]
public class IngredientData : ScriptableObject
{
    // Данные одного ингредиента. На этот ассет ссылается бутылка через свой игровой скрипт.
    public string ingredientName;
    public Sprite icon;
}
