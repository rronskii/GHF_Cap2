using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

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

    [Header("Win State UI")]
    public GameObject levelClearPanel; // Drag your new UI Panel here

    [Header("Economy UI - HUD")]
    public TextMeshProUGUI hudCashText;
    public TextMeshProUGUI hudPointsText;

    [Header("Economy UI - Win Screen")]
    public TextMeshProUGUI winPointsText;
    public TextMeshProUGUI winCashText;
    public TextMeshProUGUI winTotalText;

    [Header("Window Trash Settings")]
    public Transform windowTrashPoint; // Drag the trashcan near the window here

    [Header("Claim Line Settings")]
    public Transform[] claimLinePositions; // You will drag 5 empty GameObjects here
    public int maxActiveTickets = 5;

    [Header("Exit Configuration")]
    public Transform customerExitPoint; // NEW: Drag your destination exit transform here

    // Operational Queues & Lists
    private List<CustomerController> physicalLine = new List<CustomerController>();
    private List<OrderTicketUI> activeTickets = new List<OrderTicketUI>();
    private List<GameObject> spawnedDishesOnWindow = new List<GameObject>();
    private List<CustomerController> claimLine = new List<CustomerController>();

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

        if (PlayerEconomyManager.Instance != null)
        {
            PlayerEconomyManager.Instance.StartNewShift();
        }
        UpdateEconomyHUD();
    }

    private void Update()
    {
        // NEW: Constantly orchestrates the spatial flow of your queue lines every frame
        UpdateLineQueueFlow();
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

        script.orderedDish = masterRecipeList[Random.Range(0, masterRecipeList.Count)];
        physicalLine.Add(script);
        totalCustomersSpawned++;
    }

    // UPDATED: Removed the distance check. The front slot now fills immediately.
    private void UpdateLineQueueFlow()
    {
        for (int i = 0; i < physicalLine.Count; i++)
        {
            if (i >= linePositions.Length) break;

            Vector3 targetPositionSlot = linePositions[i].position;

            if (i == 0)
            {
                // Front Slot: Step up immediately
                physicalLine[i].UpdateLinePosition(targetPositionSlot, i);
            }
            else
            {
                // Behind Slots: Only advance if the person directly in front of you has finished moving
                if (!physicalLine[i - 1].isMoving)
                {
                    physicalLine[i].UpdateLinePosition(targetPositionSlot, i);
                }
            }
        }
    }

    // NEW: Validates if the customer whose order was just taken has physically cleared space
    private bool IsOrderCounterClear()
    {
        if (linePositions.Length == 0) return true;

        foreach (CustomerController customer in claimLine)
        {
            // If anyone in the claim line is still within 10 units of the order desk, keep the line locked
            if (Vector3.Distance(customer.transform.position, linePositions[0].position) < 10f)
            {
                return false;
            }
        }
        return true;
    }

    public void TakeFrontCustomerOrder(CustomerController customer)
    {
        if (activeTickets.Count >= maxActiveTickets)
        {
            Debug.Log("[OrderManager] Ticket queue full! Serve some dishes first.");
            return;
        }

        // 1. Transfer the customer from the ordering line to the claim line
        physicalLine.Remove(customer);
        claimLine.Add(customer);
        UpdateClaimLinePositions();

        // 2. Spawn Ticket UI
        GameObject newTicket = Instantiate(ticketPrefab, ticketContainerParent);
        OrderTicketUI ticketScript = newTicket.GetComponent<OrderTicketUI>();
        ticketScript.SetupTicket(customer.orderedDish);

        activeTickets.Add(ticketScript);
        UpdateTicketLayout();
    }

    private void UpdateClaimLinePositions()
    {
        for (int i = 0; i < claimLine.Count; i++)
        {
            if (i < claimLinePositions.Length)
            {
                claimLine[i].MoveToClaimLine(claimLinePositions[i].position, i);
            }
        }
    }

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
        if (spawnedDishesOnWindow.Count >= windowDishSpawnPoints.Length) return false;

        int openSlotIndex = spawnedDishesOnWindow.Count;
        Transform targetSpawn = windowDishSpawnPoints[openSlotIndex];

        GameObject windowFood = Instantiate(dish.windowPrefab, targetSpawn.position, Quaternion.identity);

        WindowDishInteractable interactScript = windowFood.AddComponent<WindowDishInteractable>();
        interactScript.associatedDishData = dish;

        spawnedDishesOnWindow.Add(windowFood);
        return true;
    }

    public void TryServeDishToTopTicket(WindowDishInteractable cleanWindowDish, DishData data)
    {
        if (activeTickets.Count == 0) return;

        if (activeTickets[0].assignedDish == data)
        {
            OrderTicketUI matchingTicket = activeTickets[0];
            CustomerController matchingCustomer = claimLine[0];

            if (matchingCustomer.isMoving)
            {
                Debug.Log("[OrderManager] Customer is still walking to the window! Please wait.");
                return;
            }

            spawnedDishesOnWindow.Remove(cleanWindowDish.gameObject);
            activeTickets.RemoveAt(0);
            Destroy(matchingTicket.gameObject);

            UpdateTicketLayout();
            CleanAndShiftCounterWindowPositions();

            // --- STRICTLY CONTAINED ECONOMY LOGIC ---
            int earnedPoints = 0;
            foreach (IngredientData ingredient in data.requiredIngredients)
            {
                earnedPoints += ingredient.basePoints;
            }

            if (PlayerEconomyManager.Instance != null)
            {
                PlayerEconomyManager.Instance.AddShiftRevenue(data.basePrice, earnedPoints);
                UpdateEconomyHUD();
            }
            // ----------------------------------------

            cleanWindowDish.AnimateSuccessDelivery(matchingCustomer.transform);

            // UPDATED: Pass the exit transform point data over to the leaving customer script
            claimLine.RemoveAt(0);
            Vector3 exitPos = customerExitPoint != null ? customerExitPoint.position : transform.position + new Vector3(15f, 0, 0);
            matchingCustomer.LeaveCounterAndDestroy(exitPos);

            UpdateClaimLinePositions();
            CheckWinCondition();
        }
        else
        {
            Debug.Log($"[OrderManager] Incorrect dish! The customer at the front wants: {activeTickets[0].assignedDish.dishName}");
        }
    }

    private void CleanAndShiftCounterWindowPositions()
    {
        for (int i = 0; i < spawnedDishesOnWindow.Count; i++)
        {
            spawnedDishesOnWindow[i].transform.position = windowDishSpawnPoints[i].position;
        }
    }

    private void UpdateTicketLayout()
    {
        float effectiveSpacing = ticketHeight - overlapAmount;

        for (int i = 0; i < activeTickets.Count; i++)
        {
            float targetY = -(i * effectiveSpacing);
            Vector2 newPosition = new Vector2(0, targetY);

            activeTickets[i].SetTargetPosition(newPosition, i);
        }
    }

    public void RemoveDishFromCounter(GameObject dishObj)
    {
        if (spawnedDishesOnWindow.Contains(dishObj))
        {
            spawnedDishesOnWindow.Remove(dishObj);
            CleanAndShiftCounterWindowPositions();
        }
    }

    private void CheckWinCondition()
    {
        if (totalCustomersSpawned >= MAX_TOTAL_CUSTOMERS && physicalLine.Count == 0 && activeTickets.Count == 0 && claimLine.Count == 0)
        {
            Debug.Log("[OrderManager] Stage Cleared!");

            if (PlayerEconomyManager.Instance != null)
            {
                int tips;
                int finalTotalCash = PlayerEconomyManager.Instance.CalculateTotalShiftEarnings(out tips);

                if (winPointsText != null) winPointsText.text = $"Points Earned: {PlayerEconomyManager.Instance.shiftPoints}";
                if (winCashText != null) winCashText.text = $"Base Cash: {PlayerEconomyManager.Instance.shiftCash} P";
                if (winTotalText != null) winTotalText.text = $"Total Cash (w/ Tips): {finalTotalCash} P";

                PlayerEconomyManager.Instance.DepositShiftEarnings();
            }

            if (levelClearPanel != null)
            {
                levelClearPanel.SetActive(true);
            }
        }
    }

    private void UpdateEconomyHUD()
    {
        if (PlayerEconomyManager.Instance == null) return;

        if (hudCashText != null) hudCashText.text = $"{PlayerEconomyManager.Instance.shiftCash} P";
        if (hudPointsText != null) hudPointsText.text = $"{PlayerEconomyManager.Instance.shiftPoints} pts";
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}