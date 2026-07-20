using System;
using UnityEngine;
using UnityEngine.EventSystems; // Required for UI overlap detection

[RequireComponent(typeof(Collider))]
public class ShopItemInteractable : MonoBehaviour
{
    // The UI Manager will listen to this event to know what to display
    // UPDATED: Now passes the script itself so the UI knows where the camera targets are
    public static event Action<IngredientData, ShopItemInteractable> OnShopItemClicked;

    [Header("Item Data")]
    public IngredientData ingredientData;
    public bool isUpgrade = false; // --- NEW: Tells the UI to hide the +/- buttons

    [Header("Inspect Showcase Settings")]
    public Transform inspectCameraTarget;
    public Transform inspectSpawnPoint;
    public Light showcaseSpotlight;       // --- NEW: Optional dramatic lighting!

    [Header("Hover Settings")]
    public float hoverScaleMultiplier = 1.2f;
    public float scaleSpeed = 10f;

    private Vector3 originalScale;
    private Vector3 targetScale;

    // We will toggle this from the UI Manager to prevent clicking items through the UI
    public static bool isInteractionLocked = false;

    private void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void Start()
    {
        if (showcaseSpotlight != null)
        {
            showcaseSpotlight.enabled = false;
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
        if (isInteractionLocked) return;

        // Prevent hover animation if mouse is over a UI element
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        targetScale = originalScale * hoverScaleMultiplier;
    }

    private void OnMouseExit()
    {
        targetScale = originalScale;
    }

    private void OnMouseDown()
    {
        if (isInteractionLocked) return;

        // Prevent clicking the item if the mouse is clicking a UI button
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        targetScale = originalScale; // Reset scale on click

        if (ingredientData != null)
        {
            OnShopItemClicked?.Invoke(ingredientData, this);
        }
    }
}