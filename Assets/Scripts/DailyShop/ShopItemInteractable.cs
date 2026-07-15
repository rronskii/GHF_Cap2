using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ShopItemInteractable : MonoBehaviour
{
    // The UI Manager will listen to this event to know what to display
    public static event Action<IngredientData> OnShopItemClicked;

    [Header("Item Data")]
    public IngredientData ingredientData;

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
        targetScale = originalScale * hoverScaleMultiplier;
    }

    private void OnMouseExit()
    {
        targetScale = originalScale;
    }

    private void OnMouseDown()
    {
        if (isInteractionLocked) return;

        targetScale = originalScale; // Reset scale on click

        if (ingredientData != null)
        {
            if (OnShopItemClicked != null)
            {
                OnShopItemClicked(ingredientData);
            }
        }
    }
}