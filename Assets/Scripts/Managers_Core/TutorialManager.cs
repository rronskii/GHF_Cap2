using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    private enum TutorialState
    {
        None,
        IntroDialogue,
        WaitingForInventoryNav,
        WaitingForCardDraw,
        WaitingForOrderNav,
        WaitingForOrderTake,
        WaitingForCookingNav,
        WaitingForBellRing,  // NEW
        WaitingForServeNav,  // NEW
        WaitingForDishServe, // NEW
        Complete
    }

    private TutorialState currentState = TutorialState.None;

    private void Start()
    {
        Invoke(nameof(StartIntro), 1.5f);
    }

    private void OnEnable()
    {
        StationCameraController.OnStationChanged += HandleStationChanged;
        InventoryStation.OnTutorialCardDrawn += HandleCardDrawn;
        CustomerController.OnTutorialOrderTaken += HandleOrderTaken;
        WindowDishInteractable.OnTutorialDishServed += HandleDishServed;    // NEW
    }

    private void OnDisable()
    {
        StationCameraController.OnStationChanged -= HandleStationChanged;
        InventoryStation.OnTutorialCardDrawn -= HandleCardDrawn;
        CustomerController.OnTutorialOrderTaken -= HandleOrderTaken;
        WindowDishInteractable.OnTutorialDishServed -= HandleDishServed;    // NEW
    }

    private void StartIntro()
    {
        currentState = TutorialState.IntroDialogue;
        DialogueManager.Instance.StartDialogue(new string[]
        {
            "Welcome to Malinomnom! Let's get cooking.",
            "You can look around the jeep by moving your mouse to the edges of the screen, or by pressing A and D.",
            "To start, look to the LEFT to switch to the Inventory Station."
        }, () => { currentState = TutorialState.WaitingForInventoryNav; });
    }

    private void HandleStationChanged(int stationIndex)
    {
        // 0 = Cooking, 1 = Inventory, 2 = Order

        if (stationIndex == 1 && currentState == TutorialState.WaitingForInventoryNav)
        {
            currentState = TutorialState.None;
            DialogueManager.Instance.StartDialogue(new string[]
            {
                "This is the inventory.",
                "Here we store raw beef, raw hotdog, cooked rice, raw egg, and raw garlic.",
                "Go ahead, take an ingredient from any inventory by clicking on it."
            }, () => { currentState = TutorialState.WaitingForCardDraw; });
        }
        else if (stationIndex == 2 && currentState == TutorialState.WaitingForOrderNav)
        {
            currentState = TutorialState.None;
            DialogueManager.Instance.StartDialogue(new string[]
            {
                "This is the order window. Customers will line up here.",
                "Click on the customer at the front of the line to take their ticket."
            }, () => { currentState = TutorialState.WaitingForOrderTake; });
        }
        else if (stationIndex == 0 && currentState == TutorialState.WaitingForCookingNav)
        {
            currentState = TutorialState.None;
            DialogueManager.Instance.StartDialogue(new string[]
            {
                "Great! Now let's cook.",
                "Drag cards from your hand onto the stove or chopping board.",
                "You can pick up the knife and drop it onto ingredients on the chopping board to slice them.",
                "Place cooked ingredients next to each other on the stove to combine them, like making fried rice!",
                "Once you assemble the final dish on the Plate Station, hit the Service Bell!"
            }, () => { currentState = TutorialState.WaitingForBellRing; }); // UPDATED
        }
        else if (stationIndex == 2 && currentState == TutorialState.WaitingForServeNav)
        {
            currentState = TutorialState.None;
            DialogueManager.Instance.StartDialogue(new string[]
            {
                "There's the food!",
                "Click on the plated dish sitting on the counter to serve it to the matching ticket."
            }, () => { currentState = TutorialState.WaitingForDishServe; }); // NEW
        }
    }

    private void HandleCardDrawn()
    {
        if (currentState == TutorialState.WaitingForCardDraw)
        {
            currentState = TutorialState.None;
            DialogueManager.Instance.StartDialogue(new string[]
            {
                "Perfect! It's now in your hand.",
                "Look to the LEFT again to find the Order Station."
            }, () => { currentState = TutorialState.WaitingForOrderNav; });
        }
    }

    private void HandleOrderTaken()
    {
        if (currentState == TutorialState.WaitingForOrderTake)
        {
            currentState = TutorialState.None;
            DialogueManager.Instance.StartDialogue(new string[]
            {
                "Got it! The ticket is now active.",
                "Now go back to the Cooking Station behind you."
            }, () => { currentState = TutorialState.WaitingForCookingNav; });
        }
    }

    // --- NEW METHODS ---

    private void HandleBellRung()
    {
        if (currentState == TutorialState.WaitingForBellRing)
        {
            currentState = TutorialState.None;
            DialogueManager.Instance.StartDialogue(new string[]
            {
                "Order up!",
                "The dish has been sent to the window.",
                "Go back to the Order Station on the LEFT to serve it."
            }, () => { currentState = TutorialState.WaitingForServeNav; });
        }
    }

    private void HandleDishServed()
    {
        if (currentState == TutorialState.WaitingForDishServe)
        {
            currentState = TutorialState.None;
            DialogueManager.Instance.StartDialogue(new string[]
            {
                "Awesome! You completed your first order.",
                "Keep taking tickets and serving dishes to hit your daily quota.",
                "Good luck, Chef!"
            }, () => { currentState = TutorialState.Complete; });
        }
    }
}