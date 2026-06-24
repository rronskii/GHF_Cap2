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

    // NEW: A helper method that can mathematically rotate ANY shape array
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

        originalRotationSteps = currentRotationSteps;
        originalRotation = transform.rotation;

        isDragging = true;
        if (processor != null) processor.StopBurning();

        startPosition = transform.position;
        itemCollider.enabled = false;

        // FIXED: Now we correctly erase the rotated footprint when picking the item up!
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

            bool willCook = myData.isCookable && tile.parentStation.stationType == StationType.Stove && myData.cookedPrefab != null;

            // FIX: Pull the properly rotated shape of the item in our hand!
            Vector2Int[] offsetsToPreview = GetCurrentRotatedOffsets();

            if (willCook)
            {
                Draggable3DItem cookedScript = myData.cookedPrefab.GetComponent<Draggable3DItem>();
                if (cookedScript != null)
                {
                    // FIX: We must also mathematically rotate the future cooked shape!
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

        if (Physics.Raycast(ray, out RaycastHit trashHit, Mathf.Infinity))
        {
            if (trashHit.collider.GetComponent<TrashDropZone>() != null)
            {
                if (processor != null) processor.StopBurning();
                visuals.TrashItem(this);
                return;
            }
        }

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

        bool willCook = myData.isCookable && tile.parentStation.stationType == StationType.Stove && myData.cookedPrefab != null;

        // FIX: Check using the rotated footprint!
        Vector2Int[] offsetsToCheck = GetCurrentRotatedOffsets();

        if (willCook)
        {
            Draggable3DItem cookedScript = myData.cookedPrefab.GetComponent<Draggable3DItem>();
            if (cookedScript != null)
            {
                // FIX: Check using the rotated future cooked shape!
                offsetsToCheck = RotateOffsets(cookedScript.myData.shapeOffsets, currentRotationSteps);
            }
        }

        if (!tile.parentStation.CanPlaceItem(tile.gridCoordinate, offsetsToCheck)) return false;

        ForceDropOnTile(tile);
        return true;
    }

    public void ForceDropOnTile(GridTileVisual tile)
    {
        bool willCook = myData.isCookable && tile.parentStation.stationType == StationType.Stove && myData.cookedPrefab != null;

        if (willCook && processor != null)
        {
            if (myData.instantCookOnStove)
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

        cookedScript.currentStation = tile.parentStation;
        cookedScript.currentCoordinate = tile.gridCoordinate;
        tile.parentStation.SetOccupancy(tile.gridCoordinate, cookedScript.myData.shapeOffsets, cookedScript);

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
            offsetsToReserve = cookedScriptRef.myData.shapeOffsets;
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
}