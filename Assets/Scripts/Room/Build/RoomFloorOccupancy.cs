using System.Collections.Generic;
using UnityEngine;

public sealed class RoomFloorOccupancy
{
    public int RoomWidthInBlocks { get; }
    public int RoomDepthInBlocks { get; }
    public HashSet<Vector2Int> OccupiedFloorCells { get; }

    public RoomFloorOccupancy(int roomWidthInBlocks, int roomDepthInBlocks, HashSet<Vector2Int> occupiedFloorCells)
    {
        RoomWidthInBlocks = roomWidthInBlocks;
        RoomDepthInBlocks = roomDepthInBlocks;
        OccupiedFloorCells = occupiedFloorCells;
    }

    public bool IsFree(Vector2Int floorCell)
    {
        return OccupiedFloorCells.Contains(floorCell) == false;
    }
}
