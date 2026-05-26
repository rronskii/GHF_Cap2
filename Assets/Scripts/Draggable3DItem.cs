using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Draggable3DItem : MonoBehaviour
{
    [Header("Data Reference")]
    public IngredientData myData;

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

    public void StartCookingLock(float cookTime)
    {
        StartCoroutine(CookingLockRoutine(cookTime));
    }

    private IEnumerator CookingLockRoutine(float cookTime)
    {
        isLocked = true;
        yield return new WaitForSeconds(cookTime);
        isLocked = false;
    }

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
        currentStation.SetOccupancy(currentCoordinate, myData.shapeOffsets, this); // Send "this" script reference

        itemCollider.enabled = true;
        CheckForCombinations(); // NEW: Check neighbors after landing
    }

    private void CookItem(GridTileVisual tile)
    {
        Vector3 spawnPosition = tile.transform.position + new Vector3(0, 0.1f, 0);
        GameObject cookedItem = Instantiate(myData.cookedPrefab, spawnPosition, Quaternion.identity);
        Draggable3DItem cookedScript = cookedItem.GetComponent<Draggable3DItem>();

        cookedScript.currentStation = tile.parentStation;
        cookedScript.currentCoordinate = tile.gridCoordinate;
        tile.parentStation.SetOccupancy(tile.gridCoordinate, cookedScript.myData.shapeOffsets, cookedScript);

        cookedScript.StartCookingLock(myData.cookTime);
        cookedScript.CheckForCombinations(); // Check if cooking it instantly created a combo

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

    // NEW: Adjacency Crafting Logic
    public void CheckForCombinations()
    {
        if (currentStation.stationType != StationType.Stove) return;

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int checkCoord = currentCoordinate + dir;
            Draggable3DItem adjacentItem = currentStation.GetOccupantAt(checkCoord);

            if (adjacentItem != null && adjacentItem != this)
            {
                // 1. Check if WE have a recipe that requires THEM
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

                // 2. Check if THEY have a recipe that requires US
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
        // Decide which tile the final food spawns on
        GridTileVisual spawnTile = combo.spawnOnPartnerTile ?
            partner.currentStation.tileVisuals[partner.currentCoordinate.x, partner.currentCoordinate.y] :
            initiator.currentStation.tileVisuals[initiator.currentCoordinate.x, initiator.currentCoordinate.y];

        BaseStation station = initiator.currentStation;

        // Free up the original grid spaces
        station.SetOccupancy(initiator.currentCoordinate, initiator.myData.shapeOffsets, null);
        station.SetOccupancy(partner.currentCoordinate, partner.myData.shapeOffsets, null);

        // Spawn result
        Vector3 spawnPos = spawnTile.transform.position + new Vector3(0, 0.1f, 0);
        GameObject resultObj = Instantiate(combo.resultPrefab, spawnPos, Quaternion.identity);
        Draggable3DItem resultItem = resultObj.GetComponent<Draggable3DItem>();

        resultItem.currentStation = station;
        resultItem.currentCoordinate = spawnTile.gridCoordinate;
        station.SetOccupancy(resultItem.currentCoordinate, resultItem.myData.shapeOffsets, resultItem);

        resultItem.StartCookingLock(combo.cookTime);

        // Clean up the raw ingredients
        Destroy(initiator.gameObject);
        Destroy(partner.gameObject);
    }
}