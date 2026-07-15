using System.Collections;
using UnityEngine;

public class IngredientVisuals : MonoBehaviour
{
    private Vector3 baseScale;
    private Coroutine hoverCoroutine;
    private Collider itemCollider;

    private void Awake()
    {
        baseScale = transform.localScale;
        itemCollider = GetComponent<Collider>();
    }

    public void SetHoverGrowth(bool isHovering)
    {
        if (hoverCoroutine != null) StopCoroutine(hoverCoroutine);

        Vector3 targetScale = baseScale;
        if (isHovering)
        {
            targetScale = baseScale * 1.2f;
        }

        hoverCoroutine = StartCoroutine(ScaleToTarget(targetScale));
    }

    private IEnumerator ScaleToTarget(Vector3 target)
    {
        float speed = 12f;
        while (Vector3.Distance(transform.localScale, target) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * speed);
            yield return null;
        }
        transform.localScale = target;
    }

    public void StartPopAnimation()
    {
        StartCoroutine(PopRoutine());
    }

    private IEnumerator PopRoutine()
    {
        Vector3 targetScale = baseScale * 1.3f;
        float halfDuration = 0.15f;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            transform.localScale = Vector3.Lerp(baseScale, targetScale, elapsed / halfDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            transform.localScale = Vector3.Lerp(targetScale, baseScale, elapsed / halfDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = baseScale;
    }

    public void TrashItem(Draggable3DItem core)
    {
        itemCollider.enabled = false;
        if (core.currentStation != null)
        {
            core.currentStation.SetOccupancy(core.currentCoordinate, core.GetCurrentRotatedOffsets(), null);
        }

        StartCoroutine(PopOutRoutine());
    }

    private IEnumerator PopOutRoutine()
    {
        Vector3 startScale = transform.localScale;
        float duration = 0.15f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / duration);
            yield return null;
        }
        Destroy(gameObject);
    }
}