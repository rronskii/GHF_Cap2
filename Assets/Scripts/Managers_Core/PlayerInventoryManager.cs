using System.Collections.Generic;
using UnityEngine;

public class PlayerInventoryManager : MonoBehaviour
{
    public static PlayerInventoryManager Instance;

    [System.Serializable]
    public class StartingStock
    {
        public IngredientData ingredientData;
        public int amount;
    }

    [Header("Initial Loadout")]
    [Tooltip("Set your starting stock here in the Inspector.")]
    public List<StartingStock> initialInventory;

    // The active dictionary we use during gameplay for fast lookups
    private Dictionary<IngredientData, int> currentStock = new Dictionary<IngredientData, int>();

    private void Awake()
    {
        // Standard Singleton pattern with persistence
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keeps this alive across all scenes!
            InitializeStock();
        }
        else
        {
            Destroy(gameObject); // Destroys duplicates if you reload the main scene
        }
    }

    // Called once when the game first boots up
    private void InitializeStock()
    {
        foreach (StartingStock stockItem in initialInventory)
        {
            if (stockItem.ingredientData != null && !currentStock.ContainsKey(stockItem.ingredientData))
            {
                currentStock.Add(stockItem.ingredientData, stockItem.amount);
            }
        }
    }

    public bool HasStock(IngredientData ingredient)
    {
        return currentStock.ContainsKey(ingredient) && currentStock[ingredient] > 0;
    }

    public void ConsumeStock(IngredientData ingredient)
    {
        if (HasStock(ingredient))
        {
            currentStock[ingredient]--;
            Debug.Log($"[Inventory] Dispensed {ingredient.name}. Remaining stock: {currentStock[ingredient]}");
        }
    }

    // Optional: Useful for your Shop UI later!
    public int GetStockCount(IngredientData ingredient)
    {
        return currentStock.ContainsKey(ingredient) ? currentStock[ingredient] : 0;
    }

    public void RefundStock(IngredientData ingredient)
    {
        if (currentStock.ContainsKey(ingredient))
        {
            currentStock[ingredient]++;
            Debug.Log($"[Inventory] Refunded {ingredient.name}. Total back in storage: {currentStock[ingredient]}");
        }
    }
}