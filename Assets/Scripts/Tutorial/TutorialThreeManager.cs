using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialThreeManager : MonoBehaviour
{
    [Header("Tutorial Data")]
    public DishData tutSilogDish;

    [Header("Level Transition")]
    public string nextSceneName = "01_MainLevel"; // Where they go after the tutorial
    public GameObject tutorialCompletePanel;

    private bool hasOrderBeenTaken = false;
    private int ticketsServed = 0;
    private int bellsRung = 0;
    private bool hasExplainedCloseEarly = false;

    private void Start()
    {
        if (tutorialCompletePanel != null) tutorialCompletePanel.SetActive(false);

        // Trap them in the cooking station initially
        if (StationCameraController.Instance != null)
        {
            StationCameraController.Instance.isLocked = true;
            StationCameraController.Instance.allowLooping = false;
            StationCameraController.Instance.maxStationIndex = 0;
            StationCameraController.Instance.ForceGoToStation(0);
        }

        // Subscribe to our new OrderManager events
        OrderManager.OnTutorialOrderTaken += HandleOrderTaken;
        OrderManager.OnTutorialTicketServed += HandleTicketServed;
        ServiceBell.OnTutorialBellRung += HandleTutorialBell;

        StartCoroutine(TutorialThreeFlowRoutine());
    }

    private IEnumerator TutorialThreeFlowRoutine()
    {
        bool dialogueDone = false;

        // ==========================================
        // STEP 1: MOVE TO WINDOW STATION
        // ==========================================
        yield return new WaitForSeconds(1.5f);

        // UPDATED: Changed dialogue to Left (A) since looping backwards from 0 goes to 2 (Window)
        DialogueManager.Instance.StartDialogue(new string[] {
            "Alright, it's time to open shop!",
            "Look to the LEFT (Press A) to move to the Window Station."
        }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        // Unlock the camera so they can look at the window
        if (StationCameraController.Instance != null)
        {
            StationCameraController.Instance.isLocked = false;

            // --- THE FIX: Re-enable looping so the camera can wrap around to the Window! ---
            StationCameraController.Instance.allowLooping = true;
            StationCameraController.Instance.maxStationIndex = 2;
        }

        // Wait until they physically arrive at the Window Station
        while (StationCameraController.Instance.currentStationIndex != 2) yield return null;

        // ==========================================
        // STEP 2: QUOTA SYSTEM EXPLANATION
        // ==========================================
        dialogueDone = false;

        // Trap them at the window temporarily
        StationCameraController.Instance.isLocked = true;

        DialogueManager.Instance.StartDialogue(new string[] {
            "This is the Window Station, where you will take orders and serve customers.",
            "See that Cash Register? It tracks your Daily Quota.",
            "Hitting 40% of your quota earns you 1 Star, 75% gets you 2 Stars, and 100% earns you 3 Stars.",
            "You need at least 1 Star to pass a level!"
        }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        // ==========================================
        // STEP 3: FIRST CUSTOMER
        // ==========================================
        dialogueDone = false;

        // Spawn our specific single-Silog customer!
        if (OrderManager.Instance != null)
        {
            OrderManager.Instance.SpawnSpecificCustomer(tutSilogDish);
        }

        DialogueManager.Instance.StartDialogue(new string[] {
            "Here comes your first customer now.",
            "Wait for them to reach the counter, then click on them to take their order."
        }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        // Wait for the player to click the customer (triggers our event)
        while (!hasOrderBeenTaken) yield return null;

        // ==========================================
        // STEP 4: TRAINING WHEELS OFF
        // ==========================================
        dialogueDone = false;
        DialogueManager.Instance.StartDialogue(new string[] {
            "Great! Now go to the Cooking Station and make that Silog.",
            "Since you aced the first two tutorials, I'll let you handle it from here.",
            "Keep serving customers to fill up the register!"
        }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        // Unlock the camera entirely for full gameplay!
        StationCameraController.Instance.isLocked = false;

        // Turn on the automatic customer spawner in the OrderManager
        if (OrderManager.Instance != null)
        {
            OrderManager.Instance.StartTutorialSpawning();
        }

        // ==========================================
        // STEP 4.5: TEACH SERVING THE DISH
        // ==========================================
        // Wait for them to cook the food and ring the bell
        while (bellsRung < 1) yield return null;

        // Wait until they navigate back to the Window Station
        while (StationCameraController.Instance.currentStationIndex != 2) yield return null;

        dialogueDone = false;
        DialogueManager.Instance.StartDialogue(new string[] {
            "The food is ready!",
            "Left-click the dish on the counter to hand it to the customer."
        }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        // ==========================================
        // STEP 5: WATCH FOR FIRST SERVE
        // ==========================================
        while (ticketsServed < 1) yield return null;

        dialogueDone = false;
        DialogueManager.Instance.StartDialogue(new string[] {
            "Awesome! Look at the register—the green bar filled up a bit.",
            "Keep serving customers before closing time at 5:00 PM!"
        }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        // ==========================================
        // STEP 6: MONITOR FOR 2-STAR QUOTA
        // ==========================================
        if (PlayerEconomyManager.Instance != null && OrderManager.Instance != null)
        {
            while (PlayerEconomyManager.Instance.shiftCash < (OrderManager.Instance.dailyQuota * 0.75f))
            {
                yield return null;
            }

            // Quota hit! Explain closing early.
            dialogueDone = false;
            DialogueManager.Instance.StartDialogue(new string[] {
                "You've reached 2 Stars! The register is glowing.",
                "When you hit 2 stars, you can hover over the register and click it to close shop early.",
                "Or, you can finish out the shift for extra money. It's up to you!"
            }, () => dialogueDone = true);
            while (!dialogueDone) yield return null;

            hasExplainedCloseEarly = true;
        }
    }

    private void HandleOrderTaken() { hasOrderBeenTaken = true; }
    private void HandleTicketServed() { ticketsServed++; }

    private void HandleTutorialBell() { bellsRung++; }

    // --- NEW: Triggered by the Cash Register's OnCloseShopEarly UnityEvent ---
    public void OnTutorialRegisterClicked()
    {
        // Only trigger this if we've actually taught them about it!
        if (hasExplainedCloseEarly)
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(new string[] {
                    "You closed up early! Congratulations, you've mastered the basics.",
                    "You're fully prepared to run the restaurant on your own."
                }, FinishTutorial);
            }
        }
        else
        {
            // If they somehow spam-clicked it before the tutorial got there, just finish it normally.
            FinishTutorial();
        }
    }

    private void FinishTutorial()
    {
        if (OrderManager.Instance != null)
        {
            OrderManager.Instance.CloseStore();
        }

        if (tutorialCompletePanel != null)
        {
            tutorialCompletePanel.SetActive(true);
        }
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void OnDestroy()
    {
        OrderManager.OnTutorialOrderTaken -= HandleOrderTaken;
        OrderManager.OnTutorialTicketServed -= HandleTicketServed;
        ServiceBell.OnTutorialBellRung -= HandleTutorialBell;
    }
}