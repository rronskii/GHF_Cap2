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

    private void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    // UPDATED: Now accepts a specific pool of cards passed directly from the clicked station
    public void DrawCardFromPool(GameObject[] cardPool)
    {
        if (currentCards.Count >= maxCards) return; // Hand is full
        if (cardPool == null || cardPool.Length == 0) return; // Guard clause for empty station arrays

        // 1. Pick a random card prefab from the provided pool
        int randomIndex = Random.Range(0, cardPool.Length);
        GameObject newCardObj = Instantiate(cardPool[randomIndex], handContainer);

        CardDragTransition newCardScript = newCardObj.GetComponent<CardDragTransition>();
        currentCards.Add(newCardScript);

        // 2. Recalculate target positions for EVERY card so they slide to make room
        UpdateCardPositions(newCardScript);
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
    public void UpdateStationState(int stationIndex)
    {
        // 0 = Cooking, 1 = Inventory, 2 = Order

        // Handle Visibility
        if (stationIndex == 2)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        // Handle Interactability
        bool canDrag = (stationIndex == 0); // Only interactable at Cooking Station
        foreach (CardDragTransition card in currentCards)
        {
            card.isInteractable = canDrag;
        }
    }
}