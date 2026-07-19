using UnityEngine;

public class BouncingArrow3D : MonoBehaviour
{
    public float bounceSpeed = 6f;
    public float bounceDistance = 0.2f;

    private Vector3 startLocalPos;
    private Draggable3DItem parentItem;
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        startLocalPos = transform.localPosition;

        parentItem = GetComponentInParent<Draggable3DItem>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (parentItem != null)
        {
            // --- NEW: Immediately destroy the arrow if the item is trashed/locked ---
            if (parentItem.isLocked)
            {
                Destroy(gameObject);
                return;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = !parentItem.isDragging;
            }
        }

        float offset = Mathf.Sin(Time.time * bounceSpeed) * bounceDistance;
        transform.localPosition = startLocalPos + new Vector3(0, 0, offset);
    }
}