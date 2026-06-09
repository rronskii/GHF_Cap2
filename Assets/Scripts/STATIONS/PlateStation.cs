using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlateStation : BaseStation
{
    [Header("Serving Mechanics")]
    public Transform plateModel;
    public GameObject plateModelPrefab;
    public float serveSpeed = 8f;
    public float serveDistance = 10f;

    private bool isServing = false;
    private Vector3 initialPlateLocalPos; // NEW: Remembers your original (-0.5) Y offset!

    protected override void InitializeGrid()
    {
        base.InitializeGrid();

        // Trim the corners for the round plate
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                bool isCorner = (x == 0 && y == 0) ||
                               (x == 0 && y == gridHeight - 1) ||
                               (x == gridWidth - 1 && y == 0) ||
                               (x == gridWidth - 1 && y == gridHeight - 1);

                if (isCorner)
                {
                    gridMatrix[x, y].isValid = false;
                    if (tileVisuals[x, y] != null) tileVisuals[x, y].gameObject.SetActive(false);
                }
            }
        }

        // NEW: Memorize exactly where the plate model is sitting relative to the parent
        if (plateModel != null)
        {
            initialPlateLocalPos = plateModel.localPosition;
        }
    }

    public void ServePlate()
    {
        if (isServing) return;
        StartCoroutine(ServeRoutine());
    }

    private IEnumerator ServeRoutine()
    {
        // Create our tracking collections
        List<IngredientData> ingredientsOnPlate = new List<IngredientData>();
        HashSet<Draggable3DItem> foodScripts = new HashSet<Draggable3DItem>();

        // 1. Gather all UNIQUE food item scripts from the grid layout matrix first
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (occupantMatrix[x, y] != null)
                {
                    foodScripts.Add(occupantMatrix[x, y]); // HashSets automatically ignore duplicates
                }
            }
        }

        // 2. Extract the actual ingredient data from our clean, unique list of items
        foreach (Draggable3DItem uniqueFood in foodScripts)
        {
            ingredientsOnPlate.Add(uniqueFood.myData);
        }

        // NEW LOGIC: Ask OrderManager if this mixture forms a real menu item
        DishData validatedDish = OrderManager.Instance.ValidateRecipe(ingredientsOnPlate);

        if (validatedDish == null)
        {
            Debug.Log("[Plate Station] This configuration does not form a valid menu item. Bell ring rejected!");
            yield break; // Abort completely. The food stays put on plate!
        }

        // NEW LOGIC: Check if there is an available counter window slot to take it
        bool sentToWindowSuccess = OrderManager.Instance.TrySpawnDishToWindow(validatedDish);
        if (!sentToWindowSuccess)
        {
            Debug.Log("[Plate Station] Counter window display slots are full!");
            yield break; // Abort out. Food remains preserved on plate until room is cleared.
        }

        // Proceed with clearing plate since validation succeeded!
        isServing = true;

        // Clear the logical grid occupancy matrices
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                occupantMatrix[x, y] = null;
                gridMatrix[x, y].isOccupied = false;
            }
        }

        // Lock the food objects and parent them to the physical plate model to slide away
        foreach (Draggable3DItem food in foodScripts)
        {
            food.GetComponent<Collider>().enabled = false;
            food.transform.SetParent(plateModel);
        }

        // Hide interactive grid visuals
        foreach (GridTileVisual tile in tileVisuals)
        {
            if (tile != null) tile.gameObject.SetActive(false);
        }

        // Slide physical plate + food structures away along Z axis
        Vector3 targetPosition = plateModel.position + new Vector3(0, 0, serveDistance);
        while (Vector3.Distance(plateModel.position, targetPosition) > 0.05f)
        {
            plateModel.position = Vector3.MoveTowards(plateModel.position, targetPosition, serveSpeed * Time.deltaTime);
            yield return null;
        }

        // Cleanup and delete old instances
        Destroy(plateModel.gameObject);
        yield return new WaitForSeconds(0.5f);

        // Reconstruct and pop in replacement clean plate
        GameObject newPlate = Instantiate(plateModelPrefab, transform);
        newPlate.transform.localPosition = initialPlateLocalPos;
        plateModel = newPlate.transform;

        Vector3 finalScale = newPlate.transform.localScale;
        newPlate.transform.localScale = Vector3.zero;

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 5f;
            newPlate.transform.localScale = Vector3.Lerp(Vector3.zero, finalScale, t);
            yield return null;
        }

        // Re-enable grid visuals
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                bool isCorner = (x == 0 && y == 0) ||
                               (x == 0 && y == gridHeight - 1) ||
                               (x == gridWidth - 1 && y == 0) ||
                               (x == gridWidth - 1 && y == gridHeight - 1);

                if (tileVisuals[x, y] != null && !isCorner)
                {
                    tileVisuals[x, y].gameObject.SetActive(true);
                }
            }
        }

        isServing = false;
    }
}