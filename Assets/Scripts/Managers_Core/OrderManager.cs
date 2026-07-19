using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance;

    [Header("Level Flow Settings")]
    public float shiftDuration = 360f; // 6 Minutes in seconds
    public int dailyQuota = 600;
    public TMP_Text orderLineSignText;
    public GameObject closeEarlyButton;

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
    public float ticketWidth = 250f;
    public float spacingAmount = 20f;

    [Header("Counter Window Settings")]
    public Transform[] windowDishSpawnPoints;

    [Header("Window Trash Settings")]
    public Transform windowTrashPoint;

    [Header("Win State UI")]
    public GameObject levelClearPanel;

    [Header("Economy UI - HUD")]
    public TextMeshProUGUI hudCashText;
    public TextMeshProUGUI hudQuotaText; // CHANGED from points to quota
    public TextMeshProUGUI clockText;    // NEW: The digital clock

    [Header("Economy UI - Win Screen")]
    public TextMeshProUGUI winPointsText;
    public TextMeshProUGUI winCashText;
    public TextMeshProUGUI winTotalText;

    [Header("Claim Line Settings")]
    public Transform[] claimLinePositions;
    public int maxActiveTickets = 5;

    [Header("Exit Configuration")]
    public Transform customerExitPoint;

    [HideInInspector] public bool isLevelCleared = false;

    private List<CustomerController> physicalLine = new List<CustomerController>();
    public List<OrderTicketUI> activeTickets = new List<OrderTicketUI>();
    [HideInInspector] public GameObject[] windowDishSlots;
    private CustomerController[] claimSlots;

    private float currentShiftTime = 0f;
    private int lastCalculatedMinute = -1;
    private bool isStoreClosed = false;
    private const int MAX_SIMULTANEOUS_CUSTOMERS = 3;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        claimSlots = new CustomerController[claimLinePositions.Length];
        windowDishSlots = new GameObject[windowDishSpawnPoints.Length];

        if (closeEarlyButton != null) closeEarlyButton.SetActive(false);

        StartCoroutine(CustomerSpawningRoutine());

        if (PlayerEconomyManager.Instance != null)
            PlayerEconomyManager.Instance.StartNewShift(dailyQuota);

        UpdateEconomyHUD();
    }

    private void Update()
    {
        UpdateLineQueueFlow();
        UpdateDigitalClock(); // Continually format and update the time

        if (!isStoreClosed)
        {
            currentShiftTime += Time.deltaTime;

            if (PlayerEconomyManager.Instance != null && PlayerEconomyManager.Instance.shiftCash >= dailyQuota)
            {
                if (closeEarlyButton != null) closeEarlyButton.SetActive(true);
            }

            if (currentShiftTime >= shiftDuration)
            {
                CloseStore();
            }
        }
        else
        {
            // Even if the store is closed, keep time ticking for overtime!
            currentShiftTime += Time.deltaTime;
        }
    }

    // NEW: Calculates the exact time. 1 IRL Second = 1 In-Game Minute.
    private void UpdateDigitalClock()
    {
        if (clockText == null) return;

        // 1. Calculate how many in-game minutes have passed.
        int elapsedInGameMinutes = Mathf.FloorToInt(currentShiftTime / 1f);

        // Start at 12:00 PM (720 minutes)
        int startingMinutes = 720;
        int currentTotalMinutes = startingMinutes + elapsedInGameMinutes;

        // OPTIMIZATION: Only run the math and rebuild the string if the minute actually changed!
        if (currentTotalMinutes != lastCalculatedMinute)
        {
            lastCalculatedMinute = currentTotalMinutes;

            int displayHour = (currentTotalMinutes / 60) % 24;
            int displayMinute = currentTotalMinutes % 60;

            // STRICT RULE: Replaced the old ternary (? :) operator with a standard if-statement
            string amPm = "AM";
            if (displayHour >= 12)
            {
                amPm = "PM";
            }

            displayHour = displayHour % 12;
            if (displayHour == 0)
            {
                displayHour = 12; // Formats 0 to 12
            }

            clockText.text = $"{displayHour:00}:{displayMinute:00} {amPm}";
        }
    }

    public void CloseStore()
    {
        if (isStoreClosed) return;
        isStoreClosed = true;

        if (closeEarlyButton != null) closeEarlyButton.SetActive(false);
        if (orderLineSignText != null) orderLineSignText.text = "Closed";

        for (int i = physicalLine.Count - 1; i >= 0; i--)
        {
            physicalLine[i].LeaveCounterAndDestroy(customerSpawnPoint.position);
        }
        physicalLine.Clear();

        CheckWinCondition();
    }

    private IEnumerator CustomerSpawningRoutine()
    {
        while (!isStoreClosed)
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
    }

    private void UpdateLineQueueFlow()
    {
        for (int i = 0; i < physicalLine.Count; i++)
        {
            if (i >= linePositions.Length) break;
            Vector3 targetPositionSlot = linePositions[i].position;

            if (i == 0) physicalLine[i].UpdateLinePosition(targetPositionSlot, i);
            else if (!physicalLine[i - 1].isMoving) physicalLine[i].UpdateLinePosition(targetPositionSlot, i);
        }
    }

    public void TakeFrontCustomerOrder(CustomerController customer)
    {
        if (activeTickets.Count >= maxActiveTickets) return;

        int availableSlot = -1;
        for (int i = 0; i < claimSlots.Length; i++)
        {
            if (claimSlots[i] == null)
            {
                availableSlot = i;
                break;
            }
        }

        if (availableSlot == -1) return;

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
        int openSlotIndex = -1;
        for (int i = 0; i < windowDishSlots.Length; i++)
        {
            if (windowDishSlots[i] == null)
            {
                openSlotIndex = i;
                break;
            }
        }

        if (openSlotIndex == -1) return false;

        Transform targetSpawn = windowDishSpawnPoints[openSlotIndex];
        GameObject windowFood = Instantiate(dish.windowPrefab, targetSpawn.position, Quaternion.identity);

        WindowDishInteractable interactScript = windowFood.AddComponent<WindowDishInteractable>();
        interactScript.associatedDishData = dish;

        windowDishSlots[openSlotIndex] = windowFood;
        return true;
    }

    public void TryServeDishToOldestMatchingTicket(WindowDishInteractable cleanWindowDish, DishData data)
    {
        foreach (OrderTicketUI ticket in activeTickets)
        {
            if (ticket.pendingDishes.Contains(data))
            {
                ExecuteServe(ticket, cleanWindowDish, data);
                return;
            }
        }
    }

    private void ExecuteServe(OrderTicketUI ticket, WindowDishInteractable dishInteractable, DishData dishData)
    {
        CustomerController customer = ticket.assignedCustomer;

        if (customer.isMoving && !customer.isWalkingAway) return;

        int earnedPoints = 0;
        foreach (IngredientData ingredient in dishData.requiredIngredients)
        {
            earnedPoints += ingredient.basePoints;
        }

        float patiencePercentage = ticket.currentPatience / ticket.maxPatience;
        earnedPoints += Mathf.RoundToInt(100f * patiencePercentage);

        if (PlayerEconomyManager.Instance != null)
        {
            PlayerEconomyManager.Instance.AddShiftRevenue(dishData.basePrice, earnedPoints);
            UpdateEconomyHUD();
        }

        for (int i = 0; i < windowDishSlots.Length; i++)
        {
            if (windowDishSlots[i] == dishInteractable.gameObject)
            {
                windowDishSlots[i] = null;
                break;
            }
        }

        dishInteractable.AnimateSuccessDelivery(customer.transform);
        ticket.MarkDishServed(dishData);

        if (ticket.IsFullyServed())
        {
            activeTickets.Remove(ticket);
            Destroy(ticket.gameObject);
            UpdateTicketLayout();

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

    public void HandleTicketTimeout(OrderTicketUI ticket)
    {
        CustomerController customer = ticket.assignedCustomer;

        // UPDATED: Tell the ticket to animate itself instead of instant Destroy
        activeTickets.Remove(ticket);
        ticket.TriggerTimeoutAnimation();

        UpdateTicketLayout();

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

    private void CheckWinCondition()
    {
        bool isClaimLineEmpty = true;
        foreach (var slot in claimSlots)
        {
            if (slot != null) isClaimLineEmpty = false;
        }

        if (isStoreClosed && physicalLine.Count == 0 && activeTickets.Count == 0 && isClaimLineEmpty)
        {
            isLevelCleared = true;

            if (levelClearPanel != null) levelClearPanel.SetActive(true);

            // NEW: Hide the HUD elements when the level clears
            if (hudCashText != null) hudCashText.gameObject.SetActive(false);
            if (hudQuotaText != null) hudQuotaText.gameObject.SetActive(false);
            if (clockText != null) clockText.gameObject.SetActive(false);

            if (HandManager.Instance != null)
            {
                HandManager.Instance.RefundAllCards();
            }

            if (PlayerEconomyManager.Instance != null)
            {
                int tips;
                int finalTotalCash = PlayerEconomyManager.Instance.CalculateTotalShiftEarnings(out tips);

                if (winPointsText != null) winPointsText.text = $"Points Earned: {PlayerEconomyManager.Instance.shiftPoints}";
                if (winCashText != null) winCashText.text = $"Base Cash: {PlayerEconomyManager.Instance.shiftCash} P";
                if (winTotalText != null) winTotalText.text = $"Total Cash (w/ Tips): {finalTotalCash} P";

                PlayerEconomyManager.Instance.DepositShiftEarnings();
            }
        }
    }

    private void UpdateEconomyHUD()
    {
        if (PlayerEconomyManager.Instance == null) return;

        if (hudCashText != null) hudCashText.text = $"Cash: {PlayerEconomyManager.Instance.shiftCash} P";

        // UPDATED: Now displays Quota instead of Points
        if (hudQuotaText != null) hudQuotaText.text = $"Quota: {dailyQuota} P";
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadDailyShop()
    {
        // Make sure your shop scene is added to the Build Settings!
        SceneManager.LoadScene("02_DailyShop");
    }
}