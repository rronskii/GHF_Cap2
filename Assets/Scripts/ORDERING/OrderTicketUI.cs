using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(Canvas))]
public class OrderTicketUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Text References")]
    [SerializeField] private TextMeshProUGUI dishNameText;
    [SerializeField] private TextMeshProUGUI ingredientsListText;

    [Header("Hover Settings")]
    [SerializeField] private float hoverScaleFactor = 1.15f;
    [SerializeField] private float smoothSpeed = 10f;

    [HideInInspector] public DishData assignedDish;
    [HideInInspector] public int baseSortingOrder = 0; // NEW: Remembers where it belongs in the visual stack

    private Canvas localCanvas;
    private Vector3 targetScale = Vector3.one;
    private RectTransform rectTransform;
    // Add this variable to track where the ticket should slide to
    private Vector2 targetAnchoredPosition;

    private void Awake()
    {
        localCanvas = GetComponent<Canvas>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void SetupTicket(DishData dish)
    {
        assignedDish = dish;
        dishNameText.text = dish.dishName;

        ingredientsListText.text = "";
        foreach (IngredientData ingredient in dish.requiredIngredients)
        {
            ingredientsListText.text += $"- {ingredient.displayName}\n";
        }
    }

    // Update this method
    public void SetTargetPosition(Vector2 targetPos, int index)
    {
        baseSortingOrder = 100 - index;
        localCanvas.sortingOrder = baseSortingOrder;

        // REPLACED: Instead of snapping instantly, we set the target for the Update loop
        targetAnchoredPosition = targetPos;
    }

    // Update your Update() loop
    private void Update()
    {
        // Smoothly handle the hover scaling
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * smoothSpeed);

        // NEW: Smoothly slide the ticket up or down the UI layout if its index changes
        if (Vector2.Distance(rectTransform.anchoredPosition, targetAnchoredPosition) > 0.1f)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, targetAnchoredPosition, Time.deltaTime * smoothSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = Vector3.one * hoverScaleFactor;

        localCanvas.overrideSorting = true;
        localCanvas.sortingOrder = 999; // Pop to very front on hover
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = Vector3.one;

        localCanvas.overrideSorting = true;
        localCanvas.sortingOrder = baseSortingOrder; // Return to its proper place in the stack
    }
}