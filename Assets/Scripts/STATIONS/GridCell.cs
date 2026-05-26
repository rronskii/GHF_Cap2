using UnityEngine;

public class GridCell
{
    public Vector2Int gridCoordinate;
    public bool isValid;      // Is this a playable space? (e.g., false for stove dead zones)
    public bool isOccupied;   // Is an item currently resting here?

    // Constructor to initialize the cell
    public GridCell(Vector2Int coord, bool valid)
    {
        gridCoordinate = coord;
        isValid = valid;
        isOccupied = false;
    }
}