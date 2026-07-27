using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class ShopItemInteractable : MonoBehaviour
{
    // --- UPDATED: We now pass the WHOLE interactable to the UI, so it can check what's inside! ---
    public static event Action<ShopItemInteractable> OnShopItemClicked;

    [Header("Item Data (ASSIGN ONLY ONE)")]
    public IngredientData ingredientData;
    public UpgradeData upgradeData; // --- NEW: Slot for appliances/decorations ---

    [Header("Inspect Showcase Settings")]
    public Transform inspectCameraTarget;
    public Transform inspectSpawnPoint;
    public Light showcaseSpotlight;

    [Header("Hover Settings")]
    public float hoverScaleMultiplier = 1.2f;
    public float scaleSpeed = 10f;

    private Vector3 originalScale;
    private Vector3 targetScale;
    public static bool isInteractionLocked = false;

    private void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void Start()
    {
        if (showcaseSpotlight != null) showcaseSpotlight.enabled = false;

        if (PlayerInventoryManager.Instance != null)
        {
            // --- UPGRADE FILTER ---
            if (upgradeData != null)
            {
                if (PlayerInventoryManager.Instance.currentPlayerLevel < upgradeData.unlockLevel)
                {
                    gameObject.SetActive(false); return;
                }
                if (PlayerInventoryManager.Instance.HasPurchasedUpgrade(upgradeData.uniqueUpgradeID))
                {
                    Destroy(gameObject); return;
                }
            }
            // --- INGREDIENT FILTER ---
            else if (ingredientData != null)
            {
                if (PlayerInventoryManager.Instance.currentPlayerLevel < ingredientData.unlockLevel)
                {
                    gameObject.SetActive(false); return;
                }
            }
        }
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
        if (isInteractionLocked || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) return;
        targetScale = originalScale * hoverScaleMultiplier;
    }

    private void OnMouseExit()
    {
        targetScale = originalScale;
    }

    private void OnMouseDown()
    {
        if (isInteractionLocked || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) return;

        targetScale = originalScale;

        // Pass ourselves to the UI Manager
        if (OnShopItemClicked != null) OnShopItemClicked(this);
    }
}