using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CookingTimerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private float fadeSpeed = 2f;

    // Stores the total time so we know how fast to fill
    private float totalCookTime;
    private float currentTimer;
    private bool isCooking = false;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        // Start invisible
        canvasGroup.alpha = 0f;
        fillImage.fillAmount = 0f;
    }

    // Called by the Draggable3DItem when cooking starts
    public void StartTimer(float duration)
    {
        totalCookTime = duration;
        currentTimer = 0f;
        fillImage.fillAmount = 0f;
        canvasGroup.alpha = 1f; // Show immediately
        isCooking = true;
    }

    private void Update()
    {
        if (!isCooking) return;

        currentTimer += Time.deltaTime;

        // Calculate fill percentage (0.0 to 1.0)
        fillImage.fillAmount = currentTimer / totalCookTime;

        // Check if finished
        if (currentTimer >= totalCookTime)
        {
            isCooking = false;
            StartCoroutine(FadeOutRoutine());
        }
    }

    // NEW: We want the bar to face the camera constantly so the player can always see it
    private void LateUpdate()
    {
        if (canvasGroup.alpha > 0)
        {
            // Simple billboarding: Look at camera, then flip 180 so text/UI isn't backward
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                             Camera.main.transform.rotation * Vector3.up);
        }
    }

    private IEnumerator FadeOutRoutine()
    {
        // Ensure fill is maxed
        fillImage.fillAmount = 1f;

        // Wait a tiny fraction of a second so the player sees it hit 100%
        yield return new WaitForSeconds(0.2f);

        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }

        // Optional: Destroy the timer object entirely after fading to save performance
        Destroy(gameObject);
    }
}