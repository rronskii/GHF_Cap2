using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // NEW: Required for hover detection!

// Notice we added the interfaces to the end of the class declaration
public class PiggyBankUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public Image fillImage;
    public Button piggyButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip popSound;

    private bool hasPopped = false;
    private bool isHovered = false;
    private bool isAnimatingPop = false;

    private void Start()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = 0f;
        }

        if (piggyButton != null)
        {
            piggyButton.interactable = false;
        }

        hasPopped = false;
        isHovered = false;
        isAnimatingPop = false;

        // Ensure it is visible at the start of the day
        gameObject.SetActive(true);
    }

    private void Update()
    {
        // NEW: Smoothly scale up if hovered, but ONLY if it's full and not currently exploding!
        if (isAnimatingPop == false)
        {
            if (hasPopped == true)
            {
                Vector3 targetScale = Vector3.one;

                if (isHovered == true)
                {
                    targetScale = new Vector3(1.1f, 1.1f, 1.1f); // Expand by 10% on hover
                }

                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 10f);
            }
        }
    }

    // NEW: Built-in Unity Event that fires the moment the mouse enters the UI element
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    // NEW: Built-in Unity Event that fires the moment the mouse leaves the UI element
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    public void UpdatePiggyBank(float currentCash, float dailyQuota)
    {
        if (fillImage == null) return;

        float targetFill = Mathf.Clamp(currentCash / dailyQuota, 0f, 1f);

        StopAllCoroutines();
        StartCoroutine(SmoothFillRoutine(targetFill));

        if (currentCash >= dailyQuota)
        {
            if (hasPopped == false)
            {
                hasPopped = true;
                TriggerPop();
            }
        }
    }

    private IEnumerator SmoothFillRoutine(float targetFill)
    {
        float currentFill = fillImage.fillAmount;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            fillImage.fillAmount = Mathf.Lerp(currentFill, targetFill, t);
            yield return null;
        }
    }

    private void TriggerPop()
    {
        if (audioSource != null)
        {
            if (popSound != null)
            {
                audioSource.PlayOneShot(popSound);
            }
        }

        if (piggyButton != null)
        {
            piggyButton.interactable = true;
        }

        StartCoroutine(PopAnimationRoutine());
    }

    private IEnumerator PopAnimationRoutine()
    {
        // Lock the Update() loop from overriding our scale animation
        isAnimatingPop = true;

        Vector3 originalScale = Vector3.one;
        Vector3 poppedScale = new Vector3(1.3f, 1.3f, 1.3f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 10f;
            transform.localScale = Vector3.Lerp(originalScale, poppedScale, t);
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 5f;
            transform.localScale = Vector3.Lerp(poppedScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;

        // Unlock the Update() loop so hover scaling can take over
        isAnimatingPop = false;
    }

    // NEW: Call this from your Level Manager when the shift screen appears!
    public void HidePiggyBank()
    {
        gameObject.SetActive(false);
    }
}