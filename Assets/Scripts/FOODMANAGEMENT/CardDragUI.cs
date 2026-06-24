using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CardGridPlacer))]
[RequireComponent(typeof(CanvasGroup))]
public class CardDragUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("UI Settings")]
    public float hoverGlideAmount = 50f;
    [Tooltip("Lower is faster for SmoothDamp. 0.05 is snappy, 0.15 is floaty.")]
    public float smoothTime = 0.05f;
    public int inventoryStationIndex = 1;

    [HideInInspector] public bool isInteractable = true;
    [HideInInspector] public bool isDragging = false;

    private RectTransform rectTransform;
    public CanvasGroup canvasGroup { get; private set; }
    private Vector2 handPosition;
    private Vector2 targetPosition;
    private bool isHovering = false;

    private Vector2 velocity = Vector2.zero;
    private CardGridPlacer gridPlacer;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        gridPlacer = GetComponent<CardGridPlacer>();
    }

    public void SetHandPosition(Vector2 newPosition)
    {
        handPosition = newPosition;
        if (!isDragging && !isHovering)
        {
            targetPosition = handPosition;
        }
    }

    private void Update()
    {
        if (isDragging && Time.timeScale == 0f)
        {
            CancelDrag();
        }

        // --- NEW: 'R' KEY TO QUICK RETURN ---
        if (Input.GetKeyDown(KeyCode.R))
        {
            // Only trigger if our mouse is over THIS specific card, or we are holding it
            if (isHovering || isDragging)
            {
                // Only allow the refund if we are actively looking at the inventory station
                if (HandManager.Instance != null && HandManager.Instance.currentStationIndex == inventoryStationIndex)
                {
                    if (isDragging) CancelDrag();
                    gridPlacer.TriggerRefund();
                    return;
                }
            }
        }

        if (!isDragging)
        {
            rectTransform.anchoredPosition = Vector2.SmoothDamp(rectTransform.anchoredPosition, targetPosition, ref velocity, smoothTime);

            if (Vector2.Distance(rectTransform.anchoredPosition, targetPosition) < 0.5f)
            {
                rectTransform.anchoredPosition = targetPosition;
            }
        }
    }

    public void CancelDrag()
    {
        if (!isDragging) return;

        isDragging = false;
        canvasGroup.alpha = 1f;
        targetPosition = handPosition;
        gridPlacer.CancelDragAndClearPreview();
    }

    // --- Mouse Events ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDragging || !isInteractable) return;
        isHovering = true;
        targetPosition = handPosition + new Vector2(0, hoverGlideAmount);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isDragging || !isInteractable) return;
        isHovering = false;
        targetPosition = handPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
        if (!isInteractable) return;

        StartDrag();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (Time.timeScale == 0f) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
        if (!isInteractable || !isDragging) return;

        rectTransform.position = Input.mousePosition;
        gridPlacer.ProcessDragUpdate();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        AttemptDrop();
    }

    // --- Hotkey Simulation ---
    public void SimulateKeyDown()
    {
        if (!isInteractable) return;
        StartDrag();
    }

    public void SimulateKeyHold()
    {
        if (!isInteractable || !isDragging) return;
        rectTransform.position = Input.mousePosition;
        gridPlacer.ProcessDragUpdate();
    }

    public void SimulateKeyUp()
    {
        if (!isInteractable || !isDragging) return;
        AttemptDrop();
    }

    private void StartDrag()
    {
        isDragging = true;
        canvasGroup.alpha = 0.5f;
    }

    private void AttemptDrop()
    {
        if (!isInteractable || !isDragging) return;
        isDragging = false;

        bool success = gridPlacer.AttemptDrop();

        if (!success)
        {
            gridPlacer.CancelDragAndClearPreview();
            canvasGroup.alpha = 1f;
            targetPosition = handPosition;
        }
    }
}