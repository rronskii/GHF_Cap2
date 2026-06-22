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
    public Transform[] linePositions;
    public float spawnInterval = 6f;

    [Header("Ticket UI Settings")]
    public GameObject ticketPrefab;
    public Transform ticketContainerParent;

    [Header("Ticket Horizontal Layout Settings")]
    public float ticketWidth = 250f;
    public float spacingAmount = 20f;

    [Header("Counter Window Settings")]
    public Transform[] windowDishSpawnPoints;

    [Header("Win State UI")]
    public GameObject levelClearPanel;

    [Header("Economy UI - HUD")]
    public TextMeshProUGUI hudCashText;
    public TextMeshProUGUI hudPointsText;

    [Header("Economy UI - Win Screen")]
    public TextMeshProUGUI winPointsText;
    public TextMeshProUGUI winCashText;
    public TextMeshProUGUI winTotalText;

    [Header("Window Trash Settings")]
    public Transform windowTrashPoint;

    [Header("Claim Line Settings")]
    public Transform[] claimLinePositions;
    public int maxActiveTickets = 5;

    [Header("Exit Configuration")]
    public Transform customerExitPoint;

    private List<CustomerController> physicalLine = new List<CustomerController>();
    public List<OrderTicketUI> activeTickets = new List<OrderTicketUI>();
    [HideInInspector] public GameObject[] windowDishSlots;

    // NEW: Fixed Array for Claim Slots so they don't slide forward
    private CustomerController[] claimSlots;

    private int totalCustomersSpawned = 0;
    private const int MAX_TOTAL_CUSTOMERS = 10;
    private const int MAX_SIMULTANEOUS_CUSTOMERS = 3;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        claimSlots = new CustomerController[claimLinePositions.Length];
        windowDishSlots = new GameObject[windowDishSpawnPoints.Length];
        StartCoroutine(CustomerSpawningRoutine());

        if (PlayerEconomyManager.Instance != null)
            PlayerEconomyManager.Instance.StartNewShift();

        UpdateEconomyHUD();
    }

    private void Update()
    {
        UpdateLineQueueFlow();
    }

    private IEnumerator CustomerSpawningRoutine()
    {
        while (totalCustomersSpawned < MAX_TOTAL_CUSTOMERS)
        {
            if (physicalLine.Count < MAX_SIMULTANEOUS_CUSTOMERS)
                SpawnCustomer();

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnCustomer()
    {
        GameObject newCustomer = Instantiate(customerPrefab, customerSpawnPoint.position, Quaternion.identity);
        CustomerController script = newCustomer.GetComponent<CustomerController>();

        int dishCount = Random.value > 0.2f ? 1 : 2;
        for (int i = 0; i < dishCount; i++)
        {
            script.orderedDishes.Add(masterRecipeList[Random.Range(0, masterRecipeList.Count)]);
        }

        physicalLine.Add(script);
        totalCustomersSpawned++;
    }

    private void UpdateLineQueueFlow()
    {
        for (int i = 0; i < physicalLine.Count; i++)
        {
            if (i >= linePositions.Length) break;
            Vector3 targetPositionSlot = linePositions[i].position;

            if (i == 0)
            {
                physicalLine[i].UpdateLinePosition(targetPositionSlot, i);
            }
            else
            {
                if (!physicalLine[i - 1].isMoving)
                    physicalLine[i].UpdateLinePosition(targetPositionSlot, i);
            }
        }
    }

    public void TakeFrontCustomerOrder(CustomerController customer)
    {
        if (activeTickets.Count >= maxActiveTickets)
        {
            Debug.Log("[OrderManager] Ticket queue full! Serve some dishes first.");
            return;
        }

        // Find the first empty claim slot
        int availableSlot = -1;
        for (int i = 0; i < claimSlots.Length; i++)
        {
            if (claimSlots[i] == null)
            {
                availableSlot = i;
                break;
            }
        }

        if (availableSlot == -1) return; // Should not trigger due to maxActiveTickets

        physicalLine.Remove(customer);
        claimSlots[availableSlot] = customer;
        customer.MoveToClaimLine(claimLinePositions[availableSlot].position, availableSlot);

        GameObject newTicket = Instantiate(ticketPrefab, ticketContainerParent);
        OrderTicketUI ticketScript = newTicket.GetComponent<OrderTicketUI>();

        ticketScript.assignedCustomer = customer;
        ticketScript.SetupTicket(customer.orderedDishes);

        activeTickets.Add(ticketScript);
        UpdateTicketLayout();
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
        // 1. Find the first empty slot
        int openSlotIndex = -1;
        for (int i = 0; i < windowDishSlots.Length; i++)
        {
            if (windowDishSlots[i] == null)
            {
                openSlotIndex = i;
                break;
            }
        }

        // 2. If it's -1, the window is completely full
        if (openSlotIndex == -1) return false;

        // 3. Spawn directly into that specific slot
        Transform targetSpawn = windowDishSpawnPoints[openSlotIndex];
        GameObject windowFood = Instantiate(dish.windowPrefab, targetSpawn.position, Quaternion.identity);

        WindowDishInteractable interactScript = windowFood.AddComponent<WindowDishInteractable>();
        interactScript.associatedDishData = dish;

        windowDishSlots[openSlotIndex] = windowFood;
        return true;
    }

    // --- NEW: DUAL SERVING LOGIC ---

    // Option A: Click a Dish on the window -> Finds the oldest ticket that wants it
    public void TryServeDishToOldestMatchingTicket(WindowDishInteractable cleanWindowDish, DishData data)
    {
        foreach (OrderTicketUI ticket in activeTickets)
        {
            if (ticket.pendingDishes.Contains(data))
            {
                ExecuteServe(ticket, cleanWindowDish, data);
                return; // Served successfully, stop checking
            }
        }
        Debug.Log($"[OrderManager] No tickets currently require: {data.dishName}");
    }

    // Option B: Click a Ticket -> Finds the oldest dish on the window that it wants
    public void TryServeOldestDishToTicket(OrderTicketUI ticket)
    {
        // Loop through the fixed array instead of a list
        for (int i = 0; i < windowDishSlots.Length; i++)
        {
            GameObject dishObj = windowDishSlots[i];
            if (dishObj == null) continue; // Skip empty slots!

            WindowDishInteractable dishInteractable = dishObj.GetComponent<WindowDishInteractable>();
            if (dishInteractable != null && ticket.pendingDishes.Contains(dishInteractable.associatedDishData))
            {
                ExecuteServe(ticket, dishInteractable, dishInteractable.associatedDishData);
                return;
            }
        }
        Debug.Log("[OrderManager] No dishes on the window match this ticket's pending orders.");
    }

    // Shared execution logic for both methods
    private void ExecuteServe(OrderTicketUI ticket, WindowDishInteractable dishInteractable, DishData dishData)
    {
        CustomerController customer = ticket.assignedCustomer;

        if (customer.isMoving && !customer.isWalkingAway)
        {
            Debug.Log("[OrderManager] Customer is still walking to their claim spot! Please wait.");
            return;
        }

        // Economy Logic
        int earnedPoints = 0;
        foreach (IngredientData ingredient in dishData.requiredIngredients)
        {
            earnedPoints += ingredient.basePoints;
        }

        if (PlayerEconomyManager.Instance != null)
        {
            PlayerEconomyManager.Instance.AddShiftRevenue(dishData.basePrice, earnedPoints);
            UpdateEconomyHUD();
        }

        // Remove dish and animate
        for (int i = 0; i < windowDishSlots.Length; i++)
        {
            if (windowDishSlots[i] == dishInteractable.gameObject)
            {
                windowDishSlots[i] = null;
                break;
            }
        }
        dishInteractable.AnimateSuccessDelivery(customer.transform);

        // Update Ticket
        ticket.MarkDishServed(dishData);

        if (ticket.IsFullyServed())
        {
            activeTickets.Remove(ticket);
            Destroy(ticket.gameObject);
            UpdateTicketLayout();

            // Clear their claim slot so someone else can use it
            for (int i = 0; i < claimSlots.Length; i++)
            {
                if (claimSlots[i] == customer)
                {
                    claimSlots[i] = null;
                    break;
                }
            }

            Vector3 exitPos = customerExitPoint != null ? customerExitPoint.position : transform.position + new Vector3(15f, 0, 0);
            customer.LeaveCounterAndDestroy(exitPos);

            CheckWinCondition();
        }
    }
    // -------------------------------

    private void UpdateTicketLayout()
    {
        float effectiveSpacing = ticketWidth + spacingAmount;

        for (int i = 0; i < activeTickets.Count; i++)
        {
            float targetX = i * effectiveSpacing;
            Vector2 newPosition = new Vector2(targetX, 0);
            activeTickets[i].SetTargetPosition(newPosition, i);
        }
    }

    public void RemoveDishFromCounter(GameObject dishObj)
    {
        for (int i = 0; i < windowDishSlots.Length; i++)
        {
            if (windowDishSlots[i] == dishObj)
            {
                windowDishSlots[i] = null;
                break;
            }
        }
    }

    private void CheckWinCondition()
    {
        bool isClaimLineEmpty = true;
        foreach (var slot in claimSlots)
        {
            if (slot != null) isClaimLineEmpty = false;
        }

        if (totalCustomersSpawned >= MAX_TOTAL_CUSTOMERS && physicalLine.Count == 0 && activeTickets.Count == 0 && isClaimLineEmpty)
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