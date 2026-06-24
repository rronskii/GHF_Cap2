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

    private List<CardDragUI> currentCards = new List<CardDragUI>();
    private CanvasGroup canvasGroup;

    public int currentStationIndex = 0;

    private void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        // Smoothly handle visibility based on Dialogue AND Station views
        bool hideForDialogue = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;
        bool hideForStation = (currentStationIndex == 2); // Hide at Order Window

        float targetAlpha = (hideForDialogue || hideForStation) ? 0f : 1f;

        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * 10f);
        canvasGroup.blocksRaycasts = (targetAlpha > 0.5f);

        // --- NEW: HOTKEY DRAGGING ---
        // Do not allow hotkeys if the UI is currently hidden or fading out
        if (hideForDialogue || targetAlpha < 0.5f) return;

        if (Time.timeScale == 0f) return;

        for (int i = 0; i < currentCards.Count; i++)
        {
            // Maps Index 0 to Key 1, Index 1 to Key 2, etc. (Safely caps at 9)
            if (i > 8) break;
            KeyCode key = KeyCode.Alpha1 + i;

            if (Input.GetKeyDown(key))
            {
                currentCards[i].SimulateKeyDown();
            }
            else if (Input.GetKey(key))
            {
                currentCards[i].SimulateKeyHold();
            }
            else if (Input.GetKeyUp(key))
            {
                currentCards[i].SimulateKeyUp();
            }
        }
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
        CardDragUI newCardScript = newCardObj.GetComponent<CardDragUI>();
        currentCards.Add(newCardScript);

        UpdateCardPositions(newCardScript);

        return true; // Successfully added to hand
    }

    private void UpdateCardPositions(CardDragUI freshlyDrawnCard = null)
    {
        int cardCount = currentCards.Count;
        float totalWidth = (cardCount - 1) * cardSpacing;
        float startX = -totalWidth / 2f; // This centers the fanned hand perfectly

        for (int i = 0; i < cardCount; i++)
        {
            CardDragUI card = currentCards[i];
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

    public void RemoveCard(CardDragUI cardToRemove)
    {
        currentCards.Remove(cardToRemove);
        UpdateCardPositions(); // Recalculate so the remaining cards slide together to close the gap
    }

    // Called by the Camera Controller when shifting stations
    // Called by the Camera Controller when shifting stations
    public void UpdateStationState(int stationIndex)
    {
        currentStationIndex = stationIndex;

        bool canDrag = (stationIndex == 0 || stationIndex == 1);
        foreach (CardDragUI card in currentCards)
        {
            card.isInteractable = canDrag;

            // NEW: The manager forcefully cancels any active drags when the camera shifts
            if (card.isDragging)
            {
                card.CancelDrag();
            }
        }
    }
}