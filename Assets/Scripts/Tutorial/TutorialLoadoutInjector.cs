using System.Collections.Generic;
using UnityEngine;

public class TutorialLoadoutInjector : MonoBehaviour
{
    [System.Serializable]
    public struct InjectedItem
    {
        public IngredientData ingredient;
        public int amount;
        [Tooltip("Leave empty if you don't want to auto-equip it to a counter slot.")]
        public string slotID; // e.g., "Slot_1", "Slot_2"
    }

    [Header("Override Settings")]
    [Tooltip("If true, wipes the inventory completely before injecting these exact amounts.")]
    public bool wipePreviousData = true;

    public List<InjectedItem> itemsToInject;

    // --- CHANGED TO AWAKE: Runs before the Inventory Stations call Start() ---
    private void Awake()
    {
        if (PlayerInventoryManager.Instance != null)
        {
            if (wipePreviousData)
            {
                // This clears the live background data (activeLoadout), NOT the default quick-start blueprint!
                PlayerInventoryManager.Instance.ClearAllData();
            }

            foreach (InjectedItem item in itemsToInject)
            {
                if (item.ingredient == null)
                {
                    Debug.LogWarning("[Tutorial Injector] Warning: An ingredient slot is empty in the Inspector list! Skipping.");
                    continue;
                }

                // 1. Unlock it in the meta progression
                if (!PlayerInventoryManager.Instance.unlockedIngredients.Contains(item.ingredient))
                {
                    PlayerInventoryManager.Instance.unlockedIngredients.Add(item.ingredient);
                }

                // 2. Add the stock amount
                PlayerInventoryManager.Instance.AddStock(item.ingredient, item.amount);

                // 3. Auto-assign to a food truck slot if an ID was provided
                if (!string.IsNullOrEmpty(item.slotID))
                {
                    PlayerInventoryManager.Instance.SaveSlotAssignment(item.slotID, item.ingredient);
                }
            }

            Debug.Log($"[Tutorial Injector] Successfully injected {itemsToInject.Count} ingredients before the stations spawned.");
        }
    }
}