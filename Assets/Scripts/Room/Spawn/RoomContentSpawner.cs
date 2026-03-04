using System.Collections.Generic;
using UnityEngine;

public sealed class RoomContentSpawner : MonoBehaviour
{
    private struct SpawnCandidate
    {
        public Vector2Int Cell;
        public float Score;

        public SpawnCandidate(Vector2Int cell, float score)
        {
            Cell = cell;
            Score = score;
        }
    }

    [SerializeField] private Transform _objectsRoot;
    [SerializeField] private Transform _enemiesRoot;
    [SerializeField] private float _blockSize = 1f;

    [SerializeField, Min(0)] private int _cornerAvoidanceInCells = 3;
    [SerializeField, Min(0)] private int _minimumResourceDistanceFromCorridorInCells = 3;
    [SerializeField, Min(0)] private int _maximumResourceDistanceFromCorridorInCells = 12;
    [SerializeField, Min(0)] private int _minimumDistanceFromEntranceInCells = 4;

    [SerializeField, Min(1)] private int _resourceClusterRadiusInCells = 3;
    [SerializeField, Min(1)] private int _enemyGuardRadiusInCells = 4;

    [SerializeField, Min(0)] private int _minimumEnemySpacingInCells = 2;
    [SerializeField, Min(0)] private int _enemyEncounterMinCorridorDistanceInCells = 2;
    [SerializeField, Min(0)] private int _enemyEncounterMaxCorridorDistanceInCells = 6;

    [SerializeField, Min(0)] private int _enemyReservedFloorAvoidanceInCells = 1;
    [SerializeField, Min(0)] private int _enemyWallAvoidanceInCells = 4;
    [SerializeField, Min(0)] private int _enemyPreferredWallDistanceInCells = 10;
    [SerializeField, Range(0f, 1f)] private float _enemyPreferredCover = 0.2f;

    public void Spawn(
        RoomTypeProfile roomTypeProfile,
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        IReadOnlyCollection<Vector2Int> reservedFloorCells,
        IReadOnlyList<RoomDoorPlan> doorPlans,
        System.Random random
    )
    {
        ClearChildren(_objectsRoot);
        ClearChildren(_enemiesRoot);

        float[,] noiseValues = CreateFlatNoise(roomSizeInBlocks.x, roomSizeInBlocks.z);

        if (roomTypeProfile.NoiseProfile != null)
        {
            noiseValues = roomTypeProfile.NoiseProfile.GenerateNoiseMap(roomSizeInBlocks.x, roomSizeInBlocks.z);
        }

        HashSet<Vector2Int> reservedFloorCellSet = new HashSet<Vector2Int>(reservedFloorCells);
        HashSet<Vector2Int> obstacleFloorCellSet = CreateObstacleFloorCells(floorOccupancy.OccupiedFloorCells, reservedFloorCellSet);

        HashSet<Vector2Int> enemyForbiddenCellSet = CreateExpandedCellSet(roomSizeInBlocks, reservedFloorCellSet, _enemyReservedFloorAvoidanceInCells);

        Vector2Int entranceCell = GetReferenceEntranceCell(roomSizeInBlocks, doorPlans);

        int[,] distanceFromEntrance = ComputeDistanceFieldFromSingleStart(roomSizeInBlocks, entranceCell, obstacleFloorCellSet);
        int maximumEntranceDistance = GetMaximumDistance(roomSizeInBlocks, distanceFromEntrance);

        HashSet<Vector2Int> corridorStartCells = new HashSet<Vector2Int>(reservedFloorCellSet);

        if (corridorStartCells.Count == 0)
        {
            corridorStartCells.Add(entranceCell);
        }

        int[,] distanceFromCorridor = ComputeDistanceFieldFromMultipleStarts(roomSizeInBlocks, corridorStartCells, obstacleFloorCellSet);

        List<Vector2Int> freeCells = CreateFreeCells(roomSizeInBlocks, floorOccupancy);

        List<Vector2Int> resourceCenters = SpawnResources(
            roomTypeProfile,
            roomSizeInBlocks,
            floorOccupancy,
            freeCells,
            reservedFloorCellSet,
            obstacleFloorCellSet,
            noiseValues,
            distanceFromEntrance,
            maximumEntranceDistance,
            distanceFromCorridor,
            random
        );

        SpawnEnemies(
            roomTypeProfile,
            roomSizeInBlocks,
            floorOccupancy,
            freeCells,
            reservedFloorCellSet,
            obstacleFloorCellSet,
            enemyForbiddenCellSet,
            noiseValues,
            distanceFromEntrance,
            maximumEntranceDistance,
            distanceFromCorridor,
            resourceCenters,
            random
        );
    }

