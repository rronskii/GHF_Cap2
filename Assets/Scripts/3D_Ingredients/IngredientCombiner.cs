using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Draggable3DItem))]
public class IngredientCombiner : MonoBehaviour
{
    private Draggable3DItem core;
    private IngredientVisuals visuals;
    private IngredientProcessor processor;

    private void Awake()
    {
        core = GetComponent<Draggable3DItem>();
        visuals = GetComponent<IngredientVisuals>();
        processor = GetComponent<IngredientProcessor>();
    }

    public bool CheckForCombinations()
    {
        if (core.currentStation == null || !core.currentStation.isCookingStation) return false;

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int checkCoord = core.currentCoordinate + dir;
            Draggable3DItem adjacentItem = core.currentStation.GetOccupantAt(checkCoord);

            if (adjacentItem != null && adjacentItem != core && !adjacentItem.isLocked)
            {
                if (core.myData.combinations != null)
                {
                    foreach (RecipeCombo combo in core.myData.combinations)
                    {
                        if (combo.partnerIngredient == adjacentItem.myData)
                        {
                            ExecuteCombination(core, adjacentItem, combo);
                            return true;
                        }
                    }
                }

                if (adjacentItem.myData.combinations != null)
                {
                    foreach (RecipeCombo combo in adjacentItem.myData.combinations)
                    {
                        if (combo.partnerIngredient == core.myData)
                        {
                            ExecuteCombination(adjacentItem, core, combo);
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    private void ExecuteCombination(Draggable3DItem initiator, Draggable3DItem partner, RecipeCombo combo)
    {
        GridTileVisual spawnTile;
        if (combo.spawnOnPartnerTile)
        {
            spawnTile = partner.currentStation.tileVisuals[partner.currentCoordinate.x, partner.currentCoordinate.y];
        }
        else
        {
            spawnTile = initiator.currentStation.tileVisuals[initiator.currentCoordinate.x, initiator.currentCoordinate.y];
        }

        BaseStation station = initiator.currentStation;

        // UPDATED: Clear using their respective rotated offsets
        station.SetOccupancy(initiator.currentCoordinate, initiator.GetCurrentRotatedOffsets(), null);
        station.SetOccupancy(partner.currentCoordinate, partner.GetCurrentRotatedOffsets(), null);

        initiator.isLocked = true;
        partner.isLocked = true;

        if (initiator.itemCollider != null) initiator.itemCollider.enabled = false;
        if (partner.itemCollider != null) partner.itemCollider.enabled = false;

        IngredientProcessor initiatorProcessor = initiator.GetComponent<IngredientProcessor>();
        if (initiatorProcessor != null)
        {
            initiatorProcessor.StopBurning();
        }

        IngredientProcessor partnerProcessor = partner.GetComponent<IngredientProcessor>();
        if (partnerProcessor != null)
        {
            partnerProcessor.StopBurning();
        }

        IngredientCombiner initiatorCombiner = initiator.GetComponent<IngredientCombiner>();
        if (initiatorCombiner != null)
        {
            initiator.StartCoroutine(initiatorCombiner.MergeAndCookRoutine(partner, combo, station, spawnTile));
        }
    }

    public IEnumerator MergeAndCookRoutine(Draggable3DItem partner, RecipeCombo combo, BaseStation station, GridTileVisual spawnTile)
    {
        Vector3 startPos = partner.transform.position;
        Vector3 targetPos = transform.position;
        Vector3 direction = (startPos - targetPos).normalized;
        Vector3 pullBackPos = startPos + (direction * 0.3f);

        float pullDuration = 0.15f;
        float elapsed = 0f;

        while (elapsed < pullDuration)
        {
            elapsed += Time.deltaTime;
            partner.transform.position = Vector3.Lerp(startPos, pullBackPos, elapsed / pullDuration);
            yield return null;
        }

        float snapDuration = 0.1f;
        elapsed = 0f;

        while (elapsed < snapDuration)
        {
            elapsed += Time.deltaTime;
            partner.transform.position = Vector3.Lerp(pullBackPos, targetPos, elapsed / snapDuration);
            yield return null;
        }

        Destroy(partner.gameObject);

        Vector3 finalPos = spawnTile.transform.position + new Vector3(0, 0.1f, 0);
        transform.position = finalPos;
        core.currentStation = station;
        core.currentCoordinate = spawnTile.gridCoordinate;

        if (combo.cookTime > 0)
        {
            // UPDATED: Re-occupy using the rotated offsets while it cooks
            station.SetOccupancy(core.currentCoordinate, core.GetCurrentRotatedOffsets(), core);
            if (visuals != null) visuals.StartPopAnimation();

            core.isLocked = true;

            // 1. Borrow the timer from the processor
            CookingTimerUI activeTimer = null;
            if (processor != null)
            {
                activeTimer = processor.SpawnCookingTimer(combo.cookTime);
            }

            // 2. Actually wait for the cooking to finish!
            yield return new WaitForSeconds(combo.cookTime);

            // 3. Unparent the timer so it doesn't get destroyed with the raw ingredient
            if (activeTimer != null)
            {
                activeTimer.transform.SetParent(null);
            }

            SpawnComboResult(combo, station, spawnTile);
        }
        else
        {
            SpawnComboResult(combo, station, spawnTile);
        }
    }

    private void SpawnComboResult(RecipeCombo combo, BaseStation station, GridTileVisual spawnTile)
    {
        station.SetOccupancy(core.currentCoordinate, core.GetCurrentRotatedOffsets(), null);

        Vector3 spawnPos = spawnTile.transform.position + new Vector3(0, 0.1f, 0);
        GameObject resultObj = Instantiate(combo.resultPrefab, spawnPos, Quaternion.identity);
        Draggable3DItem resultItem = resultObj.GetComponent<Draggable3DItem>();

        // NEW: Inherit the rotation of the initiator!
        resultItem.currentRotationSteps = core.currentRotationSteps;
        resultObj.transform.rotation = transform.rotation;

        resultItem.currentStation = station;
        resultItem.currentCoordinate = spawnTile.gridCoordinate;

        // UPDATED: Set occupancy using the new rotated shape
        station.SetOccupancy(resultItem.currentCoordinate, resultItem.GetCurrentRotatedOffsets(), resultItem);

        IngredientVisuals resultVisuals = resultItem.GetComponent<IngredientVisuals>();
        if (resultVisuals != null)
        {
            resultVisuals.StartPopAnimation();
        }

        bool combined = false;
        IngredientCombiner newCombiner = resultItem.GetComponent<IngredientCombiner>();
        if (newCombiner != null)
        {
            combined = newCombiner.CheckForCombinations();
        }

        if (!combined)
        {
            IngredientProcessor resultProcessor = resultItem.GetComponent<IngredientProcessor>();
            if (resultProcessor != null)
            {
                resultProcessor.EvaluateBurnState();
            }
        }

        Destroy(gameObject);
    }
}