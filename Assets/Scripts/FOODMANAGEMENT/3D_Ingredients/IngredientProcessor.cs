using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Draggable3DItem))]
[RequireComponent(typeof(IngredientVisuals))]
public class IngredientProcessor : MonoBehaviour
{
    [Header("Visual Prefabs")]
    [SerializeField] private GameObject timerUIPrefab;

    private Draggable3DItem core;
    private IngredientVisuals visuals;
    private Coroutine burnCoroutine;
    private CookingTimerUI activeBurnTimer;
    private float accumulatedBurnTime = 0f;

    private void Awake()
    {
        core = GetComponent<Draggable3DItem>();
        visuals = GetComponent<IngredientVisuals>();
    }

    // --- CHOPPING ---
    public bool TryChop()
    {
        if (!core.myData.isChoppable || core.myData.choppedPrefab == null) return false;
        if (core.currentStation == null || core.currentStation.stationType != StationType.ChoppingBoard) return false;

        Draggable3DItem choppedScriptRef = core.myData.choppedPrefab.GetComponent<Draggable3DItem>();
        Vector2Int[] newOffsets = core.GetCurrentRotatedOffsets();
        if (choppedScriptRef != null)
        {
            newOffsets = choppedScriptRef.myData.shapeOffsets;
        }

        // 1. Clear the old rotated shape
        core.currentStation.SetOccupancy(core.currentCoordinate, core.GetCurrentRotatedOffsets(), null);

        // 2. We use the raw offsets here just to see if the NEW item fits mathematically
        if (!core.currentStation.CanPlaceItem(core.currentCoordinate, newOffsets))
        {
            core.currentStation.SetOccupancy(core.currentCoordinate, core.GetCurrentRotatedOffsets(), core);
            return false;
        }

        GameObject choppedItem = Instantiate(core.myData.choppedPrefab, transform.position, Quaternion.identity);
        Draggable3DItem choppedScript = choppedItem.GetComponent<Draggable3DItem>();

        // 3. NEW: Pass the rotation state to the new object!
        choppedScript.currentRotationSteps = core.currentRotationSteps;
        choppedItem.transform.rotation = transform.rotation;

        choppedScript.currentStation = core.currentStation;
        choppedScript.currentCoordinate = core.currentCoordinate;

        // 4. Set occupancy using the new object's rotated shape
        core.currentStation.SetOccupancy(core.currentCoordinate, choppedScript.GetCurrentRotatedOffsets(), choppedScript);

        IngredientVisuals choppedVisuals = choppedItem.GetComponent<IngredientVisuals>();
        if (choppedVisuals != null)
        {
            choppedVisuals.StartPopAnimation();
        }

        IngredientCombiner choppedCombiner = choppedItem.GetComponent<IngredientCombiner>();
        if (choppedCombiner != null)
        {
            choppedCombiner.CheckForCombinations();
        }

        Destroy(gameObject);
        return true;
    }

    // --- COOKING & BURNING ---
    public void StartCookingLock(float cookTime)
    {
        StartCoroutine(CookingLockRoutine(cookTime));
    }

    private IEnumerator CookingLockRoutine(float cookTime)
    {
        core.isLocked = true;
        CookingTimerUI activeTimer = SpawnCookingTimer(cookTime);

        yield return new WaitForSeconds(cookTime);

        if (activeTimer != null) activeTimer.transform.SetParent(null);

        visuals.StartPopAnimation();
        core.isLocked = false;

        bool combined = false;
        IngredientCombiner combiner = GetComponent<IngredientCombiner>();
        if (combiner != null)
        {
            combined = combiner.CheckForCombinations();
        }

        if (!combined) EvaluateBurnState();
    }