    public void Clear()
    {
        ClearChildren(_objectsRoot);
        ClearChildren(_enemiesRoot);
    }

    private Vector2Int GetReferenceEntranceCell(Vector3Int roomSizeInBlocks, IReadOnlyList<RoomDoorPlan> doorPlans)
    {
        if (doorPlans.Count == 0)
        {
            int centerX = Mathf.Clamp(roomSizeInBlocks.x / 2, 1, roomSizeInBlocks.x - 2);
            int centerZ = Mathf.Clamp(roomSizeInBlocks.z / 2, 1, roomSizeInBlocks.z - 2);
            return new Vector2Int(centerX, centerZ);
        }

        RoomDoorPlan entranceDoorPlan;

        if (TryGetDoorPlanByRole(doorPlans, DoorRole.Entrance, out entranceDoorPlan) == true)
        {
            return GetInsideCenterCell(entranceDoorPlan, roomSizeInBlocks);
        }

        RoomDoorPlan exitDoorPlan;

        if (TryGetDoorPlanByRole(doorPlans, DoorRole.Exit, out exitDoorPlan) == true)
        {
            return GetInsideCenterCell(exitDoorPlan, roomSizeInBlocks);
        }

        return GetInsideCenterCell(doorPlans[0], roomSizeInBlocks);
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

    private void ClearChildren(Transform rootTransform)
    {
        int childCount = rootTransform.childCount;

        for (int childIndex = childCount - 1; childIndex >= 0; childIndex--)
        {
            Transform childTransform = rootTransform.GetChild(childIndex);

            if (Application.isPlaying == false)
            {
                DestroyImmediate(childTransform.gameObject);
            }
            else
            {
                Destroy(childTransform.gameObject);
            }
        }
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

    private HashSet<Vector2Int> CreateObstacleFloorCells(HashSet<Vector2Int> occupiedFloorCells, HashSet<Vector2Int> reservedFloorCellSet)
    {
        HashSet<Vector2Int> obstacleFloorCellSet = new HashSet<Vector2Int>();

        foreach (Vector2Int occupiedCell in occupiedFloorCells)
        {
            if (reservedFloorCellSet.Contains(occupiedCell) == true)
            {
                continue;
            }

            obstacleFloorCellSet.Add(occupiedCell);
        }

        return obstacleFloorCellSet;
    }

    private HashSet<Vector2Int> CreateExpandedCellSet(Vector3Int roomSizeInBlocks, HashSet<Vector2Int> sourceCells, int radiusInCells)
    {
        HashSet<Vector2Int> expandedCells = new HashSet<Vector2Int>();

        if (radiusInCells <= 0)
        {
            foreach (Vector2Int sourceCell in sourceCells)
            {
                expandedCells.Add(sourceCell);
            }

            return expandedCells;
        }

        foreach (Vector2Int sourceCell in sourceCells)
        {
            for (int offsetX = -radiusInCells; offsetX <= radiusInCells; offsetX++)
            {
                for (int offsetZ = -radiusInCells; offsetZ <= radiusInCells; offsetZ++)
                {
                    int manhattan = Mathf.Abs(offsetX) + Mathf.Abs(offsetZ);

                    if (manhattan > radiusInCells)
                    {
                        continue;
                    }

                    Vector2Int cell = new Vector2Int(sourceCell.x + offsetX, sourceCell.y + offsetZ);

                    if (IsInteriorCell(cell, roomSizeInBlocks) == false)
                    {
                        continue;
                    }

                    expandedCells.Add(cell);
                }
            }
        }

        return expandedCells;
    }

    private bool IsTooCloseToRoomWalls(Vector2Int cell, Vector3Int roomSizeInBlocks, int wallAvoidanceInCells)
    {
        if (wallAvoidanceInCells <= 0)
        {
            return false;
        }

        int minimumX = 1 + wallAvoidanceInCells;
        int maximumX = (roomSizeInBlocks.x - 2) - wallAvoidanceInCells;

        int minimumZ = 1 + wallAvoidanceInCells;
        int maximumZ = (roomSizeInBlocks.z - 2) - wallAvoidanceInCells;

        if (cell.x < minimumX)
        {
            return true;
        }

        if (cell.x > maximumX)
        {
            return true;
        }

        if (cell.y < minimumZ)
        {
            return true;
        }

        if (cell.y > maximumZ)
        {
            return true;
        }

        return false;
    }

    private int GetDistanceToNearestRoomWall(Vector2Int cell, Vector3Int roomSizeInBlocks)
    {
        int distanceToWest = cell.x - 1;
        int distanceToEast = (roomSizeInBlocks.x - 2) - cell.x;
        int distanceToSouth = cell.y - 1;
        int distanceToNorth = (roomSizeInBlocks.z - 2) - cell.y;

        int minimumDistance = distanceToWest;

        if (distanceToEast < minimumDistance)
        {
            minimumDistance = distanceToEast;
        }

        if (distanceToSouth < minimumDistance)
        {
            minimumDistance = distanceToSouth;
        }

        if (distanceToNorth < minimumDistance)
        {
            minimumDistance = distanceToNorth;
        }

        if (minimumDistance < 0)
        {
            minimumDistance = 0;
        }

        return minimumDistance;
    }

    private float ComputeWallDistanceScore(Vector2Int cell, Vector3Int roomSizeInBlocks, int preferredWallDistanceInCells)
    {
        if (preferredWallDistanceInCells <= 0)
        {
            return 1f;
        }

        int wallDistance = GetDistanceToNearestRoomWall(cell, roomSizeInBlocks);
        return Mathf.Clamp01((float)wallDistance / preferredWallDistanceInCells);
    }

    private float ComputeCoverPreferenceScore(float cover)
    {
        float preferredCover = Mathf.Clamp01(_enemyPreferredCover);
        return 1f - Mathf.Abs(cover - preferredCover);
    }

    private List<Vector2Int> CreateFreeCells(Vector3Int roomSizeInBlocks, RoomFloorOccupancy floorOccupancy)
    {
        List<Vector2Int> freeCells = new List<Vector2Int>();

        for (int cellX = 1; cellX <= roomSizeInBlocks.x - 2; cellX++)
        {
            for (int cellZ = 1; cellZ <= roomSizeInBlocks.z - 2; cellZ++)
            {
                Vector2Int cell = new Vector2Int(cellX, cellZ);

                if (floorOccupancy.IsFree(cell) == false)
                {
                    continue;
                }

                freeCells.Add(cell);
            }
        }

        return freeCells;
    }

    private int[,] ComputeDistanceFieldFromSingleStart(Vector3Int roomSizeInBlocks, Vector2Int startCell, HashSet<Vector2Int> obstacleCells)
    {
        int[,] distances = CreateDistanceArray(roomSizeInBlocks);
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        if (IsInteriorCell(startCell, roomSizeInBlocks) == true && obstacleCells.Contains(startCell) == false)
        {
            distances[startCell.x, startCell.y] = 0;
            queue.Enqueue(startCell);
        }

        FloodFillDistances(roomSizeInBlocks, obstacleCells, distances, queue);
        return distances;
    }

    private int[,] ComputeDistanceFieldFromMultipleStarts(Vector3Int roomSizeInBlocks, HashSet<Vector2Int> startCells, HashSet<Vector2Int> obstacleCells)
    {
        int[,] distances = CreateDistanceArray(roomSizeInBlocks);
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        foreach (Vector2Int startCell in startCells)
        {
            if (IsInteriorCell(startCell, roomSizeInBlocks) == false)
            {
                continue;
            }

            if (obstacleCells.Contains(startCell) == true)
            {
                continue;
            }

            if (distances[startCell.x, startCell.y] != -1)
            {
                continue;
            }

            distances[startCell.x, startCell.y] = 0;
            queue.Enqueue(startCell);
        }

        FloodFillDistances(roomSizeInBlocks, obstacleCells, distances, queue);
        return distances;
    }

    private int[,] CreateDistanceArray(Vector3Int roomSizeInBlocks)
    {
        int[,] distances = new int[roomSizeInBlocks.x, roomSizeInBlocks.z];

        for (int x = 0; x < roomSizeInBlocks.x; x++)
        {
            for (int z = 0; z < roomSizeInBlocks.z; z++)
            {
                distances[x, z] = -1;
            }
        }

        return distances;
    }

    private void FloodFillDistances(Vector3Int roomSizeInBlocks, HashSet<Vector2Int> obstacleCells, int[,] distances, Queue<Vector2Int> queue)
    {
        int[] offsetX = new int[4] { 1, -1, 0, 0 };
        int[] offsetZ = new int[4] { 0, 0, 1, -1 };

        while (queue.Count > 0)
        {
            Vector2Int currentCell = queue.Dequeue();
            int currentDistance = distances[currentCell.x, currentCell.y];

            for (int directionIndex = 0; directionIndex < 4; directionIndex++)
            {
                Vector2Int neighborCell = new Vector2Int(currentCell.x + offsetX[directionIndex], currentCell.y + offsetZ[directionIndex]);

                if (IsInteriorCell(neighborCell, roomSizeInBlocks) == false)
                {
                    continue;
                }

                if (obstacleCells.Contains(neighborCell) == true)
                {
                    continue;
                }

                if (distances[neighborCell.x, neighborCell.y] != -1)
                {
                    continue;
                }

                distances[neighborCell.x, neighborCell.y] = currentDistance + 1;
                queue.Enqueue(neighborCell);
            }
        }
    }

    private int GetMaximumDistance(Vector3Int roomSizeInBlocks, int[,] distances)
    {
        int maximumDistance = 0;

        for (int cellX = 1; cellX <= roomSizeInBlocks.x - 2; cellX++)
        {
            for (int cellZ = 1; cellZ <= roomSizeInBlocks.z - 2; cellZ++)
            {
                int distance = distances[cellX, cellZ];

                if (distance > maximumDistance)
                {
                    maximumDistance = distance;
                }
            }
        }

        return maximumDistance;
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

    private float ComputeCornerPenalty(Vector2Int cell, Vector3Int roomSizeInBlocks, int cornerAvoidanceInCells)
    {
        if (cornerAvoidanceInCells <= 0)
        {
            return 0f;
        }

        Vector2Int cornerA = new Vector2Int(1, 1);
        Vector2Int cornerB = new Vector2Int(1, roomSizeInBlocks.z - 2);
        Vector2Int cornerC = new Vector2Int(roomSizeInBlocks.x - 2, 1);
        Vector2Int cornerD = new Vector2Int(roomSizeInBlocks.x - 2, roomSizeInBlocks.z - 2);

        int distanceA = Mathf.Abs(cell.x - cornerA.x) + Mathf.Abs(cell.y - cornerA.y);
        int distanceB = Mathf.Abs(cell.x - cornerB.x) + Mathf.Abs(cell.y - cornerB.y);
        int distanceC = Mathf.Abs(cell.x - cornerC.x) + Mathf.Abs(cell.y - cornerC.y);
        int distanceD = Mathf.Abs(cell.x - cornerD.x) + Mathf.Abs(cell.y - cornerD.y);

        int minimum = distanceA;

        if (distanceB < minimum)
        {
            minimum = distanceB;
        }

        if (distanceC < minimum)
        {
            minimum = distanceC;
        }

        if (distanceD < minimum)
        {
            minimum = distanceD;
        }

        if (minimum >= cornerAvoidanceInCells)
        {
            return 0f;
        }

        float normalized = 1f - Mathf.Clamp01((float)minimum / Mathf.Max(1, cornerAvoidanceInCells));
        return normalized;
    }

    private int CountObstacleNeighbors(Vector2Int cell, HashSet<Vector2Int> obstacleCells)
    {
        int count = 0;

        Vector2Int right = new Vector2Int(cell.x + 1, cell.y);
        Vector2Int left = new Vector2Int(cell.x - 1, cell.y);
        Vector2Int up = new Vector2Int(cell.x, cell.y + 1);
        Vector2Int down = new Vector2Int(cell.x, cell.y - 1);

        if (obstacleCells.Contains(right) == true)
        {
            count++;
        }

        if (obstacleCells.Contains(left) == true)
        {
            count++;
        }

        if (obstacleCells.Contains(up) == true)
        {
            count++;
        }

        if (obstacleCells.Contains(down) == true)
        {
            count++;
        }

        return count;
    }

    private Vector3 GetLocalPosition(Vector2Int floorCell)
    {
        float localPositionX = (floorCell.x + 0.5f) * _blockSize;
        float localPositionZ = (floorCell.y + 0.5f) * _blockSize;
        float localPositionY = 1f * _blockSize;

        return new Vector3(localPositionX, localPositionY, localPositionZ);
    }

    private List<Vector2Int> SpawnResources(
        RoomTypeProfile roomTypeProfile,
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        List<Vector2Int> freeCells,
        HashSet<Vector2Int> reservedFloorCellSet,
        HashSet<Vector2Int> obstacleFloorCellSet,
        float[,] noiseValues,
        int[,] distanceFromEntrance,
        int maximumEntranceDistance,
        int[,] distanceFromCorridor,
        System.Random random
    )
    {
        List<Vector2Int> resourceCenters = new List<Vector2Int>();

        if (roomTypeProfile.ObjectPrefabs.Count == 0)
        {
            return resourceCenters;
        }

        int objectSpawnCount = random.Next(roomTypeProfile.ObjectSpawnCountRange.x, roomTypeProfile.ObjectSpawnCountRange.y + 1);

        if (objectSpawnCount <= 0)
        {
            return resourceCenters;
        }

        int clusterCount = Mathf.Clamp((objectSpawnCount + 2) / 3, 1, 4);

        float progressWindow = 0.25f;
        int desiredSpacingInCells = 6;

        for (int clusterIndex = 0; clusterIndex < clusterCount; clusterIndex++)
        {
            float targetProgress = (clusterIndex + 1f) / (clusterCount + 1f);

            Vector2Int bestCell = Vector2Int.zero;
            float bestScore = -1f;
            bool found = false;

            for (int index = 0; index < freeCells.Count; index++)
            {
                Vector2Int cell = freeCells[index];

                if (floorOccupancy.IsFree(cell) == false)
                {
                    continue;
                }

                int entranceDistance = distanceFromEntrance[cell.x, cell.y];

                if (entranceDistance < _minimumDistanceFromEntranceInCells)
                {
                    continue;
                }

                if (maximumEntranceDistance > 0)
                {
                    float progress = (float)entranceDistance / (float)maximumEntranceDistance;
                    float progressDifference = Mathf.Abs(progress - targetProgress);
                    float progressScore = 1f - Mathf.Clamp01(progressDifference / progressWindow);

                    if (progressScore <= 0f)
                    {
                        continue;
                    }
                }

                int corridorDistance = distanceFromCorridor[cell.x, cell.y];

                if (corridorDistance != -1)
                {
                    if (corridorDistance < _minimumResourceDistanceFromCorridorInCells)
                    {
                        continue;
                    }

                    if (corridorDistance > _maximumResourceDistanceFromCorridorInCells)
                    {
                        continue;
                    }
                }

                float noiseValue = noiseValues[cell.x, cell.y];
                float cover = (float)CountObstacleNeighbors(cell, obstacleFloorCellSet) / 4f;
                float cornerPenalty = ComputeCornerPenalty(cell, roomSizeInBlocks, _cornerAvoidanceInCells);

                float corridorBand = 0.5f;

                if (corridorDistance != -1)
                {
                    float clamped = Mathf.Clamp01((float)(corridorDistance - _minimumResourceDistanceFromCorridorInCells) / Mathf.Max(1, _maximumResourceDistanceFromCorridorInCells - _minimumResourceDistanceFromCorridorInCells));
                    corridorBand = clamped;
                }

                float baseScore = (noiseValue * 0.55f) + (cover * 0.30f) + (corridorBand * 0.20f) - (cornerPenalty * 0.35f);

                float progressScoreFinal = 0.5f;

                if (maximumEntranceDistance > 0)
                {
                    float progress = (float)entranceDistance / (float)maximumEntranceDistance;
                    float progressDifference = Mathf.Abs(progress - targetProgress);
                    progressScoreFinal = 1f - Mathf.Clamp01(progressDifference / progressWindow);
                }

                float spreadScore = 1f;

                if (resourceCenters.Count > 0)
                {
                    int minimumDistanceToCenters = GetMinimumManhattanDistance(resourceCenters, cell);
                    spreadScore = Mathf.Clamp01((float)minimumDistanceToCenters / desiredSpacingInCells);
                }

                float finalScore = (baseScore * 0.60f) + (progressScoreFinal * 0.25f) + (spreadScore * 0.15f);
                finalScore += (float)random.NextDouble() * 0.01f;

                if (found == false || finalScore > bestScore)
                {
                    found = true;
                    bestScore = finalScore;
                    bestCell = cell;
                }
            }

            if (found == true)
            {
                resourceCenters.Add(bestCell);
            }
        }

        int basePerCluster = objectSpawnCount / Mathf.Max(1, resourceCenters.Count);
        int remainder = objectSpawnCount - (basePerCluster * Mathf.Max(1, resourceCenters.Count));

        for (int clusterIndex = 0; clusterIndex < resourceCenters.Count; clusterIndex++)
        {
            int clusterSpawnCount = basePerCluster;

            if (clusterIndex < remainder)
            {
                clusterSpawnCount++;
            }

            SpawnResourcesAroundCenter(
                roomTypeProfile,
                roomSizeInBlocks,
                floorOccupancy,
                obstacleFloorCellSet,
                noiseValues,
                distanceFromCorridor,
                resourceCenters[clusterIndex],
                clusterSpawnCount,
                random
            );
        }

        return resourceCenters;
    }

    private void SpawnResourcesAroundCenter(
        RoomTypeProfile roomTypeProfile,
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        HashSet<Vector2Int> obstacleFloorCellSet,
        float[,] noiseValues,
        int[,] distanceFromCorridor,
        Vector2Int centerCell,
        int spawnCount,
        System.Random random
    )
    {
        for (int spawnIndex = 0; spawnIndex < spawnCount; spawnIndex++)
        {
            Vector2Int chosenCell;

            bool found = TryPickResourceCellNearCenter(
                roomSizeInBlocks,
                floorOccupancy,
                obstacleFloorCellSet,
                noiseValues,
                distanceFromCorridor,
                centerCell,
                _resourceClusterRadiusInCells,
                out chosenCell,
                random
            );

            if (found == false)
            {
                break;
            }

            GameObject prefab = WeightedPrefabPicker.PickPrefab(roomTypeProfile.ObjectPrefabs, random);
            GameObject instance = Instantiate(prefab, _objectsRoot);

            instance.transform.localPosition = GetLocalPosition(chosenCell);
            instance.transform.localRotation = Quaternion.identity;

            floorOccupancy.OccupiedFloorCells.Add(chosenCell);
        }
    }

    private bool TryPickResourceCellNearCenter(
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        HashSet<Vector2Int> obstacleFloorCellSet,
        float[,] noiseValues,
        int[,] distanceFromCorridor,
        Vector2Int centerCell,
        int radiusInCells,
        out Vector2Int chosenCell,
        System.Random random
    )
    {
        bool hasCandidate = false;
        float bestScore = -1f;
        Vector2Int bestCell = Vector2Int.zero;

        for (int offsetX = -radiusInCells; offsetX <= radiusInCells; offsetX++)
        {
            for (int offsetZ = -radiusInCells; offsetZ <= radiusInCells; offsetZ++)
            {
                Vector2Int candidateCell = new Vector2Int(centerCell.x + offsetX, centerCell.y + offsetZ);

                if (IsInteriorCell(candidateCell, roomSizeInBlocks) == false)
                {
                    continue;
                }

                if (floorOccupancy.IsFree(candidateCell) == false)
                {
                    continue;
                }

                int corridorDistance = distanceFromCorridor[candidateCell.x, candidateCell.y];

                if (corridorDistance != -1 && corridorDistance < _minimumResourceDistanceFromCorridorInCells)
                {
                    continue;
                }

                float noiseValue = noiseValues[candidateCell.x, candidateCell.y];
                float cover = (float)CountObstacleNeighbors(candidateCell, obstacleFloorCellSet) / 4f;

                float score = (noiseValue * 0.70f) + (cover * 0.30f);
                score += (float)random.NextDouble() * 0.01f;

                if (hasCandidate == false || score > bestScore)
                {
                    hasCandidate = true;
                    bestScore = score;
                    bestCell = candidateCell;
                }
            }
        }

        chosenCell = bestCell;
        return hasCandidate;
    }

    private void SpawnEnemies(
        RoomTypeProfile roomTypeProfile,
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        List<Vector2Int> freeCells,
        HashSet<Vector2Int> reservedFloorCellSet,
        HashSet<Vector2Int> obstacleFloorCellSet,
        HashSet<Vector2Int> enemyForbiddenCellSet,
        float[,] noiseValues,
        int[,] distanceFromEntrance,
        int maximumEntranceDistance,
        int[,] distanceFromCorridor,
        List<Vector2Int> resourceCenters,
        System.Random random
    )
    {
        if (roomTypeProfile.EnemyPrefabs.Count == 0)
        {
            return;
        }

        int enemySpawnCount = random.Next(roomTypeProfile.EnemySpawnCountRange.x, roomTypeProfile.EnemySpawnCountRange.y + 1);

        if (enemySpawnCount <= 0)
        {
            return;
        }

        HashSet<Vector2Int> enemyCells = new HashSet<Vector2Int>();

        int guardCount = enemySpawnCount;
        int maximumGuardCount = resourceCenters.Count * 2;

        if (guardCount > maximumGuardCount)
        {
            guardCount = maximumGuardCount;
        }

        int remainingCount = enemySpawnCount - guardCount;

        for (int centerIndex = 0; centerIndex < resourceCenters.Count; centerIndex++)
        {
            if (guardCount <= 0)
            {
                break;
            }

            int guardsForCenter = 2;

            if (guardsForCenter > guardCount)
            {
                guardsForCenter = guardCount;
            }

            SpawnGuardsNearResource(
                roomTypeProfile,
                roomSizeInBlocks,
                floorOccupancy,
                obstacleFloorCellSet,
                enemyForbiddenCellSet,
                noiseValues,
                distanceFromCorridor,
                resourceCenters[centerIndex],
                guardsForCenter,
                enemyCells,
                random
            );

            guardCount -= guardsForCenter;
        }

        if (remainingCount <= 0)
        {
            return;
        }

        float progressWindow = 0.30f;
        int desiredSpacingInCells = 4;

        for (int enemyIndex = 0; enemyIndex < remainingCount; enemyIndex++)
        {
            float targetProgress = (enemyIndex + 1f) / (remainingCount + 1f);

            Vector2Int bestCell = Vector2Int.zero;
            float bestScore = -1f;
            bool found = false;

            for (int index = 0; index < freeCells.Count; index++)
            {
                Vector2Int cell = freeCells[index];

                if (floorOccupancy.IsFree(cell) == false)
                {
                    continue;
                }

                if (enemyForbiddenCellSet.Contains(cell) == true)
                {
                    continue;
                }

                if (IsTooCloseToRoomWalls(cell, roomSizeInBlocks, _enemyWallAvoidanceInCells) == true)
                {
                    continue;
                }

                int entranceDistance = distanceFromEntrance[cell.x, cell.y];

                if (entranceDistance < _minimumDistanceFromEntranceInCells)
                {
                    continue;
                }

                int corridorDistance = distanceFromCorridor[cell.x, cell.y];

                if (corridorDistance != -1)
                {
                    if (corridorDistance < _enemyEncounterMinCorridorDistanceInCells)
                    {
                        continue;
                    }

                    if (corridorDistance > _enemyEncounterMaxCorridorDistanceInCells)
                    {
                        continue;
                    }
                }

                if (IsTooCloseToExisting(cell, enemyCells, _minimumEnemySpacingInCells) == true)
                {
                    continue;
                }

                float cover = (float)CountObstacleNeighbors(cell, obstacleFloorCellSet) / 4f;
                float coverScore = ComputeCoverPreferenceScore(cover);
                float wallDistanceScore = ComputeWallDistanceScore(cell, roomSizeInBlocks, _enemyPreferredWallDistanceInCells);

                float progressScoreFinal = 0.5f;

                if (maximumEntranceDistance > 0)
                {
                    float progress = (float)entranceDistance / (float)maximumEntranceDistance;
                    float progressDifference = Mathf.Abs(progress - targetProgress);
                    progressScoreFinal = 1f - Mathf.Clamp01(progressDifference / progressWindow);
                }

                float spreadScore = 1f;

                if (enemyCells.Count > 0)
                {
                    int nearestDistance = GetMinimumManhattanDistance(enemyCells, cell);
                    spreadScore = Mathf.Clamp01((float)nearestDistance / desiredSpacingInCells);
                }

                float noiseValue = noiseValues[cell.x, cell.y];

                float score = (coverScore * 0.30f) + (wallDistanceScore * 0.30f) + (progressScoreFinal * 0.20f) + (noiseValue * 0.10f) + (spreadScore * 0.10f);
                score += (float)random.NextDouble() * 0.01f;

                if (found == false || score > bestScore)
                {
                    found = true;
                    bestScore = score;
                    bestCell = cell;
                }
            }

            if (found == false)
            {
                break;
            }

            GameObject prefab = WeightedPrefabPicker.PickPrefab(roomTypeProfile.EnemyPrefabs, random);
            GameObject instance = Instantiate(prefab, _enemiesRoot);

            instance.transform.localPosition = GetLocalPosition(bestCell);
            instance.transform.localRotation = Quaternion.identity;

            floorOccupancy.OccupiedFloorCells.Add(bestCell);
            enemyCells.Add(bestCell);
        }
    }

    private void SpawnGuardsNearResource(
        RoomTypeProfile roomTypeProfile,
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        HashSet<Vector2Int> obstacleFloorCellSet,
        HashSet<Vector2Int> enemyForbiddenCellSet,
        float[,] noiseValues,
        int[,] distanceFromCorridor,
        Vector2Int centerCell,
        int spawnCount,
        HashSet<Vector2Int> enemyCells,
        System.Random random
    )
    {
        for (int spawnIndex = 0; spawnIndex < spawnCount; spawnIndex++)
        {
            Vector2Int chosenCell;

            bool found = TryPickGuardCellNearCenter(
                roomSizeInBlocks,
                floorOccupancy,
                obstacleFloorCellSet,
                enemyForbiddenCellSet,
                noiseValues,
                distanceFromCorridor,
                centerCell,
                _enemyGuardRadiusInCells,
                enemyCells,
                out chosenCell,
                random
            );

            if (found == false)
            {
                break;
            }

            GameObject prefab = WeightedPrefabPicker.PickPrefab(roomTypeProfile.EnemyPrefabs, random);
            GameObject instance = Instantiate(prefab, _enemiesRoot);

            instance.transform.localPosition = GetLocalPosition(chosenCell);
            instance.transform.localRotation = Quaternion.identity;

            floorOccupancy.OccupiedFloorCells.Add(chosenCell);
            enemyCells.Add(chosenCell);
        }
    }

    private bool TryPickGuardCellNearCenter(
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        HashSet<Vector2Int> obstacleFloorCellSet,
        HashSet<Vector2Int> enemyForbiddenCellSet,
        float[,] noiseValues,
        int[,] distanceFromCorridor,
        Vector2Int centerCell,
        int radiusInCells,
        HashSet<Vector2Int> enemyCells,
        out Vector2Int chosenCell,
        System.Random random
    )
    {
        bool hasCandidate = false;
        float bestScore = -1f;
        Vector2Int bestCell = Vector2Int.zero;

        for (int offsetX = -radiusInCells; offsetX <= radiusInCells; offsetX++)
        {
            for (int offsetZ = -radiusInCells; offsetZ <= radiusInCells; offsetZ++)
            {
                Vector2Int candidateCell = new Vector2Int(centerCell.x + offsetX, centerCell.y + offsetZ);

                if (IsInteriorCell(candidateCell, roomSizeInBlocks) == false)
                {
                    continue;
                }

                if (floorOccupancy.IsFree(candidateCell) == false)
                {
                    continue;
                }

                if (enemyForbiddenCellSet.Contains(candidateCell) == true)
                {
                    continue;
                }

                if (IsTooCloseToRoomWalls(candidateCell, roomSizeInBlocks, _enemyWallAvoidanceInCells) == true)
                {
                    continue;
                }

                if (IsTooCloseToExisting(candidateCell, enemyCells, _minimumEnemySpacingInCells) == true)
                {
                    continue;
                }

                int corridorDistance = distanceFromCorridor[candidateCell.x, candidateCell.y];

                if (corridorDistance != -1 && corridorDistance < 1)
                {
                    continue;
                }

                float cover = (float)CountObstacleNeighbors(candidateCell, obstacleFloorCellSet) / 4f;
                float coverScore = ComputeCoverPreferenceScore(cover);
                float wallDistanceScore = ComputeWallDistanceScore(candidateCell, roomSizeInBlocks, _enemyPreferredWallDistanceInCells);
                int distanceToCenter = Mathf.Abs(offsetX) + Mathf.Abs(offsetZ);
                float centerScore = 1f - Mathf.Clamp01((float)distanceToCenter / Mathf.Max(1, radiusInCells));

                float noiseValue = noiseValues[candidateCell.x, candidateCell.y];
                float score = (coverScore * 0.35f) + (wallDistanceScore * 0.35f) + (centerScore * 0.20f) + (noiseValue * 0.10f);
                score += (float)random.NextDouble() * 0.01f;

                if (hasCandidate == false || score > bestScore)
                {
                    hasCandidate = true;
                    bestScore = score;
                    bestCell = candidateCell;
                }
            }
        }

        chosenCell = bestCell;
        return hasCandidate;
    }

    private bool IsTooCloseToExisting(Vector2Int cell, HashSet<Vector2Int> existingCells, int minimumDistanceInCells)
    {
        if (minimumDistanceInCells <= 0)
        {
            return false;
        }

        foreach (Vector2Int existingCell in existingCells)
        {
            int distance = Mathf.Abs(existingCell.x - cell.x) + Mathf.Abs(existingCell.y - cell.y);

            if (distance < minimumDistanceInCells)
            {
                return true;
            }
        }

        return false;
    }

    private int GetMinimumManhattanDistance(List<Vector2Int> cells, Vector2Int cell)
    {
        int best = int.MaxValue;

        for (int index = 0; index < cells.Count; index++)
        {
            int distance = Mathf.Abs(cells[index].x - cell.x) + Mathf.Abs(cells[index].y - cell.y);

            if (distance < best)
            {
                best = distance;
            }
        }

        return best;
    }

    private int GetMinimumManhattanDistance(HashSet<Vector2Int> cells, Vector2Int cell)
    {
        int best = int.MaxValue;

        foreach (Vector2Int otherCell in cells)
        {
            int distance = Mathf.Abs(otherCell.x - cell.x) + Mathf.Abs(otherCell.y - cell.y);

            if (distance < best)
            {
                best = distance;
            }
        }

        return best;
    }
}
