using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlateStation : BaseStation
{
    [Header("Serving Mechanics")]
    public Transform plateModel;
    public GameObject plateModelPrefab;
    public Transform stackSpawnPoint;

    public float serveSpeed = 20f;
    public float serveDistance = 10f;
    public float respawnDelay = 0.2f;
    public float popInSpeed = 10f;

    [HideInInspector] public bool isServing = false;
    private Vector3 initialPlateLocalPos;

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

        if (plateModel != null)
        {
            initialPlateLocalPos = plateModel.localPosition;
        }
    }

    // --- NEW: Helper method for the Bell ---
    public List<IngredientData> GetIngredientsOnPlate()
    {
        List<IngredientData> ingredients = new List<IngredientData>();
        HashSet<Draggable3DItem> uniqueItems = GetUniqueFoodItems();

        foreach (Draggable3DItem item in uniqueItems)
        {
            ingredients.Add(item.myData);
        }
        return ingredients;
    }

    private HashSet<Draggable3DItem> GetUniqueFoodItems()
    {
        HashSet<Draggable3DItem> foodScripts = new HashSet<Draggable3DItem>();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (occupantMatrix[x, y] != null)
                {
                    foodScripts.Add(occupantMatrix[x, y]);
                }
            }
        }
        return foodScripts;
    }

    public void ServePlate()
    {
        if (isServing) return;
        StartCoroutine(ServeRoutine());
    }

    private IEnumerator ServeRoutine()
    {
        isServing = true;
        HashSet<Draggable3DItem> foodScripts = GetUniqueFoodItems();

        // Clear the internal grid
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                occupantMatrix[x, y] = null;
                gridMatrix[x, y].isOccupied = false;
            }
        }

        // Attach food to the plate
        foreach (Draggable3DItem food in foodScripts)
        {
            if (food.GetComponent<Collider>() != null)
            {
                food.GetComponent<Collider>().enabled = false;
            }
            food.transform.SetParent(plateModel);
        }

        // Hide visuals
        foreach (GridTileVisual tile in tileVisuals)
        {
            if (tile != null) tile.gameObject.SetActive(false);
        }

        // Slide away
        Vector3 targetPosition = plateModel.position + new Vector3(0, 0, serveDistance);
        while (Vector3.Distance(plateModel.position, targetPosition) > 0.05f)
        {
            plateModel.position = Vector3.MoveTowards(plateModel.position, targetPosition, serveSpeed * Time.deltaTime);
            yield return null;
        }

        Destroy(plateModel.gameObject);
        yield return new WaitForSeconds(respawnDelay);

        // Slide in new plate
        GameObject newPlate = Instantiate(plateModelPrefab, transform);
        plateModel = newPlate.transform;

        if (stackSpawnPoint != null)
        {
            newPlate.transform.position = stackSpawnPoint.position;
            Vector3 startLocalPos = newPlate.transform.localPosition;
            Vector3 targetLocalPos = initialPlateLocalPos;

            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * popInSpeed;
                newPlate.transform.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, t);
                yield return null;
            }
            newPlate.transform.localPosition = targetLocalPos;
        }
        else
        {
            newPlate.transform.localPosition = initialPlateLocalPos;
        }

        // Restore visuals
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