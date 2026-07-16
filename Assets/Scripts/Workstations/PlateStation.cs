using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlateStation : BaseStation
{
    [Header("Serving Mechanics")]
    public Transform plateModel;
    public GameObject plateModelPrefab;

    [Tooltip("The empty GameObject representing the top of your plate stack")]
    public Transform stackSpawnPoint;

    public float serveSpeed = 20f;
    public float serveDistance = 10f;

    [Tooltip("How long to wait before the new empty plate appears")]
    public float respawnDelay = 0.2f;
    [Tooltip("How fast the new plate slides in from the stack")]
    public float popInSpeed = 10f;

    private bool isServing = false;
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

    private void OnMouseOver()
    {
        if (DialogueManager.Instance != null)
        {
            if (DialogueManager.Instance.IsDialogueActive) return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            ServePlate();
        }
    }

    public void ServePlate()
    {
        if (isServing) return;
        StartCoroutine(ServeRoutine());
    }

    private IEnumerator ServeRoutine()
    {
        List<IngredientData> ingredientsOnPlate = new List<IngredientData>();
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

        foreach (Draggable3DItem uniqueFood in foodScripts)
        {
            ingredientsOnPlate.Add(uniqueFood.myData);
        }

        DishData validatedDish = null;

        // --- NEW: Safe Routing for Tutorial vs Main Game ---
        if (OrderManager.Instance != null)
        {
            validatedDish = OrderManager.Instance.ValidateRecipe(ingredientsOnPlate);
        }
        else if (TutorialOrderManager.Instance != null)
        {
            // Find any active ticket that matches what is on the plate
            foreach (OrderTicketUI ticket in TutorialOrderManager.Instance.activeTickets)
            {
                if (ticket.pendingDishes.Count > 0)
                {
                    if (ticket.pendingDishes[0].MatchesIngredients(ingredientsOnPlate))
                    {
                        validatedDish = ticket.pendingDishes[0];
                        break; // Found a match, stop looking
                    }
                }
            }
        }

        if (validatedDish == null)
        {
            Debug.Log("[Plate Station] This configuration does not form a valid menu item. Bell ring rejected!");
            yield break;
        }

        isServing = true;

        if (OrderManager.Instance != null)
        {
            bool sentToWindowSuccess = OrderManager.Instance.TrySpawnDishToWindow(validatedDish);
            if (!sentToWindowSuccess)
            {
                Debug.Log("[Plate Station] Counter window display slots are full!");
                isServing = false;
                yield break;
            }
        }
        else if (TutorialOrderManager.Instance != null)
        {
            TutorialOrderManager.Instance.TryServeTutorialDish(validatedDish);
        }
        // ----------------------------------------------------

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                occupantMatrix[x, y] = null;
                gridMatrix[x, y].isOccupied = false;
            }
        }

        foreach (Draggable3DItem food in foodScripts)
        {
            food.GetComponent<Collider>().enabled = false;
            food.transform.SetParent(plateModel);
        }

        foreach (GridTileVisual tile in tileVisuals)
        {
            if (tile != null) tile.gameObject.SetActive(false);
        }

        // Slides away using the serveSpeed
        Vector3 targetPosition = plateModel.position + new Vector3(0, 0, serveDistance);
        while (Vector3.Distance(plateModel.position, targetPosition) > 0.05f)
        {
            plateModel.position = Vector3.MoveTowards(plateModel.position, targetPosition, serveSpeed * Time.deltaTime);
            yield return null;
        }

        Destroy(plateModel.gameObject);

        yield return new WaitForSeconds(respawnDelay);

        // --- NEW SLIDE-IN ANIMATION ---
        GameObject newPlate = Instantiate(plateModelPrefab, transform);
        plateModel = newPlate.transform;

        if (stackSpawnPoint != null)
        {
            // Start at the stack's position
            newPlate.transform.position = stackSpawnPoint.position;

            // The target is where the plate normally sits (local offset)
            Vector3 startLocalPos = newPlate.transform.localPosition;
            Vector3 targetLocalPos = initialPlateLocalPos;

            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * popInSpeed;
                newPlate.transform.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, t);
                yield return null;
            }

            // Snap perfectly into place
            newPlate.transform.localPosition = targetLocalPos;
        }
        else
        {
            // Fallback just in case you forget to assign the point in the inspector!
            newPlate.transform.localPosition = initialPlateLocalPos;
        }
        // ------------------------------

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