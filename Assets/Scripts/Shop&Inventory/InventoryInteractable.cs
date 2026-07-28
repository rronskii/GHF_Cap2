using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class InventoryInteractable : MonoBehaviour
{
    public enum InteractableType { Tablet, BulletinBoard, Pantry }

    [Header("Item Settings")]
    public InteractableType itemType;
    public Transform cameraInspectTarget; // Where the camera flies to when clicked

    [Header("Hover Settings")]
    public float hoverScaleMultiplier = 1.05f; // A subtle pop effect
    public float scaleSpeed = 10f;

    private Vector3 originalScale;
    private Vector3 targetScale;

    // The UI Manager will listen for this event!
    public static event Action<InventoryInteractable> OnItemClicked;
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
        OnItemClicked?.Invoke(this);
    }
}