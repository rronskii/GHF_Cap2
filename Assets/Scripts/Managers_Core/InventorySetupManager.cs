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

    [Header("UI Panels - Cookbook")]
    public GameObject cookbookPanel;
    public Transform cookbookContentParent;

    [Header("UI Panels - Menu Board")]
    public GameObject menuBoardPanel;
    public TextMeshProUGUI menuContentText; // A single text box to list the dishes and ingredients

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
        if (cookbookPanel != null) cookbookPanel.SetActive(false);
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

    // --- COOKBOOK LOGIC ---
    public void OpenCookbook()
    {
        if (cookbookPanel != null) cookbookPanel.SetActive(true);
        PopulateCookbook();
    }

    public void CloseCookbook()
    {
        if (cookbookPanel != null) cookbookPanel.SetActive(false);
    }

    private void PopulateCookbook()
    {
        foreach (Transform child in cookbookContentParent)
        {
            Destroy(child.gameObject);
        }

        if (PlayerInventoryManager.Instance != null)
        {
            foreach (IngredientData ingredient in PlayerInventoryManager.Instance.unlockedIngredients)
            {
                if (ingredient.cardUIPrefab != null)
                {
                    GameObject cardObj = Instantiate(ingredient.cardUIPrefab, cookbookContentParent);

                    CardDragUI[] dragScripts = cardObj.GetComponentsInChildren<CardDragUI>(true);
                    foreach (CardDragUI drag in dragScripts)
                    {
                        if (drag != null)
                        {
                            drag.enabled = false;
                            Destroy(drag);
                        }
                    }

                    CardGridPlacer[] placerScripts = cardObj.GetComponentsInChildren<CardGridPlacer>(true);
                    foreach (CardGridPlacer placer in placerScripts)
                    {
                        if (placer != null)
                        {
                            placer.enabled = false;
                            Destroy(placer);
                        }
                    }

                    CookbookCardUI interactiveScript = cardObj.AddComponent<CookbookCardUI>();
                    interactiveScript.myData = ingredient;
                }
            }
        }
    }

    public void SelectItemForPlacement(IngredientData ingredient)
    {
        currentPlacementItem = ingredient;

        if (errorCoroutine != null)
        {
            StopCoroutine(errorCoroutine);
        }

        if (placeModeTextObj != null)
        {
            placeModeTextObj.SetActive(true);
            if (placeModeText != null)
            {
                placeModeText.text = defaultPlaceText;
                placeModeText.color = originalTextColor;
            }
        }

        CloseCookbook();
    }

    public void ShowError(string message)
    {
        if (errorCoroutine != null)
        {
            StopCoroutine(errorCoroutine);
        }
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

    public IngredientData GetPlacementItem()
    {
        return currentPlacementItem;
    }

    public void ClearPlacementItem()
    {
        currentPlacementItem = null;

        if (placeModeTextObj != null)
        {
            placeModeTextObj.SetActive(false);
        }
    }

    public void ForceSyncAllStations()
    {
        InventoryStation[] allStations = FindObjectsOfType<InventoryStation>();
        foreach (InventoryStation station in allStations)
        {
            if (station != null)
            {
                station.SyncWithLoadout();
            }
        }
    }

    // --- MENU BOARD LOGIC ---
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
                    if (i < dish.requiredIngredients.Count - 1)
                    {
                        menuString += ", ";
                    }
                }
                menuString += "</color></size>\n\n";
            }

            menuContentText.text = menuString;
        }
    }

    public void CloseMenuBoardUI()
    {
        if (menuBoardPanel != null) menuBoardPanel.SetActive(false);
    }

    // --- SHIFT VALIDATION LOGIC ---
    public void OnStartShiftClicked()
    {
        if (currentShiftData == null || PlayerInventoryManager.Instance == null)
        {
            StartShift();
            return;
        }

        List<string> missingIngredientNames = new List<string>();

        // Check if every required base ingredient is equipped somewhere in the active loadout
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

            if (!isEquipped)
            {
                missingIngredientNames.Add(requiredData.displayName);
            }
        }

        if (missingIngredientNames.Count > 0)
        {
            // Player is missing ingredients! Show the warning prompt.
            if (warningPanel != null) warningPanel.SetActive(true);

            if (warningMessageText != null)
            {
                string missingList = string.Join(", ", missingIngredientNames);
                warningMessageText.text = $"Hold on, you forgot to bring: <color=red>{missingList}</color>.\nAre you sure you want to continue?";
            }
        }
        else
        {
            // Loadout is perfect, start the shift immediately
            StartShift();
        }
    }

    public void CloseWarningPrompt()
    {
        if (warningPanel != null) warningPanel.SetActive(false);
    }

    public void StartShift()
    {
        Debug.Log("StartShift button was clicked! Attempting to load scene...");
        SceneManager.LoadScene("01_FoodTruckLevel");
    }
}