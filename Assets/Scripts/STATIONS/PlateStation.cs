using UnityEngine;

public class PlateStation : BaseStation
{
    protected override void InitializeGrid()
    {
        // 1. Run the core BaseStation matrix generation first
        base.InitializeGrid();

        // 2. Trim the corners of the grid matrix to make it feel circular/oval
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // This formula isolates the 4 extreme corners of any given grid dimensions
                bool isCorner = (x == 0 && y == 0) ||
                               (x == 0 && y == gridHeight - 1) ||
                               (x == gridWidth - 1 && y == 0) ||
                               (x == gridWidth - 1 && y == gridHeight - 1);

                if (isCorner)
                {
                    // Invalidate the underlying logic tile
                    gridMatrix[x, y].isValid = false;

                    // New Step: Hide the visual component entirely so it looks round
                    if (tileVisuals[x, y] != null)
                    {
                        tileVisuals[x, y].gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}