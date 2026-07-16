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

    // --- NEW: DEFAULT LOADOUT STRUCT ---
    [System.Serializable]
    public struct DefaultSlotAssignment
    {
        public string slotID;
        public IngredientData ingredient;
    }

    [Header("Tutorial Settings")]
    public bool isInfiniteMode = false;

    [Header("System Settings")]
    public bool persistAcrossScenes = true; // Uncheck this in the tutorial scenes!

    [Header("Initial Loadout")]
    [Tooltip("Set your starting stock here in the Inspector.")]
    public List<StartingStock> initialInventory;

    [Header("Meta Progression")]
    public List<IngredientData> unlockedIngredients;

    [Header("Default Quick-Start Loadout")]
    [Tooltip("If booting directly into Scene 01, these items will automatically be assigned to these slots.")]
    public List<DefaultSlotAssignment> defaultQuickStartLoadout;

    // The Active Loadout: Key is slotID, Value is the assigned ingredient
    public Dictionary<string, IngredientData> activeLoadout = new Dictionary<string, IngredientData>();

    private Dictionary<IngredientData, int> currentStock = new Dictionary<IngredientData, int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
            InitializeStock();
            InitializeDefaultLoadout();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- NEW METHOD ---
    private void InitializeDefaultLoadout()
    {
        if (activeLoadout.Count == 0)
        {
            foreach (DefaultSlotAssignment assignment in defaultQuickStartLoadout)
            {
                if (assignment.ingredient != null)
                {
                    activeLoadout.Add(assignment.slotID, assignment.ingredient);
                }
            }
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
            if (isInfiniteMode == false)
            {
                currentStock[ingredient]--;
                Debug.Log("[Inventory] Dispensed " + ingredient.name + ". Remaining stock: " + currentStock[ingredient]);
            }
            else
            {
                Debug.Log("[Inventory] Infinite Mode Active. Dispensed " + ingredient.name + " without deducting stock.");
            }
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

    public void AddStock(IngredientData ingredient, int amount)
    {
        if (currentStock.ContainsKey(ingredient))
        {
            currentStock[ingredient] += amount;
        }
        else
        {
            currentStock.Add(ingredient, amount);
        }

        Debug.Log("[Inventory] Added " + amount + " " + ingredient.displayName + " to stock.");
    }

    public void SaveSlotAssignment(string slotID, IngredientData ingredient)
    {
        if (activeLoadout.ContainsKey(slotID))
        {
            activeLoadout[slotID] = ingredient;
        }
        else
        {
            activeLoadout.Add(slotID, ingredient);
        }
    }

    public void ClearSlotAssignment(string slotID)
    {
        if (activeLoadout.ContainsKey(slotID))
        {
            activeLoadout.Remove(slotID);
        }
    }

    public void RemoveIngredientFromAllSlots(IngredientData ingredient)
    {
        // We can't remove items from a dictionary while looping through it, 
        // so we track the keys to remove first.
        System.Collections.Generic.List<string> keysToRemove = new System.Collections.Generic.List<string>();

        foreach (System.Collections.Generic.KeyValuePair<string, IngredientData> kvp in activeLoadout)
        {
            if (kvp.Value == ingredient)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (string key in keysToRemove)
        {
            activeLoadout.Remove(key);
        }
    }
}