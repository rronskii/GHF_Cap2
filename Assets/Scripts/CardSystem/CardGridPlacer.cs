using UnityEngine;

[RequireComponent(typeof(CardDragUI))]
public class CardGridPlacer : MonoBehaviour
{
    [Header("Data")]
    public IngredientData ingredientData;

    [Header("Physics Settings")]
    public LayerMask gridLayerMask;

    private Camera mainCamera;
    private CardDragUI dragUI;
    private BaseStation lastHoveredStation;

    [Header("3D Ghost Preview")]
    public int rotationSteps = 0;
    private GameObject active3DPreview;
    private Draggable3DItem previewScript;

    private void Awake()
    {
        mainCamera = Camera.main;
        dragUI = GetComponent<CardDragUI>();
    }

    private void Update()
    {
        if (dragUI.isDragging && Input.GetMouseButtonDown(1))
        {
            rotationSteps = (rotationSteps + 1) % 4;

            if (active3DPreview != null)
            {
                if (previewScript != null)
                {
                    previewScript.currentRotationSteps = rotationSteps;
                }

                active3DPreview.transform.rotation = Quaternion.Euler(0, rotationSteps * 90f, 0);
            }

            UpdateGridHighlighting();
        }
    }

    public void ProcessDragUpdate()
    {
        UpdateGridHighlighting();

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, gridLayerMask))
        {
            GridTileVisual tile = hit.collider.GetComponent<GridTileVisual>();
            if (tile != null)
            {
                if (active3DPreview == null)
                {
                    Show3DPreview(tile);
                }

                Vector3 targetPosition = hit.point;

                // --- THE UPGRADE: Shape-Based Sensor Array ---
                float maxPhysicalHeight = hit.point.y;
                float tileSize = 1.0f;

                // Get offsets: from the active 3D preview if it exists, otherwise fallback to unrotated raw data
                Vector2Int[] offsets = ingredientData.shapeOffsets;
                if (previewScript != null)
                {
                    offsets = previewScript.GetCurrentRotatedOffsets();
                }

                foreach (Vector2Int gridOffset in offsets)
                {
                    float offsetX = gridOffset.x * tileSize;
                    float offsetZ = gridOffset.y * tileSize;

                    Vector3 skyPosition = new Vector3(targetPosition.x + offsetX, targetPosition.y + 10f, targetPosition.z + offsetZ);

                    if (Physics.Raycast(skyPosition, Vector3.down, out RaycastHit heightHit, 20f, gridLayerMask))
                    {
                        if (heightHit.point.y > maxPhysicalHeight)
                        {
                            maxPhysicalHeight = heightHit.point.y;
                        }
                    }
                }

                // Smoothly glide to the height-adjusted position
                targetPosition.y = maxPhysicalHeight + 0.5f;
                active3DPreview.transform.position = Vector3.Lerp(active3DPreview.transform.position, targetPosition, Time.deltaTime * 20f);

                UpdateGridHighlighting();
            }
        }
        else
        {
            if (active3DPreview != null)
            {
                Clear3DPreview();
                if (lastHoveredStation != null)
                {
                    lastHoveredStation.ClearAllHighlights();
                    lastHoveredStation = null;
                }
            }
        }
    }

    public bool AttemptDrop()
    {
        if (lastHoveredStation != null)
        {
            lastHoveredStation.ClearAllHighlights();
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (HandManager.Instance != null && HandManager.Instance.currentStationIndex == dragUI.inventoryStationIndex)
        {
            if (Physics.Raycast(ray, out RaycastHit invHit, Mathf.Infinity))
            {
                InventoryStation invStation = invHit.collider.GetComponent<InventoryStation>();
                if (invStation != null && invStation.GetStationIngredient() == ingredientData)
                {
                    Clear3DPreview();
                    rotationSteps = 0;
                    TriggerRefund();
                    return true;
                }
            }
        }

        if (active3DPreview != null)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, gridLayerMask))
            {
                GridTileVisual tile = hit.collider.GetComponent<GridTileVisual>();
                if (tile != null)
                {
                    if (ingredientData.validStations.Contains(tile.parentStation.stationType))
                    {
                        bool willCook = false;
                        if (ingredientData.isCookable && tile.parentStation.isCookingStation)
                        {
                            willCook = true;
                        }

                        Vector2Int[] offsetsToCheck = previewScript.GetCurrentRotatedOffsets();

                        if (willCook && ingredientData.cookedPrefab != null)
                        {
                            Draggable3DItem cookedRef = ingredientData.cookedPrefab.GetComponent<Draggable3DItem>();
                            if (cookedRef != null)
                            {
                                offsetsToCheck = previewScript.RotateOffsets(cookedRef.myData.shapeOffsets, rotationSteps);
                            }
                        }

                        if (tile.parentStation.CanPlaceItem(tile.gridCoordinate, offsetsToCheck))
                        {
                            if (previewScript.itemCollider != null)
                            {
                                previewScript.itemCollider.enabled = true;
                            }

                            previewScript.ForceDropOnTile(tile);

                            active3DPreview = null;
                            previewScript = null;

                            if (HandManager.Instance != null)
                            {
                                HandManager.Instance.RemoveCard(dragUI);
                            }

                            Destroy(gameObject);
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    public void CancelDragAndClearPreview()
    {
        Clear3DPreview();
        rotationSteps = 0;
        if (lastHoveredStation != null)
        {
            lastHoveredStation.ClearAllHighlights();
            lastHoveredStation = null;
        }
    }

    private void UpdateGridHighlighting()
    {
        Ray mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(mouseRay, out RaycastHit hit, Mathf.Infinity, gridLayerMask))
        {
            GridTileVisual tile = hit.collider.GetComponent<GridTileVisual>();

            if (lastHoveredStation != null && lastHoveredStation != tile.parentStation)
            {
                lastHoveredStation.ClearAllHighlights();
            }
            lastHoveredStation = tile.parentStation;

            bool willCook = false;
            if (ingredientData.isCookable && tile.parentStation.isCookingStation)
            {
                willCook = true;
            }

            Vector2Int[] offsetsToPreview = ingredientData.shapeOffsets;
            if (previewScript != null)
            {
                offsetsToPreview = previewScript.GetCurrentRotatedOffsets();
            }

            if (willCook && ingredientData.cookedPrefab != null)
            {
                Draggable3DItem cookedScript = ingredientData.cookedPrefab.GetComponent<Draggable3DItem>();
                if (cookedScript != null && previewScript != null)
                {
                    offsetsToPreview = previewScript.RotateOffsets(cookedScript.myData.shapeOffsets, rotationSteps);
                }
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

    public void TriggerRefund()
    {
        if (!dragUI.isInteractable) return;

        dragUI.isInteractable = false;

        if (HandManager.Instance != null)
        {
            HandManager.Instance.RemoveCard(dragUI);
        }

        if (PlayerInventoryManager.Instance != null)
        {
            PlayerInventoryManager.Instance.RefundStock(ingredientData);
        }

        StartCoroutine(RefundGlideRoutine());
    }

    private System.Collections.IEnumerator RefundGlideRoutine()
    {
        dragUI.canvasGroup.blocksRaycasts = false;
        RectTransform rect = GetComponent<RectTransform>();
        Vector2 startPos = rect.anchoredPosition;
        Vector2 targetPos = startPos + new Vector2(0, 150f);

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            dragUI.canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void Show3DPreview(GridTileVisual tile)
    {
        active3DPreview = Instantiate(ingredientData.worldPrefab, tile.transform.position, Quaternion.identity);
        previewScript = active3DPreview.GetComponent<Draggable3DItem>();

        if (previewScript != null)
        {
            if (previewScript.itemCollider != null)
            {
                previewScript.itemCollider.enabled = false;
            }
            previewScript.currentRotationSteps = rotationSteps;
        }

        active3DPreview.transform.rotation = Quaternion.Euler(0, rotationSteps * 90f, 0);

        if (dragUI.canvasGroup != null)
        {
            dragUI.canvasGroup.alpha = 0f;
        }
    }

    private void Clear3DPreview()
    {
        if (active3DPreview != null)
        {
            Destroy(active3DPreview);
            active3DPreview = null;
            previewScript = null;
        }

        if (dragUI.canvasGroup != null)
        {
            if (dragUI.isDragging)
            {
                dragUI.canvasGroup.alpha = 0.5f;
            }
            else
            {
                dragUI.canvasGroup.alpha = 1f;
            }
        }
    }
}