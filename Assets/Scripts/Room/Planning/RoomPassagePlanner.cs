using System.Collections.Generic;
using UnityEngine;

public sealed class RoomPassagePlanner : MonoBehaviour
{
    private enum CornerId
    {
        SouthWest = 0,
        SouthEast = 1,
        NorthWest = 2,
        NorthEast = 3
    }

    [SerializeField] private int _doorClearDepthInCells = 3;
    [SerializeField] private int _additionalPassageWidthPaddingInCells = 0;
    [SerializeField, Min(0f)] private float _noiseCostMultiplier = 20f;
    [SerializeField, Min(0)] private int _cornerSmoothingAdditionalRadiusInCells = 1;

    private readonly HashSet<Vector2Int> _additionalNoFillCells = new HashSet<Vector2Int>();

    private bool _hasGuaranteedNookCell = false;
    private Vector2Int _guaranteedNookCell;

    public IReadOnlyCollection<Vector2Int> AdditionalNoFillCells => _additionalNoFillCells;

    public bool TryGetGuaranteedNookCell(out Vector2Int guaranteedNookCell)
    {
        guaranteedNookCell = _guaranteedNookCell;
        return _hasGuaranteedNookCell;
    }

    public HashSet<Vector2Int> CreateReservedFloorCells(Vector3Int roomSizeInBlocks, IReadOnlyList<RoomDoorPlan> doorPlans, RoomTypeProfile roomTypeProfile)
    {
        _additionalNoFillCells.Clear();
        _hasGuaranteedNookCell = false;
        _guaranteedNookCell = Vector2Int.zero;

        HashSet<Vector2Int> corridorReservedFloorCells = new HashSet<Vector2Int>();
        HashSet<Vector2Int> networkSpineCells = new HashSet<Vector2Int>();
        Vector2Int entranceInsideCell = GetFallbackCenterCell(roomSizeInBlocks);

        float[,] noiseValues = CreateFlatNoise(roomSizeInBlocks.x, roomSizeInBlocks.z);

        if (roomTypeProfile.NoiseProfile != null)
        {
            noiseValues = roomTypeProfile.NoiseProfile.GenerateNoiseMap(roomSizeInBlocks.x, roomSizeInBlocks.z);
        }

        int maximumDoorWidthInBlocks = 1;

        for (int doorIndex = 0; doorIndex < doorPlans.Count; doorIndex++)
        {
            int doorWidth = doorPlans[doorIndex].OpeningWidthInBlocks;

            if (doorWidth > maximumDoorWidthInBlocks)
            {
                maximumDoorWidthInBlocks = doorWidth;
            }
        }

        int passageWidthInCells = maximumDoorWidthInBlocks + (_additionalPassageWidthPaddingInCells * 2);

        if (passageWidthInCells < 1)
        {
            passageWidthInCells = 1;
        }

        for (int doorIndex = 0; doorIndex < doorPlans.Count; doorIndex++)
        {
            ReserveDoorFrontArea(corridorReservedFloorCells, roomSizeInBlocks, doorPlans[doorIndex]);
        }

        RoomDoorPlan startDoorPlan = null;
        RoomDoorPlan endDoorPlan = null;

        if (doorPlans.Count == 1)
        {
            startDoorPlan = doorPlans[0];
            entranceInsideCell = GetInsideCenterCell(startDoorPlan, roomSizeInBlocks);
            networkSpineCells.Add(entranceInsideCell);
        }
        else if (doorPlans.Count >= 2)
        {
            bool hasEntrance = TryGetDoorPlanByRole(doorPlans, DoorRole.Entrance, out startDoorPlan);
            bool hasExit = TryGetDoorPlanByRole(doorPlans, DoorRole.Exit, out endDoorPlan);

            if (hasEntrance == false || hasExit == false)
            {
                PickFarthestPair(roomSizeInBlocks, doorPlans, out startDoorPlan, out endDoorPlan);
            }

            entranceInsideCell = GetInsideCenterCell(startDoorPlan, roomSizeInBlocks);
            Vector2Int exitInsideCell = GetInsideCenterCell(endDoorPlan, roomSizeInBlocks);

            List<Vector2Int> mainPath = BuildAStarPath(roomSizeInBlocks, entranceInsideCell, exitInsideCell, noiseValues);

            ReserveCorridorForPath(corridorReservedFloorCells, roomSizeInBlocks, mainPath, passageWidthInCells);

            for (int index = 0; index < mainPath.Count; index++)
            {
                networkSpineCells.Add(mainPath[index]);
            }
        }

        for (int doorIndex = 0; doorIndex < doorPlans.Count; doorIndex++)
        {
            RoomDoorPlan doorPlan = doorPlans[doorIndex];

            if (startDoorPlan != null && IsSameDoorPlan(doorPlan, startDoorPlan) == true)
            {
                continue;
            }

            if (endDoorPlan != null && IsSameDoorPlan(doorPlan, endDoorPlan) == true)
            {
                continue;
            }

            Vector2Int doorInsideCell = GetInsideCenterCell(doorPlan, roomSizeInBlocks);
            Vector2Int nearestNetworkCell = FindNearestNetworkCell(doorInsideCell, networkSpineCells);
            List<Vector2Int> connectorPath = BuildAStarPath(roomSizeInBlocks, doorInsideCell, nearestNetworkCell, noiseValues);

            ReserveCorridorForPath(corridorReservedFloorCells, roomSizeInBlocks, connectorPath, passageWidthInCells);

            for (int index = 0; index < connectorPath.Count; index++)
            {
                networkSpineCells.Add(connectorPath[index]);
            }
        }

        if (roomTypeProfile.HasGuaranteedNookDemand == true && roomTypeProfile.NookPrefabs.Count > 0)
        {
            TryPlanGuaranteedNook(roomSizeInBlocks, roomTypeProfile, noiseValues, networkSpineCells, corridorReservedFloorCells, passageWidthInCells, entranceInsideCell);
        }

        return corridorReservedFloorCells;
    }

