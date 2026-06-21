using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class HandManager : MonoBehaviour
{
    public static HandManager Instance; // Singleton so storage stations and camera can find it

    [Header("Hand References")]
    public RectTransform handContainer; // Drag the parent UI panel holding the cards here

    [Header("Hand Settings")]
    public int maxCards = 5;
    public float cardSpacing = 220f; // Width of card (200) + 20px gap
    public float defaultYPosition = -65f;
    public Vector2 spawnOffset = new Vector2(50f, 100f); // Spawns slightly right and higher

    private List<CardDragTransition> currentCards = new List<CardDragTransition>();
    private CanvasGroup canvasGroup;

    private int currentStationIndex = 0;

    private void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        // NEW: Smoothly handle visibility based on Dialogue AND Station views
        bool hideForDialogue = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;
        bool hideForStation = (currentStationIndex == 2); // Hide at Order Window

        float targetAlpha = (hideForDialogue || hideForStation) ? 0f : 1f;

        // Smooth fade in/out
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * 10f);
        canvasGroup.blocksRaycasts = (targetAlpha > 0.5f);
    }

    // UPDATED: Now accepts a specific pool of cards passed directly from the clicked station
    // NEW: Replaces the old array-based DrawCardFromPool method
    public bool TryDrawCard(GameObject cardPrefab)
    {
        if (currentCards.Count >= maxCards)
        {
            Debug.Log("[HandManager] Hand is full! Cannot draw card.");
            return false;
        }

        if (cardPrefab == null) return false;

        GameObject newCardObj = Instantiate(cardPrefab, handContainer);
        CardDragTransition newCardScript = newCardObj.GetComponent<CardDragTransition>();
        currentCards.Add(newCardScript);

        UpdateCardPositions(newCardScript);

        return true; // Successfully added to hand
    }

    private void UpdateCardPositions(CardDragTransition freshlyDrawnCard = null)
    {
        int cardCount = currentCards.Count;
        float totalWidth = (cardCount - 1) * cardSpacing;
        float startX = -totalWidth / 2f; // This centers the fanned hand perfectly

        for (int i = 0; i < cardCount; i++)
        {
            CardDragTransition card = currentCards[i];
            float targetX = startX + (i * cardSpacing);
            Vector2 finalTargetPos = new Vector2(targetX, defaultYPosition);

            card.SetHandPosition(finalTargetPos); // Tell the card where its new home is

            // If this is the brand new card, physically move it to the offset starting point so it slides in
            if (card == freshlyDrawnCard)
            {
                card.GetComponent<RectTransform>().anchoredPosition = finalTargetPos + spawnOffset;
            }
        }
    }

    public void RemoveCard(CardDragTransition cardToRemove)
    {
        currentCards.Remove(cardToRemove);
        UpdateCardPositions(); // Recalculate so the remaining cards slide together to close the gap
    }

    // Called by the Camera Controller when shifting stations
    // Called by the Camera Controller when shifting stations
    public void UpdateStationState(int stationIndex)
    {
        currentStationIndex = stationIndex;

        // NEW: Cards should be interactable at the Stove (0) AND the Inventory (1) so we can return them!
        bool canDrag = (stationIndex == 0 || stationIndex == 1);
        foreach (CardDragTransition card in currentCards)
        {
            card.isInteractable = canDrag;
        }
    }
}