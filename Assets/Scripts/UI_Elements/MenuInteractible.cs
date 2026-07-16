using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MenuInteractable : MonoBehaviour
{
    [Header("Hover Settings")]
    public float hoverScaleMultiplier = 1.15f;
    public float scaleSpeed = 10f;

    private Vector3 originalScale;
    private Vector3 targetScale;

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

        if (InventorySetupManager.Instance != null)
        {
            InventorySetupManager.Instance.OpenMenuBoardUI();
        }
    }
}