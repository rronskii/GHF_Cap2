using UnityEngine;
using UnityEngine.EventSystems;

public class CookbookCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public IngredientData myData;

    [Header("Animation Settings")]
    public float hoverScaleMultiplier = 1.1f;
    public float scaleSpeed = 15f;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private void Start()
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScaleMultiplier;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (InventorySetupManager.Instance != null)
        {
            InventorySetupManager.Instance.SelectItemForPlacement(myData);
        }
    }
}