using System;
using System.Collections;
using UnityEngine;

public class WindowDishInteractable : MonoBehaviour
{
    public static event Action OnTutorialDishServed;

    public DishData associatedDishData;
    private bool isServing = false;

    private void OnMouseDown()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
        if (isServing) return;

        // Note: Make sure the method name matches what we just added to OrderManager
        OrderManager.Instance.TryServeDishToTopTicket(this, associatedDishData);
    }

    // --- NEW: RIGHT CLICK TO TRASH ---
    private void OnMouseOver()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
        if (isServing) return;

        // Input.GetMouseButtonDown(1) specifically detects Right-Clicks
        if (Input.GetMouseButtonDown(1))
        {
            TrashDish();
        }
    }

    private void TrashDish()
    {
        isServing = true;
        GetComponent<Collider>().enabled = false;

        // Tell the manager to clear this slot and shift other dishes down
        OrderManager.Instance.RemoveDishFromCounter(this.gameObject);

        StartCoroutine(TrashGlideRoutine());
    }

    private IEnumerator TrashGlideRoutine()
    {
        Transform trashTarget = OrderManager.Instance.windowTrashPoint;

        // Failsafe if you forgot to assign the trashcan in the Inspector
        if (trashTarget == null)
        {
            Destroy(gameObject);
            yield break;
        }

        Vector3 startPos = transform.position;
        // Hover point: Directly above the trash can
        Vector3 hoverPos = new Vector3(trashTarget.position.x, startPos.y + 1f, trashTarget.position.z);
        // Drop point: Deep inside the trash can
        Vector3 dropPos = trashTarget.position + new Vector3(0, -1f, 0);

        float duration = 0.3f;
        float elapsed = 0f;

        // Phase 1: Glide horizontally to the trash can
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, hoverPos, elapsed / duration);
            yield return null;
        }

        elapsed = 0f;
        Vector3 initialScale = transform.localScale;

        // Phase 2: Drop down into the can while shrinking to simulate fading away
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(hoverPos, dropPos, t);
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, t);
            yield return null;
        }

        Destroy(gameObject);
    }
    // ---------------------------------

    public void AnimateSuccessDelivery(Transform targetCustomer)
    {
        isServing = true;
        GetComponent<Collider>().enabled = false;

        OnTutorialDishServed?.Invoke();
        StartCoroutine(ServeGlideRoutine(targetCustomer));
    }

    private IEnumerator ServeGlideRoutine(Transform targetCustomer)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startPos = transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Glide to the customer's chest height (offset by 1.5 units)
            Vector3 targetPos = targetCustomer != null ? targetCustomer.position + new Vector3(0, 1.5f, 0) : startPos + new Vector3(0, 2f, 0);

            transform.position = Vector3.Lerp(startPos, targetPos, t);

            // Shrink as it reaches them to look like they "took" it
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);

            yield return null;
        }

        Destroy(gameObject);
    }
}