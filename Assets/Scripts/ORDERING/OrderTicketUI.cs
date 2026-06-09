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

    // NEW: Method for the OrderManager to smoothly move the ticket up the stack
    public void SetTargetPosition(Vector2 targetPos, int index)
    {
        // Lower index (older tickets) get a HIGHER base sorting order so they render on top
        baseSortingOrder = 100 - index;
        localCanvas.sortingOrder = baseSortingOrder;

        // You could use a coroutine to smooth lerp this position later if you want them to slide up!
        rectTransform.anchoredPosition = targetPos;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * smoothSpeed);
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