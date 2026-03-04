using UnityEngine;

public static class RoomCubeFillCalculator
{
    public static void CalculateCubeCounts(
        Vector3Int roomSizeInBlocks,
        int reservedFloorCellsCount,
        float blockFillPercent,
        float largeCubeAreaPercent,
        out int smallCubeCount,
        out int largeCubeCount
    )
    {
        int interiorWidthInCells = roomSizeInBlocks.x - 2;
        int interiorDepthInCells = roomSizeInBlocks.z - 2;

        if (interiorWidthInCells < 0)
        {
            interiorWidthInCells = 0;
        }

        if (interiorDepthInCells < 0)
        {
            interiorDepthInCells = 0;
        }

        int interiorCellCount = interiorWidthInCells * interiorDepthInCells;

        int placeableCellCount = interiorCellCount - reservedFloorCellsCount;
        if (placeableCellCount < 0)
        {
            placeableCellCount = 0;
        }

        float clampedFillPercent = Mathf.Clamp01(blockFillPercent);
        float clampedLargeAreaPercent = Mathf.Clamp01(largeCubeAreaPercent);

        int targetFilledCellCount = Mathf.RoundToInt(placeableCellCount * clampedFillPercent);
        int targetLargeFilledCellCount = Mathf.RoundToInt(targetFilledCellCount * clampedLargeAreaPercent);

        int computedLargeCubeCount = targetLargeFilledCellCount / 4;

        int maximumLargeCubeCount = placeableCellCount / 4;
        if (computedLargeCubeCount > maximumLargeCubeCount)
        {
            computedLargeCubeCount = maximumLargeCubeCount;
        }

        int usedLargeCells = computedLargeCubeCount * 4;

        int computedSmallCubeCount = targetFilledCellCount - usedLargeCells;
        if (computedSmallCubeCount < 0)
        {
            computedSmallCubeCount = 0;
        }

        smallCubeCount = computedSmallCubeCount;
        largeCubeCount = computedLargeCubeCount;
    }
}
