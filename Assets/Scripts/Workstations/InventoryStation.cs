using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class InventoryStation : MonoBehaviour
{
    [Header("Slot Setup")]
    public string slotID;
    public StorageType storageType;
    public GameObject emptyPrefab;

    [Header("Hover Settings")]
    public float hoverScaleMultiplier = 1.15f;
    public float scaleSpeed = 10f;

    public static event Action OnTutorialCardDrawn;
    // --- NEW: Event fires when stock hits 0 ---
    public static event Action OnTutorialStockEmpty;
    // Add this near your other events at the top
    public static event Action OnInventoryVisualsUpdate;

    private IngredientData myIngredientData;
    private GameObject currentVisualInstance;
    private GameObject stationCardPrefab;

    private bool isSetupScene = false;
    private Vector3 originalScale;
    private Vector3 targetScale;
    private Collider myCollider;

    private void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        myCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        isSetupScene = SceneManager.GetActiveScene().name == "03_Inventory";
        SyncWithLoadout();
    }

    public void SyncWithLoadout()
    {
        if (PlayerInventoryManager.Instance != null)
        {
            if (PlayerInventoryManager.Instance.activeLoadout.ContainsKey(slotID))
            {
                myIngredientData = PlayerInventoryManager.Instance.activeLoadout[slotID];

                if (myIngredientData != null)
                {
                    stationCardPrefab = myIngredientData.cardUIPrefab;
                }
            }
            else
            {
                myIngredientData = null;
                stationCardPrefab = null;
            }
        }
        UpdateVisual();
    }

    private void Update()
    {
        if (Vector3.Distance(transform.localScale, targetScale) > 0.001f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
        }
    }

    private void OnMouseEnter()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
        targetScale = originalScale * hoverScaleMultiplier;
    }

    private void OnMouseExit()
    {
        targetScale = originalScale;
    }

    private void OnMouseDown()
    {
        if (Time.timeScale == 0f) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        if (isSetupScene)
        {
            if (InventorySetupManager.Instance != null)
            {
                IngredientData placementItem = InventorySetupManager.Instance.GetPlacementItem();

                if (placementItem != null)
                {
                    if (myIngredientData != null)
                    {
                        InventorySetupManager.Instance.ShowError("Slot is already occupied!");
                        return;
                    }

                    if (placementItem.allowedStorageType != storageType)
                    {
                        InventorySetupManager.Instance.ShowError("Cannot place " + placementItem.displayName + " in " + storageType.ToString() + "!");
                        return;
                    }

                    if (PlayerInventoryManager.Instance != null)
                    {
                        PlayerInventoryManager.Instance.RemoveIngredientFromAllSlots(placementItem);
                        PlayerInventoryManager.Instance.SaveSlotAssignment(slotID, placementItem);
                    }

                    InventorySetupManager.Instance.ForceSyncAllStations();
                    InventorySetupManager.Instance.ClearPlacementItem();
                }
            }
        }
        else
        {
            if (HandManager.Instance == null || PlayerInventoryManager.Instance == null || myIngredientData == null || stationCardPrefab == null) return;

            if (!PlayerInventoryManager.Instance.HasStock(myIngredientData)) return;

            bool drawnSuccessfully = HandManager.Instance.TryDrawCard(stationCardPrefab);

            if (drawnSuccessfully)
            {
                PlayerInventoryManager.Instance.ConsumeStock(myIngredientData);
                if (OnTutorialCardDrawn != null) OnTutorialCardDrawn();

                // --- NEW: If that was the last one, clear the model and fire the event! ---
                if (!PlayerInventoryManager.Instance.HasStock(myIngredientData))
                {
                    UpdateVisual();
                    if (OnTutorialStockEmpty != null) OnTutorialStockEmpty();
                }
            }
        }
    }

    private void OnMouseOver()
    {
        if (isSetupScene)
        {
            if (Input.GetMouseButtonDown(1))
            {
                if (InventorySetupManager.Instance != null && InventorySetupManager.Instance.GetPlacementItem() == null)
                {
                    if (myIngredientData != null)
                    {
                        myIngredientData = null;
                        if (PlayerInventoryManager.Instance != null)
                        {
                            PlayerInventoryManager.Instance.ClearSlotAssignment(slotID);
                        }
                        UpdateVisual();
                    }
                }
            }
        }
    }

    public void UpdateVisual()
    {
        if (currentVisualInstance != null)
        {
            Destroy(currentVisualInstance);
        }

        GameObject prefabToSpawn = emptyPrefab;

        if (myIngredientData != null)
        {
            // --- NEW: Hide the prefab if we are out of stock during gameplay ---
            bool isOutOfStock = false;
            if (!isSetupScene && PlayerInventoryManager.Instance != null)
            {
                isOutOfStock = !PlayerInventoryManager.Instance.HasStock(myIngredientData);
            }

            if (isOutOfStock)
            {
                prefabToSpawn = null;
            }
            else if (myIngredientData.storagePrefab != null)
            {
                prefabToSpawn = myIngredientData.storagePrefab;
            }
        }
        // --- NEW: Hide the empty placeholder completely if we aren't in the setup scene ---
        else if (!isSetupScene)
        {
            prefabToSpawn = null;
            if (myCollider != null) myCollider.enabled = false; // Turn off interaction too!
        }

        if (prefabToSpawn != null)
        {
            currentVisualInstance = Instantiate(prefabToSpawn, transform.position, transform.rotation, transform);
        }
    }

    // Add these lifecycle methods anywhere in the class
    private void OnEnable() { OnInventoryVisualsUpdate += UpdateVisual; }
    private void OnDisable() { OnInventoryVisualsUpdate -= UpdateVisual; }

    // Add this public method to easily trigger the refresh globally
    public static void RefreshAllStations()
    {
        if (OnInventoryVisualsUpdate != null) OnInventoryVisualsUpdate();
    }

    public IngredientData GetStationIngredient()
    {
        return myIngredientData;
    }
}