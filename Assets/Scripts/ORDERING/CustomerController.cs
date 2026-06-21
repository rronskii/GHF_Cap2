using System;
using System.Collections;
using UnityEngine;

public class CustomerController : MonoBehaviour
{
    public static event Action OnTutorialOrderTaken;

    public DishData orderedDish;
    public int currentLineIndex;
    public bool isWalkingAway = false;
    private bool isClaiming = false;

    // Public getter so the OrderManager knows if they are still shifting in line
    public bool isMoving { get; private set; } = true;

    private Vector3 targetWorldPosition;
    public float moveSpeed = 5f;

    private void Start()
    {
        targetWorldPosition = transform.position;
    }

    private void Update()
    {
        // Smoothly glide towards the target position
        if (Vector3.Distance(transform.position, targetWorldPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetWorldPosition, moveSpeed * Time.deltaTime);
            isMoving = true;
        }
        else
        {
            isMoving = false;

            // NEW: If they are walking away and have successfully reached the exit point vector, delete them
            if (isWalkingAway)
            {
                Destroy(gameObject);
            }
        }
    }

    public void UpdateLinePosition(Vector3 newPosition, int index)
    {
        // Guard Clause: Prevent redundant assignments from overwriting their active walk state every frame
        if (currentLineIndex == index && targetWorldPosition == newPosition) return;

        currentLineIndex = index;
        targetWorldPosition = newPosition;
        isMoving = true;
    }

    public void MoveToClaimLine(Vector3 newPosition, int index)
    {
        if (currentLineIndex == index && targetWorldPosition == newPosition) return;

        isClaiming = true;
        currentLineIndex = index;
        targetWorldPosition = newPosition;
        isMoving = true;
    }

    private void OnMouseDown()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        // Prevent clicking to take an order if the customer hasn't fully stepped up yet
        if (isMoving) return;

        if (currentLineIndex == 0 && !isWalkingAway && !isClaiming)
        {
            OrderManager.Instance.TakeFrontCustomerOrder(this);
            OnTutorialOrderTaken?.Invoke();
        }
    }

    // NEW: Accepts the exit point vector passed down from the OrderManager
    public void LeaveCounterAndDestroy(Vector3 exitWorldPosition)
    {
        isWalkingAway = true;
        targetWorldPosition = exitWorldPosition; 
        isMoving = true;
    }
}