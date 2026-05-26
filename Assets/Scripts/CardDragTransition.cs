using UnityEngine;
using UnityEngine.EventSystems;

public class CardDragTransition : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Data")]
    public IngredientData ingredientData;
    public float hoverGlideAmount = 50f;
    public float transitionSpeed = 10f;

    private Camera mainCamera;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Vector2 targetPosition;

    private bool isHovering = false;
    private bool isDragging = false;
    private BaseStation lastHoveredStation;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        mainCamera = Camera.main;
        originalPosition = rectTransform.anchoredPosition;
        targetPosition = originalPosition;
    }

    private void Update()
    {
        if (!isDragging) rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, targetPosition, Time.deltaTime * transitionSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDragging) return;
        isHovering = true;
        targetPosition = originalPosition + new Vector2(0, hoverGlideAmount);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isDragging) return;
        isHovering = false;
        targetPosition = originalPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        canvasGroup.alpha = 0.5f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position += (Vector3)eventData.delta;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.GetComponent<GridTileVisual>() != null)
        {
            GridTileVisual tile = hit.collider.GetComponent<GridTileVisual>();

            if (lastHoveredStation != null && lastHoveredStation != tile.parentStation) lastHoveredStation.ClearAllHighlights();
            lastHoveredStation = tile.parentStation;

            bool isAllowedStation = ingredientData.validStations.Contains(tile.parentStation.stationType);
            bool willCook = ingredientData.isCookable && tile.parentStation.stationType == StationType.Stove;
            Vector2Int[] offsetsToCheck = (willCook && ingredientData.cookedPrefab != null) ? ingredientData.cookedPrefab.GetComponent<Draggable3DItem>().myData.shapeOffsets : ingredientData.shapeOffsets;

            bool fitsInGrid = tile.parentStation.CanPlaceItem(tile.gridCoordinate, offsetsToCheck);
            tile.parentStation.HighlightTiles(tile.gridCoordinate, offsetsToCheck, isAllowedStation && fitsInGrid);
        }
        else if (lastHoveredStation != null)
        {
            lastHoveredStation.ClearAllHighlights();
            lastHoveredStation = null;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
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

                    // UPDATED: Pass the actual item, not 'true'
                    tile.parentStation.SetOccupancy(tile.gridCoordinate, itemScript.myData.shapeOffsets, itemScript);

                    if (willCook) itemScript.StartCookingLock(ingredientData.cookTime);

                    itemScript.CheckForCombinations(); // NEW: Trigger combo check immediately

                    ResetCardToHand();
                }
            }
        }

        if (!droppedSuccessfully) ResetCardToHand();
    }

    private void ResetCardToHand()
    {
        canvasGroup.alpha = 1f;
        targetPosition = isHovering ? originalPosition + new Vector2(0, hoverGlideAmount) : originalPosition;
    }
}