    private bool TryGetDoorPlanByRole(IReadOnlyList<RoomDoorPlan> doorPlans, DoorRole role, out RoomDoorPlan result)
    {
        for (int doorIndex = 0; doorIndex < doorPlans.Count; doorIndex++)
        {
            RoomDoorPlan doorPlan = doorPlans[doorIndex];

            if (doorPlan.Role == role)
            {
                result = doorPlan;
                return true;
            }
        }

        result = null;
        return false;
    }

    private void PickFarthestPair(Vector3Int roomSizeInBlocks, IReadOnlyList<RoomDoorPlan> doorPlans, out RoomDoorPlan a, out RoomDoorPlan b)
    {
        a = doorPlans[0];
        b = doorPlans[1];

        int bestDistance = -1;

        for (int i = 0; i < doorPlans.Count; i++)
        {
            Vector2Int cellA = GetInsideCenterCell(doorPlans[i], roomSizeInBlocks);

            for (int j = i + 1; j < doorPlans.Count; j++)
            {
                Vector2Int cellB = GetInsideCenterCell(doorPlans[j], roomSizeInBlocks);
                int distance = Mathf.Abs(cellA.x - cellB.x) + Mathf.Abs(cellA.y - cellB.y);

                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    a = doorPlans[i];
                    b = doorPlans[j];
                }
            }
        }
    }

    private Vector2Int GetFallbackCenterCell(Vector3Int roomSizeInBlocks)
    {
        int centerX = Mathf.Clamp(roomSizeInBlocks.x / 2, 1, roomSizeInBlocks.x - 2);
        int centerZ = Mathf.Clamp(roomSizeInBlocks.z / 2, 1, roomSizeInBlocks.z - 2);
        return new Vector2Int(centerX, centerZ);
    }

    private bool IsSameDoorPlan(RoomDoorPlan a, RoomDoorPlan b)
    {
        if (a == null || b == null)
        {
            return false;
        }

        if (a.Side != b.Side)
        {
            return false;
        }

        if (a.Role != b.Role)
        {
            return false;
        }

        if (a.OpeningOffset != b.OpeningOffset)
        {
            return false;
        }

        if (a.OpeningWidthInBlocks != b.OpeningWidthInBlocks)
        {
            return false;
        }

        if (a.OpeningHeightInBlocks != b.OpeningHeightInBlocks)
        {
            return false;
        }

        return true;
    }

    private void TryPlanGuaranteedNook(
        Vector3Int roomSizeInBlocks,
        RoomTypeProfile roomTypeProfile,
        float[,] noiseValues,
        HashSet<Vector2Int> networkSpineCells,
        HashSet<Vector2Int> corridorReservedFloorCells,
        int passageWidthInCells,
        Vector2Int entranceInsideCell
    )
    {
        if (roomTypeProfile.RoomType == RoomType.Start)
        {
            TryPlanStartGuaranteedNook(roomSizeInBlocks, roomTypeProfile, entranceInsideCell);

            return;
        }

        int wallMargin = roomTypeProfile.MaximumNookWallMarginInCells;
        int footprintRadius = roomTypeProfile.MaximumNookFootprintRadiusInCells;
        int minimumDistance = roomTypeProfile.MaximumNookMinimumDistanceFromCorridorInCells;

        int pocketSize = minimumDistance + (footprintRadius * 2) + 2;

        if (pocketSize < 4)
        {
            pocketSize = 4;
        }

        int maxPocketSizeX = (roomSizeInBlocks.x - 2) - (1 + wallMargin);
        int maxPocketSizeZ = (roomSizeInBlocks.z - 2) - (1 + wallMargin);

        int maximumPocketSize = Mathf.Min(maxPocketSizeX, maxPocketSizeZ);

        if (maximumPocketSize < 4)
        {
            return;
        }

        if (pocketSize > maximumPocketSize)
        {
            pocketSize = maximumPocketSize;
        }

        CornerId bestCorner = CornerId.SouthWest;
        int bestCornerScore = -1;

        for (int cornerIndex = 0; cornerIndex < 4; cornerIndex++)
        {
            CornerId corner = (CornerId)cornerIndex;

            Vector2Int deepCell = ComputePocketDeepCell(roomSizeInBlocks, wallMargin, footprintRadius, pocketSize, corner);

            if (IsInteriorCell(deepCell, roomSizeInBlocks) == false)
            {
                continue;
            }

            int distanceFromEntrance = Mathf.Abs(deepCell.x - entranceInsideCell.x) + Mathf.Abs(deepCell.y - entranceInsideCell.y);

            if (distanceFromEntrance > bestCornerScore)
            {
                bestCornerScore = distanceFromEntrance;
                bestCorner = corner;
            }
        }

        PocketRect pocketRect = ComputePocketRect(roomSizeInBlocks, wallMargin, pocketSize, bestCorner);

        if (pocketRect.IsValid == false)
        {
            return;
        }

        Vector2Int mouthCell = ComputePocketMouthCell(pocketRect, bestCorner);
        Vector2Int guaranteedCell = ComputePocketDeepCell(roomSizeInBlocks, wallMargin, footprintRadius, pocketSize, bestCorner);

        if (IsInteriorCell(mouthCell, roomSizeInBlocks) == false)
        {
            return;
        }

        if (IsInteriorCell(guaranteedCell, roomSizeInBlocks) == false)
        {
            return;
        }

        Vector2Int nearestNetworkCell = FindNearestNetworkCell(mouthCell, networkSpineCells);

        List<Vector2Int> branchPath = BuildAStarPath(roomSizeInBlocks, nearestNetworkCell, mouthCell, noiseValues);

        ReserveCorridorForPath(corridorReservedFloorCells, roomSizeInBlocks, branchPath, passageWidthInCells);

        for (int index = 0; index < branchPath.Count; index++)
        {
            networkSpineCells.Add(branchPath[index]);
        }

        ReservePocketNoFillCells(roomSizeInBlocks, pocketRect);

        _hasGuaranteedNookCell = true;
        _guaranteedNookCell = guaranteedCell;
    }

    private void TryPlanStartGuaranteedNook(Vector3Int roomSizeInBlocks, RoomTypeProfile roomTypeProfile, Vector2Int entranceInsideCell)
    {
        int wallMargin = roomTypeProfile.MaximumNookWallMarginInCells;
        int footprintRadius = roomTypeProfile.MaximumNookFootprintRadiusInCells;

        Vector2Int centerCell = GetFallbackCenterCell(roomSizeInBlocks);
        Vector2Int laneStep = GetDominantAxisStep(entranceInsideCell, centerCell);

        if (laneStep == Vector2Int.zero)
        {
            laneStep = new Vector2Int(0, 1);
        }

        int forwardOffset = Mathf.Max(footprintRadius + 2, 3);
        Vector2Int guaranteedCell = centerCell + (laneStep * forwardOffset);

        if (IsCellInsideMargin(guaranteedCell, roomSizeInBlocks, wallMargin) == false)
        {
            guaranteedCell = centerCell + (laneStep * Mathf.Max(footprintRadius + 1, 2));
        }

        if (IsCellInsideMargin(guaranteedCell, roomSizeInBlocks, wallMargin) == false)
        {
            guaranteedCell = centerCell;
        }

        if (IsCellInsideMargin(guaranteedCell, roomSizeInBlocks, wallMargin) == false)
        {
            return;
        }

        ReserveLineNoFillCells(roomSizeInBlocks, centerCell, guaranteedCell, footprintRadius + 1);
        ReserveCellAreaNoFill(roomSizeInBlocks, guaranteedCell, footprintRadius + 1);

        _hasGuaranteedNookCell = true;
        _guaranteedNookCell = guaranteedCell;
    }

    private struct PocketRect
    {
        public int MinX;
        public int MaxX;
        public int MinZ;
        public int MaxZ;
        public bool IsValid;

        public PocketRect(int minX, int maxX, int minZ, int maxZ, bool isValid)
        {
            MinX = minX;
            MaxX = maxX;
            MinZ = minZ;
            MaxZ = maxZ;
            IsValid = isValid;
        }
    }

    private PocketRect ComputePocketRect(Vector3Int roomSizeInBlocks, int wallMargin, int pocketSize, CornerId corner)
    {
        int interiorMinX = 1 + wallMargin;
        int interiorMaxX = (roomSizeInBlocks.x - 2) - wallMargin;

        int interiorMinZ = 1 + wallMargin;
        int interiorMaxZ = (roomSizeInBlocks.z - 2) - wallMargin;

        int minX = interiorMinX;
        int maxX = interiorMinX + pocketSize - 1;

        int minZ = interiorMinZ;
        int maxZ = interiorMinZ + pocketSize - 1;

        if (corner == CornerId.SouthEast)
        {
            maxX = interiorMaxX;
            minX = interiorMaxX - pocketSize + 1;
        }

        if (corner == CornerId.NorthWest)
        {
            maxZ = interiorMaxZ;
            minZ = interiorMaxZ - pocketSize + 1;
        }

        if (corner == CornerId.NorthEast)
        {
            maxX = interiorMaxX;
            minX = interiorMaxX - pocketSize + 1;

            maxZ = interiorMaxZ;
            minZ = interiorMaxZ - pocketSize + 1;
        }

        if (minX > maxX)
        {
            return new PocketRect(0, 0, 0, 0, false);
        }

        if (minZ > maxZ)
        {
            return new PocketRect(0, 0, 0, 0, false);
        }

        if (minX < 1 || maxX > roomSizeInBlocks.x - 2)
        {
            return new PocketRect(0, 0, 0, 0, false);
        }

        if (minZ < 1 || maxZ > roomSizeInBlocks.z - 2)
        {
            return new PocketRect(0, 0, 0, 0, false);
        }

        return new PocketRect(minX, maxX, minZ, maxZ, true);
    }

    private Vector2Int ComputePocketMouthCell(PocketRect pocketRect, CornerId corner)
    {
        if (corner == CornerId.SouthWest)
        {
            return new Vector2Int(pocketRect.MaxX, pocketRect.MaxZ);
        }

        if (corner == CornerId.SouthEast)
        {
            return new Vector2Int(pocketRect.MinX, pocketRect.MaxZ);
        }

        if (corner == CornerId.NorthWest)
        {
            return new Vector2Int(pocketRect.MaxX, pocketRect.MinZ);
        }

        return new Vector2Int(pocketRect.MinX, pocketRect.MinZ);
    }

    private Vector2Int ComputePocketDeepCell(Vector3Int roomSizeInBlocks, int wallMargin, int footprintRadius, int pocketSize, CornerId corner)
    {
        PocketRect pocketRect = ComputePocketRect(roomSizeInBlocks, wallMargin, pocketSize, corner);

        if (pocketRect.IsValid == false)
        {
            return Vector2Int.zero;
        }

        int x = pocketRect.MinX;
        int z = pocketRect.MinZ;

        if (corner == CornerId.SouthEast)
        {
            x = pocketRect.MaxX;
            z = pocketRect.MinZ;
        }

        if (corner == CornerId.NorthWest)
        {
            x = pocketRect.MinX;
            z = pocketRect.MaxZ;
        }

        if (corner == CornerId.NorthEast)
        {
            x = pocketRect.MaxX;
            z = pocketRect.MaxZ;
        }

        int offsetX = 0;
        int offsetZ = 0;

        if (corner == CornerId.SouthWest)
        {
            offsetX = footprintRadius;
            offsetZ = footprintRadius;
        }

        if (corner == CornerId.SouthEast)
        {
            offsetX = -footprintRadius;
            offsetZ = footprintRadius;
        }

        if (corner == CornerId.NorthWest)
        {
            offsetX = footprintRadius;
            offsetZ = -footprintRadius;
        }

        if (corner == CornerId.NorthEast)
        {
            offsetX = -footprintRadius;
            offsetZ = -footprintRadius;
        }

        Vector2Int cell = new Vector2Int(x + offsetX, z + offsetZ);

        if (IsInteriorCell(cell, roomSizeInBlocks) == false)
        {
            return Vector2Int.zero;
        }

        return cell;
    }

    private void ReservePocketNoFillCells(Vector3Int roomSizeInBlocks, PocketRect pocketRect)
    {
        for (int x = pocketRect.MinX; x <= pocketRect.MaxX; x++)
        {
            for (int z = pocketRect.MinZ; z <= pocketRect.MaxZ; z++)
            {
                Vector2Int cell = new Vector2Int(x, z);

                if (IsInteriorCell(cell, roomSizeInBlocks) == false)
                {
                    continue;
                }

                _additionalNoFillCells.Add(cell);
            }
        }
    }

    private void ReserveLineNoFillCells(Vector3Int roomSizeInBlocks, Vector2Int startCell, Vector2Int endCell, int radius)
    {
        Vector2Int step = GetDominantAxisStep(startCell, endCell);
        Vector2Int currentCell = startCell;

        for (int i = 0; i < 256; i++)
        {
            ReserveCellAreaNoFill(roomSizeInBlocks, currentCell, radius);

            if (currentCell == endCell)
            {
                return;
            }

            currentCell = new Vector2Int(currentCell.x + step.x, currentCell.y + step.y);
        }
    }

    private void ReserveCellAreaNoFill(Vector3Int roomSizeInBlocks, Vector2Int centerCell, int radius)
    {
        for (int offsetX = -radius; offsetX <= radius; offsetX++)
        {
            for (int offsetZ = -radius; offsetZ <= radius; offsetZ++)
            {
                Vector2Int cell = new Vector2Int(centerCell.x + offsetX, centerCell.y + offsetZ);

                if (IsInteriorCell(cell, roomSizeInBlocks) == false)
                {
                    continue;
                }

                _additionalNoFillCells.Add(cell);
            }
        }
    }

    private Vector2Int GetDominantAxisStep(Vector2Int fromCell, Vector2Int toCell)
    {
        int deltaX = toCell.x - fromCell.x;
        int deltaZ = toCell.y - fromCell.y;

        if (Mathf.Abs(deltaX) >= Mathf.Abs(deltaZ))
        {
            return new Vector2Int(GetStep(deltaX), 0);
        }

        return new Vector2Int(0, GetStep(deltaZ));
    }

    private bool IsCellInsideMargin(Vector2Int cell, Vector3Int roomSizeInBlocks, int wallMargin)
    {
        if (IsInteriorCell(cell, roomSizeInBlocks) == false)
        {
            return false;
        }

        int minX = 1 + wallMargin;
        int maxX = (roomSizeInBlocks.x - 2) - wallMargin;
        int minZ = 1 + wallMargin;
        int maxZ = (roomSizeInBlocks.z - 2) - wallMargin;

        if (cell.x < minX)
        {
            return false;
        }

        if (cell.x > maxX)
        {
            return false;
        }

        if (cell.y < minZ)
        {
            return false;
        }

        if (cell.y > maxZ)
        {
            return false;
        }

        return true;
    }

    private int GetStep(int delta)
    {
        if (delta > 0)
        {
            return 1;
        }

        if (delta < 0)
        {
            return -1;
        }

        return 0;
    }

    private float[,] CreateFlatNoise(int width, int height)
    {
        float[,] values = new float[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                values[x, y] = 0.5f;
            }
        }

        return values;
    }

    private void ReserveDoorFrontArea(HashSet<Vector2Int> reservedFloorCells, Vector3Int roomSizeInBlocks, RoomDoorPlan doorPlan)
    {
        for (int widthIndex = 0; widthIndex < doorPlan.OpeningWidthInBlocks; widthIndex++)
        {
            for (int depthIndex = 0; depthIndex < _doorClearDepthInCells; depthIndex++)
            {
                Vector2Int floorCell = GetDoorFrontCell(roomSizeInBlocks, doorPlan, widthIndex, depthIndex);

                if (IsInteriorCell(floorCell, roomSizeInBlocks) == false)
                {
                    continue;
                }

                reservedFloorCells.Add(floorCell);
            }
        }
    }

    private Vector2Int GetDoorFrontCell(Vector3Int roomSizeInBlocks, RoomDoorPlan doorPlan, int widthIndex, int depthIndex)
    {
        if (doorPlan.Side == DoorSide.North)
        {
            int cellX = doorPlan.OpeningOffset + widthIndex;
            int cellZ = roomSizeInBlocks.z - 2 - depthIndex;
            return new Vector2Int(cellX, cellZ);
        }

        if (doorPlan.Side == DoorSide.South)
        {
            int cellX = doorPlan.OpeningOffset + widthIndex;
            int cellZ = 1 + depthIndex;
            return new Vector2Int(cellX, cellZ);
        }

        if (doorPlan.Side == DoorSide.East)
        {
            int cellX = roomSizeInBlocks.x - 2 - depthIndex;
            int cellZ = doorPlan.OpeningOffset + widthIndex;
            return new Vector2Int(cellX, cellZ);
        }

        int cellXWest = 1 + depthIndex;
        int cellZWest = doorPlan.OpeningOffset + widthIndex;
        return new Vector2Int(cellXWest, cellZWest);
    }

    private Vector2Int GetInsideCenterCell(RoomDoorPlan doorPlan, Vector3Int roomSizeInBlocks)
    {
        int centerOffset = doorPlan.OpeningOffset + (doorPlan.OpeningWidthInBlocks / 2);

        if (doorPlan.Side == DoorSide.North)
        {
            return new Vector2Int(centerOffset, roomSizeInBlocks.z - 2);
        }

        if (doorPlan.Side == DoorSide.South)
        {
            return new Vector2Int(centerOffset, 1);
        }

        if (doorPlan.Side == DoorSide.East)
        {
            return new Vector2Int(roomSizeInBlocks.x - 2, centerOffset);
        }

        return new Vector2Int(1, centerOffset);
    }

    private Vector2Int FindNearestNetworkCell(Vector2Int fromCell, HashSet<Vector2Int> networkCells)
    {
        Vector2Int bestCell = fromCell;
        int bestDistance = int.MaxValue;

        foreach (Vector2Int cell in networkCells)
        {
            int distance = Mathf.Abs(cell.x - fromCell.x) + Mathf.Abs(cell.y - fromCell.y);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestCell = cell;
            }
        }

        return bestCell;
    }

    private List<Vector2Int> BuildAStarPath(Vector3Int roomSizeInBlocks, Vector2Int startCell, Vector2Int endCell, float[,] noiseValues)
    {
        int width = roomSizeInBlocks.x;
        int depth = roomSizeInBlocks.z;

        int[,] gScore = new int[width, depth];
        int[,] fScore = new int[width, depth];
        bool[,] isClosed = new bool[width, depth];
        bool[,] isInOpen = new bool[width, depth];
        Vector2Int[,] parent = new Vector2Int[width, depth];
        bool[,] hasParent = new bool[width, depth];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                gScore[x, z] = int.MaxValue;
                fScore[x, z] = int.MaxValue;
                isClosed[x, z] = false;
                isInOpen[x, z] = false;
                hasParent[x, z] = false;
            }
        }

        List<Vector2Int> openList = new List<Vector2Int>();

        gScore[startCell.x, startCell.y] = 0;
        fScore[startCell.x, startCell.y] = HeuristicCost(startCell, endCell);

        openList.Add(startCell);
        isInOpen[startCell.x, startCell.y] = true;

        int[] neighborOffsetX = new int[4] { 1, -1, 0, 0 };
        int[] neighborOffsetZ = new int[4] { 0, 0, 1, -1 };

        while (openList.Count > 0)
        {
            Vector2Int currentCell = PickLowestFScoreCell(openList, fScore);

            if (currentCell == endCell)
            {
                return ReconstructPath(startCell, endCell, parent, hasParent);
            }

            RemoveFromOpen(openList, currentCell);
            isInOpen[currentCell.x, currentCell.y] = false;
            isClosed[currentCell.x, currentCell.y] = true;

            for (int directionIndex = 0; directionIndex < 4; directionIndex++)
            {
                Vector2Int neighborCell = new Vector2Int(currentCell.x + neighborOffsetX[directionIndex], currentCell.y + neighborOffsetZ[directionIndex]);

                if (IsInteriorCell(neighborCell, roomSizeInBlocks) == false)
                {
                    continue;
                }

                if (isClosed[neighborCell.x, neighborCell.y] == true)
                {
                    continue;
                }

                int stepCost = 10 + Mathf.RoundToInt(noiseValues[neighborCell.x, neighborCell.y] * _noiseCostMultiplier);
                int currentG = gScore[currentCell.x, currentCell.y];

                if (currentG == int.MaxValue)
                {
                    continue;
                }

                int tentativeG = currentG + stepCost;

                int neighborG = gScore[neighborCell.x, neighborCell.y];

                if (tentativeG < neighborG)
                {
                    parent[neighborCell.x, neighborCell.y] = currentCell;
                    hasParent[neighborCell.x, neighborCell.y] = true;

                    gScore[neighborCell.x, neighborCell.y] = tentativeG;
                    fScore[neighborCell.x, neighborCell.y] = tentativeG + HeuristicCost(neighborCell, endCell);

                    if (isInOpen[neighborCell.x, neighborCell.y] == false)
                    {
                        openList.Add(neighborCell);
                        isInOpen[neighborCell.x, neighborCell.y] = true;
                    }
                }
            }
        }

        return BuildManhattanFallbackPath(startCell, endCell);
    }

    private int HeuristicCost(Vector2Int fromCell, Vector2Int toCell)
    {
        int manhattan = Mathf.Abs(fromCell.x - toCell.x) + Mathf.Abs(fromCell.y - toCell.y);
        return manhattan * 10;
    }

    private Vector2Int PickLowestFScoreCell(List<Vector2Int> openList, int[,] fScore)
    {
        Vector2Int bestCell = openList[0];
        int bestScore = fScore[bestCell.x, bestCell.y];

        for (int index = 1; index < openList.Count; index++)
        {
            Vector2Int cell = openList[index];
            int score = fScore[cell.x, cell.y];

            if (score < bestScore)
            {
                bestScore = score;
                bestCell = cell;
            }
        }

        return bestCell;
    }

    private void RemoveFromOpen(List<Vector2Int> openList, Vector2Int cell)
    {
        for (int index = openList.Count - 1; index >= 0; index--)
        {
            if (openList[index] == cell)
            {
                openList.RemoveAt(index);
                return;
            }
        }
    }

    private List<Vector2Int> ReconstructPath(Vector2Int startCell, Vector2Int endCell, Vector2Int[,] parent, bool[,] hasParent)
    {
        List<Vector2Int> path = new List<Vector2Int>();

        Vector2Int current = endCell;
        path.Add(current);

        int safetyLimit = 4096;

        for (int stepIndex = 0; stepIndex < safetyLimit; stepIndex++)
        {
            if (current == startCell)
            {
                break;
            }

            if (hasParent[current.x, current.y] == false)
            {
                break;
            }

            current = parent[current.x, current.y];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    private List<Vector2Int> BuildManhattanFallbackPath(Vector2Int startCell, Vector2Int endCell)
    {
        List<Vector2Int> path = new List<Vector2Int>();

        Vector2Int currentCell = startCell;
        path.Add(currentCell);

        int stepX = 0;
        if (endCell.x > startCell.x)
        {
            stepX = 1;
        }
        else if (endCell.x < startCell.x)
        {
            stepX = -1;
        }

        int stepZ = 0;
        if (endCell.y > startCell.y)
        {
            stepZ = 1;
        }
        else if (endCell.y < startCell.y)
        {
            stepZ = -1;
        }

        while (currentCell.x != endCell.x)
        {
            currentCell = new Vector2Int(currentCell.x + stepX, currentCell.y);
            path.Add(currentCell);
        }

        while (currentCell.y != endCell.y)
        {
            currentCell = new Vector2Int(currentCell.x, currentCell.y + stepZ);
            path.Add(currentCell);
        }

        return path;
    }

    private void ReserveCorridorForPath(HashSet<Vector2Int> reservedFloorCells, Vector3Int roomSizeInBlocks, List<Vector2Int> pathCells, int passageWidthInCells)
    {
        int leftExtent = passageWidthInCells / 2;
        int rightExtent = (passageWidthInCells - 1) / 2;

        if (pathCells.Count == 1)
        {
            ReservePoint(reservedFloorCells, roomSizeInBlocks, pathCells[0], leftExtent, rightExtent);

            ReserveEndCapsSoftening(reservedFloorCells, roomSizeInBlocks, pathCells, leftExtent, rightExtent);

            return;
        }

        for (int index = 0; index < pathCells.Count - 1; index++)
        {
            Vector2Int fromCell = pathCells[index];
            Vector2Int toCell = pathCells[index + 1];

            int deltaX = toCell.x - fromCell.x;
            int deltaZ = toCell.y - fromCell.y;

            if (deltaX != 0 && deltaZ == 0)
            {
                ReserveHorizontalStep(reservedFloorCells, roomSizeInBlocks, fromCell, leftExtent, rightExtent);
                ReserveHorizontalStep(reservedFloorCells, roomSizeInBlocks, toCell, leftExtent, rightExtent);
            }

            if (deltaZ != 0 && deltaX == 0)
            {
                ReserveVerticalStep(reservedFloorCells, roomSizeInBlocks, fromCell, leftExtent, rightExtent);
                ReserveVerticalStep(reservedFloorCells, roomSizeInBlocks, toCell, leftExtent, rightExtent);
            }
        }

        ReserveCornerSoftening(reservedFloorCells, roomSizeInBlocks, pathCells, leftExtent, rightExtent);

        ReserveEndCapsSoftening(reservedFloorCells, roomSizeInBlocks, pathCells, leftExtent, rightExtent);
    }

    private void ReserveCornerSoftening(HashSet<Vector2Int> reservedFloorCells, Vector3Int roomSizeInBlocks, List<Vector2Int> pathCells, int leftExtent, int rightExtent)
    {
        int baseRadius = Mathf.Max(leftExtent, rightExtent);

        int smoothingRadius = baseRadius + _cornerSmoothingAdditionalRadiusInCells;

        if (smoothingRadius <= 0)
        {
            return;
        }

        for (int index = 1; index < pathCells.Count - 1; index++)
        {
            Vector2Int previousCell = pathCells[index - 1];
            Vector2Int currentCell = pathCells[index];
            Vector2Int nextCell = pathCells[index + 1];

            int deltaXToCurrent = currentCell.x - previousCell.x;
            int deltaZToCurrent = currentCell.y - previousCell.y;

            int deltaXFromCurrent = nextCell.x - currentCell.x;
            int deltaZFromCurrent = nextCell.y - currentCell.y;

            if (deltaXToCurrent == deltaXFromCurrent && deltaZToCurrent == deltaZFromCurrent)
            {
                continue;
            }

            int diagonalX = Mathf.Clamp(deltaXToCurrent + deltaXFromCurrent, -1, 1);
            int diagonalZ = Mathf.Clamp(deltaZToCurrent + deltaZFromCurrent, -1, 1);

            if (diagonalX == 0 || diagonalZ == 0)
            {
                continue;
            }

            ReserveQuarterDisk(reservedFloorCells, roomSizeInBlocks, currentCell, diagonalX, diagonalZ, smoothingRadius);
        }
    }

    private void ReserveEndCapsSoftening(HashSet<Vector2Int> reservedFloorCells, Vector3Int roomSizeInBlocks, List<Vector2Int> pathCells, int leftExtent, int rightExtent)
    {
        int baseRadius = Mathf.Max(leftExtent, rightExtent);

        int smoothingRadius = baseRadius + _cornerSmoothingAdditionalRadiusInCells;

        if (smoothingRadius <= 0)
        {
            return;
        }

        if (pathCells.Count <= 0)
        {
            return;
        }

        Vector2Int firstCell = pathCells[0];
        Vector2Int lastCell = pathCells[pathCells.Count - 1];

        ReserveDisk(reservedFloorCells, roomSizeInBlocks, firstCell, smoothingRadius);

        ReserveDisk(reservedFloorCells, roomSizeInBlocks, lastCell, smoothingRadius);
    }

    private void ReserveQuarterDisk(HashSet<Vector2Int> reservedFloorCells, Vector3Int roomSizeInBlocks, Vector2Int originCell, int signX, int signZ, int radius)
    {
        int radiusSquared = radius * radius;

        for (int offsetX = 0; offsetX <= radius; offsetX++)
        {
            for (int offsetZ = 0; offsetZ <= radius; offsetZ++)
            {
                int distanceSquared = (offsetX * offsetX) + (offsetZ * offsetZ);

                if (distanceSquared > radiusSquared)
                {
                    continue;
                }

                Vector2Int cell = new Vector2Int(originCell.x + (offsetX * signX), originCell.y + (offsetZ * signZ));

                if (IsInteriorCell(cell, roomSizeInBlocks) == false)
                {
                    continue;
                }

                reservedFloorCells.Add(cell);
            }
        }
    }

    private void ReserveDisk(HashSet<Vector2Int> reservedFloorCells, Vector3Int roomSizeInBlocks, Vector2Int originCell, int radius)
    {
        int radiusSquared = radius * radius;

        for (int offsetX = -radius; offsetX <= radius; offsetX++)
        {
            for (int offsetZ = -radius; offsetZ <= radius; offsetZ++)
            {
                int distanceSquared = (offsetX * offsetX) + (offsetZ * offsetZ);

                if (distanceSquared > radiusSquared)
                {
                    continue;
                }

                Vector2Int cell = new Vector2Int(originCell.x + offsetX, originCell.y + offsetZ);

                if (IsInteriorCell(cell, roomSizeInBlocks) == false)
                {
                    continue;
                }

                reservedFloorCells.Add(cell);
            }
        }
    }

    private void ReserveHorizontalStep(HashSet<Vector2Int> reservedFloorCells, Vector3Int roomSizeInBlocks, Vector2Int centerCell, int leftExtent, int rightExtent)
    {
        for (int offset = -leftExtent; offset <= rightExtent; offset++)
        {
            Vector2Int cell = new Vector2Int(centerCell.x, centerCell.y + offset);

            if (IsInteriorCell(cell, roomSizeInBlocks) == false)
            {
                continue;
            }

            reservedFloorCells.Add(cell);
        }
    }

    private void ReserveVerticalStep(HashSet<Vector2Int> reservedFloorCells, Vector3Int roomSizeInBlocks, Vector2Int centerCell, int leftExtent, int rightExtent)
    {
        for (int offset = -leftExtent; offset <= rightExtent; offset++)
        {
            Vector2Int cell = new Vector2Int(centerCell.x + offset, centerCell.y);

            if (IsInteriorCell(cell, roomSizeInBlocks) == false)
            {
                continue;
            }

            reservedFloorCells.Add(cell);
        }
    }

    private void ReservePoint(HashSet<Vector2Int> reservedFloorCells, Vector3Int roomSizeInBlocks, Vector2Int centerCell, int leftExtent, int rightExtent)
    {
        ReserveHorizontalStep(reservedFloorCells, roomSizeInBlocks, centerCell, leftExtent, rightExtent);
        ReserveVerticalStep(reservedFloorCells, roomSizeInBlocks, centerCell, leftExtent, rightExtent);
    }

    private bool IsInteriorCell(Vector2Int cell, Vector3Int roomSizeInBlocks)
    {
        if (cell.x < 1)
        {
            return false;
        }

        if (cell.x > roomSizeInBlocks.x - 2)
        {
            return false;
        }

        if (cell.y < 1)
        {
            return false;
        }

        if (cell.y > roomSizeInBlocks.z - 2)
        {
            return false;
        }

        return true;
    }
}
