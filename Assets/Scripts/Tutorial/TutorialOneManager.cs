using UnityEngine;

public class TutorialOneManager : MonoBehaviour
{
    [Header("Tutorial Data")]
    public DishData tutSilogDish;

    [Header("End Screen")]
    public GameObject tutorialCompletePanel;

    private int practiceTicketsRemaining = 3;
    private bool isPracticePhase = false;

    private void Start()
    {
        if (tutorialCompletePanel != null)
        {
            tutorialCompletePanel.SetActive(false);
        }
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
        StationCameraController.OnStationChanged += HandleFirstStationChange;
    }

    private void HandleFirstStationChange(int stationIndex)
    {
        if (stationIndex == 1) // Inventory Station
        {
            StationCameraController.OnStationChanged -= HandleFirstStationChange;

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(new string[]
                {
                    "Here are your ingredients. Because this is a tutorial, you have infinite stock!",
                    "Click to draw Raw Garlic, Raw Egg, and Cooked Rice into your hand.",
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
        if (stationIndex == 0) // Cooking Station
        {
            StationCameraController.OnStationChanged -= HandleReturnToCooking;

            if (TutorialOrderManager.Instance != null)
            {
                TutorialOrderManager.Instance.SpawnDummyTicket(tutSilogDish);
            }

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(new string[]
                {
                    "I just gave you a ticket for a Silog.",
                    "1. Place Garlic on the Chopping Board and use the Knife to chop it.",
                    "2. Place the Egg on the Stove to cook it.",
                    "3. Place Cooked Rice on the Stove next to the cooked Egg and chopped Garlic to combine them!",
                    "4. Finally, drag the finished Silog onto the Plate and right-click the plate to serve it."
                }, WaitForTicketClear);
            }
        }
    }

    private void WaitForTicketClear()
    {
        TutorialOrderManager.OnTutorialTicketCleared += HandleFirstTutorialComplete;
    }

    private void HandleFirstTutorialComplete()
    {
        if (!isPracticePhase)
        {
            isPracticePhase = true;

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(new string[]
                {
                    "Excellent work! You've mastered the basics.",
                    "Now, let's build some muscle memory. I am going to give you 3 more Silog tickets.",
                    "Complete them all to finish this lesson!"
                }, StartPracticePhase);
            }
        }
        else
        {
            practiceTicketsRemaining--;

            if (practiceTicketsRemaining <= 0)
            {
                TutorialOrderManager.OnTutorialTicketCleared -= HandleFirstTutorialComplete;
                ShowWinScreen();
            }
        }
    }

    private void StartPracticePhase()
    {
        if (TutorialOrderManager.Instance != null)
        {
            // Spawn 3 tickets at once
            TutorialOrderManager.Instance.SpawnDummyTicket(tutSilogDish);
            TutorialOrderManager.Instance.SpawnDummyTicket(tutSilogDish);
            TutorialOrderManager.Instance.SpawnDummyTicket(tutSilogDish);
        }
    }

    private void ShowWinScreen()
    {
        if (tutorialCompletePanel != null)
        {
            tutorialCompletePanel.SetActive(true);
        }
    }
}