    public IEnumerator CookAndTransformRoutine(float cookTime, Vector2Int[] reservedOffsets)
    {
        core.isLocked = true;
        CookingTimerUI activeTimer = SpawnCookingTimer(cookTime);

        yield return new WaitForSeconds(cookTime);

        if (activeTimer != null) activeTimer.transform.SetParent(null);

        core.currentStation.SetOccupancy(core.currentCoordinate, reservedOffsets, null);

        GameObject cookedItem = Instantiate(core.myData.cookedPrefab, transform.position, Quaternion.identity);
        Draggable3DItem cookedScript = cookedItem.GetComponent<Draggable3DItem>();

        // NEW: Pass the rotation!
        cookedScript.currentRotationSteps = core.currentRotationSteps;
        cookedItem.transform.rotation = transform.rotation;

        cookedScript.currentStation = core.currentStation;
        cookedScript.currentCoordinate = core.currentCoordinate;

        // UPDATED: Use the new rotated offsets
        core.currentStation.SetOccupancy(core.currentCoordinate, cookedScript.GetCurrentRotatedOffsets(), cookedScript);

        IngredientVisuals cookedVisuals = cookedItem.GetComponent<IngredientVisuals>();
        if (cookedVisuals != null)
        {
            cookedVisuals.StartPopAnimation();
        }

        bool combined = false;
        IngredientCombiner newCombiner = cookedItem.GetComponent<IngredientCombiner>();
        if (newCombiner != null)
        {
            combined = newCombiner.CheckForCombinations();
        }

        if (!combined)
        {
            IngredientProcessor cookedProcessor = cookedItem.GetComponent<IngredientProcessor>();
            if (cookedProcessor != null)
            {
                cookedProcessor.EvaluateBurnState();
            }
        }

        Destroy(gameObject);
    }

    public void EvaluateBurnState()
    {
        if (core.currentStation != null && core.currentStation.stationType == StationType.Stove && core.myData.canBurn && core.myData.burntPrefab != null && !core.isLocked)
        {
            if (burnCoroutine == null) burnCoroutine = StartCoroutine(BurnRoutine());
        }
        else
        {
            StopBurning();
        }
    }

    private IEnumerator BurnRoutine()
    {
        activeBurnTimer = SpawnCookingTimer(core.myData.burnTime, true, accumulatedBurnTime);

        float remainingTime = core.myData.burnTime - accumulatedBurnTime;
        yield return new WaitForSeconds(remainingTime);

        if (activeBurnTimer != null) Destroy(activeBurnTimer.gameObject);

        core.currentStation.SetOccupancy(core.currentCoordinate, core.GetCurrentRotatedOffsets(), null);

        GameObject burntItem = Instantiate(core.myData.burntPrefab, transform.position, Quaternion.identity);
        Draggable3DItem burntScript = burntItem.GetComponent<Draggable3DItem>();

        // NEW: Pass the rotation!
        burntScript.currentRotationSteps = core.currentRotationSteps;
        burntItem.transform.rotation = transform.rotation;

        burntScript.currentStation = core.currentStation;
        burntScript.currentCoordinate = core.currentCoordinate;

        // UPDATED: Use the new rotated offsets
        core.currentStation.SetOccupancy(core.currentCoordinate, burntScript.GetCurrentRotatedOffsets(), burntScript);

        IngredientVisuals burntVisuals = burntItem.GetComponent<IngredientVisuals>();
        if (burntVisuals != null)
        {
            burntVisuals.StartPopAnimation();
        }
        Destroy(gameObject);
    }

    public void StopBurning()
    {
        if (activeBurnTimer != null)
        {
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

    public CookingTimerUI SpawnCookingTimer(float duration, bool isBurnTimer = false, float startAt = 0f)
    {
        if (timerUIPrefab == null) return null;
        Physics.SyncTransforms();

        Vector3 visualCenter = transform.position;
        if (core.itemCollider != null)
        {
            visualCenter = core.itemCollider.bounds.center;
        }

        Vector3 spawnPos = new Vector3(visualCenter.x, transform.position.y + 1.2f, visualCenter.z);

        GameObject timerObj = Instantiate(timerUIPrefab, spawnPos, Quaternion.identity, this.transform);
        CookingTimerUI timerScript = timerObj.GetComponent<CookingTimerUI>();
        if (timerScript != null)
        {
            timerScript.StartTimer(duration, isBurnTimer, startAt);
        }

        return timerScript;
    }
}