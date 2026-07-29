using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class ShopItemInteractable : MonoBehaviour
{
    public static event Action<ShopItemInteractable> OnShopItemClicked;

    [Header("Item Data (ASSIGN ONLY ONE)")]
    public IngredientData ingredientData;
    public UpgradeData upgradeData;

    [Header("Bulk Purchase Settings")]
    [Tooltip("How many cards this single purchase gives (e.g., 6 for an egg carton).")]
    public int yieldAmount = 1;

    [Header("UI Inspect Override")]
    [Tooltip("The prefab to show in the 3D UI. If empty, defaults to the ingredient's standard model.")]
    public GameObject customShopPrefab;

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
        if (OnShopItemClicked != null) OnShopItemClicked(this);
    }
}