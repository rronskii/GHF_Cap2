using UnityEngine;
using UnityEngine.UI; // Required for the Image component

public class BouncingArrowUI : MonoBehaviour
{
    public float bounceSpeed = 6f;
    public float bounceHeight = 15f;

    private Vector3 startPos;
    private RectTransform rectTransform;
    private Image arrowImage;
    private CardDragUI parentCard;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        arrowImage = GetComponent<Image>();
        parentCard = GetComponentInParent<CardDragUI>(); // Finds the card it is sitting on

        if (rectTransform != null)
        {
            startPos = rectTransform.anchoredPosition;
        }
        else
        {
            startPos = transform.localPosition;
        }
    }

    private void Update()
    {
        // 1. Handle visibility based on whether the player is holding the card
        if (parentCard != null && arrowImage != null)
        {
            arrowImage.enabled = !parentCard.isDragging;
        }

        // 2. Handle the bouncing animation
        Vector3 offset = new Vector3(0, Mathf.Sin(Time.time * bounceSpeed) * bounceHeight, 0);

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = startPos + offset;
        }
        else
        {
            transform.localPosition = startPos + offset;
        }
    }
}