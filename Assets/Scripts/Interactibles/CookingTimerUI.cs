using UnityEngine;
using UnityEngine.UI;

public class CookingTimerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Colors")]
    [SerializeField] private Color cookColor = Color.green;
    [SerializeField] private Color burnColor = Color.red;

    private float totalCookTime;
    private float currentTimer;
    private bool isCooking = false;
    private Camera mainCam;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        mainCam = Camera.main;

        canvasGroup.alpha = 0f;
        fillImage.fillAmount = 0f;
    }

    // UPDATED: Now accepts a startTime so we can resume paused timers!
    public void StartTimer(float duration, bool isBurnTimer = false, float startTime = 0f)
    {
        totalCookTime = duration;
        currentTimer = startTime;
        fillImage.fillAmount = currentTimer / totalCookTime;
        canvasGroup.alpha = 1f;
        isCooking = true;

        fillImage.color = isBurnTimer ? burnColor : cookColor;
    }

    // NEW: Lets the 3D item ask exactly how far along the timer was when it got picked up
    public float GetCurrentTime()
    {
        return currentTimer;
    }

    private void Update()
    {
        if (!isCooking) return;

        currentTimer += Time.deltaTime;
        fillImage.fillAmount = currentTimer / totalCookTime;

        if (currentTimer >= totalCookTime)
        {
            isCooking = false;
            Destroy(gameObject);
        }
    }

    private void LateUpdate()
    {
        if (canvasGroup.alpha > 0)
        {
            transform.LookAt(transform.position + mainCam.transform.rotation * Vector3.forward,
                             mainCam.transform.rotation * Vector3.up);
        }
    }
}