using UnityEngine;

public class FryingPanStation : BaseStation
{
    protected override void InitializeGrid()
    {
        // 1. Run the normal base setup first to generate the grid arrays
        base.InitializeGrid();

        // 2. Trim the four corners to simulate the round shape of a pan
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                bool isCorner = (x == 0 && y == 0) ||
                               (x == 0 && y == gridHeight - 1) ||
                               (x == gridWidth - 1 && y == 0) ||
                               (x == gridWidth - 1 && y == gridHeight - 1);

                if (isCorner)
                {
                    // Mark the logical grid as invalid so items can't be placed here
                    gridMatrix[x, y].isValid = false;

                    // Hide the visual tile
                    if (tileVisuals[x, y] != null)
                    {
                        tileVisuals[x, y].gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}