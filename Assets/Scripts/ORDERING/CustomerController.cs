using System.Collections;
using UnityEngine;

public class CustomerController : MonoBehaviour
{
    public DishData orderedDish;

    private float moveSpeed = 10f;
    private int currentLineIndex = -1;
    private Vector3 targetWorldPosition;
    private bool isWalkingAway = false;

    private Renderer[] myRenderers;

    private void Awake()
    {
        myRenderers = GetComponentsInChildren<Renderer>();
    }

    private void Update()
    {
        // Smoothly slide towards whatever line position the OrderManager assigns us
        if (currentLineIndex != -1 || isWalkingAway)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetWorldPosition, moveSpeed * Time.deltaTime);
        }
    }

    public void UpdateLinePosition(Vector3 newPosition, int index)
    {
        currentLineIndex = index;
        targetWorldPosition = newPosition;
    }

    private void OnMouseDown()
    {
        // Only accept clicks if they are standing at the front of the line (Index 0)
        if (currentLineIndex == 0 && !isWalkingAway)
        {
            OrderManager.Instance.TakeFrontCustomerOrder(this);
        }
    }

    public void LeaveCounterAndDestroy()
    {
        isWalkingAway = true;
        currentLineIndex = -1;

        // Walk right off-screen
        targetWorldPosition = transform.position + new Vector3(30f, 0, 0);

        StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
        yield return new WaitForSeconds(0.5f); // Let them walk a bit before starting fade

        float duration = 1f;
        float elapsed = 0f;

        // Change materials to Transparent mode if needed, or simply fade standard properties
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);

            foreach (Renderer ren in myRenderers)
            {
                if (ren.material.HasProperty("_Color"))
                {
                    Color c = ren.material.color;
                    c.a = alpha;
                    ren.material.color = c;
                }
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}