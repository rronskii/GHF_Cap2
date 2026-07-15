using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(IngredientVisuals))]
public class Draggable3DItem : MonoBehaviour
{
    [Header("Data Reference")]
    public IngredientData myData;

    [Header("Physics Settings")]
    public LayerMask gridLayerMask;

    [Header("Placement Memory")]
    public BaseStation currentStation;
    public Vector2Int currentCoordinate;

    [Header("Rotation Settings")]
    public int currentRotationSteps = 0;
    private int originalRotationSteps = 0;
    private Quaternion originalRotation;

    [Header("Item Type Flags")]
    [Tooltip("Check this ONLY on your burnt charcoal prefabs")]
    public bool isBurntItem = false;

    [Tooltip("Check this ONLY on your final plated dishes")]
    public bool isPlate = false;

    public bool isLocked = false;
    public Collider itemCollider { get; private set; }

    private Camera mainCamera;
    private Vector3 startPosition;
    private BaseStation lastHoveredStation;
    private bool isDragging = false;

    // Component References
    private IngredientVisuals visuals;
    private IngredientProcessor processor;
    private IngredientCombiner combiner;

    private void Awake()
    {
        mainCamera = Camera.main;
        itemCollider = GetComponent<Collider>();
        visuals = GetComponent<IngredientVisuals>();
        processor = GetComponent<IngredientProcessor>();
        combiner = GetComponent<IngredientCombiner>();
    }

    private void Update()
    {
        if (isDragging && Input.GetMouseButtonDown(1))
        {
            currentRotationSteps = (currentRotationSteps + 1) % 4;
            transform.rotation = Quaternion.Euler(0, currentRotationSteps * 90f, 0);

            RefreshGridPreview();
        }
    }

    public Vector2Int[] GetCurrentRotatedOffsets()
    {
        return RotateOffsets(myData.shapeOffsets, currentRotationSteps);
    }

    public Vector2Int[] RotateOffsets(Vector2Int[] original, int rotationSteps)
    {
        Vector2Int[] rotated = new Vector2Int[original.Length];

        for (int i = 0; i < original.Length; i++)
        {
            int x = original[i].x;
            int y = original[i].y;

            if (rotationSteps == 1) rotated[i] = new Vector2Int(y, -x);
            else if (rotationSteps == 2) rotated[i] = new Vector2Int(-x, -y);
            else if (rotationSteps == 3) rotated[i] = new Vector2Int(-y, x);
            else rotated[i] = new Vector2Int(x, y);
        }
        return rotated;
    }

    private void OnEnable() { StationCameraController.OnStationChanged += ForceDrop3DItem; }
    private void OnDisable() { StationCameraController.OnStationChanged -= ForceDrop3DItem; }

    private void ForceDrop3DItem(int stationIndex)
    {
        if (isDragging)
        {
            isDragging = false;
            if (lastHoveredStation != null) lastHoveredStation.ClearAllHighlights();
            HandleInvalidDrop();
        }
    }

    private void OnMouseDown()
    {
        if (Time.timeScale == 0f) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
        if (isLocked) return;
        if (isBurntItem) return;

        originalRotationSteps = currentRotationSteps;
        originalRotation = transform.rotation;

        isDragging = true;
        if (processor != null) processor.StopBurning();

        startPosition = transform.position;
        itemCollider.enabled = false;

        if (currentStation != null)
        {
            currentStation.SetOccupancy(currentCoordinate, GetCurrentRotatedOffsets(), null);
        }
    }

