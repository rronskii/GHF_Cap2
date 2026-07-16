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

    private IngredientData myIngredientData;
    private GameObject currentVisualInstance;
    private GameObject stationCardPrefab;

    private bool isSetupScene = false;
    private Vector3 originalScale;
    private Vector3 targetScale;

    private void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
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
                // If it's no longer in the dictionary, clear it out
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
        if (DialogueManager.Instance != null)
        {
            if (DialogueManager.Instance.IsDialogueActive) return;
        }
        targetScale = originalScale * hoverScaleMultiplier;
    }

    private void OnMouseExit()
    {
        targetScale = originalScale;
    }

    private void OnMouseDown()
    {
        if (Time.timeScale == 0f) return;
        if (DialogueManager.Instance != null)
        {
            if (DialogueManager.Instance.IsDialogueActive) return;
        }

        if (isSetupScene)
        {
            if (InventorySetupManager.Instance != null)
            {
                IngredientData placementItem = InventorySetupManager.Instance.GetPlacementItem();

                if (placementItem != null)
                {
                    // --- TWEAK 2: OCCUPIED ERROR ---
                    if (myIngredientData != null)
                    {
                        InventorySetupManager.Instance.ShowError("Slot is already occupied!");
                        return;
                    }

                    // --- TWEAK 2: WRONG INVENTORY ERROR ---
                    if (placementItem.allowedStorageType != storageType)
                    {
                        InventorySetupManager.Instance.ShowError("Cannot place " + placementItem.displayName + " in " + storageType.ToString() + "!");
                        return;
                    }

                    // --- TWEAK 1: UNIQUE PLACEMENT / MOVE LOGIC ---
                    if (PlayerInventoryManager.Instance != null)
                    {
                        // Strip it from whatever slot it currently lives in
                        PlayerInventoryManager.Instance.RemoveIngredientFromAllSlots(placementItem);
                        // Save it to this new slot
                        PlayerInventoryManager.Instance.SaveSlotAssignment(slotID, placementItem);
                    }

                    // Tell all stations to update their visuals (this clears the old slot's physical prefab)
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
                if (OnTutorialCardDrawn != null)
                {
                    OnTutorialCardDrawn();
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
                if (InventorySetupManager.Instance != null)
                {
                    if (InventorySetupManager.Instance.GetPlacementItem() == null)
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
            if (myIngredientData.storagePrefab != null)
            {
                prefabToSpawn = myIngredientData.storagePrefab;
            }
        }

        if (prefabToSpawn != null)
        {
            currentVisualInstance = Instantiate(prefabToSpawn, transform.position, transform.rotation, transform);
        }
    }

    public IngredientData GetStationIngredient()
    {
        return myIngredientData;
    }
}