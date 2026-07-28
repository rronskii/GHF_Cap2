using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class InventorySetupManager : MonoBehaviour
{
    public static InventorySetupManager Instance;

    [Header("Shift Database")]
    public ShiftLevelData currentShiftData;

    [Header("UI Panels - Pantry (Loadout Setup)")]
    public GameObject pantryPanel; // Formerly cookbookPanel
    public Transform pantryContentParent; // Where the draggable cards spawn

    [Header("UI Panels - Menu Board")]
    public GameObject menuBoardPanel;
    public TextMeshProUGUI menuContentText;

    [Header("UI Panels - Warning Prompt")]
    public GameObject warningPanel;
    public TextMeshProUGUI warningMessageText;

    [Header("UI Feedback")]
    public GameObject placeModeTextObj;

    private IngredientData currentPlacementItem;
    private TextMeshProUGUI placeModeText;
    private Color originalTextColor;
    private Coroutine errorCoroutine;
    private string defaultPlaceText = "Choose a slot!";

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (pantryPanel != null) pantryPanel.SetActive(false);
        if (menuBoardPanel != null) menuBoardPanel.SetActive(false);
        if (warningPanel != null) warningPanel.SetActive(false);

        if (placeModeTextObj != null)
        {
            placeModeText = placeModeTextObj.GetComponent<TextMeshProUGUI>();
            if (placeModeText != null)
            {
                originalTextColor = placeModeText.color;
            }
            placeModeTextObj.SetActive(false);
        }
    }

    // ==========================================
    // 3D INTERACTION LISTENER
    // ==========================================
    private void OnEnable()
    {
        InventoryInteractable.OnItemClicked += Handle3DItemClicked;
    }

    private void OnDisable()
    {
        InventoryInteractable.OnItemClicked -= Handle3DItemClicked;
    }

    private void Handle3DItemClicked(InventoryInteractable interactable)
    {
        if (interactable.itemType == InventoryInteractable.InteractableType.Tablet)
        {
            if (PauseMenuController.Instance != null) PauseMenuController.Instance.OpenCookbookDirectly();
        }
        else if (interactable.itemType == InventoryInteractable.InteractableType.BulletinBoard)
        {
            if (InventoryCameraController.Instance != null && interactable.cameraInspectTarget != null)
            {
                InventoryCameraController.Instance.MoveToTarget(interactable.cameraInspectTarget);
            }
            OpenMenuBoardUI();
        }
        else if (interactable.itemType == InventoryInteractable.InteractableType.Pantry)
        {
            // --- NEW: Clicking the physical pantry opens the loadout UI ---
            if (InventoryCameraController.Instance != null && interactable.cameraInspectTarget != null)
            {
                InventoryCameraController.Instance.MoveToTarget(interactable.cameraInspectTarget);
            }
            OpenPantry();
        }
    }

    // ==========================================
    // PANTRY LOGIC (Formerly Cookbook)
    // ==========================================
    public void OpenPantry()
    {
        if (pantryPanel != null) pantryPanel.SetActive(true);
        PopulatePantry();
    }

    public void ClosePantry()
    {
        if (pantryPanel != null) pantryPanel.SetActive(false);
        if (InventoryCameraController.Instance != null) InventoryCameraController.Instance.ReturnHome();
    }

    private void PopulatePantry()
    {
        foreach (Transform child in pantryContentParent) Destroy(child.gameObject);

        if (PlayerInventoryManager.Instance != null)
        {
            foreach (IngredientData ingredient in PlayerInventoryManager.Instance.unlockedIngredients)
            {
                if (ingredient.cardUIPrefab != null)
                {
                    GameObject cardObj = Instantiate(ingredient.cardUIPrefab, pantryContentParent);

                    CardDragUI[] dragScripts = cardObj.GetComponentsInChildren<CardDragUI>(true);
                    foreach (CardDragUI drag in dragScripts) { drag.enabled = false; Destroy(drag); }

                    CardGridPlacer[] placerScripts = cardObj.GetComponentsInChildren<CardGridPlacer>(true);
                    foreach (CardGridPlacer placer in placerScripts) { placer.enabled = false; Destroy(placer); }

                    CookbookCardUI interactiveScript = cardObj.AddComponent<CookbookCardUI>();
                    interactiveScript.myData = ingredient;
                }
            }
        }
    }

    // ==========================================
    // PLACEMENT & SYNC
    // ==========================================
    public void SelectItemForPlacement(IngredientData ingredient)
    {
        currentPlacementItem = ingredient;
        if (errorCoroutine != null) StopCoroutine(errorCoroutine);

        if (placeModeTextObj != null)
        {
            placeModeTextObj.SetActive(true);
            if (placeModeText != null)
            {
                placeModeText.text = defaultPlaceText;
                placeModeText.color = originalTextColor;
            }
        }

        // Hide the pantry panel so they can click the 3D room slots!
        ClosePantry();
    }

    public void ShowError(string message)
    {
        if (errorCoroutine != null) StopCoroutine(errorCoroutine);
        errorCoroutine = StartCoroutine(ErrorRoutine(message));
    }

    private IEnumerator ErrorRoutine(string message)
    {
        if (placeModeText != null)
        {
            placeModeText.text = message;
            placeModeText.color = Color.red;
        }
        yield return new WaitForSeconds(1.5f);
        if (currentPlacementItem != null && placeModeText != null)
        {
            placeModeText.text = defaultPlaceText;
            placeModeText.color = originalTextColor;
        }
    }

    public IngredientData GetPlacementItem() { return currentPlacementItem; }

    public void ClearPlacementItem()
    {
        currentPlacementItem = null;
        if (placeModeTextObj != null) placeModeTextObj.SetActive(false);
    }

    public void ForceSyncAllStations()
    {
        InventoryStation[] allStations = FindObjectsOfType<InventoryStation>();
        foreach (InventoryStation station in allStations)
        {
            if (station != null) station.SyncWithLoadout();
        }
    }

    // ==========================================
    // MENU BOARD LOGIC
    // ==========================================
    public void OpenMenuBoardUI()
    {
        if (currentShiftData == null) return;
        if (menuBoardPanel != null) menuBoardPanel.SetActive(true);

        if (menuContentText != null)
        {
            string menuString = "<b>Today's Menu</b>\n\n";
            foreach (DishData dish in currentShiftData.activeDishes)
            {
                menuString += $"<size=120%>{dish.dishName}</size>\n";
                menuString += "<size=80%><color=#A0A0A0>Requires: ";

                for (int i = 0; i < dish.requiredIngredients.Count; i++)
                {
                    menuString += dish.requiredIngredients[i].displayName;
                    if (i < dish.requiredIngredients.Count - 1) menuString += ", ";
                }
                menuString += "</color></size>\n\n";
            }
            menuContentText.text = menuString;
        }
    }

    public void CloseMenuBoardUI()
    {
        if (menuBoardPanel != null) menuBoardPanel.SetActive(false);
        if (InventoryCameraController.Instance != null) InventoryCameraController.Instance.ReturnHome();
    }

    // ==========================================
    // SHIFT VALIDATION LOGIC
    // ==========================================
    public void OnStartShiftClicked()
    {
        if (currentShiftData == null || PlayerInventoryManager.Instance == null)
        {
            StartShift();
            return;
        }

        List<string> missingIngredientNames = new List<string>();

        foreach (IngredientData requiredData in currentShiftData.requiredBaseIngredients)
        {
            bool isEquipped = false;
            foreach (KeyValuePair<string, IngredientData> kvp in PlayerInventoryManager.Instance.activeLoadout)
            {
                if (kvp.Value == requiredData)
                {
                    isEquipped = true;
                    break;
                }
            }
            if (!isEquipped) missingIngredientNames.Add(requiredData.displayName);
        }

        if (missingIngredientNames.Count > 0)
        {
            if (warningPanel != null) warningPanel.SetActive(true);
            if (warningMessageText != null)
            {
                string missingList = string.Join(", ", missingIngredientNames);
                warningMessageText.text = $"Hold on, you forgot to bring: <color=red>{missingList}</color>.\nAre you sure you want to continue?";
            }
        }
        else
        {
            StartShift();
        }
    }

    public void CloseWarningPrompt()
    {
        if (warningPanel != null) warningPanel.SetActive(false);
    }

    public void StartShift()
    {
        SceneManager.LoadScene("01_FoodTruckLevel");
    }
}