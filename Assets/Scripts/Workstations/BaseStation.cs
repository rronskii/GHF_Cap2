using UnityEngine;

public class BaseStation : MonoBehaviour
{
    [Header("Station Dimensions")]
    public int gridWidth;
    public int gridHeight;

    [Header("Visuals")]
    public GameObject tilePrefab;
    public float tileSize = 1.0f;

    [Header("Station Type")]
    public StationType stationType;

    [Header("Station Capabilities")]
    [Tooltip("If true, cookable items placed here will cook and eventually burn.")]
    public bool isCookingStation = false;

    protected GridCell[,] gridMatrix;
    public GridTileVisual[,] tileVisuals;
    public Draggable3DItem[,] occupantMatrix; // NEW: Tracks the exact food items

    protected virtual void Awake()
    {
        InitializeGrid();
    }

    protected virtual void InitializeGrid()
    {
        gridMatrix = new GridCell[gridWidth, gridHeight];
        tileVisuals = new GridTileVisual[gridWidth, gridHeight];
        occupantMatrix = new Draggable3DItem[gridWidth, gridHeight]; // NEW

        float offsetX = (gridWidth * tileSize) / 2f - (tileSize / 2f);
        float offsetZ = (gridHeight * tileSize) / 2f - (tileSize / 2f);

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                gridMatrix[x, y] = new GridCell(new Vector2Int(x, y), true);
                gridMatrix[x, y].isOccupied = false;

                Vector3 spawnPosition = new Vector3(
                    transform.position.x + (x * tileSize) - offsetX,
                    transform.position.y + 0.01f,
                    transform.position.z + (y * tileSize) - offsetZ
                );

                GameObject spawnedTile = Instantiate(tilePrefab, spawnPosition, tilePrefab.transform.rotation, this.transform);
                spawnedTile.name = $"Tile_{x}_{y}";

                GridTileVisual visual = spawnedTile.GetComponent<GridTileVisual>();
                if (visual != null)
                {
                    visual.gridCoordinate = new Vector2Int(x, y);
                    tileVisuals[x, y] = visual;
                }
            }
        }
    }

    public bool CanPlaceItem(Vector2Int pivot, Vector2Int[] shapeOffsets)
    {
        if (shapeOffsets == null || shapeOffsets.Length == 0) return true;

        foreach (Vector2Int offset in shapeOffsets)
        {
            int checkX = pivot.x + offset.x;
            int checkY = pivot.y + offset.y;

            if (checkX < 0 || checkX >= gridWidth || checkY < 0 || checkY >= gridHeight) return false;
            if (!gridMatrix[checkX, checkY].isValid) return false;
            if (gridMatrix[checkX, checkY].isOccupied) return false;
        }
        return true;
    }

    // UPDATED: Now takes the exact item, or "null" if clearing space
    public virtual void SetOccupancy(Vector2Int pivot, Vector2Int[] shapeOffsets, Draggable3DItem occupantItem)
    {
        if (shapeOffsets == null || shapeOffsets.Length == 0) return;

        foreach (Vector2Int offset in shapeOffsets)
        {
            int cx = pivot.x + offset.x;
            int cy = pivot.y + offset.y;

            if (cx >= 0 && cx < gridWidth && cy >= 0 && cy < gridHeight)
            {
                gridMatrix[cx, cy].isOccupied = (occupantItem != null);
                occupantMatrix[cx, cy] = occupantItem; // Store the reference
            }
        }
    }

    // NEW: Easy lookup for neighbor checking
    public Draggable3DItem GetOccupantAt(Vector2Int coord)
    {
        if (coord.x >= 0 && coord.x < gridWidth && coord.y >= 0 && coord.y < gridHeight)
        {
            return occupantMatrix[coord.x, coord.y];
        }
        return null;
    }

    public void HighlightTiles(Vector2Int pivot, Vector2Int[] shapeOffsets, bool isValid)
    {
        ClearAllHighlights();
        Color highlightColor = isValid ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.5f);
        if (shapeOffsets == null || shapeOffsets.Length == 0) return;

        foreach (Vector2Int offset in shapeOffsets)
        {
            int cx = pivot.x + offset.x;
            int cy = pivot.y + offset.y;

            if (cx >= 0 && cx < gridWidth && cy >= 0 && cy < gridHeight && gridMatrix[cx, cy].isValid)
            {
                if (tileVisuals[cx, cy] != null) tileVisuals[cx, cy].SetHighlight(highlightColor);
            }
        }
    }

    public void ClearAllHighlights()
    {
        if (tileVisuals == null) return;
        foreach (GridTileVisual tile in tileVisuals)
        {
            if (tile != null) tile.ClearHighlight();
        }
    }
}