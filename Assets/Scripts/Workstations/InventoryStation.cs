using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InventoryStation : MonoBehaviour
{
    [Header("Station Inventory")]
    [Tooltip("The specific card prefab this storage container dispenses")]
    public GameObject stationCardPrefab;

    public static event Action OnTutorialCardDrawn;

    private IngredientData myIngredientData;

    private void Start()
    {
        if (stationCardPrefab != null)
        {
            CardGridPlacer cardScript = stationCardPrefab.GetComponent<CardGridPlacer>();
            if (cardScript != null)
            {
                myIngredientData = cardScript.ingredientData;
            }
        }
    }

    private void OnMouseDown()
    {
        if (Time.timeScale == 0f) return;

        // NEW BUGFIX: Block all clicks if the tutorial dialogue is currently active!
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        if (HandManager.Instance == null || PlayerInventoryManager.Instance == null || myIngredientData == null) return;

        // 1. Ask the persistent manager if we have any left
        if (!PlayerInventoryManager.Instance.HasStock(myIngredientData))
        {
            Debug.LogWarning($"[Inventory Station] EMPTY! No more {myIngredientData.name} left in storage.");
            return;
        }

        // 2. Try to put the card in the player's hand
        bool drawnSuccessfully = HandManager.Instance.TryDrawCard(stationCardPrefab);

        // 3. ONLY deduct from the persistent inventory if it successfully went into the hand
        if (drawnSuccessfully)
        {
            PlayerInventoryManager.Instance.ConsumeStock(myIngredientData);
            OnTutorialCardDrawn?.Invoke();
        }
    }

    // NEW: Lets the dragged card verify if this is its matching home
    public IngredientData GetStationIngredient()
    {
        return myIngredientData;
    }
}