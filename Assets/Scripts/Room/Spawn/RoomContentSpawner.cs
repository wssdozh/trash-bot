using System.Collections.Generic;
using Unity.AI.Navigation;
using JunkyardBoss;
using UnityEngine;

public sealed class RoomContentSpawner : MonoBehaviour
{
    private const int CombatBaseMin = 5;
    private const int CombatBaseMax = 7;
    private const int CombatAddMin = 3;
    private const int CombatAddMax = 5;
    private const int CombatWeaponTankCount = 2;
    private const int WeaponTankFootprintRadiusInCells = 1;
    private const int GroundPatrolCount = 8;
    private const int GroundPatrolMin = 2;
    private const int GroundPatrolGap = 2;
    private const int NotWalkableArea = 1;
    private const int SpawnFixTryCount = 8;
    private const int SpawnSearchDirections = 8;
    private const int SpawnCheckBufferSize = 32;
    private const float SpawnFixSkin = 0.02f;
    private const float SpawnMaxShift = 1.35f;
    private const float SpawnSearchStep = 0.35f;
    private const float SpawnCheckPadding = 0.25f;
    private const float GroundPatrolRadius = 0.58f;
    private const float FloorTopY = 1f;
    private const float ZeroThreshold = 0.0001f;
    private static readonly int[] s_neighborOffsetX = new int[4] { 1, -1, 0, 0 };
    private static readonly int[] s_neighborOffsetZ = new int[4] { 0, 0, 1, -1 };

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

    private readonly Collider[] _spawnCheckBuffer = new Collider[SpawnCheckBufferSize];
    private readonly HashSet<Vector2Int> _reservedFloorCellSetBuffer = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> _resourceObstacleFloorCellSetBuffer = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> _enemyObstacleFloorCellSetBuffer = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> _enemyForbiddenCellSetBuffer = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> _corridorStartCellsBuffer = new HashSet<Vector2Int>();
    private readonly List<Vector2Int> _freeCellsBuffer = new List<Vector2Int>();
    private readonly List<Vector2Int> _groundPatrolCellsBuffer = new List<Vector2Int>();
    private readonly List<Vector3> _groundPatrolPointsBuffer = new List<Vector3>();
    private readonly List<Vector2Int> _resourceCentersBuffer = new List<Vector2Int>();
    private readonly List<GameObject> _weaponTankPlanBuffer = new List<GameObject>();
    private readonly HashSet<Vector2Int> _enemyCellsBuffer = new HashSet<Vector2Int>();
    private readonly List<EnemySpawnConfig> _spawnedEnemiesBuffer = new List<EnemySpawnConfig>();
    private readonly Queue<Vector2Int> _distanceFillQueue = new Queue<Vector2Int>();
    private readonly HashSet<Vector2Int> _localAreaVisitedCells = new HashSet<Vector2Int>();
    private readonly Queue<Vector2Int> _localAreaCellsQueue = new Queue<Vector2Int>();
    private readonly Queue<int> _localAreaDistanceQueue = new Queue<int>();

    [SerializeField] private Transform _objectsRoot;
    [SerializeField] private Transform _enemiesRoot;
    [SerializeField] private float _blockSize = 1f;
    [SerializeField] private float _objectSpawnHeight = 1f;
    [SerializeField] private float _enemySpawnHeight = 1.75f;

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
    [SerializeField, Min(1)] private int _enemyOpenAreaRadiusInCells = 3;
    [SerializeField, Min(0)] private int _enemyMinOpenAreaInCells = 9;
    [SerializeField, Min(0)] private int _enemyPreferredOpenAreaInCells = 16;
    [SerializeField, Min(1)] private int _enemyMinSpanInCells = 3;

    private RoomRuntimeState _roomRuntimeState;

    public void Spawn(
        RoomTypeProfile roomTypeProfile,
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        IReadOnlyCollection<Vector2Int> reservedFloorCells,
        IReadOnlyList<RoomDoorPlan> doorPlans,
        int combatRoomIndex,
        System.Random random
    )
    {
        _roomRuntimeState = GetComponentInParent<RoomRuntimeState>();
        EnsureEnemyNavMeshIgnore();
        ClearOldBlocks();

        ClearChildren(_objectsRoot);
        ClearChildren(_enemiesRoot);

        float[,] noiseValues = CreateFlatNoise(roomSizeInBlocks.x, roomSizeInBlocks.z);

        if (roomTypeProfile.NoiseProfile != null)
        {
            noiseValues = roomTypeProfile.NoiseProfile.GenerateNoiseMap(roomSizeInBlocks.x, roomSizeInBlocks.z);
        }

        HashSet<Vector2Int> reservedFloorCellSet = _reservedFloorCellSetBuffer;
        FillReservedFloorCells(reservedFloorCellSet, reservedFloorCells);
        HashSet<Vector2Int> resourceObstacleFloorCellSet = _resourceObstacleFloorCellSetBuffer;
        FillObstacleFloorCells(resourceObstacleFloorCellSet, floorOccupancy.OccupiedFloorCells, reservedFloorCellSet);

        HashSet<Vector2Int> enemyForbiddenCellSet = _enemyForbiddenCellSetBuffer;
        FillExpandedCellSet(enemyForbiddenCellSet, roomSizeInBlocks, reservedFloorCellSet, _enemyReservedFloorAvoidanceInCells);

        Vector2Int entranceCell = GetReferenceEntranceCell(roomSizeInBlocks, doorPlans);

        int[,] distanceFromEntrance = ComputeDistanceFieldFromSingleStart(roomSizeInBlocks, entranceCell, resourceObstacleFloorCellSet);
        int maximumEntranceDistance = GetMaximumDistance(roomSizeInBlocks, distanceFromEntrance);

        HashSet<Vector2Int> corridorStartCells = _corridorStartCellsBuffer;
        FillCopySet(corridorStartCells, reservedFloorCellSet);

        if (corridorStartCells.Count == 0)
        {
            corridorStartCells.Add(entranceCell);
        }

        int[,] distanceFromCorridor = ComputeDistanceFieldFromMultipleStarts(roomSizeInBlocks, corridorStartCells, resourceObstacleFloorCellSet);

        List<Vector2Int> freeCells = _freeCellsBuffer;
        FillFreeCells(freeCells, roomSizeInBlocks, floorOccupancy);

        List<Vector2Int> resourceCenters = SpawnResources(
            roomTypeProfile,
            roomSizeInBlocks,
            floorOccupancy,
            freeCells,
            reservedFloorCellSet,
            resourceObstacleFloorCellSet,
            noiseValues,
            distanceFromEntrance,
            maximumEntranceDistance,
            distanceFromCorridor,
            combatRoomIndex,
            random
        );

        HashSet<Vector2Int> enemyObstacleFloorCellSet = _enemyObstacleFloorCellSetBuffer;
        FillObstacleFloorCells(enemyObstacleFloorCellSet, floorOccupancy.OccupiedFloorCells, reservedFloorCellSet);
        int[,] enemyDistanceFromEntrance = ComputeDistanceFieldFromSingleStart(roomSizeInBlocks, entranceCell, enemyObstacleFloorCellSet);
        int enemyMaximumEntranceDistance = GetMaximumDistance(roomSizeInBlocks, enemyDistanceFromEntrance);
        int[,] enemyDistanceFromCorridor = ComputeDistanceFieldFromMultipleStarts(roomSizeInBlocks, corridorStartCells, enemyObstacleFloorCellSet);
        BuildGroundPatrol(roomSizeInBlocks, floorOccupancy, enemyObstacleFloorCellSet, enemyDistanceFromCorridor, resourceCenters);

        SpawnEnemies(
            roomTypeProfile,
            roomSizeInBlocks,
            floorOccupancy,
            freeCells,
            reservedFloorCellSet,
            enemyObstacleFloorCellSet,
            enemyForbiddenCellSet,
            noiseValues,
            enemyDistanceFromEntrance,
            enemyMaximumEntranceDistance,
            enemyDistanceFromCorridor,
            resourceCenters,
            combatRoomIndex,
            random
        );

        EnsureCombatLock();
    }

    public void SetBlockSize(float blockSize)
    {
        if (blockSize <= 0.0001f)
        {
            _blockSize = 0.0001f;

            return;
        }

        _blockSize = blockSize;
    }

