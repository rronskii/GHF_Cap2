using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance;

    [Header("Recipe Configuration Pool")]
    public List<DishData> masterRecipeList;

    [Header("Customer Settings")]
    public GameObject customerPrefab;
    public Transform customerSpawnPoint;
    public Transform[] linePositions; // Array of 3 points representing the queue layout slots
    public float spawnInterval = 6f;

    [Header("Ticket UI Settings")]
    public GameObject ticketPrefab;
    public Transform ticketContainerParent; // Object containing your Vertical Layout Group

    [Header("Ticket Cascading Settings")]
    public float ticketHeight = 300f; // The raw height of your ticket prefab
    public float overlapAmount = 100f; // How much should they overlap?

    [Header("Counter Window Settings")]
    public Transform[] windowDishSpawnPoints; // Available counter display slots

    // Operational Queues & Lists
    private List<CustomerController> physicalLine = new List<CustomerController>();
    private List<OrderTicketUI> activeTickets = new List<OrderTicketUI>();
    private List<GameObject> spawnedDishesOnWindow = new List<GameObject>();

    private int totalCustomersSpawned = 0;
    private const int MAX_TOTAL_CUSTOMERS = 10;
    private const int MAX_SIMULTANEOUS_CUSTOMERS = 3;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(CustomerSpawningRoutine());
    }

    private IEnumerator CustomerSpawningRoutine()
    {
        while (totalCustomersSpawned < MAX_TOTAL_CUSTOMERS)
        {
            if (physicalLine.Count < MAX_SIMULTANEOUS_CUSTOMERS)
            {
                SpawnCustomer();
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnCustomer()
    {
        GameObject newCustomer = Instantiate(customerPrefab, customerSpawnPoint.position, Quaternion.identity);
        CustomerController script = newCustomer.GetComponent<CustomerController>();

        // Pick a random dish from available recipes
        script.orderedDish = masterRecipeList[Random.Range(0, masterRecipeList.Count)];

        physicalLine.Add(script);
        totalCustomersSpawned++;

        UpdateCustomerLinePositions();
    }

    private void UpdateCustomerLinePositions()
    {
        for (int i = 0; i < physicalLine.Count; i++)
        {
            if (i < linePositions.Length)
            {
                physicalLine[i].UpdateLinePosition(linePositions[i].position, i);
            }
        }
    }

    public void TakeFrontCustomerOrder(CustomerController customer)
    {
        physicalLine.Remove(customer);
        UpdateCustomerLinePositions(); // Move the remaining customers up in line

        // Spawn Ticket UI
        GameObject newTicket = Instantiate(ticketPrefab, ticketContainerParent);
        OrderTicketUI ticketScript = newTicket.GetComponent<OrderTicketUI>();
        ticketScript.SetupTicket(customer.orderedDish);

        activeTickets.Add(ticketScript); // Appends to bottom of list (Newest)
        UpdateTicketLayout(); // ADD THIS LINE HERE

        customer.LeaveCounterAndDestroy();
    }

    // Called by PlateStation to check if the food cooked is real
    public DishData ValidateRecipe(List<IngredientData> foodOnPlate)
    {
        foreach (DishData dish in masterRecipeList)
        {
            if (dish.MatchesIngredients(foodOnPlate)) return dish;
        }
        return null;
    }

    public bool TrySpawnDishToWindow(DishData dish)
    {
        if (spawnedDishesOnWindow.Count >= windowDishSpawnPoints.Length) return false; // Window full

        int openSlotIndex = spawnedDishesOnWindow.Count;
        Transform targetSpawn = windowDishSpawnPoints[openSlotIndex];

        GameObject windowFood = Instantiate(dish.windowPrefab, targetSpawn.position, Quaternion.identity);

        // Attach window click serving behavior dynamically
        WindowDishInteractable interactScript = windowFood.AddComponent<WindowDishInteractable>();
        interactScript.associatedDishData = dish;

        spawnedDishesOnWindow.Add(windowFood);
        return true;
    }

    public void TryServeDishToTopTicket(WindowDishInteractable cleanWindowDish, DishData data)
    {
        if (activeTickets.Count == 0) return;

        // RULE: Must match the OLDEST (Index 0) active ticket exactly
        if (activeTickets[0].assignedDish == data)
        {
            // Success! Remove from tracking lists
            spawnedDishesOnWindow.Remove(cleanWindowDish.gameObject);

            OrderTicketUI ticketToDestroy = activeTickets[0];
            activeTickets.RemoveAt(0);
            Destroy(ticketToDestroy.gameObject);

            UpdateTicketLayout(); // ADD THIS LINE HERE

            // Trigger the clean up movement logic for items remaining on the counter window
            CleanAndShiftCounterWindowPositions();

            // Animate 3D item out of scene
            cleanWindowDish.AnimateSuccessDelivery();
        }
        else
        {
            Debug.Log("This dish does not match our oldest active ticket order!");
        }
    }

    private void CleanAndShiftCounterWindowPositions()
    {
        // Move remaining 3D dishes down into lower index open window slots
        for (int i = 0; i < spawnedDishesOnWindow.Count; i++)
        {
            spawnedDishesOnWindow[i].transform.position = windowDishSpawnPoints[i].position;
        }
    }

    private void UpdateTicketLayout()
    {
        // We calculate the Y position offset for each ticket in the list.
        // A lower Y value moves the UI element downward.
        float effectiveSpacing = ticketHeight - overlapAmount;

        for (int i = 0; i < activeTickets.Count; i++)
        {
            // Index 0 (Oldest) will have Y = 0.
            // Index 1 will have Y = -effectiveSpacing.
            // Index 2 will have Y = -(effectiveSpacing * 2).
            float targetY = -(i * effectiveSpacing);
            Vector2 newPosition = new Vector2(0, targetY);

            activeTickets[i].SetTargetPosition(newPosition, i);
        }
    }
}