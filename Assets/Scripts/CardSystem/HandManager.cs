using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class HandManager : MonoBehaviour
{
    public static HandManager Instance;

    [Header("Hand References")]
    public RectTransform handContainer;

    [Header("Hand Settings")]
    public int maxCards = 5;
    public float cardSpacing = 220f;
    public float defaultYPosition = -65f;
    public Vector2 spawnOffset = new Vector2(50f, 100f);

    [Header("Tutorial Settings")]
    public bool enforceSingleIngredientLimit = false;
    public bool ignoreStationUnlocks = false; // --- NEW: Protects tutorial locks from the camera! ---

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
        bool hideForDialogue = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;
        bool hideForStation = (currentStationIndex == 2);

        float targetAlpha = (hideForDialogue || hideForStation) ? 0f : 1f;

        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * 10f);
        canvasGroup.blocksRaycasts = (targetAlpha > 0.5f);

        if (hideForDialogue || targetAlpha < 0.5f) return;
        if (Time.timeScale == 0f) return;

        for (int i = 0; i < currentCards.Count; i++)
        {
            if (i > 8) break;

            if (!currentCards[i].isInteractable) continue;

            KeyCode key = KeyCode.Alpha1 + i;

            if (Input.GetKeyDown(key)) currentCards[i].SimulateKeyDown();
            else if (Input.GetKey(key)) currentCards[i].SimulateKeyHold();
            else if (Input.GetKeyUp(key)) currentCards[i].SimulateKeyUp();
        }
    }

    public bool TryDrawCard(GameObject cardPrefab)
    {
        if (currentCards.Count >= maxCards) return false;
        if (cardPrefab == null) return false;

        if (enforceSingleIngredientLimit)
        {
            CardGridPlacer placerCheck = cardPrefab.GetComponent<CardGridPlacer>();
            if (placerCheck != null && GetCountOfIngredient(placerCheck.ingredientData) >= 1) return false;
        }

        GameObject newCardObj = Instantiate(cardPrefab, handContainer);
        CardDragUI newCardScript = newCardObj.GetComponent<CardDragUI>();
        currentCards.Add(newCardScript);

        UpdateCardPositions(newCardScript);
        return true;
    }

    public int GetCountOfIngredient(IngredientData data)
    {
        int count = 0;
        foreach (CardDragUI card in currentCards)
        {
            if (card != null)
            {
                CardGridPlacer placer = card.GetComponent<CardGridPlacer>();
                if (placer != null && placer.ingredientData == data) count++;
            }
        }
        return count;
    }

    // --- REVERTED TO CLEAN LOCKING ---
    public CardDragUI GetCard(IngredientData data)
    {
        foreach (CardDragUI card in currentCards)
        {
            if (card != null)
            {
                CardGridPlacer placer = card.GetComponent<CardGridPlacer>();
                if (placer != null && placer.ingredientData == data) return card;
            }
        }
        return null;
    }

    public void LockAllCardsExcept(IngredientData allowedData)
    {
        foreach (CardDragUI card in currentCards)
        {
            if (card != null)
            {
                CardGridPlacer placer = card.GetComponent<CardGridPlacer>();
                card.isInteractable = (placer != null && placer.ingredientData == allowedData);
            }
        }
    }

    public void UnlockAllCards()
    {
        foreach (CardDragUI card in currentCards)
        {
            if (card != null) card.isInteractable = true;
        }
    }
    // ---------------------------------

    private void UpdateCardPositions(CardDragUI freshlyDrawnCard = null)
    {
        int cardCount = currentCards.Count;
        float totalWidth = (cardCount - 1) * cardSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < cardCount; i++)
        {
            CardDragUI card = currentCards[i];
            float targetX = startX + (i * cardSpacing);
            Vector2 finalTargetPos = new Vector2(targetX, defaultYPosition);

            card.SetHandPosition(finalTargetPos);

            if (card == freshlyDrawnCard)
            {
                card.GetComponent<RectTransform>().anchoredPosition = finalTargetPos + spawnOffset;
            }
        }
    }

    public void RemoveCard(CardDragUI cardToRemove)
    {
        currentCards.Remove(cardToRemove);
        UpdateCardPositions();
    }

    public void UpdateStationState(int stationIndex)
    {
        currentStationIndex = stationIndex;

        bool canDrag = (stationIndex == 0 || stationIndex == 1);
        foreach (CardDragUI card in currentCards)
        {
            // --- NEW: Only override interactability if the tutorial isn't actively locking things! ---
            if (!ignoreStationUnlocks)
            {
                card.isInteractable = canDrag;
            }

            if (card.isDragging) card.CancelDrag();
        }
    }

    public void RefundAllCards()
    {
        List<CardDragUI> cardsToRefund = new List<CardDragUI>(currentCards);
        foreach (CardDragUI card in cardsToRefund)
        {
            if (card != null)
            {
                CardGridPlacer placer = card.GetComponent<CardGridPlacer>();
                if (placer != null) placer.TriggerRefund();
            }
        }
    }
}