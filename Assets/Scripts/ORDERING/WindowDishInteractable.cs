using System.Collections;
using UnityEngine;

public class WindowDishInteractable : MonoBehaviour
{
    public DishData associatedDishData;
    private bool isServing = false;

    private void OnMouseDown()
    {
        if (isServing) return;
        OrderManager.Instance.TryServeDishToTopTicket(this, associatedDishData);
    }

    public void AnimateSuccessDelivery()
    {
        isServing = true;
        GetComponent<Collider>().enabled = false; // Disable further interactions
        StartCoroutine(ServeGlideRoutine());
    }

    private IEnumerator ServeGlideRoutine()
    {
        float duration = 0.8f;
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + new Vector3(0, 2f, 0); // Float upwards into completion

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.position = Vector3.Lerp(startPos, targetPos, t);

            // Optional: Add spin or scale shrinkage here for polish
            yield return null;
        }

        Destroy(gameObject);
    }
}