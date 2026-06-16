using UnityEngine;

[CreateAssetMenu(fileName = "Ingredient", menuName = "Craft/Ingredient")]
public class IngredientData : ScriptableObject
{
    public string ingredientName;
    public Sprite icon;
}