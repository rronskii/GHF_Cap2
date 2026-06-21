using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Draggable3DItem : MonoBehaviour
{
    [Header("Data Reference")]
    public IngredientData myData;

    [Header("Visual Prefabs")]
    [SerializeField] private GameObject timerUIPrefab;

    [Header("Physics Settings")]
    public LayerMask gridLayerMask;

    [Header("Placement Memory")]
    public BaseStation currentStation;
    public Vector2Int currentCoordinate;

    public bool isLocked = false;
    public Collider itemCollider { get; private set; }

    private Camera mainCamera;
    private Vector3 startPosition;
    private BaseStation lastHoveredStation;

    private bool isDragging = false;
    private Coroutine burnCoroutine;
    private CookingTimerUI activeBurnTimer;
    private Vector3 baseScale;
    private Coroutine hoverCoroutine;

    // NEW: Remembers how much the item has burned across pickups!
    private float accumulatedBurnTime = 0f;

    private void Awake()
    {
        mainCamera = Camera.main;
        itemCollider = GetComponent<Collider>();
        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        StationCameraController.OnStationChanged += ForceDrop3DItem;
    }

    private void OnDisable()
    {
        StationCameraController.OnStationChanged -= ForceDrop3DItem;
    }

    private void ForceDrop3DItem(int stationIndex)
    {
        if (isDragging)
        {
            isDragging = false;
            if (lastHoveredStation != null) lastHoveredStation.ClearAllHighlights();
            HandleInvalidDrop();
        }
    }

    // --- VISUAL FEEDBACK LOGIC ---

    public void SetHoverGrowth(bool isHovering)
    {
        if (hoverCoroutine != null) StopCoroutine(hoverCoroutine);
        Vector3 targetScale = isHovering ? baseScale * 1.2f : baseScale;
        hoverCoroutine = StartCoroutine(ScaleToTarget(targetScale));
    }

    private IEnumerator ScaleToTarget(Vector3 target)
    {
        float speed = 12f;
        while (Vector3.Distance(transform.localScale, target) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * speed);
            yield return null;
        }
        transform.localScale = target;
    }

    public void StartPopAnimation()
    {
        StartCoroutine(PopRoutine());
    }

    private IEnumerator PopRoutine()
    {
        Vector3 targetScale = baseScale * 1.3f;
        float halfDuration = 0.15f;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            transform.localScale = Vector3.Lerp(baseScale, targetScale, elapsed / halfDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            transform.localScale = Vector3.Lerp(targetScale, baseScale, elapsed / halfDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = baseScale;
    }

    public void TrashItem()
    {
        StopBurning();
        itemCollider.enabled = false;

        if (currentStation != null) currentStation.SetOccupancy(currentCoordinate, myData.shapeOffsets, null);

        StartCoroutine(PopOutRoutine());
    }

    private IEnumerator PopOutRoutine()
    {
        Vector3 startScale = transform.localScale;
        float duration = 0.15f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / duration);
            yield return null;
        }
        Destroy(gameObject);
    }

    // --- COOKING & BURNING LOGIC ---

    public void StartCookingLock(float cookTime)
    {
        StartCoroutine(CookingLockRoutine(cookTime));
    }

    private IEnumerator CookingLockRoutine(float cookTime)
    {
        isLocked = true;
        CookingTimerUI activeTimer = SpawnCookingTimer(cookTime);

        yield return new WaitForSeconds(cookTime);

        if (activeTimer != null) activeTimer.transform.SetParent(null);

        StartPopAnimation();
        isLocked = false;

        bool combined = CheckForCombinations();
        if (!combined) EvaluateBurnState();
    }

    private IEnumerator CookAndTransformRoutine(float cookTime, Vector2Int[] reservedOffsets)
    {
        isLocked = true;
        CookingTimerUI activeTimer = SpawnCookingTimer(cookTime);

        yield return new WaitForSeconds(cookTime);

        if (activeTimer != null) activeTimer.transform.SetParent(null);

        GameObject cookedItem = Instantiate(myData.cookedPrefab, transform.position, Quaternion.identity);
        Draggable3DItem cookedScript = cookedItem.GetComponent<Draggable3DItem>();

        cookedScript.currentStation = currentStation;
        cookedScript.currentCoordinate = currentCoordinate;
        currentStation.SetOccupancy(currentCoordinate, reservedOffsets, cookedScript);

        cookedScript.StartPopAnimation();

        bool combined = cookedScript.CheckForCombinations();
        if (!combined) cookedScript.EvaluateBurnState();

        Destroy(gameObject);
    }

    public void EvaluateBurnState()
    {
        if (currentStation != null && currentStation.stationType == StationType.Stove && myData.canBurn && myData.burntPrefab != null && !isLocked)
        {
            if (burnCoroutine == null)
            {
                burnCoroutine = StartCoroutine(BurnRoutine());
            }
        }
        else
        {
            StopBurning();
        }
    }

    private IEnumerator BurnRoutine()
    {
        // UPDATED: Spawns the timer starting from wherever it was paused
        activeBurnTimer = SpawnCookingTimer(myData.burnTime, true, accumulatedBurnTime);

        float remainingTime = myData.burnTime - accumulatedBurnTime;
        yield return new WaitForSeconds(remainingTime);

        if (activeBurnTimer != null) Destroy(activeBurnTimer.gameObject);

        GameObject burntItem = Instantiate(myData.burntPrefab, transform.position, Quaternion.identity);
        Draggable3DItem burntScript = burntItem.GetComponent<Draggable3DItem>();

        burntScript.currentStation = currentStation;
        burntScript.currentCoordinate = currentCoordinate;
        currentStation.SetOccupancy(currentCoordinate, burntScript.myData.shapeOffsets, burntScript);

        burntScript.StartPopAnimation();
        Destroy(gameObject);
    }

    private void StopBurning()
    {
        if (activeBurnTimer != null)
        {
            // UPDATED: Save the exact time before destroying the UI!
            accumulatedBurnTime = activeBurnTimer.GetCurrentTime();
            Destroy(activeBurnTimer.gameObject);
            activeBurnTimer = null;
        }
        if (burnCoroutine != null)
        {
            StopCoroutine(burnCoroutine);
            burnCoroutine = null;
        }
    }

    // UPDATED: Passes the startTime down to the UI
    private CookingTimerUI SpawnCookingTimer(float duration, bool isBurnTimer = false, float startAt = 0f)
    {
        if (timerUIPrefab == null) return null;

        Physics.SyncTransforms();

        Vector3 visualCenter = itemCollider != null ? itemCollider.bounds.center : transform.position;
        Vector3 spawnPos = new Vector3(visualCenter.x, transform.position.y + 1.2f, visualCenter.z);

        GameObject timerObj = Instantiate(timerUIPrefab, spawnPos, Quaternion.identity, this.transform);

        CookingTimerUI timerScript = timerObj.GetComponent<CookingTimerUI>();
        if (timerScript != null)
        {
            timerScript.StartTimer(duration, isBurnTimer, startAt);
        }

        return timerScript;
    }

    // --- CHOPPING LOGIC ---

    public bool TryChop()
    {
        if (!myData.isChoppable || myData.choppedPrefab == null) return false;
        if (currentStation == null || currentStation.stationType != StationType.ChoppingBoard) return false;

        Draggable3DItem choppedScriptRef = myData.choppedPrefab.GetComponent<Draggable3DItem>();
        Vector2Int[] newOffsets = choppedScriptRef != null ? choppedScriptRef.myData.shapeOffsets : myData.shapeOffsets;

        currentStation.SetOccupancy(currentCoordinate, myData.shapeOffsets, null);

        if (!currentStation.CanPlaceItem(currentCoordinate, newOffsets))
        {
            currentStation.SetOccupancy(currentCoordinate, myData.shapeOffsets, this);
            return false;
        }

        GameObject choppedItem = Instantiate(myData.choppedPrefab, transform.position, Quaternion.identity);
        Draggable3DItem choppedScript = choppedItem.GetComponent<Draggable3DItem>();

        choppedScript.currentStation = currentStation;
        choppedScript.currentCoordinate = currentCoordinate;
        currentStation.SetOccupancy(currentCoordinate, newOffsets, choppedScript);

        choppedScript.StartPopAnimation();
        choppedScript.CheckForCombinations();

        Destroy(gameObject);
        return true;
    }

    // --- DRAG MECHANICS ---

    private void OnMouseDown()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
        if (isLocked) return;

        isDragging = true;
        StopBurning(); // Saves the time and pauses the burn!

        startPosition = transform.position;
        itemCollider.enabled = false;

        if (currentStation != null) currentStation.SetOccupancy(currentCoordinate, myData.shapeOffsets, null);
    }

    private void OnMouseDrag()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
        if (isLocked || !isDragging) return;

        Plane dragPlane = new Plane(Vector3.up, new Vector3(0, startPosition.y, 0));
        Ray mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (dragPlane.Raycast(mouseRay, out float distance))
        {
            transform.position = Vector3.Lerp(transform.position, mouseRay.GetPoint(distance), Time.deltaTime * 20f);
        }

        if (Physics.Raycast(mouseRay, out RaycastHit hit, Mathf.Infinity, gridLayerMask))
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
        if (isLocked || !isDragging) return;
        isDragging = false;

        if (lastHoveredStation != null) lastHoveredStation.ClearAllHighlights();

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit trashHit, Mathf.Infinity))
        {
            if (trashHit.collider.GetComponent<TrashDropZone>() != null)
            {
                TrashItem();
                return;
            }
        }

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, gridLayerMask))
        {
            GridTileVisual tile = hit.collider.GetComponent<GridTileVisual>();
            if (tile != null && TryDropOnTile(tile)) return;
        }

        HandleInvalidDrop();
    }

    private bool TryDropOnTile(GridTileVisual tile)
    {
        if (!myData.validStations.Contains(tile.parentStation.stationType)) return false;

        bool willCook = myData.isCookable && tile.parentStation.stationType == StationType.Stove && myData.cookedPrefab != null;

        Vector2Int[] offsetsToCheck = myData.shapeOffsets;
        if (willCook)
        {
            Draggable3DItem cookedScript = myData.cookedPrefab.GetComponent<Draggable3DItem>();
            if (cookedScript != null) offsetsToCheck = cookedScript.myData.shapeOffsets;
        }

        if (!tile.parentStation.CanPlaceItem(tile.gridCoordinate, offsetsToCheck)) return false;

        ForceDropOnTile(tile);
        return true;
    }

    public void ForceDropOnTile(GridTileVisual tile)
    {
        bool willCook = myData.isCookable && tile.parentStation.stationType == StationType.Stove && myData.cookedPrefab != null;

        if (willCook)
        {
            if (myData.instantCookOnStove) InstantTransformAndCook(tile);
            else CookItem(tile);
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
        currentStation.SetOccupancy(currentCoordinate, myData.shapeOffsets, this);

        itemCollider.enabled = true;

        bool combined = CheckForCombinations();
        if (!combined) EvaluateBurnState();
    }

    private void InstantTransformAndCook(GridTileVisual tile)
    {
        Vector3 spawnPosition = tile.transform.position + new Vector3(0, 0.1f, 0);
        GameObject cookedItem = Instantiate(myData.cookedPrefab, spawnPosition, Quaternion.identity);
        Draggable3DItem cookedScript = cookedItem.GetComponent<Draggable3DItem>();

        cookedScript.currentStation = tile.parentStation;
        cookedScript.currentCoordinate = tile.gridCoordinate;
        tile.parentStation.SetOccupancy(tile.gridCoordinate, cookedScript.myData.shapeOffsets, cookedScript);

        cookedScript.StartCookingLock(myData.cookTime);
        Destroy(gameObject);
    }

    private void CookItem(GridTileVisual tile)
    {
        transform.position = tile.transform.position + new Vector3(0, 0.1f, 0);

        currentStation = tile.parentStation;
        currentCoordinate = tile.gridCoordinate;

        Draggable3DItem cookedScriptRef = myData.cookedPrefab.GetComponent<Draggable3DItem>();
        Vector2Int[] offsetsToReserve = cookedScriptRef != null ? cookedScriptRef.myData.shapeOffsets : myData.shapeOffsets;

        currentStation.SetOccupancy(currentCoordinate, offsetsToReserve, this);
        itemCollider.enabled = true;

        StartCoroutine(CookAndTransformRoutine(myData.cookTime, offsetsToReserve));
    }

    private void HandleInvalidDrop()
    {
        transform.position = startPosition;
        itemCollider.enabled = true;

        if (currentStation != null)
        {
            currentStation.SetOccupancy(currentCoordinate, myData.shapeOffsets, this);
        }

        EvaluateBurnState();
    }

    // --- ADJACENCY CRAFTING ---

    public bool CheckForCombinations()
    {
        if (currentStation == null || currentStation.stationType != StationType.Stove) return false;

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int checkCoord = currentCoordinate + dir;
            Draggable3DItem adjacentItem = currentStation.GetOccupantAt(checkCoord);

            if (adjacentItem != null && adjacentItem != this && !adjacentItem.isLocked)
            {
                if (myData.combinations != null)
                {
                    foreach (RecipeCombo combo in myData.combinations)
                    {
                        if (combo.partnerIngredient == adjacentItem.myData)
                        {
                            ExecuteCombination(this, adjacentItem, combo);
                            return true;
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
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    // UPDATED: Now delegates the actual sequence to a Coroutine hosted by the Initiator
    private void ExecuteCombination(Draggable3DItem initiator, Draggable3DItem partner, RecipeCombo combo)
    {
        GridTileVisual spawnTile = combo.spawnOnPartnerTile ?
            partner.currentStation.tileVisuals[partner.currentCoordinate.x, partner.currentCoordinate.y] :
            initiator.currentStation.tileVisuals[initiator.currentCoordinate.x, initiator.currentCoordinate.y];

        BaseStation station = initiator.currentStation;

        station.SetOccupancy(initiator.currentCoordinate, initiator.myData.shapeOffsets, null);
        station.SetOccupancy(partner.currentCoordinate, partner.myData.shapeOffsets, null);

        initiator.isLocked = true;
        partner.isLocked = true;
        initiator.itemCollider.enabled = false;
        partner.itemCollider.enabled = false;

        initiator.StopBurning();
        partner.StopBurning();

        // Run the sequence on the initiator, since the partner is going to get destroyed!
        initiator.StartCoroutine(initiator.MergeAndCookRoutine(partner, combo, station, spawnTile));
    }

    // NEW: Multi-phase routine that absorbs the partner, cooks, and then transforms
    private IEnumerator MergeAndCookRoutine(Draggable3DItem partner, RecipeCombo combo, BaseStation station, GridTileVisual spawnTile)
    {
        Vector3 startPos = partner.transform.position;
        Vector3 targetPos = transform.position; // Initiator position

        Vector3 direction = (startPos - targetPos).normalized;
        Vector3 pullBackPos = startPos + (direction * 0.3f);

        float pullDuration = 0.15f;
        float elapsed = 0f;

        // 1. Tension Pull
        while (elapsed < pullDuration)
        {
            elapsed += Time.deltaTime;
            partner.transform.position = Vector3.Lerp(startPos, pullBackPos, elapsed / pullDuration);
            yield return null;
        }

        float snapDuration = 0.1f;
        elapsed = 0f;

        // 2. Snap Release
        while (elapsed < snapDuration)
        {
            elapsed += Time.deltaTime;
            partner.transform.position = Vector3.Lerp(pullBackPos, targetPos, elapsed / snapDuration);
            yield return null;
        }

        // 3. Partner is Absorbed!
        Destroy(partner.gameObject);

        // 4. Move Initiator to final tile and reserve space
        Vector3 finalPos = spawnTile.transform.position + new Vector3(0, 0.1f, 0);
        transform.position = finalPos;
        currentStation = station;
        currentCoordinate = spawnTile.gridCoordinate;

        // Check if there is a cooking phase for this combo (like Fried Rice)
        if (combo.cookTime > 0)
        {
            station.SetOccupancy(currentCoordinate, myData.shapeOffsets, this);
            StartPopAnimation(); // Visually confirm absorption

            isLocked = true;
            CookingTimerUI activeTimer = SpawnCookingTimer(combo.cookTime);

            yield return new WaitForSeconds(combo.cookTime);

            if (activeTimer != null) activeTimer.transform.SetParent(null);

            SpawnComboResult(combo, station, spawnTile);
        }
        else
        {
            // Instant combinations
            SpawnComboResult(combo, station, spawnTile);
        }
    }

    private void SpawnComboResult(RecipeCombo combo, BaseStation station, GridTileVisual spawnTile)
    {
        Vector3 spawnPos = spawnTile.transform.position + new Vector3(0, 0.1f, 0);
        GameObject resultObj = Instantiate(combo.resultPrefab, spawnPos, Quaternion.identity);
        Draggable3DItem resultItem = resultObj.GetComponent<Draggable3DItem>();

        resultItem.currentStation = station;
        resultItem.currentCoordinate = spawnTile.gridCoordinate;
        station.SetOccupancy(resultItem.currentCoordinate, resultItem.myData.shapeOffsets, resultItem);

        resultItem.StartPopAnimation();

        bool combined = resultItem.CheckForCombinations();
        if (!combined) resultItem.EvaluateBurnState();

        Destroy(gameObject); // Cleanup the initiator
    }
}