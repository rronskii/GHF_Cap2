using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewIngredient", menuName = "Luto/Ingredient Data")]

[System.Serializable]
public class RecipeCombo
{
    public IngredientData partnerIngredient; // The other ingredient needed (e.g., Chopped Garlic)
    public GameObject resultPrefab;          // The combined result (e.g., Fried Rice)
    public float cookTime = 5f;              // Time it takes to lock
    public bool spawnOnPartnerTile = false;  // False = spawn where dropped. True = spawn on the partner's tile.
}

public class IngredientData : ScriptableObject
{
    [Header("Basic Info")]
    public string ingredientID;
    public string displayName;

    [Header("Grid Shape")]
    public Vector2Int[] shapeOffsets;
    public GameObject worldPrefab;

    [Header("Economy")]
    public int basePoints = 0; // Unprocessed items can remain 0

    // --- NEW ADDITIONS FOR COMPATIBILITY & COOKING ---
    [Header("Station Compatibility")]
    [Tooltip("Which stations can this item be placed on?")]
    public List<StationType> validStations;

    [Header("Cooking (Optional)")]
    public bool isCookable;
    public float cookTime = 5f;
    public GameObject cookedPrefab;
    public bool instantCook = false;

    [Header("Chopping Info")]
    public bool isChoppable = false;
    public GameObject choppedPrefab;

    [Header("Burn Settings")]
    public bool canBurn = false;
    public float burnTime = 5f;
    public GameObject burntPrefab;

    // Add this to your existing fields:
    public System.Collections.Generic.List<RecipeCombo> combinations;
}