using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class OrderTicketUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI References")]
    public TextMeshProUGUI ordersText;

    [Header("Hover Settings (Window Station)")]
    public float windowHoverScale = 1.15f; // Tweak this in Inspector to change how big it gets!

    [Header("Hide Settings (Other Stations)")]
    public Vector2 hiddenBaseOffset = new Vector2(0, 150f);   // Shifts it up to hide
    public Vector2 hiddenHoverOffset = new Vector2(0, -120f); // Shifts it down when hovered

    [HideInInspector] public CustomerController assignedCustomer;

    public List<DishData> pendingDishes = new List<DishData>();
    private List<DishData> completedDishes = new List<DishData>();

    private RectTransform rectTransform;
    private Vector2 targetPosition;
    private Vector3 targetScale = Vector3.one;
    private Vector2 basePosition;

    private bool isHovered = false;
    private bool isWindowStation = true;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        StationCameraController.OnStationChanged += HandleStationChange;
    }

    private void OnDisable()
    {
        StationCameraController.OnStationChanged -= HandleStationChange;
    }

    private void HandleStationChange(int stationIndex)
    {
        // Station 2 is the Order Window
        isWindowStation = (stationIndex == 2);
        UpdateTargetTransforms();
    }

    public void SetupTicket(List<DishData> orderedDishes)
    {
        pendingDishes = new List<DishData>(orderedDishes);
        UpdateTicketText();
    }

    public void MarkDishServed(DishData dish)
    {
        if (pendingDishes.Contains(dish))
        {
            pendingDishes.Remove(dish);
            completedDishes.Add(dish);
            UpdateTicketText();
        }
    }

    public bool IsFullyServed()
    {
        return pendingDishes.Count == 0;
    }

    private void UpdateTicketText()
    {
        string displayText = "";

        foreach (DishData d in completedDishes)
        {
            displayText += $"<s><color=#555555>{d.dishName}</color></s>\n";
        }

        foreach (DishData d in pendingDishes)
        {
            displayText += $"{d.dishName}\n";
        }

        ordersText.text = displayText;
    }

    public void SetTargetPosition(Vector2 newPos, int index)
    {
        basePosition = newPos;
        UpdateTargetTransforms();
    }

    // UPDATED: Now handles both Position and Scale based on the current station!
    private void UpdateTargetTransforms()
    {
        if (isWindowStation)
        {
            targetPosition = basePosition; // Stays exactly in place
            targetScale = isHovered ? new Vector3(windowHoverScale, windowHoverScale, 1f) : Vector3.one;
        }
        else
        {
            targetPosition = basePosition + hiddenBaseOffset + (isHovered ? hiddenHoverOffset : Vector2.zero);
            targetScale = Vector3.one; // Keep normal scale
        }
    }

    private void Update()
    {
        // Smoothly glide position and scale every frame
        rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, targetPosition, Time.deltaTime * 15f);
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.deltaTime * 15f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
        isHovered = true;
        transform.SetAsLastSibling();
        UpdateTargetTransforms();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateTargetTransforms();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        if (OrderManager.Instance != null)
        {
            OrderManager.Instance.TryServeOldestDishToTicket(this);
        }
    }
}