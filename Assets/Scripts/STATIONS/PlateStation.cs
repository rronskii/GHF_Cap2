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
        isServing = true;

        // 1. Gather all food currently on the plate
        HashSet<Draggable3DItem> foodOnPlate = new HashSet<Draggable3DItem>();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (occupantMatrix[x, y] != null)
                {
                    foodOnPlate.Add(occupantMatrix[x, y]);

                    occupantMatrix[x, y] = null;
                    gridMatrix[x, y].isOccupied = false;
                }
            }
        }

        // 2. Lock the food and parent it to the physical plate model
        foreach (Draggable3DItem food in foodOnPlate)
        {
            food.GetComponent<Collider>().enabled = false;
            food.transform.SetParent(plateModel);
        }

        // 3. Hide the interactive grid visuals
        foreach (GridTileVisual tile in tileVisuals)
        {
            if (tile != null) tile.gameObject.SetActive(false);
        }

        // 4. Slide the plate away along the Z-axis
        Vector3 targetPosition = plateModel.position + new Vector3(0, 0, serveDistance);
        while (Vector3.Distance(plateModel.position, targetPosition) > 0.05f)
        {
            plateModel.position = Vector3.MoveTowards(plateModel.position, targetPosition, serveSpeed * Time.deltaTime);
            yield return null;
        }

        // 5. Delete the old plate and the food attached to it
        Destroy(plateModel.gameObject);

        yield return new WaitForSeconds(0.5f);

        // 6. Spawn the replacement plate
        GameObject newPlate = Instantiate(plateModelPrefab, transform);

        // UPDATED: Use the memorized local position instead of Vector3.zero
        newPlate.transform.localPosition = initialPlateLocalPos;

        plateModel = newPlate.transform;

        // 7. Pop-up Animation for the new plate
        Vector3 finalScale = newPlate.transform.localScale;
        newPlate.transform.localScale = Vector3.zero;

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 5f;
            newPlate.transform.localScale = Vector3.Lerp(Vector3.zero, finalScale, t);
            yield return null;
        }

        // 8. Turn the grid back on 
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