using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class OrderTicketUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public bool isTutorialTicket = false;

    [Header("UI References")]
    public TextMeshProUGUI ordersText;
    public Image patienceFillBar;

    [Header("Hide Settings (Other Stations)")]
    public Vector2 hiddenBaseOffset = new Vector2(0, 150f);
    public Vector2 hiddenHoverOffset = new Vector2(0, -120f);

    [Header("Patience Settings")]
    public float maxPatience = 25f;
    [HideInInspector] public float currentPatience;

    [HideInInspector] public CustomerController assignedCustomer;

    public List<DishData> pendingDishes = new List<DishData>();
    private List<DishData> completedDishes = new List<DishData>();

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 targetPosition;
    private Vector2 basePosition;

    private bool isHovered = false;
    private bool isWindowStation = true;
    private bool isFailed = false;
    private bool isDead = false; // NEW: Stops standard updates during the death animation

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        StationCameraController.OnStationChanged += HandleStationChange;
    }

    private void OnDisable()
    {
        StationCameraController.OnStationChanged -= HandleStationChange;
    }

    private void HandleStationChange(int stationIndex)
    {
        isWindowStation = (stationIndex == 2);
        UpdateTargetTransforms();
    }

    public void SetupTicket(List<DishData> orderedDishes)
    {
        pendingDishes = new List<DishData>(orderedDishes);
        currentPatience = maxPatience;

        // Ensure it knows exactly where the camera is the moment it spawns
        if (HandManager.Instance != null)
        {
            if (HandManager.Instance.currentStationIndex == 2)
            {
                isWindowStation = true;
            }
            else
            {
                isWindowStation = false;
            }
        }
        else
        {
            isWindowStation = false;
        }

        UpdateTicketText();
    }

    public void MarkDishServed(DishData dish)
    {
        if (pendingDishes.Contains(dish))
        {
            pendingDishes.Remove(dish);
            completedDishes.Add(dish);
            UpdateTicketText();
        }
    }

    public bool IsFullyServed()
    {
        return pendingDishes.Count == 0;
    }

    private void UpdateTicketText()
    {
        string displayText = "";

        foreach (DishData d in completedDishes)
        {
            displayText += $"<s><color=#555555>{d.dishName}</color></s>\n";
        }

        foreach (DishData d in pendingDishes)
        {
            displayText += $"{d.dishName}\n";
        }

        ordersText.text = displayText;
    }

    public void SetTargetPosition(Vector2 newPos, int index)
    {
        basePosition = newPos;
        UpdateTargetTransforms();
    }

    private void UpdateTargetTransforms()
    {
        // We revert this back to your original math so the hover offset works again!
        if (isWindowStation)
        {
            targetPosition = basePosition;
        }
        else
        {
            Vector2 hoverOffset = Vector2.zero;
            if (isHovered)
            {
                hoverOffset = hiddenHoverOffset;
            }

            targetPosition = basePosition + hiddenBaseOffset + hoverOffset;
        }
    }

    private void Update()
    {
        if (isDead) return;

        bool dialogueActive = false;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
        {
            dialogueActive = true;
        }

        Vector2 activeTargetPosition = basePosition;

        // REMOVED the dialogueActive override here so it hides normally
        if (isWindowStation)
        {
            activeTargetPosition = basePosition;
        }
        else
        {
            Vector2 hoverOffset = Vector2.zero;
            // Prevent it from peeking down if they accidentally hover over it during dialogue
            if (isHovered && !dialogueActive)
            {
                hoverOffset = hiddenHoverOffset;
            }

            activeTargetPosition = basePosition + hiddenBaseOffset + hoverOffset;
        }

        rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, activeTargetPosition, Time.deltaTime * 15f);

        if (!isTutorialTicket)
        {
            if (!isFailed && currentPatience > 0)
            {
                currentPatience -= Time.deltaTime;

                if (patienceFillBar != null)
                {
                    patienceFillBar.fillAmount = currentPatience / maxPatience;
                }

                if (currentPatience <= 0)
                {
                    isFailed = true;
                    if (OrderManager.Instance != null) OrderManager.Instance.HandleTicketTimeout(this);
                }
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (DialogueManager.Instance != null)
        {
            if (DialogueManager.Instance.IsDialogueActive) return;
        }

        isHovered = true;
        transform.SetAsLastSibling();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    // NEW: Animation triggered by the Order Manager when patience runs out
    public void TriggerTimeoutAnimation()
    {
        isDead = true;
        StartCoroutine(TimeoutRoutine());
    }

    private IEnumerator TimeoutRoutine()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 upPos = startPos + new Vector2(0, 150f); // Glide straight UP

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            rectTransform.anchoredPosition = Vector2.Lerp(startPos, upPos, t);
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        Destroy(gameObject);
    }

    public void HidePatienceUI()
    {
        isTutorialTicket = true;

        if (patienceFillBar != null)
        {
            patienceFillBar.enabled = false;
        }

        // Search for the specific sibling object by its name
        Transform bg = transform.Find("BarBG");
        if (bg != null)
        {
            bg.gameObject.SetActive(false);
        }
    }
}