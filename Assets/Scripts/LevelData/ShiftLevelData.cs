using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Shift Data", menuName = "Cooking/Shift Data")]
public class ShiftLevelData : ScriptableObject
{
    public string shiftName = "Day 1";

    [Header("Menu Board")]
    public List<DishData> activeDishes;

    [Header("Validation Check")]
    [Tooltip("The raw base ingredients the player MUST have equipped in their slots to cook the active dishes.")]
    public List<IngredientData> requiredBaseIngredients;
}