    private void OnMouseDrag()
    {
        if (Time.timeScale == 0f) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
        if (isLocked || !isDragging) return;

        Plane dragPlane = new Plane(Vector3.up, new Vector3(0, startPosition.y, 0));
        Ray mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (dragPlane.Raycast(mouseRay, out float distance))
        {
            transform.position = Vector3.Lerp(transform.position, mouseRay.GetPoint(distance), Time.deltaTime * 20f);
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, gridLayerMask))
        {
            Vector3 targetPosition = hit.point;

            // --- THE UPGRADE: Shape-Based Sensor Array ---
            float maxPhysicalHeight = hit.point.y;
            float tileSize = 1.0f; // Make sure this matches your Unity grid spacing

            // Get the footprint offsets (already correctly rotated!)
            Vector2Int[] offsets = GetCurrentRotatedOffsets();

            foreach (Vector2Int gridOffset in offsets)
            {
                // Convert grid offsets to world space
                float offsetX = gridOffset.x * tileSize;
                float offsetZ = gridOffset.y * tileSize;

                // Start ray 10 units in the air
                Vector3 skyPosition = new Vector3(targetPosition.x + offsetX, targetPosition.y + 10f, targetPosition.z + offsetZ);

                // Note: We use gridLayerMask here. Ensure your Frying Pan's Mesh Collider is on this layer!
                if (Physics.Raycast(skyPosition, Vector3.down, out RaycastHit heightHit, 20f, gridLayerMask))
                {
                    if (heightHit.point.y > maxPhysicalHeight)
                    {
                        maxPhysicalHeight = heightHit.point.y;
                    }
                }
            }

            // Set the target height to the highest point detected, plus our hover offset
            targetPosition.y = maxPhysicalHeight + 0.5f;

            // Move the object smoothly
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 20f);
        }

        RefreshGridPreview();
    }

    private void RefreshGridPreview()
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

            bool willCook = myData.isCookable && tile.parentStation.isCookingStation && myData.cookedPrefab != null;

            Vector2Int[] offsetsToPreview = GetCurrentRotatedOffsets();

            if (willCook)
            {
                Draggable3DItem cookedScript = myData.cookedPrefab.GetComponent<Draggable3DItem>();
                if (cookedScript != null)
                {
                    offsetsToPreview = RotateOffsets(cookedScript.myData.shapeOffsets, currentRotationSteps);
                }
            }

            bool isAllowedStation = myData.validStations.Contains(tile.parentStation.stationType);
            bool fitsInGrid = tile.parentStation.CanPlaceItem(tile.gridCoordinate, offsetsToPreview);

            tile.parentStation.HighlightTiles(tile.gridCoordinate, offsetsToPreview, isAllowedStation && fitsInGrid);
        }
        else if (lastHoveredStation != null)
        {
            lastHoveredStation.ClearAllHighlights();
            lastHoveredStation = null;
        }
    }

    private void OnMouseUp()
    {
        if (isLocked || !isDragging) return;
        isDragging = false;

        if (lastHoveredStation != null) lastHoveredStation.ClearAllHighlights();

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, gridLayerMask))
        {
            GridTileVisual tile = hit.collider.GetComponent<GridTileVisual>();
            if (tile != null && TryDropOnTile(tile)) return;
        }

        currentRotationSteps = originalRotationSteps;
        transform.rotation = originalRotation;
        HandleInvalidDrop();
    }

    private bool TryDropOnTile(GridTileVisual tile)
    {
        if (!myData.validStations.Contains(tile.parentStation.stationType)) return false;

        bool willCook = myData.isCookable && tile.parentStation.isCookingStation && myData.cookedPrefab != null;

        Vector2Int[] offsetsToCheck = GetCurrentRotatedOffsets();

        if (willCook)
        {
            Draggable3DItem cookedScript = myData.cookedPrefab.GetComponent<Draggable3DItem>();
            if (cookedScript != null)
            {
                offsetsToCheck = RotateOffsets(cookedScript.myData.shapeOffsets, currentRotationSteps);
            }
        }

        if (!tile.parentStation.CanPlaceItem(tile.gridCoordinate, offsetsToCheck)) return false;

        ForceDropOnTile(tile);
        return true;
    }

    public void ForceDropOnTile(GridTileVisual tile)
    {
        bool willCook = myData.isCookable && tile.parentStation.isCookingStation && myData.cookedPrefab != null;

        if (willCook && processor != null)
        {
            if (myData.instantCook)
            {
                InstantTransformAndCook(tile);
            }
            else
            {
                CookItem(tile);
            }
        }
        else
        {
            PlaceItem(tile);
        }
    }

    private void PlaceItem(GridTileVisual tile)
    {
        transform.position = tile.transform.position + new Vector3(0, 0.1f, 0);
        currentStation = tile.parentStation;
        currentCoordinate = tile.gridCoordinate;
        currentStation.SetOccupancy(currentCoordinate, GetCurrentRotatedOffsets(), this);
        itemCollider.enabled = true;

        bool combined = false;
        if (combiner != null) combined = combiner.CheckForCombinations();

        if (!combined && processor != null) processor.EvaluateBurnState();
    }

    private void InstantTransformAndCook(GridTileVisual tile)
    {
        Vector3 spawnPosition = tile.transform.position + new Vector3(0, 0.1f, 0);
        GameObject cookedItem = Instantiate(myData.cookedPrefab, spawnPosition, Quaternion.identity);
        Draggable3DItem cookedScript = cookedItem.GetComponent<Draggable3DItem>();

        // FIX: Pass the rotation state to the newly spawned cooked item!
        cookedScript.currentRotationSteps = this.currentRotationSteps;
        cookedItem.transform.rotation = this.transform.rotation;

        cookedScript.currentStation = tile.parentStation;
        cookedScript.currentCoordinate = tile.gridCoordinate;

        // FIX: Use the properly rotated footprint to reserve the tiles!
        tile.parentStation.SetOccupancy(tile.gridCoordinate, cookedScript.GetCurrentRotatedOffsets(), cookedScript);

        IngredientProcessor cookedProcessor = cookedItem.GetComponent<IngredientProcessor>();
        if (cookedProcessor != null)
        {
            cookedProcessor.StartCookingLock(myData.cookTime);
        }

        Destroy(gameObject);
    }

    private void CookItem(GridTileVisual tile)
    {
        transform.position = tile.transform.position + new Vector3(0, 0.1f, 0);
        currentStation = tile.parentStation;
        currentCoordinate = tile.gridCoordinate;

        Draggable3DItem cookedScriptRef = myData.cookedPrefab.GetComponent<Draggable3DItem>();
        Vector2Int[] offsetsToReserve = GetCurrentRotatedOffsets();

        if (cookedScriptRef != null)
        {
            // FIX: We must mathematically rotate the future cooked shape's footprint 
            // before we reserve the grid tiles!
            offsetsToReserve = RotateOffsets(cookedScriptRef.myData.shapeOffsets, currentRotationSteps);
        }

        currentStation.SetOccupancy(currentCoordinate, offsetsToReserve, this);
        itemCollider.enabled = true;

        StartCoroutine(processor.CookAndTransformRoutine(myData.cookTime, offsetsToReserve));
    }

    private void HandleInvalidDrop()
    {
        transform.position = startPosition;
        itemCollider.enabled = true;

        if (currentStation != null)
        {
            currentStation.SetOccupancy(currentCoordinate, GetCurrentRotatedOffsets(), this);
        }

        if (processor != null) processor.EvaluateBurnState();
    }

    private void OnMouseOver()
    {
        if (DialogueManager.Instance != null)
        {
            if (DialogueManager.Instance.IsDialogueActive) return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (isBurntItem)
            {
                StartCoroutine(TrashRoutine());
            }
            else
            {
                if (currentStation != null)
                {
                    PlateStation plateStation = currentStation.GetComponent<PlateStation>();
                    if (plateStation != null)
                    {
                        plateStation.ServePlate();
                    }
                }
            }
        }
    }

    private System.Collections.IEnumerator TrashRoutine()
    {
        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }

        if (currentStation != null)
        {
            currentStation.SetOccupancy(currentCoordinate, GetCurrentRotatedOffsets(), null);
        }

        Vector3 startScale = transform.localScale;
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / duration);
            yield return null;
        }

        Destroy(gameObject);
    }
}