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

    [Header("UI Panels - Tablet (Pantry Management)")]
    public GameObject pantryPanel;
    public Transform pantryContentParent;

    [Header("3D World Texts - Bulletin Board")]
    [Tooltip("Requires TextMeshPro (3D Object), not TextMeshProUGUI")]
    public TextMeshPro worldDishListText;
    public TextMeshPro worldIngredientListText;

    [Header("UI Panels - Warning Prompt")]
    public GameObject warningPanel;
    public TextMeshProUGUI warningMessageText;

    [Header("UI Feedback")]
    public GameObject placeModeTextObj;

    [Header("Scene Routing")]
    public string nextSceneName = "01_FoodTruckLevel";
    public string shopSceneName = "02_DailyShop";

    public bool isTabletLocked = false; // Used by the Tutorial Manager

    private IngredientData currentPlacementItem;
    private TextMeshProUGUI placeModeText;
    private Color originalTextColor;
    private Coroutine errorCoroutine;
    private string defaultPlaceText = "Choose a slot!";

    private void Awake() { Instance = this; }

    private void Start()
    {
        if (pantryPanel != null) pantryPanel.SetActive(false);
        if (warningPanel != null) warningPanel.SetActive(false);

        if (placeModeTextObj != null)
        {
            placeModeText = placeModeTextObj.GetComponent<TextMeshProUGUI>();
            if (placeModeText != null) originalTextColor = placeModeText.color;
            placeModeTextObj.SetActive(false);
        }

        PopulateWorldBulletinBoard();
    }

    private void OnEnable() { InventoryInteractable.OnItemClicked += Handle3DItemClicked; }
    private void OnDisable() { InventoryInteractable.OnItemClicked -= Handle3DItemClicked; }

    private void Handle3DItemClicked(InventoryInteractable interactable)
    {
        if (interactable.itemType == InventoryInteractable.InteractableType.Tablet)
        {
            if (isTabletLocked) return; // Blocked by tutorial
            OpenPantry();
        }
    }

    private void PopulateWorldBulletinBoard()
    {
        // 1. Fetch today's level data from the global Progression Manager first!
        if (LevelProgressionManager.Instance != null)
        {
            currentShiftData = LevelProgressionManager.Instance.GetCurrentLevelData();
        }

        // Safety check to ensure we actually got the data
        if (currentShiftData == null)
        {
            Debug.LogWarning("[InventorySetupManager] No ShiftLevelData found for today!");
            return;
        }

        // 2. Populate Dishes
        if (worldDishListText != null)
        {
            string dishString = "<b>Today's Menu</b>\n\n";
            foreach (DishData dish in currentShiftData.activeDishes)
            {
                dishString += $"- {dish.dishName}\n";
            }
            worldDishListText.text = dishString;
        }

        // 3. Populate Ingredients (Reading purely from the RAW base requirements)
        if (worldIngredientListText != null)
        {
            string ingString = "<b>Required Ingredients</b>\n\n";

            // HashSet ensures no duplicates are shown, even if they were accidentally added twice in the inspector
            HashSet<IngredientData> uniqueIngredients = new HashSet<IngredientData>();

            foreach (IngredientData ingredient in currentShiftData.requiredBaseIngredients)
            {
                if (ingredient != null)
                {
                    uniqueIngredients.Add(ingredient);
                }
            }

            foreach (IngredientData ing in uniqueIngredients)
            {
                ingString += $"- {ing.displayName}\n";
            }

            worldIngredientListText.text = ingString;
        }
    }

    // --- PANTRY LOGIC ---
    public void OpenPantry()
    {
        if (pantryPanel != null) pantryPanel.SetActive(true);
        if (InventoryCameraController.Instance != null) InventoryCameraController.Instance.isCameraLocked = true; // Lock camera while in UI
        PopulatePantry();
    }

    public void ClosePantry()
    {
        if (pantryPanel != null) pantryPanel.SetActive(false);
        if (InventoryCameraController.Instance != null) InventoryCameraController.Instance.isCameraLocked = false;
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
                    foreach (var drag in cardObj.GetComponentsInChildren<CardDragUI>(true)) Destroy(drag);
                    foreach (var placer in cardObj.GetComponentsInChildren<CardGridPlacer>(true)) Destroy(placer);

                    CookbookCardUI interactiveScript = cardObj.AddComponent<CookbookCardUI>();
                    interactiveScript.myData = ingredient;
                }
            }
        }
    }

    // --- PLACEMENT LOGIC ---
    public void SelectItemForPlacement(IngredientData ingredient)
    {
        currentPlacementItem = ingredient;
        if (errorCoroutine != null) StopCoroutine(errorCoroutine);
        if (placeModeTextObj != null)
        {
            placeModeTextObj.SetActive(true);
            if (placeModeText != null) { placeModeText.text = defaultPlaceText; placeModeText.color = originalTextColor; }
        }
        ClosePantry();
    }

    public void ShowError(string message)
    {
        if (errorCoroutine != null) StopCoroutine(errorCoroutine);
        errorCoroutine = StartCoroutine(ErrorRoutine(message));
    }

    private IEnumerator ErrorRoutine(string message)
    {
        if (placeModeText != null) { placeModeText.text = message; placeModeText.color = Color.red; }
        yield return new WaitForSeconds(1.5f);
        if (currentPlacementItem != null && placeModeText != null)
        {
            placeModeText.text = defaultPlaceText; placeModeText.color = originalTextColor;
        }
    }

    public IngredientData GetPlacementItem() { return currentPlacementItem; }
    public void ClearPlacementItem() { currentPlacementItem = null; if (placeModeTextObj != null) placeModeTextObj.SetActive(false); }

    public void ForceSyncAllStations()
    {
        foreach (InventoryStation station in FindObjectsOfType<InventoryStation>()) { if (station != null) station.SyncWithLoadout(); }
    }

    // --- SHIFT VALIDATION ---
    public void OnStartShiftClicked()
    {
        if (currentShiftData == null || PlayerInventoryManager.Instance == null) { StartShift(); return; }

        List<string> missingIngredientNames = new List<string>();
        foreach (IngredientData requiredData in currentShiftData.requiredBaseIngredients)
        {
            bool isEquipped = false;
            foreach (KeyValuePair<string, IngredientData> kvp in PlayerInventoryManager.Instance.activeLoadout)
            {
                if (kvp.Value == requiredData) { isEquipped = true; break; }
            }
            if (!isEquipped) missingIngredientNames.Add(requiredData.displayName);
        }

        if (missingIngredientNames.Count > 0)
        {
            if (warningPanel != null) warningPanel.SetActive(true);
            if (warningMessageText != null) warningMessageText.text = $"Hold on, you forgot to bring: <color=red>{string.Join(", ", missingIngredientNames)}</color>.\nAre you sure you want to continue?";
        }
        else StartShift();
    }

    public void CloseWarningPrompt() { if (warningPanel != null) warningPanel.SetActive(false); }
    public void StartShift() { SceneManager.LoadScene(nextSceneName); }

    public void ReturnToShop()
    {
        SceneManager.LoadScene(shopSceneName);
    }
}