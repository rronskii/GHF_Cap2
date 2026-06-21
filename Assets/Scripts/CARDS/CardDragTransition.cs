using UnityEngine;
using UnityEngine.EventSystems;

public class CardDragTransition : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Data")]
    public IngredientData ingredientData;
    public float hoverGlideAmount = 50f;
    public float transitionSpeed = 10f;

    [HideInInspector] public bool isInteractable = true;

    [Header("Physics Settings")]
    public LayerMask gridLayerMask;

    private Camera mainCamera;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private Vector2 handPosition;
    private Vector2 targetPosition;

    private bool isHovering = false;
    private bool isDragging = false;
    private BaseStation lastHoveredStation;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        mainCamera = Camera.main;
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
        if (!isDragging) rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, targetPosition, Time.deltaTime * transitionSpeed);
    }

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
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        if (!isInteractable) return;
        isDragging = true;
        canvasGroup.alpha = 0.5f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        if (!isInteractable) return;

        rectTransform.position += (Vector3)eventData.delta;

        Ray mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(mouseRay, out RaycastHit hit, Mathf.Infinity, gridLayerMask))
        {
            GridTileVisual tile = hit.collider.GetComponent<GridTileVisual>();

            if (lastHoveredStation != null && lastHoveredStation != tile.parentStation)
            {
                lastHoveredStation.ClearAllHighlights();
            }
            lastHoveredStation = tile.parentStation;

            bool willCook = ingredientData.isCookable && tile.parentStation.stationType == StationType.Stove;
            Vector2Int[] offsetsToPreview = ingredientData.shapeOffsets;

            if (willCook && ingredientData.cookedPrefab != null)
            {
                Draggable3DItem cookedScript = ingredientData.cookedPrefab.GetComponent<Draggable3DItem>();
                if (cookedScript != null) offsetsToPreview = cookedScript.myData.shapeOffsets;
            }

            bool isAllowedStation = ingredientData.validStations.Contains(tile.parentStation.stationType);
            bool fitsInGrid = tile.parentStation.CanPlaceItem(tile.gridCoordinate, offsetsToPreview);

            tile.parentStation.HighlightTiles(tile.gridCoordinate, offsetsToPreview, isAllowedStation && fitsInGrid);
        }
        else if (lastHoveredStation != null)
        {
            lastHoveredStation.ClearAllHighlights();
            lastHoveredStation = null;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isInteractable) return;
        isDragging = false;
        if (lastHoveredStation != null) lastHoveredStation.ClearAllHighlights();

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        bool droppedSuccessfully = false;

        if (Physics.Raycast(ray, out RaycastHit invHit, Mathf.Infinity))
        {
            InventoryStation invStation = invHit.collider.GetComponent<InventoryStation>();

            // Verify it's an inventory station AND it matches this card's specific ingredient type
            if (invStation != null && invStation.GetStationIngredient() == ingredientData)
            {
                TriggerRefund();
                return; // Stop running the rest of the drop code
            }
        }

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, gridLayerMask))
        {
            GridTileVisual tile = hit.collider.GetComponent<GridTileVisual>();

            if (ingredientData.validStations.Contains(tile.parentStation.stationType))
            {
                bool willCook = ingredientData.isCookable && tile.parentStation.stationType == StationType.Stove;

                Vector2Int[] offsetsToCheck = (willCook && ingredientData.cookedPrefab != null) ?
                    ingredientData.cookedPrefab.GetComponent<Draggable3DItem>().myData.shapeOffsets :
                    ingredientData.shapeOffsets;

                if (tile.parentStation.CanPlaceItem(tile.gridCoordinate, offsetsToCheck))
                {
                    droppedSuccessfully = true;

                    GameObject newItem = Instantiate(ingredientData.worldPrefab, tile.transform.position + new Vector3(0, 0.1f, 0), Quaternion.identity);
                    Draggable3DItem itemScript = newItem.GetComponent<Draggable3DItem>();

                    itemScript.ForceDropOnTile(tile);

                    HandManager.Instance.RemoveCard(this);
                    Destroy(gameObject);
                }
            }
        }

        if (!droppedSuccessfully)
        {
            canvasGroup.alpha = 1f;
            targetPosition = handPosition;
        }
    }

    private void OnEnable()
    {
        StationCameraController.OnStationChanged += ForceDropCard;
    }

    private void OnDisable()
    {
        StationCameraController.OnStationChanged -= ForceDropCard;
    }

    private void ForceDropCard(int stationIndex)
    {
        if (isDragging)
        {
            isDragging = false;
            canvasGroup.alpha = 1f;
            targetPosition = handPosition; // Snap back to hand
        }
    }

    // --- NEW: RIGHT CLICK REFUND ---
    public void OnPointerClick(PointerEventData eventData)
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        // Detect Right Click
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            TriggerRefund();
        }
    }

    private void TriggerRefund()
    {
        if (!isInteractable) return;

        isInteractable = false; // Lock interactions
        HandManager.Instance.RemoveCard(this);
        PlayerInventoryManager.Instance.RefundStock(ingredientData);

        StartCoroutine(RefundGlideRoutine());
    }

    private System.Collections.IEnumerator RefundGlideRoutine()
    {
        canvasGroup.blocksRaycasts = false;
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 targetPos = startPos + new Vector2(0, 150f); // Glide up

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        Destroy(gameObject);
    }
}