    private int GetEnemySpawnCount(RoomTypeProfile roomTypeProfile, int combatRoomIndex, System.Random random)
    {
        if (roomTypeProfile.RoomType == RoomType.Combat)
        {
            int combatStep = combatRoomIndex - 1;

            if (combatStep < 0)
            {
                combatStep = 0;
            }

            int min = CombatBaseMin + (combatStep * CombatAddMin);
            int max = CombatBaseMax + (combatStep * CombatAddMax);

            return random.Next(min, max + 1);
        }

        return random.Next(roomTypeProfile.EnemySpawnCountRange.x, roomTypeProfile.EnemySpawnCountRange.y + 1);
    }

    public void Clear()
    {
        ClearOldBlocks();
        ClearChildren(_objectsRoot);
        ClearChildren(_enemiesRoot);
    }

    private void EnsureEnemyNavMeshIgnore()
    {
        NavMeshModifier navMeshModifier = _enemiesRoot.GetComponent<NavMeshModifier>();

        if (navMeshModifier == null)
        {
            navMeshModifier = _enemiesRoot.gameObject.AddComponent<NavMeshModifier>();
        }

        navMeshModifier.ignoreFromBuild = true;
        navMeshModifier.applyToChildren = true;
    }

    private void ClearOldBlocks()
    {
        Transform oldBlocks = transform.Find("__NavMeshBlocks");

        if (oldBlocks == null)
        {
            return;
        }

        if (Application.isPlaying == false)
        {
            DestroyImmediate(oldBlocks.gameObject);

            return;
        }

        Destroy(oldBlocks.gameObject);
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

    private void FillReservedFloorCells(HashSet<Vector2Int> targetCells, IReadOnlyCollection<Vector2Int> sourceCells)
    {
        targetCells.Clear();

        foreach (Vector2Int sourceCell in sourceCells)
        {
            targetCells.Add(sourceCell);
        }
    }

    private void FillCopySet(HashSet<Vector2Int> targetCells, HashSet<Vector2Int> sourceCells)
    {
        targetCells.Clear();

        foreach (Vector2Int sourceCell in sourceCells)
        {
            targetCells.Add(sourceCell);
        }
    }

    private void FillObstacleFloorCells(HashSet<Vector2Int> obstacleFloorCellSet, HashSet<Vector2Int> occupiedFloorCells, HashSet<Vector2Int> reservedFloorCellSet)
    {
        obstacleFloorCellSet.Clear();

        foreach (Vector2Int occupiedCell in occupiedFloorCells)
        {
            if (reservedFloorCellSet.Contains(occupiedCell) == true)
            {
                continue;
            }

            obstacleFloorCellSet.Add(occupiedCell);
        }
    }

    private void FillExpandedCellSet(HashSet<Vector2Int> expandedCells, Vector3Int roomSizeInBlocks, HashSet<Vector2Int> sourceCells, int radiusInCells)
    {
        expandedCells.Clear();

        if (radiusInCells <= 0)
        {
            foreach (Vector2Int sourceCell in sourceCells)
            {
                expandedCells.Add(sourceCell);
            }

            return;
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

    private Vector2Int GetRoomCenterCell(Vector3Int roomSizeInBlocks)
    {
        int centerX = Mathf.Clamp(roomSizeInBlocks.x / 2, 1, roomSizeInBlocks.x - 2);
        int centerZ = Mathf.Clamp(roomSizeInBlocks.z / 2, 1, roomSizeInBlocks.z - 2);

        return new Vector2Int(centerX, centerZ);
    }

    private float ComputeRoomCenterScore(Vector2Int cell, Vector2Int roomCenterCell, Vector3Int roomSizeInBlocks)
    {
        int distanceToCenter = GetCellDistance(roomCenterCell, cell);
        int maximumDistance = GetMaximumCenterDistance(roomCenterCell, roomSizeInBlocks);

        if (maximumDistance <= 0)
        {
            return 1f;
        }

        return 1f - Mathf.Clamp01((float)distanceToCenter / maximumDistance);
    }

    private int GetMaximumCenterDistance(Vector2Int roomCenterCell, Vector3Int roomSizeInBlocks)
    {
        Vector2Int cornerA = new Vector2Int(1, 1);
        Vector2Int cornerB = new Vector2Int(1, roomSizeInBlocks.z - 2);
        Vector2Int cornerC = new Vector2Int(roomSizeInBlocks.x - 2, 1);
        Vector2Int cornerD = new Vector2Int(roomSizeInBlocks.x - 2, roomSizeInBlocks.z - 2);

        int distanceA = GetCellDistance(roomCenterCell, cornerA);
        int distanceB = GetCellDistance(roomCenterCell, cornerB);
        int distanceC = GetCellDistance(roomCenterCell, cornerC);
        int distanceD = GetCellDistance(roomCenterCell, cornerD);
        int maximumDistance = distanceA;

        if (distanceB > maximumDistance)
        {
            maximumDistance = distanceB;
        }

        if (distanceC > maximumDistance)
        {
            maximumDistance = distanceC;
        }

        if (distanceD > maximumDistance)
        {
            maximumDistance = distanceD;
        }

        return maximumDistance;
    }

    private int GetCellDistance(Vector2Int firstCell, Vector2Int secondCell)
    {
        return Mathf.Abs(firstCell.x - secondCell.x) + Mathf.Abs(firstCell.y - secondCell.y);
    }

    private float ComputeCoverPreferenceScore(float cover)
    {
        float preferredCover = Mathf.Clamp01(_enemyPreferredCover);
        return 1f - Mathf.Abs(cover - preferredCover);
    }

    private void FillFreeCells(List<Vector2Int> freeCells, Vector3Int roomSizeInBlocks, RoomFloorOccupancy floorOccupancy)
    {
        freeCells.Clear();

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
    }

    private int[,] ComputeDistanceFieldFromSingleStart(Vector3Int roomSizeInBlocks, Vector2Int startCell, HashSet<Vector2Int> obstacleCells)
    {
        int[,] distances = CreateDistanceArray(roomSizeInBlocks);
        Queue<Vector2Int> queue = _distanceFillQueue;
        queue.Clear();

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
        Queue<Vector2Int> queue = _distanceFillQueue;
        queue.Clear();

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
        while (queue.Count > 0)
        {
            Vector2Int currentCell = queue.Dequeue();
            int currentDistance = distances[currentCell.x, currentCell.y];

            for (int directionIndex = 0; directionIndex < 4; directionIndex++)
            {
                Vector2Int neighborCell = new Vector2Int(currentCell.x + s_neighborOffsetX[directionIndex], currentCell.y + s_neighborOffsetZ[directionIndex]);

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

    private bool TryGetEnemyArenaScore(
        Vector2Int cell,
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        bool relaxedRules,
        out float arenaScore
    )
    {
        arenaScore = 0f;

        if (floorOccupancy.IsFree(cell) == false)
        {
            return false;
        }

        int minimumOpenArea = _enemyMinOpenAreaInCells;
        int minimumSpan = _enemyMinSpanInCells;

        if (relaxedRules == true)
        {
            minimumOpenArea = Mathf.Max(4, Mathf.CeilToInt(minimumOpenArea * 0.6f));
            minimumSpan = Mathf.Max(2, minimumSpan - 1);
        }

        int preferredOpenArea = Mathf.Max(minimumOpenArea, _enemyPreferredOpenAreaInCells);
        int openArea = CountLocalFreeArea(cell, roomSizeInBlocks, floorOccupancy, _enemyOpenAreaRadiusInCells, preferredOpenArea);

        if (openArea < minimumOpenArea)
        {
            return false;
        }

        int openSpan = GetOpenSpan(cell, roomSizeInBlocks, floorOccupancy);

        if (openSpan < minimumSpan)
        {
            return false;
        }

        int preferredSpan = Mathf.Max(minimumSpan, minimumSpan + 2);
        float openAreaScore = Mathf.Clamp01((float)openArea / preferredOpenArea);
        float spanScore = Mathf.Clamp01((float)openSpan / preferredSpan);

        arenaScore = (openAreaScore * 0.65f) + (spanScore * 0.35f);

        return true;
    }

    private int CountLocalFreeArea(
        Vector2Int startCell,
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        int radiusInCells,
        int stopCount
    )
    {
        if (radiusInCells <= 0)
        {
            return 1;
        }

        if (stopCount < 1)
        {
            stopCount = 1;
        }

        HashSet<Vector2Int> visitedCells = _localAreaVisitedCells;
        Queue<Vector2Int> cellsToVisit = _localAreaCellsQueue;
        Queue<int> distanceQueue = _localAreaDistanceQueue;
        visitedCells.Clear();
        cellsToVisit.Clear();
        distanceQueue.Clear();

        visitedCells.Add(startCell);
        cellsToVisit.Enqueue(startCell);
        distanceQueue.Enqueue(0);

        int openArea = 0;
        while (cellsToVisit.Count > 0)
        {
            Vector2Int currentCell = cellsToVisit.Dequeue();
            int currentDistance = distanceQueue.Dequeue();

            openArea++;

            if (openArea >= stopCount)
            {
                return openArea;
            }

            if (currentDistance >= radiusInCells)
            {
                continue;
            }

            for (int directionIndex = 0; directionIndex < 4; directionIndex++)
            {
                Vector2Int neighborCell = new Vector2Int(currentCell.x + s_neighborOffsetX[directionIndex], currentCell.y + s_neighborOffsetZ[directionIndex]);

                if (IsInteriorCell(neighborCell, roomSizeInBlocks) == false)
                {
                    continue;
                }

                if (floorOccupancy.IsFree(neighborCell) == false)
                {
                    continue;
                }

                if (visitedCells.Add(neighborCell) == false)
                {
                    continue;
                }

                cellsToVisit.Enqueue(neighborCell);
                distanceQueue.Enqueue(currentDistance + 1);
            }
        }

        return openArea;
    }

    private int GetOpenSpan(Vector2Int cell, Vector3Int roomSizeInBlocks, RoomFloorOccupancy floorOccupancy)
    {
        int horizontalSpan = 1;
        int verticalSpan = 1;

        horizontalSpan += CountFreeCellsInDirection(cell, 1, 0, roomSizeInBlocks, floorOccupancy);
        horizontalSpan += CountFreeCellsInDirection(cell, -1, 0, roomSizeInBlocks, floorOccupancy);

        verticalSpan += CountFreeCellsInDirection(cell, 0, 1, roomSizeInBlocks, floorOccupancy);
        verticalSpan += CountFreeCellsInDirection(cell, 0, -1, roomSizeInBlocks, floorOccupancy);

        if (horizontalSpan < verticalSpan)
        {
            return horizontalSpan;
        }

        return verticalSpan;
    }

    private int CountFreeCellsInDirection(
        Vector2Int cell,
        int offsetX,
        int offsetZ,
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy
    )
    {
        int count = 0;
        Vector2Int currentCell = new Vector2Int(cell.x + offsetX, cell.y + offsetZ);

        while (IsInteriorCell(currentCell, roomSizeInBlocks) == true)
        {
            if (floorOccupancy.IsFree(currentCell) == false)
            {
                break;
            }

            count++;
            currentCell = new Vector2Int(currentCell.x + offsetX, currentCell.y + offsetZ);
        }

        return count;
    }

    private Vector3 GetLocalPosition(Vector2Int floorCell, float spawnHeight)
    {
        float localPositionX = (floorCell.x + 0.5f) * _blockSize;
        float localPositionZ = (floorCell.y + 0.5f) * _blockSize;
        float localPositionY = spawnHeight * _blockSize;

        return new Vector3(localPositionX, localPositionY, localPositionZ);
    }

    private void InstantiateObjectOnCell(GameObject prefab, Vector2Int floorCell)
    {
        GameObject instance = InstantiateOnCell(prefab, _objectsRoot, floorCell, _objectSpawnHeight);

        if (instance == null)
        {
            return;
        }

        AddNavModifier(instance);
    }

    private bool TryInstantiateEnemyOnCell(RoomTypeProfile roomTypeProfile, EnemySpawnConfig enemySpawn, Vector2Int floorCell)
    {
        if (enemySpawn == null)
        {
            return false;
        }

        GameObject prefab = enemySpawn.Prefab;
        float spawnHeight = roomTypeProfile.GetEnemySpawnHeight(enemySpawn, _enemySpawnHeight);
        GameObject instance = InstantiateOnCell(prefab, _enemiesRoot, floorCell, spawnHeight);

        if (instance == null)
        {
            return false;
        }

        SetupBossEnemy(instance);
        SetupEnemy(instance, enemySpawn);

        if (TrySnapGroundEnemy(instance, spawnHeight) == false)
        {
            DestroySpawned(instance);

            return false;
        }

        Vector3 spawnPoint = instance.transform.position;

        if (TryFixEnemySpawn(instance, spawnPoint) == false)
        {
            DestroySpawned(instance);

            return false;
        }

        BindEnemyRoom(instance);

        if (TryFixEnemySpawn(instance, instance.transform.position) == false)
        {
            DestroySpawned(instance);

            return false;
        }

        return true;
    }

    private void SetupEnemy(GameObject enemyObject, EnemySpawnConfig enemySpawn)
    {
        if (enemyObject == null)
        {
            return;
        }

        if (enemySpawn == null)
        {
            return;
        }

        EnemyAnimation enemyAnimation = enemyObject.GetComponentInChildren<EnemyAnimation>(true);

        if (enemyAnimation == null)
        {
            return;
        }

        enemyAnimation.SetWeapon(enemySpawn.WeaponPrefab);
        EnemyMeleeBrain enemyMeleeBrain = enemyObject.GetComponentInChildren<EnemyMeleeBrain>(true);

        if (enemyMeleeBrain == null)
        {
            return;
        }

        enemyMeleeBrain.ApplyRole();
    }

    private void SetupBossEnemy(GameObject enemyObject)
    {
        if (enemyObject == null)
        {
            return;
        }

        if (_roomRuntimeState == null)
        {
            return;
        }

        BossExcavator bossExcavator = enemyObject.GetComponent<BossExcavator>();

        if (bossExcavator == null)
        {
            return;
        }

        BossExcavatorMove bossExcavatorMove = bossExcavator.Move;

        if (bossExcavatorMove == null)
        {
            return;
        }

        Bounds roomBounds = _roomRuntimeState.GetRoomBounds();
        bossExcavatorMove.SetArenaCenter(roomBounds.center);
    }

    private void BindEnemyRoom(GameObject enemyObject)
    {
        if (enemyObject == null)
        {
            return;
        }

        if (_roomRuntimeState == null)
        {
            return;
        }

        EnsureRoomAlert();

        EnemyRoomLock enemyRoomLock = enemyObject.GetComponent<EnemyRoomLock>();

        if (enemyRoomLock == null)
        {
            enemyRoomLock = enemyObject.AddComponent<EnemyRoomLock>();
        }

        enemyRoomLock.Setup(_roomRuntimeState);
        enemyRoomLock.SnapInside();
    }

    private void EnsureRoomAlert()
    {
        EnemyRoomAlert enemyRoomAlert = _roomRuntimeState.GetComponent<EnemyRoomAlert>();

        if (enemyRoomAlert != null)
        {
            return;
        }

        _roomRuntimeState.gameObject.AddComponent<EnemyRoomAlert>();
    }

    private void EnsureCombatLock()
    {
        if (_roomRuntimeState == null)
        {
            return;
        }

        RoomCombatLock roomCombatLock = _roomRuntimeState.GetComponent<RoomCombatLock>();

        if (roomCombatLock == null)
        {
            roomCombatLock = _roomRuntimeState.gameObject.AddComponent<RoomCombatLock>();
        }

        roomCombatLock.Setup(_roomRuntimeState, _blockSize);
    }

    private void AddNavModifier(GameObject targetObject)
    {
        NavMeshModifier navMeshModifier = targetObject.GetComponent<NavMeshModifier>();

        if (navMeshModifier == null)
        {
            navMeshModifier = targetObject.AddComponent<NavMeshModifier>();
        }

        navMeshModifier.overrideArea = true;
        navMeshModifier.area = NotWalkableArea;
        navMeshModifier.ignoreFromBuild = false;
        navMeshModifier.applyToChildren = true;
    }

    private GameObject InstantiateOnCell(GameObject prefab, Transform rootTransform, Vector2Int floorCell, float spawnHeight)
    {
        if (prefab == null)
        {
            return null;
        }

        Vector3 worldPosition = GetWorldPosition(rootTransform, floorCell, spawnHeight);
        Quaternion worldRotation = rootTransform.rotation;

        GameObject instance = Instantiate(prefab, worldPosition, worldRotation);
        instance.transform.SetParent(rootTransform, true);

        return instance;
    }

    private bool TrySnapGroundEnemy(GameObject enemyObject, float spawnHeight)
    {
        if (enemyObject == null)
        {
            return false;
        }

        if (IsAirEnemy(enemyObject) == true)
        {
            return true;
        }

        if (IsTurretEnemy(enemyObject) == true)
        {
            SetEnemyHeight(enemyObject.transform, spawnHeight);

            return true;
        }

        Collider[] bodyColliders = enemyObject.GetComponentsInChildren<Collider>();

        if (bodyColliders.Length == 0)
        {
            return true;
        }

        float bottomOffset = GetBottomOffset(enemyObject.transform, bodyColliders);
        Vector3 enemyPosition = enemyObject.transform.position;
        enemyPosition.y = GetFloorY(_enemiesRoot) + bottomOffset;
        enemyObject.transform.position = enemyPosition;

        return true;
    }

    private bool IsAirEnemy(GameObject enemyObject)
    {
        EnemyDroneMove enemyDroneMove = enemyObject.GetComponentInChildren<EnemyDroneMove>(true);

        if (enemyDroneMove != null)
        {
            return true;
        }

        return false;
    }

    private bool IsTurretEnemy(GameObject enemyObject)
    {
        Turret turret = enemyObject.GetComponentInChildren<Turret>(true);

        if (turret != null)
        {
            return true;
        }

        return false;
    }

    private bool TryFixEnemySpawn(GameObject enemyObject, Vector3 spawnPoint)
    {
        if (enemyObject == null)
        {
            return false;
        }

        Collider[] bodyColliders = enemyObject.GetComponentsInChildren<Collider>();

        if (bodyColliders.Length == 0)
        {
            return true;
        }

        int tryIndex = 0;

        while (tryIndex < SpawnFixTryCount)
        {
            Vector3 pushDirection = GetSpawnPush(enemyObject.transform, bodyColliders);

            if (pushDirection.sqrMagnitude <= ZeroThreshold)
            {
                return IsSpawnShiftValid(spawnPoint, enemyObject.transform.position);
            }

            enemyObject.transform.position += pushDirection;
            tryIndex += 1;
        }

        Vector3 fallbackPoint;

        if (TryFindSpawnPoint(enemyObject.transform, bodyColliders, spawnPoint, out fallbackPoint))
        {
            enemyObject.transform.position = fallbackPoint;

            return IsSpawnShiftValid(spawnPoint, fallbackPoint);
        }

        return false;
    }

    private Vector3 GetSpawnPush(Transform enemyTransform, Collider[] bodyColliders)
    {
        float searchRadius = GetSpawnRadius(bodyColliders) + SpawnCheckPadding;
        int hitCount = Physics.OverlapSphereNonAlloc(
            enemyTransform.position,
            searchRadius,
            _spawnCheckBuffer,
            ~0,
            QueryTriggerInteraction.Ignore);

        if (hitCount == 0)
        {
            return Vector3.zero;
        }

        Vector3 pushDirection = Vector3.zero;
        int bodyIndex = 0;

        while (bodyIndex < bodyColliders.Length)
        {
            Collider bodyCollider = bodyColliders[bodyIndex];

            if (CanUseSpawnCollider(bodyCollider))
            {
                int hitIndex = 0;

                while (hitIndex < hitCount)
                {
                    Collider hitCollider = _spawnCheckBuffer[hitIndex];

                    if (CanUseHitCollider(enemyTransform, hitCollider))
                    {
                        Vector3 overlapDirection;
                        float overlapDistance;
                        bool hasOverlap = Physics.ComputePenetration(
                            bodyCollider,
                            bodyCollider.transform.position,
                            bodyCollider.transform.rotation,
                            hitCollider,
                            hitCollider.transform.position,
                            hitCollider.transform.rotation,
                            out overlapDirection,
                            out overlapDistance);

                        if (hasOverlap)
                        {
                            if (overlapDistance > 0f)
                            {
                                overlapDirection.y = 0f;

                                if (overlapDirection.sqrMagnitude > ZeroThreshold)
                                {
                                    overlapDirection.Normalize();
                                    pushDirection += overlapDirection * (overlapDistance + SpawnFixSkin);
                                }
                            }
                        }
                    }

                    hitIndex += 1;
                }
            }

            bodyIndex += 1;
        }

        return GetFlatDirection(pushDirection);
    }

    private bool TryFindSpawnPoint(Transform enemyTransform, Collider[] bodyColliders, Vector3 startPoint, out Vector3 fallbackPoint)
    {
        int ringIndex = 1;

        while (ringIndex <= SpawnFixTryCount)
        {
            float radius = SpawnSearchStep * ringIndex;
            int directionIndex = 0;

            while (directionIndex < SpawnSearchDirections)
            {
                Vector3 candidateDirection = GetSpawnSearchDirection(directionIndex);
                Vector3 candidatePoint = startPoint + (candidateDirection * radius);
                enemyTransform.position = candidatePoint;

                if (GetSpawnPush(enemyTransform, bodyColliders).sqrMagnitude <= ZeroThreshold)
                {
                    enemyTransform.position = startPoint;
                    fallbackPoint = candidatePoint;

                    return true;
                }

                directionIndex += 1;
            }

            ringIndex += 1;
        }

        enemyTransform.position = startPoint;
        fallbackPoint = startPoint;

        return false;
    }

    private bool IsSpawnShiftValid(Vector3 spawnPoint, Vector3 currentPoint)
    {
        Vector3 flatSpawnPoint = spawnPoint;
        flatSpawnPoint.y = 0f;

        Vector3 flatCurrentPoint = currentPoint;
        flatCurrentPoint.y = 0f;

        float maximumShift = _blockSize * SpawnMaxShift;

        if (Vector3.Distance(flatSpawnPoint, flatCurrentPoint) > maximumShift)
        {
            return false;
        }

        return true;
    }

    private void DestroySpawned(GameObject spawnedObject)
    {
        if (spawnedObject == null)
        {
            return;
        }

        if (Application.isPlaying == false)
        {
            DestroyImmediate(spawnedObject);

            return;
        }

        Destroy(spawnedObject);
    }

    private float GetSpawnRadius(Collider[] bodyColliders)
    {
        float radius = 0.5f;
        int colliderIndex = 0;

        while (colliderIndex < bodyColliders.Length)
        {
            Collider bodyCollider = bodyColliders[colliderIndex];

            if (CanUseSpawnCollider(bodyCollider))
            {
                Bounds bounds = bodyCollider.bounds;
                float colliderRadius = Mathf.Max(bounds.extents.x, bounds.extents.z);

                if (colliderRadius > radius)
                {
                    radius = colliderRadius;
                }
            }

            colliderIndex += 1;
        }

        return radius;
    }

    private bool CanUseSpawnCollider(Collider bodyCollider)
    {
        if (bodyCollider == null)
        {
            return false;
        }

        if (bodyCollider.enabled == false)
        {
            return false;
        }

        if (bodyCollider.isTrigger)
        {
            return false;
        }

        return true;
    }

    private bool CanUseHitCollider(Transform enemyTransform, Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return false;
        }

        if (hitCollider.enabled == false)
        {
            return false;
        }

        if (hitCollider.isTrigger)
        {
            return false;
        }

        if (hitCollider.transform.IsChildOf(enemyTransform))
        {
            return false;
        }

        return true;
    }

    private float GetBottomOffset(Transform enemyTransform, Collider[] bodyColliders)
    {
        float lowestY = enemyTransform.position.y;
        bool hasCollider = false;
        int colliderIndex = 0;

        while (colliderIndex < bodyColliders.Length)
        {
            Collider bodyCollider = bodyColliders[colliderIndex];

            if (CanUseSpawnCollider(bodyCollider))
            {
                float colliderBottomY = bodyCollider.bounds.min.y;

                if (hasCollider == false || colliderBottomY < lowestY)
                {
                    lowestY = colliderBottomY;
                    hasCollider = true;
                }
            }

            colliderIndex += 1;
        }

        if (hasCollider == false)
        {
            return 0f;
        }

        return enemyTransform.position.y - lowestY;
    }

    private Vector3 GetSpawnSearchDirection(int directionIndex)
    {
        if (directionIndex == 0)
        {
            return Vector3.forward;
        }

        if (directionIndex == 1)
        {
            return Vector3.back;
        }

        if (directionIndex == 2)
        {
            return Vector3.left;
        }

        if (directionIndex == 3)
        {
            return Vector3.right;
        }

        if (directionIndex == 4)
        {
            return new Vector3(1f, 0f, 1f).normalized;
        }

        if (directionIndex == 5)
        {
            return new Vector3(1f, 0f, -1f).normalized;
        }

        if (directionIndex == 6)
        {
            return new Vector3(-1f, 0f, 1f).normalized;
        }

        return new Vector3(-1f, 0f, -1f).normalized;
    }

    private Vector3 GetFlatDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= ZeroThreshold)
        {
            return Vector3.zero;
        }

        direction.Normalize();

        return direction;
    }

    private Vector3 GetWorldPosition(Transform rootTransform, Vector2Int floorCell, float spawnHeight)
    {
        Vector3 localPosition = GetLocalPosition(floorCell, spawnHeight);

        return rootTransform.TransformPoint(localPosition);
    }

    private float GetFloorY(Transform rootTransform)
    {
        Vector3 floorPoint = rootTransform.TransformPoint(new Vector3(0f, FloorTopY * _blockSize, 0f));

        return floorPoint.y;
    }

    private void SetEnemyHeight(Transform enemyTransform, float spawnHeight)
    {
        Vector3 enemyPosition = enemyTransform.position;
        enemyPosition.y = GetSpawnY(_enemiesRoot, spawnHeight);
        enemyTransform.position = enemyPosition;
    }

    private float GetSpawnY(Transform rootTransform, float spawnHeight)
    {
        Vector3 localPosition = new Vector3(0f, spawnHeight * _blockSize, 0f);
        Vector3 worldPosition = rootTransform.TransformPoint(localPosition);

        return worldPosition.y;
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
        int combatRoomIndex,
        System.Random random
    )
    {
        List<Vector2Int> resourceCenters = _resourceCentersBuffer;
        resourceCenters.Clear();
        List<GameObject> weaponTankPlan = _weaponTankPlanBuffer;
        FillWeaponTankPlan(roomTypeProfile, combatRoomIndex, weaponTankPlan, random);

        if (roomTypeProfile.ObjectPrefabs.Count == 0 && weaponTankPlan.Count == 0)
        {
            return resourceCenters;
        }

        int objectSpawnCount = random.Next(roomTypeProfile.ObjectSpawnCountRange.x, roomTypeProfile.ObjectSpawnCountRange.y + 1);

        if (weaponTankPlan.Count > 0)
        {
            objectSpawnCount += weaponTankPlan.Count;
        }

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

            Vector2Int bestCell;

            bool found = TryPickResourceCenterCell(
                roomSizeInBlocks,
                floorOccupancy,
                freeCells,
                obstacleFloorCellSet,
                noiseValues,
                distanceFromEntrance,
                maximumEntranceDistance,
                distanceFromCorridor,
                resourceCenters,
                targetProgress,
                progressWindow,
                desiredSpacingInCells,
                false,
                out bestCell,
                random
            );

            if (found == false)
            {
                found = TryPickResourceCenterCell(
                    roomSizeInBlocks,
                    floorOccupancy,
                    freeCells,
                    obstacleFloorCellSet,
                    noiseValues,
                    distanceFromEntrance,
                    maximumEntranceDistance,
                    distanceFromCorridor,
                    resourceCenters,
                    targetProgress,
                    progressWindow,
                    desiredSpacingInCells,
                    true,
                    out bestCell,
                    random
                );
            }

            if (found == true)
            {
                resourceCenters.Add(bestCell);
            }
        }

        int basePerCluster = objectSpawnCount / Mathf.Max(1, resourceCenters.Count);
        int remainder = objectSpawnCount - (basePerCluster * Mathf.Max(1, resourceCenters.Count));
        int weaponTankPlanIndex = 0;

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
                weaponTankPlan,
                ref weaponTankPlanIndex,
                random
            );
        }

        return resourceCenters;
    }

