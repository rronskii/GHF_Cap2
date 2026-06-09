using UnityEngine;
using UnityEngine.EventSystems;

public class CardDragTransition : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Data")]
    public IngredientData ingredientData;
    public float hoverGlideAmount = 50f;
    public float transitionSpeed = 10f;

    [HideInInspector] public bool isInteractable = true; // Controlled by HandManager

    private Camera mainCamera;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private Vector2 handPosition; // Replaced 'originalPosition'
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

    // Called by HandManager to update its slot in the hand
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
        if (!isInteractable) return;
        isDragging = true;
        canvasGroup.alpha = 0.5f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isInteractable) return;

        // 1. Move the UI card with the mouse drag
        rectTransform.position += (Vector3)eventData.delta;

        // 2. Shoot a ray from the mouse screen position into the 3D world
        Ray mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(mouseRay, out RaycastHit hit) && hit.collider.GetComponent<GridTileVisual>() != null)
        {
            GridTileVisual tile = hit.collider.GetComponent<GridTileVisual>();

            // If we move from one station directly onto another, clear the old one first
            if (lastHoveredStation != null && lastHoveredStation != tile.parentStation)
            {
                lastHoveredStation.ClearAllHighlights();
            }
            lastHoveredStation = tile.parentStation;

            // 3. Figure out the shape size we are trying to preview
            bool willCook = ingredientData.isCookable && tile.parentStation.stationType == StationType.Stove;
            Vector2Int[] offsetsToPreview = ingredientData.shapeOffsets;

            if (willCook && ingredientData.cookedPrefab != null)
            {
                Draggable3DItem cookedScript = ingredientData.cookedPrefab.GetComponent<Draggable3DItem>();
                if (cookedScript != null) offsetsToPreview = cookedScript.myData.shapeOffsets;
            }

            // 4. Run the validity checks
            bool isAllowedStation = ingredientData.validStations.Contains(tile.parentStation.stationType);
            bool fitsInGrid = tile.parentStation.CanPlaceItem(tile.gridCoordinate, offsetsToPreview);

            // 5. Tell the station to color the tiles (Green if both true, Red if either false)
            tile.parentStation.HighlightTiles(tile.gridCoordinate, offsetsToPreview, isAllowedStation && fitsInGrid);
        }
        else if (lastHoveredStation != null)
        {
            // If the mouse leaves the grid area entirely, turn off the highlights
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

        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.GetComponent<GridTileVisual>() != null)
        {
            GridTileVisual tile = hit.collider.GetComponent<GridTileVisual>();

            if (ingredientData.validStations.Contains(tile.parentStation.stationType))
            {
                bool willCook = ingredientData.isCookable && tile.parentStation.stationType == StationType.Stove;
                Vector2Int[] offsetsToCheck = (willCook && ingredientData.cookedPrefab != null) ? ingredientData.cookedPrefab.GetComponent<Draggable3DItem>().myData.shapeOffsets : ingredientData.shapeOffsets;

                if (tile.parentStation.CanPlaceItem(tile.gridCoordinate, offsetsToCheck))
                {
                    droppedSuccessfully = true;
                    GameObject prefabToSpawn = willCook ? ingredientData.cookedPrefab : ingredientData.worldPrefab;

                    GameObject newItem = Instantiate(prefabToSpawn, tile.transform.position + new Vector3(0, 0.1f, 0), Quaternion.identity);
                    Draggable3DItem itemScript = newItem.GetComponent<Draggable3DItem>();

                    itemScript.currentStation = tile.parentStation;
                    itemScript.currentCoordinate = tile.gridCoordinate;

                    tile.parentStation.SetOccupancy(tile.gridCoordinate, itemScript.myData.shapeOffsets, itemScript);

                    if (willCook) itemScript.StartCookingLock(ingredientData.cookTime);
                    itemScript.CheckForCombinations();

                    // NEW: Success! Remove from hand and destroy the UI card
                    HandManager.Instance.RemoveCard(this);
                    Destroy(gameObject);
                }
            }
        }

        // If we missed the grid, snap back to the hand
        if (!droppedSuccessfully)
        {
            canvasGroup.alpha = 1f;
            targetPosition = handPosition; // Fall back into the calculated slot
        }
    }
}