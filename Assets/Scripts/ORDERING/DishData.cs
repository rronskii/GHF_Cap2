using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Dish", menuName = "Cooking/Dish Data")]
public class DishData : ScriptableObject
{
    public string dishName;
    public List<IngredientData> requiredIngredients; // The exact ingredients needed
    public GameObject windowPrefab;                 // The 3D model spawned on the counter window\
    [Header("Economy")]
    public int basePrice = 0; // e.g., 50 for Silog, 100 for Tapsilog

    // Helper method to check if a plate's contents match this recipe perfectly
    public bool MatchesIngredients(List<IngredientData> plateIngredients)
    {
        if (plateIngredients.Count != requiredIngredients.Count) return false;

        // Create temporary lists to track and compare without destroying original data
        List<IngredientData> checkList = new List<IngredientData>(requiredIngredients);

        foreach (IngredientData ingredient in plateIngredients)
        {
            if (checkList.Contains(ingredient))
            {
                checkList.Remove(ingredient); // Found a match, cross it off
            }
            else
            {
                return false; // Found an ingredient that doesn't belong in this recipe
            }
        }

        return checkList.Count == 0; // If all ingredients crossed off, it's a perfect match!
    }
}