using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialTwoManager : MonoBehaviour
{
    [Header("Tutorial Data")]
    public DishData hotsilogDish;
    public DishData tapsilogDish;
    public DishData silogDish;

    [Header("Required Ingredients")]
    public IngredientData tutGarlic;
    public IngredientData tutEgg;
    public IngredientData tutRice;
    public IngredientData tutHotdog;
    public IngredientData tutBeef;

    [Header("Station Setup")]
    public Transform eggSpawnPoint;
    public LayerMask gridLayerMask;
    public GameObject cookedEggPrefab;

    [Header("UI & Visuals")]
    [Tooltip("Assign the new BouncingArrow3D prefab here!")]
    public GameObject bouncingArrowPrefab;
    public float arrowXOffset = 1f;
    public float arrowYOffset = 1.05f;
    public float arrowZOffset = 2.5f;
    public GameObject tutorialCompletePanel;

    [Header("Level Transition")]
    public string nextSceneName = "00c_Level_One";

    private int bellsRung = 0;

    private void Start()
    {
        if (tutorialCompletePanel != null) tutorialCompletePanel.SetActive(false);

        if (StationCameraController.Instance != null)
        {
            StationCameraController.Instance.isLocked = true;
            StationCameraController.Instance.allowLooping = false;
            StationCameraController.Instance.maxStationIndex = 0;
            StationCameraController.Instance.ForceGoToStation(0);
        }

        if (HandManager.Instance != null) HandManager.Instance.enforceSingleIngredientLimit = false;

        StartCoroutine(TutorialTwoFlowRoutine());
    }

    private IEnumerator TutorialTwoFlowRoutine()
    {
        bool dialogueDone = false;

        // ==========================================
        // STEP 1: SPAWN THE EGG & WAIT FOR IT TO BURN
        // ==========================================
        yield return new WaitForSeconds(1.5f);

        if (Physics.Raycast(eggSpawnPoint.position, Vector3.down, out RaycastHit hit, 10f, gridLayerMask))
        {
            GridTileVisual tile = hit.collider.GetComponent<GridTileVisual>();
            if (tile != null)
            {
                GameObject eggObj = Instantiate(cookedEggPrefab, hit.point + Vector3.up, Quaternion.identity);
                Draggable3DItem eggScript = eggObj.GetComponent<Draggable3DItem>();
                if (eggScript != null) eggScript.ForceDropOnTile(tile);
            }
        }

        Draggable3DItem burntItem = null;
        while (burntItem == null)
        {
            burntItem = GetBurntItemInScene();
            yield return null;
        }

        // ==========================================
        // STEP 2: TRASH THE BURNT EGG
        // ==========================================
        DialogueManager.Instance.StartDialogue(new string[] {
            "Oh no! That egg was left on the stove too long and turned into charcoal.",
            "Food will burn if you leave it on a cooking station after it finishes.",
            "Drag the burnt egg into the Trash Bin to clear the station!"
        }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        GameObject arrow = SpawnArrowOn3DItem(burntItem);

        while (burntItem != null) yield return null;

        // ==========================================
        // STEP 3: ORDER UP (TICKETS & INVENTORY)
        // ==========================================
        dialogueDone = false;
        if (TutorialOrderManager.Instance != null)
        {
            TutorialOrderManager.Instance.SpawnDummyTicket(hotsilogDish);
            TutorialOrderManager.Instance.SpawnDummyTicket(tapsilogDish);
        }

        DialogueManager.Instance.StartDialogue(new string[] {
            "Order up! We have two new dishes on the menu today: Hotsilog and Tapsilog.",
            "Let's head LEFT (Press A) to the Inventory Station to grab what you need."
        }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        if (StationCameraController.Instance != null)
        {
            StationCameraController.Instance.isLocked = false;
            StationCameraController.Instance.maxStationIndex = 1;
        }

        while (HandManager.Instance.currentStationIndex != 1) yield return null;

        // ==========================================
        // STEP 4 & 5: DRAW EXACTLY 1 OF EACH & RETURN TIP
        // ==========================================
        dialogueDone = false;
        StationCameraController.OnStationChanged += GuardrailEarlyReturn;

        DialogueManager.Instance.StartDialogue(new string[] {
            "Let's prep for these orders. Note that you can only carry up to 5 cards at once!",
            "Draw exactly 1 Garlic, 1 Egg, 1 Rice, 1 Hotdog, and 1 Beef into your hand.",
            "If you accidentally grab the wrong one, you can return it by hovering over the card and pressing 'R'."
        }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        // Wait until they have all 5 specific ingredients
        while (!HasRequiredIngredients()) yield return null;

        // Turn off the guardrail—they are free to leave now!
        StationCameraController.OnStationChanged -= GuardrailEarlyReturn;

        // ==========================================
        // STEP 6: RETURN & COOK THE FIRST BATCH
        // ==========================================
        dialogueDone = false;
        DialogueManager.Instance.StartDialogue(new string[] {
            "Now, head back to the Cooking Station (Press D) and cook those two orders."
        }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        // Wait until they actually return to the cooking station
        while (HandManager.Instance.currentStationIndex != 0) yield return null;

        // Prompt the cookbook reminder once they arrive
        dialogueDone = false;
        DialogueManager.Instance.StartDialogue(new string[] {
            "Remember, open your Cookbook (Esc or P) if you don't know the recipe!"
        }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        ServiceBell.OnTutorialBellRung += HandleTutorialBell;
        while (bellsRung < 2) yield return null;

        // ==========================================
        // STEP 7: SECOND BATCH (PRACTICE)
        // ==========================================
        dialogueDone = false;
        DialogueManager.Instance.StartDialogue(new string[] {
            "Good job! You've got the hang of the new ingredients.",
            "Let's finish up the day. I'm sending in 3 more orders.",
            "Clear these to finish this tutorial!"
        }, () => dialogueDone = true);
        while (!dialogueDone) yield return null;

        if (TutorialOrderManager.Instance != null)
        {
            TutorialOrderManager.Instance.SpawnDummyTicket(silogDish);
            TutorialOrderManager.Instance.SpawnDummyTicket(hotsilogDish);
            TutorialOrderManager.Instance.SpawnDummyTicket(tapsilogDish);
        }

        while (bellsRung < 5) yield return null;

        // ==========================================
        // STEP 8: LEVEL COMPLETE
        // ==========================================
        ServiceBell.OnTutorialBellRung -= HandleTutorialBell;

        if (HandManager.Instance != null) HandManager.Instance.RefundAllCards();

        ShowWinScreen();
    }

    private bool HasRequiredIngredients()
    {
        if (HandManager.Instance == null) return false;
        return (HandManager.Instance.GetCountOfIngredient(tutGarlic) >= 1 &&
                HandManager.Instance.GetCountOfIngredient(tutEgg) >= 1 &&
                HandManager.Instance.GetCountOfIngredient(tutRice) >= 1 &&
                HandManager.Instance.GetCountOfIngredient(tutHotdog) >= 1 &&
                HandManager.Instance.GetCountOfIngredient(tutBeef) >= 1);
    }

    private void GuardrailEarlyReturn(int stationIndex)
    {
        if (stationIndex == 0)
        {
            if (!HasRequiredIngredients())
            {
                StationCameraController.Instance.ForceGoToStation(1);
                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.StartDialogue(new string[] {
                        "Wait! Make sure you have exactly 1 Garlic, 1 Egg, 1 Rice, 1 Hotdog, and 1 Beef before returning!"
                    }, null);
                }
            }
        }
    }

    private void HandleTutorialBell() { bellsRung++; }

    private Draggable3DItem GetBurntItemInScene()
    {
        Draggable3DItem[] items = FindObjectsOfType<Draggable3DItem>();
        foreach (Draggable3DItem item in items)
        {
            if (item.isBurntItem) return item;
        }
        return null;
    }

    private GameObject SpawnArrowOn3DItem(Draggable3DItem item)
    {
        if (item == null || bouncingArrowPrefab == null) return null;

        GameObject arrow = Instantiate(bouncingArrowPrefab, item.transform);
        arrow.transform.localPosition = new Vector3(arrowXOffset, arrowYOffset, arrowZOffset);
        return arrow;
    }

    private void ShowWinScreen()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(new string[]
            {
                "You're getting the hang of this!",
                "I think you're finally ready to serve some real customers."
            }, () => { if (tutorialCompletePanel != null) tutorialCompletePanel.SetActive(true); });
        }
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(nextSceneName)) SceneManager.LoadScene(nextSceneName);
    }

    private void OnDestroy()
    {
        ServiceBell.OnTutorialBellRung -= HandleTutorialBell;
        StationCameraController.OnStationChanged -= GuardrailEarlyReturn;
    }
}