    private bool TryPickResourceCenterCell(
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        List<Vector2Int> freeCells,
        HashSet<Vector2Int> obstacleFloorCellSet,
        float[,] noiseValues,
        int[,] distanceFromEntrance,
        int maximumEntranceDistance,
        int[,] distanceFromCorridor,
        List<Vector2Int> resourceCenters,
        float targetProgress,
        float progressWindow,
        int desiredSpacingInCells,
        bool relaxedRules,
        out Vector2Int bestCell,
        System.Random random
    )
    {
        bestCell = Vector2Int.zero;
        float bestScore = -1f;
        bool found = false;

        int minimumEntranceDistance = _minimumDistanceFromEntranceInCells;

        if (relaxedRules == true)
        {
            minimumEntranceDistance = 0;
        }

        for (int index = 0; index < freeCells.Count; index++)
        {
            Vector2Int cell = freeCells[index];

            if (floorOccupancy.IsFree(cell) == false)
            {
                continue;
            }

            int entranceDistance = distanceFromEntrance[cell.x, cell.y];

            if (entranceDistance < minimumEntranceDistance)
            {
                continue;
            }

            if (maximumEntranceDistance > 0)
            {
                float progress = (float)entranceDistance / (float)maximumEntranceDistance;
                float progressDifference = Mathf.Abs(progress - targetProgress);
                float progressScore = 1f - Mathf.Clamp01(progressDifference / progressWindow);

                if (relaxedRules == false && progressScore <= 0f)
                {
                    continue;
                }
            }

            int corridorDistance = distanceFromCorridor[cell.x, cell.y];

            if (corridorDistance == -1)
            {
                continue;
            }

            if (relaxedRules == false)
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
            else
            {
                if (corridorDistance < 1)
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
                if (relaxedRules == false)
                {
                    float clamped = Mathf.Clamp01((float)(corridorDistance - _minimumResourceDistanceFromCorridorInCells) / Mathf.Max(1, _maximumResourceDistanceFromCorridorInCells - _minimumResourceDistanceFromCorridorInCells));
                    corridorBand = clamped;
                }
                else
                {
                    corridorBand = Mathf.Clamp01((float)corridorDistance / Mathf.Max(1, _maximumResourceDistanceFromCorridorInCells));
                }
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

        return found;
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
        List<GameObject> weaponTankPlan,
        ref int weaponTankPlanIndex,
        System.Random random
    )
    {
        for (int spawnIndex = 0; spawnIndex < spawnCount; spawnIndex++)
        {
            Vector2Int chosenCell;
            bool isWeaponTankSpawn = HasWeaponTankToSpawn(weaponTankPlan, weaponTankPlanIndex);
            int footprintRadiusInCells = isWeaponTankSpawn ? WeaponTankFootprintRadiusInCells : 0;

            bool found = TryPickResourceCellNearCenter(
                roomSizeInBlocks,
                floorOccupancy,
                obstacleFloorCellSet,
                noiseValues,
                distanceFromCorridor,
                centerCell,
                _resourceClusterRadiusInCells,
                footprintRadiusInCells,
                out chosenCell,
                random
            );

            if (found == false)
            {
                found = TryPickAnyResourceCell(
                    roomSizeInBlocks,
                    floorOccupancy,
                    distanceFromCorridor,
                    footprintRadiusInCells,
                    out chosenCell);

                if (found == false)
                {
                    break;
                }
            }

            GameObject prefab = PickResourcePrefab(
                roomTypeProfile,
                weaponTankPlan,
                ref weaponTankPlanIndex,
                random);
            InstantiateObjectOnCell(prefab, chosenCell);

            MarkResourceFootprintOccupied(
                floorOccupancy,
                chosenCell,
                footprintRadiusInCells,
                roomSizeInBlocks);
        }
    }

    private void FillWeaponTankPlan(RoomTypeProfile roomTypeProfile, int combatRoomIndex, List<GameObject> weaponTankPlan, System.Random random)
    {
        weaponTankPlan.Clear();

        IReadOnlyList<GameObject> weaponTankPrefabs = roomTypeProfile.WeaponTankPrefabs;

        if (weaponTankPrefabs.Count == 0)
        {
            return;
        }

        if (roomTypeProfile.RoomType == RoomType.Start)
        {
            AddWeaponTankIfValid(weaponTankPlan, weaponTankPrefabs[0]);

            return;
        }

        if (roomTypeProfile.RoomType != RoomType.Combat)
        {
            return;
        }

        int newWeaponIndex = combatRoomIndex;

        if (newWeaponIndex > 0 && newWeaponIndex < weaponTankPrefabs.Count)
        {
            AddWeaponTankIfValid(weaponTankPlan, weaponTankPrefabs[newWeaponIndex]);
        }

        int repeatMaxIndex = newWeaponIndex - 1;

        if (repeatMaxIndex < 0)
        {
            repeatMaxIndex = 0;
        }

        if (repeatMaxIndex >= weaponTankPrefabs.Count)
        {
            repeatMaxIndex = weaponTankPrefabs.Count - 1;
        }

        while (weaponTankPlan.Count < CombatWeaponTankCount)
        {
            GameObject repeatPrefab = PickRepeatWeaponTank(weaponTankPrefabs, repeatMaxIndex, random);

            if (repeatPrefab == null)
            {
                return;
            }

            weaponTankPlan.Add(repeatPrefab);
        }
    }

    private void AddWeaponTankIfValid(List<GameObject> weaponTankPlan, GameObject prefab)
    {
        if (prefab == null)
        {
            return;
        }

        weaponTankPlan.Add(prefab);
    }

    private GameObject PickRepeatWeaponTank(IReadOnlyList<GameObject> weaponTankPrefabs, int maxIndex, System.Random random)
    {
        if (weaponTankPrefabs.Count == 0)
        {
            return null;
        }

        if (maxIndex < 0)
        {
            return null;
        }

        int attemptCount = maxIndex + 1;

        for (int attemptIndex = 0; attemptIndex < attemptCount; attemptIndex++)
        {
            int index = random.Next(0, maxIndex + 1);
            GameObject prefab = weaponTankPrefabs[index];

            if (prefab != null)
            {
                return prefab;
            }
        }

        for (int index = 0; index <= maxIndex; index++)
        {
            GameObject prefab = weaponTankPrefabs[index];

            if (prefab != null)
            {
                return prefab;
            }
        }

        return null;
    }

    private GameObject PickResourcePrefab(
        RoomTypeProfile roomTypeProfile,
        List<GameObject> weaponTankPlan,
        ref int weaponTankPlanIndex,
        System.Random random)
    {
        if (weaponTankPlanIndex < weaponTankPlan.Count)
        {
            GameObject prefab = weaponTankPlan[weaponTankPlanIndex];
            weaponTankPlanIndex += 1;

            if (prefab != null)
            {
                return prefab;
            }
        }

        return WeightedPrefabPicker.PickPrefab(roomTypeProfile.ObjectPrefabs, random);
    }

    private bool HasWeaponTankToSpawn(List<GameObject> weaponTankPlan, int weaponTankPlanIndex)
    {
        return weaponTankPlanIndex < weaponTankPlan.Count;
    }

    private bool TryPickResourceCellNearCenter(
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        HashSet<Vector2Int> obstacleFloorCellSet,
        float[,] noiseValues,
        int[,] distanceFromCorridor,
        Vector2Int centerCell,
        int radiusInCells,
        int footprintRadiusInCells,
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

                if (HasResourceFootprintClearance(
                    candidateCell,
                    roomSizeInBlocks,
                    floorOccupancy,
                    footprintRadiusInCells) == false)
                {
                    continue;
                }

                int corridorDistance = distanceFromCorridor[candidateCell.x, candidateCell.y];

                if (corridorDistance == -1)
                {
                    continue;
                }

                if (corridorDistance < _minimumResourceDistanceFromCorridorInCells)
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

    private bool TryPickAnyResourceCell(
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        int[,] distanceFromCorridor,
        int footprintRadiusInCells,
        out Vector2Int chosenCell)
    {
        for (int cellX = 1; cellX <= roomSizeInBlocks.x - 2; cellX++)
        {
            for (int cellZ = 1; cellZ <= roomSizeInBlocks.z - 2; cellZ++)
            {
                Vector2Int cell = new Vector2Int(cellX, cellZ);

                if (floorOccupancy.IsFree(cell) == false)
                {
                    continue;
                }

                if (HasResourceFootprintClearance(
                    cell,
                    roomSizeInBlocks,
                    floorOccupancy,
                    footprintRadiusInCells) == false)
                {
                    continue;
                }

                if (distanceFromCorridor[cell.x, cell.y] == -1)
                {
                    continue;
                }

                chosenCell = cell;
                return true;
            }
        }

        chosenCell = Vector2Int.zero;
        return false;
    }

    private bool HasResourceFootprintClearance(
        Vector2Int centerCell,
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        int footprintRadiusInCells)
    {
        for (int offsetX = -footprintRadiusInCells; offsetX <= footprintRadiusInCells; offsetX++)
        {
            for (int offsetZ = -footprintRadiusInCells; offsetZ <= footprintRadiusInCells; offsetZ++)
            {
                Vector2Int cell = new Vector2Int(centerCell.x + offsetX, centerCell.y + offsetZ);

                if (IsInteriorCell(cell, roomSizeInBlocks) == false)
                {
                    return false;
                }

                if (floorOccupancy.IsFree(cell) == false)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void MarkResourceFootprintOccupied(
        RoomFloorOccupancy floorOccupancy,
        Vector2Int centerCell,
        int footprintRadiusInCells,
        Vector3Int roomSizeInBlocks)
    {
        for (int offsetX = -footprintRadiusInCells; offsetX <= footprintRadiusInCells; offsetX++)
        {
            for (int offsetZ = -footprintRadiusInCells; offsetZ <= footprintRadiusInCells; offsetZ++)
            {
                Vector2Int cell = new Vector2Int(centerCell.x + offsetX, centerCell.y + offsetZ);

                if (IsInteriorCell(cell, roomSizeInBlocks) == false)
                {
                    continue;
                }

                floorOccupancy.OccupiedFloorCells.Add(cell);
            }
        }
    }

    private void BuildGroundPatrol(
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        HashSet<Vector2Int> obstacleFloorCellSet,
        int[,] distanceFromCorridor,
        List<Vector2Int> resourceCenters
    )
    {
        if (_roomRuntimeState == null)
        {
            return;
        }

        List<Vector2Int> patrolCells = _groundPatrolCellsBuffer;
        patrolCells.Clear();
        Vector2Int roomCenterCell = GetRoomCenterCell(roomSizeInBlocks);
        int maximumCenterDistance = GetMaximumCenterDistance(roomCenterCell, roomSizeInBlocks);
        int preferredDistance = Mathf.Max(2, Mathf.RoundToInt(maximumCenterDistance * GroundPatrolRadius));
        int wallAvoidanceInCells = Mathf.Max(1, _enemyWallAvoidanceInCells / 2);

        while (patrolCells.Count < GroundPatrolCount)
        {
            Vector2Int patrolCell;

            if (TryPickGroundPatrolCell(
                roomSizeInBlocks,
                floorOccupancy,
                obstacleFloorCellSet,
                distanceFromCorridor,
                resourceCenters,
                roomCenterCell,
                preferredDistance,
                wallAvoidanceInCells,
                patrolCells,
                out patrolCell
            ) == false)
            {
                break;
            }

            patrolCells.Add(patrolCell);
        }

        if (patrolCells.Count < GroundPatrolMin)
        {
            _roomRuntimeState.ClearGroundPatrolPoints();

            return;
        }

        SortGroundPatrolCells(patrolCells, roomCenterCell);

        List<Vector3> patrolPoints = _groundPatrolPointsBuffer;
        patrolPoints.Clear();

        for (int pointIndex = 0; pointIndex < patrolCells.Count; pointIndex++)
        {
            Vector3 patrolPoint = GetWorldPosition(_enemiesRoot, patrolCells[pointIndex], 0f);
            patrolPoints.Add(patrolPoint);
        }

        _roomRuntimeState.SetGroundPatrolPoints(patrolPoints);
    }

    private bool TryPickGroundPatrolCell(
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        HashSet<Vector2Int> obstacleFloorCellSet,
        int[,] distanceFromCorridor,
        List<Vector2Int> resourceCenters,
        Vector2Int roomCenterCell,
        int preferredDistance,
        int wallAvoidanceInCells,
        List<Vector2Int> patrolCells,
        out Vector2Int patrolCell
    )
    {
        bool found = false;
        float bestScore = -1f;
        Vector2Int bestCell = Vector2Int.zero;

        for (int cellX = 1; cellX <= roomSizeInBlocks.x - 2; cellX++)
        {
            for (int cellZ = 1; cellZ <= roomSizeInBlocks.z - 2; cellZ++)
            {
                Vector2Int cell = new Vector2Int(cellX, cellZ);

                if (floorOccupancy.IsFree(cell) == false)
                {
                    continue;
                }

                int corridorDistance = distanceFromCorridor[cell.x, cell.y];

                if (corridorDistance <= 0)
                {
                    continue;
                }

                if (IsTooCloseToRoomWalls(cell, roomSizeInBlocks, wallAvoidanceInCells))
                {
                    continue;
                }

                if (patrolCells.Count > 0)
                {
                    int nearestDistance = GetMinimumManhattanDistance(patrolCells, cell);

                    if (nearestDistance < GroundPatrolGap)
                    {
                        continue;
                    }
                }

                float arenaScore;

                if (TryGetEnemyArenaScore(cell, roomSizeInBlocks, floorOccupancy, true, out arenaScore) == false)
                {
                    continue;
                }

                float score = ComputeGroundPatrolScore(
                    cell,
                    roomSizeInBlocks,
                    obstacleFloorCellSet,
                    resourceCenters,
                    roomCenterCell,
                    preferredDistance,
                    patrolCells,
                    arenaScore
                );

                if (found == false || score > bestScore)
                {
                    found = true;
                    bestScore = score;
                    bestCell = cell;
                }
            }
        }

        patrolCell = bestCell;

        return found;
    }

    private float ComputeGroundPatrolScore(
        Vector2Int cell,
        Vector3Int roomSizeInBlocks,
        HashSet<Vector2Int> obstacleFloorCellSet,
        List<Vector2Int> resourceCenters,
        Vector2Int roomCenterCell,
        int preferredDistance,
        List<Vector2Int> patrolCells,
        float arenaScore
    )
    {
        int centerDistance = GetCellDistance(roomCenterCell, cell);
        float ringScore = 1f - Mathf.Clamp01(Mathf.Abs((float)centerDistance - preferredDistance) / Mathf.Max(1, preferredDistance));
        float wallDistanceScore = ComputeWallDistanceScore(cell, roomSizeInBlocks, _enemyPreferredWallDistanceInCells);
        float cornerScore = 1f - ComputeCornerPenalty(cell, roomSizeInBlocks, _cornerAvoidanceInCells);
        float cover = (float)CountObstacleNeighbors(cell, obstacleFloorCellSet) / 4f;
        float coverScore = ComputeCoverPreferenceScore(cover);
        float resourceScore = ComputeResourcePatrolScore(cell, resourceCenters);
        float spreadScore = 1f;

        if (patrolCells.Count > 0)
        {
            int nearestDistance = GetMinimumManhattanDistance(patrolCells, cell);
            spreadScore = Mathf.Clamp01((float)nearestDistance / Mathf.Max(1, GroundPatrolGap * 3));
        }

        return (arenaScore * 0.34f) + (ringScore * 0.22f) + (spreadScore * 0.18f) + (wallDistanceScore * 0.12f) + (cornerScore * 0.08f) + (resourceScore * 0.04f) + (coverScore * 0.02f);
    }

    private float ComputeResourcePatrolScore(Vector2Int cell, List<Vector2Int> resourceCenters)
    {
        if (resourceCenters.Count == 0)
        {
            return 0.5f;
        }

        int nearestDistance = GetMinimumManhattanDistance(resourceCenters, cell);
        int preferredDistance = Mathf.Max(1, _enemyGuardRadiusInCells);

        return 1f - Mathf.Clamp01(Mathf.Abs((float)nearestDistance - preferredDistance) / Mathf.Max(1, preferredDistance));
    }

    private void SortGroundPatrolCells(List<Vector2Int> patrolCells, Vector2Int roomCenterCell)
    {
        int pointIndex = 1;

        while (pointIndex < patrolCells.Count)
        {
            Vector2Int currentCell = patrolCells[pointIndex];
            float currentAngle = GetPatrolAngle(roomCenterCell, currentCell);
            int sortIndex = pointIndex - 1;

            while (sortIndex >= 0)
            {
                float sortAngle = GetPatrolAngle(roomCenterCell, patrolCells[sortIndex]);

                if (sortAngle <= currentAngle)
                {
                    break;
                }

                patrolCells[sortIndex + 1] = patrolCells[sortIndex];
                sortIndex -= 1;
            }

            patrolCells[sortIndex + 1] = currentCell;
            pointIndex += 1;
        }
    }

    private float GetPatrolAngle(Vector2Int roomCenterCell, Vector2Int patrolCell)
    {
        float offsetX = patrolCell.x - roomCenterCell.x;
        float offsetZ = patrolCell.y - roomCenterCell.y;

        return Mathf.Atan2(offsetZ, offsetX);
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
        int combatRoomIndex,
        System.Random random
    )
    {
        if (roomTypeProfile.RoomType == RoomType.Boss)
        {
            if (TrySpawnBossEnemy(roomTypeProfile, roomSizeInBlocks, floorOccupancy, freeCells, random))
            {
                return;
            }
        }

        IReadOnlyList<EnemySpawnConfig> enemyPrefabs = roomTypeProfile.EnemyPrefabs;

        if (enemyPrefabs.Count == 0)
        {
            return;
        }

        int enemySpawnCount = GetEnemySpawnCount(roomTypeProfile, combatRoomIndex, random);

        if (enemySpawnCount <= 0)
        {
            return;
        }

        HashSet<Vector2Int> enemyCells = _enemyCellsBuffer;
        List<EnemySpawnConfig> spawnedEnemies = _spawnedEnemiesBuffer;
        enemyCells.Clear();
        spawnedEnemies.Clear();
        Vector2Int roomCenterCell = GetRoomCenterCell(roomSizeInBlocks);

        int guardCount = enemySpawnCount;
        int maximumGuardCount = resourceCenters.Count * 2;

        if (guardCount > maximumGuardCount)
        {
            guardCount = maximumGuardCount;
        }

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

            int placedGuardCount = SpawnGuardsNearResource(
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
                spawnedEnemies,
                random
            );

            guardCount -= placedGuardCount;
        }

        int remainingCount = enemySpawnCount - enemyCells.Count;

        if (remainingCount <= 0)
        {
            return;
        }

        float progressWindow = 0.30f;
        int desiredSpacingInCells = Mathf.Max(_minimumEnemySpacingInCells + 2, 5);
        int placedEnemyCount = 0;
        int spawnTryCount = 0;
        int maximumSpawnTryCount = Mathf.Max(remainingCount * 6, freeCells.Count);

        while (placedEnemyCount < remainingCount)
        {
            if (spawnTryCount >= maximumSpawnTryCount)
            {
                break;
            }

            float targetProgress = (placedEnemyCount + 1f) / (remainingCount + 1f);

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

                if (corridorDistance == -1)
                {
                    continue;
                }

                if (corridorDistance < _enemyEncounterMinCorridorDistanceInCells)
                {
                    continue;
                }

                if (corridorDistance > _enemyEncounterMaxCorridorDistanceInCells)
                {
                    continue;
                }

                if (IsTooCloseToExisting(cell, enemyCells, _minimumEnemySpacingInCells) == true)
                {
                    continue;
                }

                float arenaScore;

                if (TryGetEnemyArenaScore(cell, roomSizeInBlocks, floorOccupancy, false, out arenaScore) == false)
                {
                    continue;
                }

                float cover = (float)CountObstacleNeighbors(cell, obstacleFloorCellSet) / 4f;
                float coverScore = ComputeCoverPreferenceScore(cover);
                float wallDistanceScore = ComputeWallDistanceScore(cell, roomSizeInBlocks, _enemyPreferredWallDistanceInCells);
                float roomCenterScore = ComputeRoomCenterScore(cell, roomCenterCell, roomSizeInBlocks);

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

                float score = (arenaScore * 0.28f) + (spreadScore * 0.23f) + (wallDistanceScore * 0.16f) + (roomCenterScore * 0.18f) + (progressScoreFinal * 0.10f) + (coverScore * 0.03f) + (noiseValue * 0.02f);
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
                bool hasFallbackCell = TryPickFallbackEnemyCell(
                    roomSizeInBlocks,
                    floorOccupancy,
                    distanceFromCorridor,
                    enemyForbiddenCellSet,
                    enemyCells,
                    out bestCell
                );

                if (hasFallbackCell == false)
                {
                    break;
                }
            }

            EnemySpawnConfig enemySpawn = EnemySpawnPicker.PickSpawn(enemyPrefabs, spawnedEnemies, random);
            bool isSpawned = TryInstantiateEnemyOnCell(roomTypeProfile, enemySpawn, bestCell);

            if (isSpawned)
            {
                floorOccupancy.OccupiedFloorCells.Add(bestCell);
                enemyCells.Add(bestCell);
                spawnedEnemies.Add(enemySpawn);
                placedEnemyCount += 1;
            }

            else
            {
                enemyForbiddenCellSet.Add(bestCell);
            }

            spawnTryCount += 1;
        }
    }

    private bool TrySpawnBossEnemy(
        RoomTypeProfile roomTypeProfile,
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        List<Vector2Int> freeCells,
        System.Random random
    )
    {
        IReadOnlyList<EnemySpawnConfig> enemyPrefabs = roomTypeProfile.EnemyPrefabs;

        if (enemyPrefabs.Count == 0)
        {
            return false;
        }

        _spawnedEnemiesBuffer.Clear();
        EnemySpawnConfig enemySpawn = EnemySpawnPicker.PickSpawn(enemyPrefabs, _spawnedEnemiesBuffer, random);
        Vector2Int roomCenterCell = GetRoomCenterCell(roomSizeInBlocks);
        Vector2Int bestCell = roomCenterCell;
        float bestScore = -1f;
        bool hasCandidate = false;
        int wallAvoidanceInCells = Mathf.Max(1, _enemyWallAvoidanceInCells - 1);

        for (int cellIndex = 0; cellIndex < freeCells.Count; cellIndex++)
        {
            Vector2Int cell = freeCells[cellIndex];

            if (floorOccupancy.IsFree(cell) == false)
            {
                continue;
            }

            if (IsTooCloseToRoomWalls(cell, roomSizeInBlocks, wallAvoidanceInCells))
            {
                continue;
            }

            float arenaScore;

            if (TryGetEnemyArenaScore(cell, roomSizeInBlocks, floorOccupancy, true, out arenaScore) == false)
            {
                continue;
            }

            float centerScore = ComputeRoomCenterScore(cell, roomCenterCell, roomSizeInBlocks);
            float score = (centerScore * 0.72f) + (arenaScore * 0.28f);

            if (hasCandidate == false || score > bestScore)
            {
                hasCandidate = true;
                bestScore = score;
                bestCell = cell;
            }
        }

        if (hasCandidate == false)
        {
            if (floorOccupancy.IsFree(roomCenterCell) == false)
            {
                return false;
            }
        }

        bool isSpawned = TryInstantiateEnemyOnCell(roomTypeProfile, enemySpawn, bestCell);

        if (isSpawned == false)
        {
            return false;
        }

        floorOccupancy.OccupiedFloorCells.Add(bestCell);

        return true;
    }

    private int SpawnGuardsNearResource(
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
        List<EnemySpawnConfig> spawnedEnemies,
        System.Random random
    )
    {
        int placedGuardCount = 0;
        int spawnTryCount = 0;
        int maximumSpawnTryCount = Mathf.Max(spawnCount * 6, SpawnSearchDirections * Mathf.Max(1, _enemyGuardRadiusInCells));

        while (placedGuardCount < spawnCount)
        {
            if (spawnTryCount >= maximumSpawnTryCount)
            {
                break;
            }

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

            EnemySpawnConfig enemySpawn = EnemySpawnPicker.PickSpawn(roomTypeProfile.EnemyPrefabs, spawnedEnemies, random);
            bool isSpawned = TryInstantiateEnemyOnCell(roomTypeProfile, enemySpawn, chosenCell);

            if (isSpawned)
            {
                floorOccupancy.OccupiedFloorCells.Add(chosenCell);
                enemyCells.Add(chosenCell);
                spawnedEnemies.Add(enemySpawn);
                placedGuardCount += 1;
            }

            else
            {
                enemyForbiddenCellSet.Add(chosenCell);
            }

            spawnTryCount += 1;
        }

        return placedGuardCount;
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
        int desiredSpacingInCells = Mathf.Max(_minimumEnemySpacingInCells + 1, 4);
        int preferredGuardDistanceInCells = Mathf.Max(1, radiusInCells / 2);
        Vector2Int roomCenterCell = GetRoomCenterCell(roomSizeInBlocks);

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

                float arenaScore;

                if (TryGetEnemyArenaScore(candidateCell, roomSizeInBlocks, floorOccupancy, false, out arenaScore) == false)
                {
                    continue;
                }

                int corridorDistance = distanceFromCorridor[candidateCell.x, candidateCell.y];

                if (corridorDistance == -1)
                {
                    continue;
                }

                if (corridorDistance < 1)
                {
                    continue;
                }

                float cover = (float)CountObstacleNeighbors(candidateCell, obstacleFloorCellSet) / 4f;
                float coverScore = ComputeCoverPreferenceScore(cover);
                float wallDistanceScore = ComputeWallDistanceScore(candidateCell, roomSizeInBlocks, _enemyPreferredWallDistanceInCells);
                float roomCenterScore = ComputeRoomCenterScore(candidateCell, roomCenterCell, roomSizeInBlocks);
                int distanceToCenter = Mathf.Abs(offsetX) + Mathf.Abs(offsetZ);
                float centerScore = 1f - Mathf.Clamp01(Mathf.Abs((float)distanceToCenter - preferredGuardDistanceInCells) / Mathf.Max(1, preferredGuardDistanceInCells));

                float spreadScore = 1f;

                if (enemyCells.Count > 0)
                {
                    int nearestDistance = GetMinimumManhattanDistance(enemyCells, candidateCell);
                    spreadScore = Mathf.Clamp01((float)nearestDistance / desiredSpacingInCells);
                }

                float noiseValue = noiseValues[candidateCell.x, candidateCell.y];
                float score = (arenaScore * 0.31f) + (spreadScore * 0.22f) + (centerScore * 0.19f) + (roomCenterScore * 0.12f) + (wallDistanceScore * 0.10f) + (coverScore * 0.04f) + (noiseValue * 0.02f);
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

    private bool TryPickFallbackEnemyCell(
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        int[,] distanceFromCorridor,
        HashSet<Vector2Int> enemyForbiddenCellSet,
        HashSet<Vector2Int> enemyCells,
        out Vector2Int chosenCell
    )
    {
        bool found = false;
        float bestScore = -1f;
        Vector2Int bestCell = Vector2Int.zero;
        int desiredSpacingInCells = Mathf.Max(_minimumEnemySpacingInCells + 2, 5);
        Vector2Int roomCenterCell = GetRoomCenterCell(roomSizeInBlocks);

        for (int cellX = 1; cellX <= roomSizeInBlocks.x - 2; cellX++)
        {
            for (int cellZ = 1; cellZ <= roomSizeInBlocks.z - 2; cellZ++)
            {
                Vector2Int cell = new Vector2Int(cellX, cellZ);

                if (floorOccupancy.IsFree(cell) == false)
                {
                    continue;
                }

                if (enemyForbiddenCellSet.Contains(cell) == true)
                {
                    continue;
                }

                if (distanceFromCorridor[cell.x, cell.y] == -1)
                {
                    continue;
                }

                if (IsTooCloseToExisting(cell, enemyCells, _minimumEnemySpacingInCells) == true)
                {
                    continue;
                }

                float arenaScore;

                if (TryGetEnemyArenaScore(cell, roomSizeInBlocks, floorOccupancy, true, out arenaScore) == false)
                {
                    continue;
                }

                float wallDistanceScore = ComputeWallDistanceScore(cell, roomSizeInBlocks, _enemyPreferredWallDistanceInCells);
                float roomCenterScore = ComputeRoomCenterScore(cell, roomCenterCell, roomSizeInBlocks);
                float spreadScore = 1f;

                if (enemyCells.Count > 0)
                {
                    int nearestDistance = GetMinimumManhattanDistance(enemyCells, cell);
                    spreadScore = Mathf.Clamp01((float)nearestDistance / desiredSpacingInCells);
                }

                float score = (arenaScore * 0.55f) + (spreadScore * 0.20f) + (wallDistanceScore * 0.10f) + (roomCenterScore * 0.15f);

                if (found == false || score > bestScore)
                {
                    found = true;
                    bestScore = score;
                    bestCell = cell;
                }
            }
        }

        chosenCell = bestCell;
        return found;
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
