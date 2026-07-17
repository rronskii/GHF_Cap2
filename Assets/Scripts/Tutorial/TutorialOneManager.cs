using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialOneManager : MonoBehaviour
{
    [Header("Tutorial Data")]
    public DishData tutSilogDish;

    public IngredientData tutGarlic;
    public IngredientData tutEgg;
    public IngredientData tutRice;

    [Header("Intermediate Cooking States")]
    public IngredientData tutCookedEgg;
    public IngredientData tutChoppedGarlic;
    public IngredientData tutSauteedGarlic;
    public IngredientData tutFriedRice;

    [Header("UI & Visuals")]
    public GameObject bouncingArrowPrefab;
    [Tooltip("Increase this number to move the arrow higher up from the card")]
    public float arrowYOffset = 220f;
    public GameObject tutorialCompletePanel;

    private int practiceTicketsRemaining = 3;
    private bool isPracticePhase = false;

    private void Start()
    {
        if (tutorialCompletePanel != null) tutorialCompletePanel.SetActive(false);

        if (StationCameraController.Instance != null)
        {
            StationCameraController.Instance.isLocked = true;
            StationCameraController.Instance.allowLooping = false;
            StationCameraController.Instance.maxStationIndex = 1;
        }

        if (HandManager.Instance != null) HandManager.Instance.enforceSingleIngredientLimit = true;

        Invoke("StartIntro", 1.5f);
    }

    private void StartIntro()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(new string[]
            {
                "Welcome to your first day, Chef!",
                "Today we are learning the absolute basics.",
                "Look to the LEFT (Press A) to view your Inventory."
            }, GoToInventoryStep);
        }
    }

    private void GoToInventoryStep()
    {
        if (StationCameraController.Instance != null) StationCameraController.Instance.isLocked = false;
        StationCameraController.OnStationChanged += HandleFirstStationChange;
    }

    private void HandleFirstStationChange(int stationIndex)
    {
        if (stationIndex == 1)
        {
            StationCameraController.OnStationChanged -= HandleFirstStationChange;

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(new string[]
                {
                    "Click to draw 1 Raw Garlic, 1 Raw Egg, and 1 Cooked Rice into your hand.",
                    "Then look back to the RIGHT (Press D) at the Cooking Station."
                }, GiveTicketStep);
            }
        }
    }

    private void GiveTicketStep()
    {
        StationCameraController.OnStationChanged += HandleReturnToCooking;
    }

    private void HandleReturnToCooking(int stationIndex)
    {
        if (stationIndex == 0)
        {
            if (!HasRequiredIngredients())
            {
                if (StationCameraController.Instance != null) StationCameraController.Instance.ForceGoToStation(1);
                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.StartDialogue(new string[]
                    {
                        "Hey! You haven't gotten everything you need yet.",
                        "Draw exactly 1 Garlic, 1 Egg, and 1 Rice before returning!"
                    }, null);
                }
                return;
            }

            StationCameraController.OnStationChanged -= HandleReturnToCooking;
            if (HandManager.Instance != null) HandManager.Instance.ignoreStationUnlocks = true;

            if (StationCameraController.Instance != null) StationCameraController.Instance.maxStationIndex = 0;
            if (TutorialOrderManager.Instance != null) TutorialOrderManager.Instance.SpawnDummyTicket(tutSilogDish);

            // --- MOVED: Ticket hovering instruction is now here ---
            StartCoroutine(GuidedCookingRoutine());
        }
    }

    private bool HasRequiredIngredients()
    {
        if (HandManager.Instance == null) return false;
        return (HandManager.Instance.GetCountOfIngredient(tutGarlic) >= 1 &&
                HandManager.Instance.GetCountOfIngredient(tutEgg) >= 1 &&
                HandManager.Instance.GetCountOfIngredient(tutRice) >= 1);
    }

    private IEnumerator GuidedCookingRoutine()
    {
        bool dialogueDone = false;

        // 1. COOK EGG
        DialogueManager.Instance.StartDialogue(new string[] {
            "I just gave you a ticket for a Silog.",
            "You can hover over tickets at the top of the screen anytime to see exactly what you need to make.",
            "Let's start by cooking the Egg.",
            "Drag it from your hand onto the Frying Pan."
        }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        HandManager.Instance.LockAllCardsExcept(tutEgg);
        CardDragUI eggCard = HandManager.Instance.GetCard(tutEgg);
        GameObject arrow = SpawnArrowOnCard(eggCard);

        // --- NEW: Wait until the card is actually consumed (placed on the station)
        // The arrow will move with the card and naturally destroy itself when the card is consumed!
        while (eggCard != null) yield return null;

        // Wait for it to finish cooking
        while (true)
        {
            Draggable3DItem item = FindItemInScene(tutCookedEgg);
            if (item != null && !item.isLocked) break;
            yield return null;
        }

        // 2. PLATE EGG
        dialogueDone = false;
        DialogueManager.Instance.StartDialogue(new string[] { "Great! Now drag the cooked egg onto the Plate." }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        while (!IsItemOnPlate(tutCookedEgg)) yield return null;

        // 3. CHOP GARLIC
        dialogueDone = false;
        DialogueManager.Instance.StartDialogue(new string[] { "Next, let's prep the garlic.", "Drag the Garlic onto the chopping board.", "Then, drag the Knife over it to slice it!" }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        HandManager.Instance.LockAllCardsExcept(tutGarlic);
        CardDragUI garlicCard = HandManager.Instance.GetCard(tutGarlic);
        arrow = SpawnArrowOnCard(garlicCard);

        // Wait for the card to be placed
        while (garlicCard != null) yield return null;

        while (true)
        {
            Draggable3DItem item = FindItemInScene(tutChoppedGarlic);
            if (item != null && !item.isLocked) break;
            yield return null;
        }

        // 4. SAUTEE GARLIC
        dialogueDone = false;
        DialogueManager.Instance.StartDialogue(new string[] { "Now drag the chopped garlic to the stove to sauté it." }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        while (true)
        {
            Draggable3DItem item = FindItemInScene(tutSauteedGarlic);
            if (item != null && !item.isLocked) break;
            yield return null;
        }

        // 5. COMBINE RICE
        dialogueDone = false;
        DialogueManager.Instance.StartDialogue(new string[] { "While that's hot, drag your Cooked Rice card directly onto the stove next to the garlic to combine them!" }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        HandManager.Instance.LockAllCardsExcept(tutRice);
        CardDragUI riceCard = HandManager.Instance.GetCard(tutRice);
        arrow = SpawnArrowOnCard(riceCard);

        // Wait for the card to be placed
        while (riceCard != null) yield return null;

        while (true)
        {
            Draggable3DItem item = FindItemInScene(tutFriedRice);
            if (item != null && !item.isLocked) break;
            yield return null;
        }

        // 6. PLATE RICE & SERVE
        dialogueDone = false;
        DialogueManager.Instance.StartDialogue(new string[] { "Perfect! Drag the Fried Rice onto the plate.", "Then ring the Service Bell to serve the ticket!" }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        ServiceBell.OnTutorialBellRung += HandleFirstTutorialComplete;
    }

    private GameObject SpawnArrowOnCard(CardDragUI card)
    {
        if (card == null || bouncingArrowPrefab == null) return null;
        GameObject arrow = Instantiate(bouncingArrowPrefab, card.transform);
        arrow.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, arrowYOffset);
        return arrow;
    }

    private Draggable3DItem FindItemInScene(IngredientData targetData)
    {
        Draggable3DItem[] items = FindObjectsOfType<Draggable3DItem>();
        foreach (Draggable3DItem item in items)
        {
            if (item.myData == targetData) return item;
        }
        return null;
    }

    private bool IsItemOnPlate(IngredientData targetData)
    {
        PlateStation plate = FindObjectOfType<PlateStation>();
        if (plate == null) return false;

        List<IngredientData> ingredients = plate.GetIngredientsOnPlate();
        return ingredients.Contains(targetData);
    }

    private void HandleFirstTutorialComplete()
    {
        if (!isPracticePhase)
        {
            isPracticePhase = true;

            if (StationCameraController.Instance != null) StationCameraController.Instance.maxStationIndex = 1;
            if (HandManager.Instance != null)
            {
                HandManager.Instance.enforceSingleIngredientLimit = false;
                HandManager.Instance.ignoreStationUnlocks = false;
                HandManager.Instance.UnlockAllCards();
            }

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(new string[]
                {
                    "Excellent work! You've mastered the basics.",
                    "Now, let's build some muscle memory. I am going to give you 3 more Silog tickets to complete."
                }, StartPracticePhase);
            }
        }
        else
        {
            practiceTicketsRemaining--;

            if (practiceTicketsRemaining <= 0)
            {
                ServiceBell.OnTutorialBellRung -= HandleFirstTutorialComplete;
                // --- MOVED: Cookbook explanation is now here in the final dialogue box ---
                ShowWinScreen();
            }
        }
    }

    private void StartPracticePhase()
    {
        if (TutorialOrderManager.Instance != null)
        {
            TutorialOrderManager.Instance.SpawnDummyTicket(tutSilogDish);
            TutorialOrderManager.Instance.SpawnDummyTicket(tutSilogDish);
            TutorialOrderManager.Instance.SpawnDummyTicket(tutSilogDish);
        }
    }

    private void ShowWinScreen()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(new string[]
            {
                "Well done! You have completed all your tickets.",
                "Remember, if you ever forget how to make something, you can open your Cookbook anytime by pressing Esc or P!",
                "You are ready for the next challenge."
            }, () => { if (tutorialCompletePanel != null) tutorialCompletePanel.SetActive(true); });
        }
    }
}