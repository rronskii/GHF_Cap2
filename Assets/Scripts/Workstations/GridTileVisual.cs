using UnityEngine;

public class GridTileVisual : MonoBehaviour
{
    // Add this variable at the top
    public BaseStation parentStation;
    public Vector2Int gridCoordinate; // NEW: The math coordinate of this tile

    private MeshRenderer meshRenderer;
    private Color originalColor;

    [Tooltip("The color when the mouse hovers over this tile.")]
    public Color hoverColor = new Color(0.8f, 0.8f, 0.8f, 0.5f); // Light gray, 50% transparent

    private void Awake()
    {
        // UPDATED: Now looks for the renderer on itself OR its children
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        originalColor = meshRenderer.material.color;
        parentStation = GetComponentInParent<BaseStation>();
    }

    // Triggered automatically when the mouse enters the collider
    private void OnMouseEnter()
    {
        meshRenderer.material.color = hoverColor;
    }

    // Triggered automatically when the mouse leaves the collider
    private void OnMouseExit()
    {
        meshRenderer.material.color = originalColor;
    }

    // NEW: Highlight methods
    public void SetHighlight(Color color)
    {
        meshRenderer.material.color = color;
    }

    public void ClearHighlight()
    {
        meshRenderer.material.color = originalColor;
    }
}