using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Shift Data", menuName = "Cooking/Shift Data")]
public class ShiftLevelData : ScriptableObject
{
    [Header("Level Info")]
    public string shiftName = "Day 1";

    [Header("Economy & Pacing")]
    [Tooltip("How much money the player needs to make to pass the day.")]
    public int dailyQuota = 100;

    [Tooltip("How fast customers spawn during this shift.")]
    public float customerSpawnRate = 5f;

    [Header("Menu Board")]
    [Tooltip("The recipes customers are allowed to order during this level.")]
    public List<DishData> activeDishes;

    [Header("Validation Check")]
    [Tooltip("The raw base ingredients the player MUST have equipped in their slots to cook the active dishes.")]
    public List<IngredientData> requiredBaseIngredients;
}