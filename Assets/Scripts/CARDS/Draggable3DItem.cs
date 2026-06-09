using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Draggable3DItem : MonoBehaviour
{
    [Header("Data Reference")]
    public IngredientData myData;

    [Header("Visual Prefabs")]
    [SerializeField] private GameObject timerUIPrefab; // Drag 'CookingTimer_Prefab' here in Unity

    [Header("Placement Memory")]
    public BaseStation currentStation;
    public Vector2Int currentCoordinate;

    private Camera mainCamera;
    private Vector3 startPosition;
    private Collider itemCollider;
    private bool isLocked = false;
    private BaseStation lastHoveredStation;

    private void Start()
    {
        mainCamera = Camera.main;
        itemCollider = GetComponent<Collider>();
    }

    // --- COOKING LOGIC (With Timer Link) ---

    public void StartCookingLock(float cookTime)
    {
        StartCoroutine(CookingLockRoutine(cookTime));
    }

    private IEnumerator CookingLockRoutine(float cookTime)
    {
        isLocked = true;

        // NEW: Spawn and initialize the timer bar
        SpawnCookingTimer(cookTime);

        yield return new WaitForSeconds(cookTime);
        isLocked = false;
    }

    // NEW Helper function to spawn the UI
    private void SpawnCookingTimer(float duration)
    {
        if (timerUIPrefab == null)
        {
            Debug.LogWarning($"[Draggable3DItem] Timer UI Prefab missing on {gameObject.name}!");
            return;
        }

        // Spawn it slightly above the food item's pivot
        Vector3 spawnOffset = new Vector3(1.5f, 0.5f, 0);
        GameObject timerObj = Instantiate(timerUIPrefab, transform.position + spawnOffset, Quaternion.identity, this.transform);

        CookingTimerUI timerScript = timerObj.GetComponent<CookingTimerUI>();
        if (timerScript != null)
        {
            timerScript.StartTimer(duration);
        }
    }

    // --- DRAG MECHANICS (Unchanged) ---

    private void OnMouseDown()
    {
        if (isLocked) return;

        startPosition = transform.position;
        itemCollider.enabled = false;

        if (currentStation != null) currentStation.SetOccupancy(currentCoordinate, myData.shapeOffsets, null);
    }

    private void OnMouseDrag()
    {
        if (isLocked) return;

        Plane dragPlane = new Plane(Vector3.up, new Vector3(0, startPosition.y, 0));
        Ray mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (dragPlane.Raycast(mouseRay, out float distance))
        {
            transform.position = Vector3.Lerp(transform.position, mouseRay.GetPoint(distance), Time.deltaTime * 20f);
        }

        if (Physics.Raycast(mouseRay, out RaycastHit hit) && hit.collider.GetComponent<GridTileVisual>() != null)
        {
            GridTileVisual tile = hit.collider.GetComponent<GridTileVisual>();

            if (lastHoveredStation != null && lastHoveredStation != tile.parentStation) lastHoveredStation.ClearAllHighlights();
            lastHoveredStation = tile.parentStation;

            bool willCook = myData.isCookable && tile.parentStation.stationType == StationType.Stove && myData.cookedPrefab != null;
            Vector2Int[] offsetsToPreview = myData.shapeOffsets;

            if (willCook)
            {
                Draggable3DItem cookedScript = myData.cookedPrefab.GetComponent<Draggable3DItem>();
                if (cookedScript != null) offsetsToPreview = cookedScript.myData.shapeOffsets;
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
        if (isLocked) return;
        if (lastHoveredStation != null) lastHoveredStation.ClearAllHighlights();

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GridTileVisual tile = hit.collider.GetComponent<GridTileVisual>();
            if (tile != null && TryDropOnTile(tile)) return;
        }

        HandleInvalidDrop();
    }

    private bool TryDropOnTile(GridTileVisual tile)
    {
        BaseStation station = tile.parentStation;

        if (!myData.validStations.Contains(station.stationType)) return false;

        bool willCook = myData.isCookable && station.stationType == StationType.Stove && myData.cookedPrefab != null;

        Vector2Int[] offsetsToCheck = myData.shapeOffsets;
        if (willCook)
        {
            Draggable3DItem cookedScript = myData.cookedPrefab.GetComponent<Draggable3DItem>();
            if (cookedScript != null) offsetsToCheck = cookedScript.myData.shapeOffsets;
        }

        if (!station.CanPlaceItem(tile.gridCoordinate, offsetsToCheck)) return false;

        if (willCook) CookItem(tile);
        else PlaceItem(tile);

        return true;
    }

    private void PlaceItem(GridTileVisual tile)
    {
        transform.position = tile.transform.position + new Vector3(0, 0.1f, 0);

        currentStation = tile.parentStation;
        currentCoordinate = tile.gridCoordinate;
        currentStation.SetOccupancy(currentCoordinate, myData.shapeOffsets, this);

        itemCollider.enabled = true;
        CheckForCombinations();
    }

    private void CookItem(GridTileVisual tile)
    {
        Vector3 spawnPosition = tile.transform.position + new Vector3(0, 0.1f, 0);
        GameObject cookedItem = Instantiate(myData.cookedPrefab, spawnPosition, Quaternion.identity);
        Draggable3DItem cookedScript = cookedItem.GetComponent<Draggable3DItem>();

        cookedScript.currentStation = tile.parentStation;
        cookedScript.currentCoordinate = tile.gridCoordinate;
        tile.parentStation.SetOccupancy(tile.gridCoordinate, cookedScript.myData.shapeOffsets, cookedScript);

        // This call automatically triggers the timer spawn via CookingLockRoutine
        cookedScript.StartCookingLock(myData.cookTime);
        cookedScript.CheckForCombinations();

        Destroy(gameObject);
    }

    private void HandleInvalidDrop()
    {
        transform.position = startPosition;
        itemCollider.enabled = true;

        if (currentStation != null)
        {
            currentStation.SetOccupancy(currentCoordinate, myData.shapeOffsets, this);
        }
    }

    // --- ADJACENCY CRAFTING (Unchanged) ---

    public void CheckForCombinations()
    {
        if (currentStation == null || currentStation.stationType != StationType.Stove) return;

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int checkCoord = currentCoordinate + dir;
            Draggable3DItem adjacentItem = currentStation.GetOccupantAt(checkCoord);

            if (adjacentItem != null && adjacentItem != this)
            {
                if (myData.combinations != null)
                {
                    foreach (RecipeCombo combo in myData.combinations)
                    {
                        if (combo.partnerIngredient == adjacentItem.myData)
                        {
                            ExecuteCombination(this, adjacentItem, combo);
                            return;
                        }
                    }
                }

                if (adjacentItem.myData.combinations != null)
                {
                    foreach (RecipeCombo combo in adjacentItem.myData.combinations)
                    {
                        if (combo.partnerIngredient == this.myData)
                        {
                            ExecuteCombination(adjacentItem, this, combo);
                            return;
                        }
                    }
                }
            }
        }
    }

    private void ExecuteCombination(Draggable3DItem initiator, Draggable3DItem partner, RecipeCombo combo)
    {
        GridTileVisual spawnTile = combo.spawnOnPartnerTile ?
            partner.currentStation.tileVisuals[partner.currentCoordinate.x, partner.currentCoordinate.y] :
            initiator.currentStation.tileVisuals[initiator.currentCoordinate.x, initiator.currentCoordinate.y];

        BaseStation station = initiator.currentStation;

        station.SetOccupancy(initiator.currentCoordinate, initiator.myData.shapeOffsets, null);
        station.SetOccupancy(partner.currentCoordinate, partner.myData.shapeOffsets, null);

        Vector3 spawnPos = spawnTile.transform.position + new Vector3(0, 0.1f, 0);
        GameObject resultObj = Instantiate(combo.resultPrefab, spawnPos, Quaternion.identity);
        Draggable3DItem resultItem = resultObj.GetComponent<Draggable3DItem>();

        resultItem.currentStation = station;
        resultItem.currentCoordinate = spawnTile.gridCoordinate;
        station.SetOccupancy(resultItem.currentCoordinate, resultItem.myData.shapeOffsets, resultItem);

        // This call automatically triggers the timer spawn via CookingLockRoutine
        resultItem.StartCookingLock(combo.cookTime);

        Destroy(initiator.gameObject);
        Destroy(partner.gameObject);
    }
}