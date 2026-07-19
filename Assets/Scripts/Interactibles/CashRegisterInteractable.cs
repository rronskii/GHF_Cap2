using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using TMPro; // --- NEW: Required for TextMeshPro elements!

[RequireComponent(typeof(Collider))]
public class CashRegisterInteractable : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign a World Space Canvas Image (set to Image Type: Filled, Horizontal)")]
    public Image fillBar;

    [Tooltip("Assign the TextMeshPro component of your Order Line sign here")]
    public TMP_Text orderLineSignText;

    [Header("Thresholds")]
    public float oneStarThreshold = 0.40f;
    public float twoStarThreshold = 0.75f;
    public float threeStarThreshold = 1.0f;

    [Header("Hover Settings")]
    public float hoverScaleMultiplier = 1.1f;
    public float scaleSpeed = 10f;
    public Color defaultBarColor = Color.green;
    public Color hoverBarColor = Color.red;
    public string closeEarlyString = "Close Early?";

    [Header("Events")]
    [Tooltip("Hook this up to your LevelManager's 'Win' or 'Close Shop' function")]
    public UnityEvent OnCloseShopEarly;

    private float currentFillRatio = 0f;
    private Vector3 baseScale;
    private bool isHovering = false;
    private bool canCloseEarly = false;
    private bool isAnimatingPop = false;
    private string originalSignText = "";

    // Track which stars we've hit so it only pops once per milestone
    private bool reachedOneStar = false;
    private bool reachedTwoStar = false;
    private bool reachedThreeStar = false;

    private void Start()
    {
        baseScale = transform.localScale;

        if (fillBar != null)
        {
            fillBar.color = defaultBarColor;
            fillBar.fillAmount = 0f;
        }

        // Save whatever the sign naturally says (e.g., "Order Line")
        if (orderLineSignText != null)
        {
            originalSignText = orderLineSignText.text;
        }
    }

    public void UpdateRegister(float currentCash, float dailyQuota)
    {
        if (dailyQuota > 0)
        {
            currentFillRatio = Mathf.Clamp01(currentCash / dailyQuota);
        }
        else
        {
            currentFillRatio = 0f;
        }

        canCloseEarly = currentFillRatio >= twoStarThreshold;

        // --- NEW: Check for new milestones to trigger the Pop! ---
        bool triggeredPop = false;

        if (!reachedOneStar && currentFillRatio >= oneStarThreshold)
        {
            reachedOneStar = true;
            triggeredPop = true;
        }
        if (!reachedTwoStar && currentFillRatio >= twoStarThreshold)
        {
            reachedTwoStar = true;
            triggeredPop = true;
        }
        if (!reachedThreeStar && currentFillRatio >= threeStarThreshold)
        {
            reachedThreeStar = true;
            triggeredPop = true;
        }

        if (triggeredPop)
        {
            TriggerPop();
        }
    }

    private void TriggerPop()
    {
        // Stop any existing pops before starting a new one
        StopAllCoroutines();
        StartCoroutine(PopAnimationRoutine());
    }

    private IEnumerator PopAnimationRoutine()
    {
        isAnimatingPop = true;

        Vector3 originalScale = baseScale;
        Vector3 poppedScale = baseScale * 1.3f;

        // Swell up quickly
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 10f;
            transform.localScale = Vector3.Lerp(originalScale, poppedScale, t);
            yield return null;
        }

        // Shrink back down smoothly
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 5f;
            transform.localScale = Vector3.Lerp(poppedScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
        isAnimatingPop = false;
    }

    private void Update()
    {
        Vector3 targetScale = baseScale;
        float targetFill = currentFillRatio;
        Color targetColor = defaultBarColor;

        // --- NEW: Calculate if we should show the "Close Early" state ---
        bool shouldShowCloseEarly = (isHovering && canCloseEarly && Time.timeScale > 0f);

        if (shouldShowCloseEarly)
        {
            targetScale = baseScale * hoverScaleMultiplier;
            targetFill = 1f;
            targetColor = hoverBarColor;
        }

        // --- NEW: Manage the Sign Text ---
        if (orderLineSignText != null)
        {
            orderLineSignText.text = shouldShowCloseEarly ? closeEarlyString : originalSignText;
        }

        // Smoothly apply the scale (but ONLY if it isn't currently doing a milestone pop!)
        if (!isAnimatingPop)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
        }

        // Smoothly animate the fill bar
        if (fillBar != null)
        {
            fillBar.fillAmount = Mathf.Lerp(fillBar.fillAmount, targetFill, Time.deltaTime * 5f);
            fillBar.color = Color.Lerp(fillBar.color, targetColor, Time.deltaTime * 10f);
        }
    }

    private void OnMouseEnter()
    {
        isHovering = true;
    }

    private void OnMouseExit()
    {
        isHovering = false;
    }

    private void OnMouseDown()
    {
        if (canCloseEarly && isHovering && Time.timeScale > 0f)
        {
            isHovering = false;
            canCloseEarly = false;

            // Revert the text instantly so it doesn't get stuck on "Close Early?"
            if (orderLineSignText != null) orderLineSignText.text = originalSignText;

            Debug.Log("[Cash Register] Closing shop early! Triggering End Level.");
            OnCloseShopEarly?.Invoke();
        }
    }
}