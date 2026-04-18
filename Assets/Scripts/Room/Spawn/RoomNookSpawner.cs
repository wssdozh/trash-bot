using System.Collections.Generic;
using JunkyardBoss;
using Unity.AI.Navigation;
using UnityEngine;

public sealed class RoomNookSpawner : MonoBehaviour
{
    private const int NotWalkableArea = 1;

    private struct NookComponent
    {
        public Vector2Int BestCell;
        public float Score;
        public List<Vector2Int> Cells;

        public NookComponent(Vector2Int bestCell, float score, List<Vector2Int> cells)
        {
            BestCell = bestCell;
            Score = score;
            Cells = cells;
        }
    }

    private struct PlacedNook
    {
        public int ConfigIndex;
        public Vector2Int Cell;

        public PlacedNook(int configIndex, Vector2Int cell)
        {
            ConfigIndex = configIndex;
            Cell = cell;
        }
    }

    [SerializeField] private Transform _nooksRoot;
    [SerializeField] private float _blockSize = 1f;

    public void Spawn(
        RoomTypeProfile roomTypeProfile,
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        IReadOnlyCollection<Vector2Int> corridorReservedFloorCells,
        bool hasGuaranteedNookCell,
        Vector2Int guaranteedNookCell,
        System.Random random
    )
    {
        Clear();

        IReadOnlyList<NookPrefabConfig> configs = roomTypeProfile.NookPrefabs;

        if (configs.Count == 0)
        {
            return;
        }

        float[,] noiseValues = CreateFlatNoise(roomSizeInBlocks.x, roomSizeInBlocks.z);

        if (roomTypeProfile.NoiseProfile != null)
        {
            noiseValues = roomTypeProfile.NoiseProfile.GenerateNoiseMap(roomSizeInBlocks.x, roomSizeInBlocks.z);
        }

        HashSet<Vector2Int> corridorReservedSet = new HashSet<Vector2Int>(corridorReservedFloorCells);
        HashSet<Vector2Int> obstacleFloorCellSet = CreateObstacleFloorCells(floorOccupancy.OccupiedFloorCells, corridorReservedSet);

        int[,] distanceFromCorridor = ComputeDistanceFieldFromMultipleStarts(roomSizeInBlocks, corridorReservedSet, obstacleFloorCellSet);

        HashSet<Vector2Int> candidateCellSet = BuildCandidateCellSet(roomSizeInBlocks, floorOccupancy, corridorReservedSet, distanceFromCorridor);

        List<NookComponent> nookComponents = BuildComponents(roomTypeProfile, candidateCellSet, obstacleFloorCellSet, roomSizeInBlocks, noiseValues, distanceFromCorridor);

        nookComponents.Sort(CompareComponentsDescending);

        List<int> spawnOrder = BuildSpawnOrder(roomTypeProfile, random);

        List<PlacedNook> placed = new List<PlacedNook>();

        bool usedGuaranteedCell = false;

        for (int orderIndex = 0; orderIndex < spawnOrder.Count; orderIndex++)
        {
            int configIndex = spawnOrder[orderIndex];
            NookPrefabConfig config = configs[configIndex];

            if (config == null)
            {
                continue;
            }

            if (config.Prefab == null)
            {
                continue;
            }

            Vector2Int chosenCell;
            bool found = false;

            if (usedGuaranteedCell == false && hasGuaranteedNookCell == true && config.Guaranteed == true)
            {
                bool canUse = CanPlaceGuaranteedAtCell(configIndex, config, guaranteedNookCell, roomSizeInBlocks, floorOccupancy, corridorReservedSet, placed, configs);

                if (canUse == true)
                {
                    chosenCell = guaranteedNookCell;
                    found = true;
                    usedGuaranteedCell = true;
                }
                else
                {
                    found = TryPickGuaranteedFallbackCell(configIndex, config, guaranteedNookCell, roomSizeInBlocks, floorOccupancy, corridorReservedSet, placed, configs, out chosenCell);

                    if (found == true)
                    {
                        usedGuaranteedCell = true;
                    }
                    else
                    {
                        chosenCell = Vector2Int.zero;
                    }
                }
            }
            else
            {
                chosenCell = Vector2Int.zero;
            }

            if (found == false)
            {
                found = TryPickCellFromComponents(configIndex, config, nookComponents, roomSizeInBlocks, floorOccupancy, corridorReservedSet, distanceFromCorridor, noiseValues, placed, configs, out chosenCell);
            }

            if (found == false)
            {
                found = TryPickCellGlobalFallback(configIndex, config, candidateCellSet, roomSizeInBlocks, floorOccupancy, corridorReservedSet, distanceFromCorridor, noiseValues, placed, configs, out chosenCell);
            }

            if (found == false)
            {
                continue;
            }

            GameObject instance = Object.Instantiate(config.Prefab, _nooksRoot);
            instance.transform.localPosition = GetLocalPosition(chosenCell);
            instance.transform.localRotation = Quaternion.identity;
            ApplyOriginalWorldScale(instance.transform);
            AddNavModifier(instance);
            ApplySpecialRuntime(instance);

            MarkFootprintOccupied(chosenCell, config.FootprintRadiusInCells, roomSizeInBlocks, floorOccupancy);
            placed.Add(new PlacedNook(configIndex, chosenCell));
        }
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

    public void Clear()
    {
        int childCount = _nooksRoot.childCount;

        for (int childIndex = childCount - 1; childIndex >= 0; childIndex--)
        {
            Transform childTransform = _nooksRoot.GetChild(childIndex);

            if (Application.isPlaying == false)
            {
                Object.DestroyImmediate(childTransform.gameObject);
            }
            else
            {
                Object.Destroy(childTransform.gameObject);
            }
        }
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

    private void ApplySpecialRuntime(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return;
        }

        BossRoomEnemySpawnPoint bossRoomEnemySpawnPoint = targetObject.GetComponent<BossRoomEnemySpawnPoint>();

        if (bossRoomEnemySpawnPoint == null)
        {
            return;
        }

        bossRoomEnemySpawnPoint.SetBlockSize(_blockSize);
    }

    private List<int> BuildSpawnOrder(RoomTypeProfile roomTypeProfile, System.Random random)
    {
        IReadOnlyList<NookPrefabConfig> configs = roomTypeProfile.NookPrefabs;

        List<int> order = new List<int>();

        for (int index = 0; index < configs.Count; index++)
        {
            NookPrefabConfig config = configs[index];

            if (config == null)
            {
                continue;
            }

            Vector2Int range = config.CountRange;

            int min = range.x;
            int max = range.y;

            if (max < min)
            {
                max = min;
            }

            if (min < 0)
            {
                min = 0;
            }

            if (max < 0)
            {
                max = 0;
            }

            int count = 0;

            if (max == min)
            {
                count = min;
            }
            else
            {
                count = random.Next(min, max + 1);
            }

            if (config.Guaranteed == true && count < 1)
            {
                count = 1;
            }

            for (int c = 0; c < count; c++)
            {
                order.Add(index);
            }
        }

        SortOrderByGuaranteedThenRandomWeight(order, roomTypeProfile, random);

        return order;
    }

    private void SortOrderByGuaranteedThenRandomWeight(List<int> order, RoomTypeProfile roomTypeProfile, System.Random random)
    {
        IReadOnlyList<NookPrefabConfig> configs = roomTypeProfile.NookPrefabs;

        for (int i = 0; i < order.Count - 1; i++)
        {
            for (int j = i + 1; j < order.Count; j++)
            {
                int leftIndex = order[i];
                int rightIndex = order[j];

                NookPrefabConfig left = configs[leftIndex];
                NookPrefabConfig right = configs[rightIndex];

                bool leftGuaranteed = false;
                bool rightGuaranteed = false;

                int leftWeight = 1;
                int rightWeight = 1;

                if (left != null)
                {
                    leftGuaranteed = left.Guaranteed;
                    leftWeight = left.Weight;
                }

                if (right != null)
                {
                    rightGuaranteed = right.Guaranteed;
                    rightWeight = right.Weight;
                }

                bool shouldSwap = false;

                if (leftGuaranteed == false && rightGuaranteed == true)
                {
                    shouldSwap = true;
                }
                else if (leftGuaranteed == rightGuaranteed)
                {
                    int leftRoll = random.Next(0, leftWeight + 1);
                    int rightRoll = random.Next(0, rightWeight + 1);

                    if (rightRoll > leftRoll)
                    {
                        shouldSwap = true;
                    }
                }

                if (shouldSwap == true)
                {
                    int temp = order[i];
                    order[i] = order[j];
                    order[j] = temp;
                }
            }
        }
    }

    private bool TryPickCellFromComponents(
        int configIndex,
        NookPrefabConfig config,
        List<NookComponent> components,
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        HashSet<Vector2Int> corridorReservedSet,
        int[,] distanceFromCorridor,
        float[,] noiseValues,
        List<PlacedNook> placed,
        IReadOnlyList<NookPrefabConfig> configs,
        out Vector2Int chosenCell
    )
    {
        for (int componentIndex = 0; componentIndex < components.Count; componentIndex++)
        {
            NookComponent component = components[componentIndex];

            bool found = TryPickCellInComponent(configIndex, config, component, roomSizeInBlocks, floorOccupancy, corridorReservedSet, distanceFromCorridor, noiseValues, placed, configs, out chosenCell);

            if (found == true)
            {
                return true;
            }
        }

        chosenCell = Vector2Int.zero;
        return false;
    }

    private bool TryPickCellInComponent(
        int configIndex,
        NookPrefabConfig config,
        NookComponent component,
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        HashSet<Vector2Int> corridorReservedSet,
        int[,] distanceFromCorridor,
        float[,] noiseValues,
        List<PlacedNook> placed,
        IReadOnlyList<NookPrefabConfig> configs,
        out Vector2Int chosenCell
    )
    {
        int scatterRadius = config.ScatterRadiusInCells;

        bool hasBest = false;
        float bestScore = -1f;
        Vector2Int bestCell = Vector2Int.zero;

        for (int index = 0; index < component.Cells.Count; index++)
        {
            Vector2Int cell = component.Cells[index];

            if (scatterRadius > 0)
            {
                int manhattan = Mathf.Abs(cell.x - component.BestCell.x) + Mathf.Abs(cell.y - component.BestCell.y);

                if (manhattan > scatterRadius)
                {
                    continue;
                }
            }

            bool can = CanPlaceAtCell(configIndex, config, cell, roomSizeInBlocks, floorOccupancy, corridorReservedSet, distanceFromCorridor, placed, configs);

            if (can == false)
            {
                continue;
            }

            float score = noiseValues[cell.x, cell.y];

            if (hasBest == false || score > bestScore)
            {
                hasBest = true;
                bestScore = score;
                bestCell = cell;
            }
        }

        if (hasBest == true)
        {
            chosenCell = bestCell;
            return true;
        }

        chosenCell = Vector2Int.zero;
        return false;
    }

    private bool TryPickCellGlobalFallback(
        int configIndex,
        NookPrefabConfig config,
        HashSet<Vector2Int> candidateCellSet,
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        HashSet<Vector2Int> corridorReservedSet,
        int[,] distanceFromCorridor,
        float[,] noiseValues,
        List<PlacedNook> placed,
        IReadOnlyList<NookPrefabConfig> configs,
        out Vector2Int chosenCell
    )
    {
        bool hasBest = false;
        float bestScore = -1f;
        Vector2Int bestCell = Vector2Int.zero;

        foreach (Vector2Int cell in candidateCellSet)
        {
            bool can = CanPlaceAtCell(configIndex, config, cell, roomSizeInBlocks, floorOccupancy, corridorReservedSet, distanceFromCorridor, placed, configs);

            if (can == false)
            {
                continue;
            }

            int distance = distanceFromCorridor[cell.x, cell.y];
            float distanceScore = Mathf.Clamp01((float)distance / 14f);
            float noiseScore = noiseValues[cell.x, cell.y];

            float score = (distanceScore * 0.65f) + (noiseScore * 0.35f);

            if (hasBest == false || score > bestScore)
            {
                hasBest = true;
                bestScore = score;
                bestCell = cell;
            }
        }

        if (hasBest == true)
        {
            chosenCell = bestCell;
            return true;
        }

        chosenCell = Vector2Int.zero;
        return false;
    }

    private bool TryPickGuaranteedFallbackCell(
        int configIndex,
        NookPrefabConfig config,
        Vector2Int startCell,
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        HashSet<Vector2Int> corridorReservedSet,
        List<PlacedNook> placed,
        IReadOnlyList<NookPrefabConfig> configs,
        out Vector2Int chosenCell
    )
    {
        int searchRadius = Mathf.Max(config.FootprintRadiusInCells + 2, 4);
        bool found = false;
        int bestDistance = int.MaxValue;
        Vector2Int bestCell = Vector2Int.zero;

        for (int offsetX = -searchRadius; offsetX <= searchRadius; offsetX++)
        {
            for (int offsetZ = -searchRadius; offsetZ <= searchRadius; offsetZ++)
            {
                int manhattan = Mathf.Abs(offsetX) + Mathf.Abs(offsetZ);

                if (manhattan > searchRadius)
                {
                    continue;
                }

                Vector2Int candidateCell = new Vector2Int(startCell.x + offsetX, startCell.y + offsetZ);
                bool canUse = CanPlaceGuaranteedAtCell(configIndex, config, candidateCell, roomSizeInBlocks, floorOccupancy, corridorReservedSet, placed, configs);

                if (canUse == false)
                {
                    continue;
                }

                if (found == false || manhattan < bestDistance)
                {
                    found = true;
                    bestDistance = manhattan;
                    bestCell = candidateCell;
                }
            }
        }

        chosenCell = bestCell;
        return found;
    }

    private bool CanPlaceGuaranteedAtCell(
        int configIndex,
        NookPrefabConfig config,
        Vector2Int cell,
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        HashSet<Vector2Int> corridorReservedSet,
        List<PlacedNook> placed,
        IReadOnlyList<NookPrefabConfig> configs
    )
    {
        if (IsInteriorCell(cell, roomSizeInBlocks) == false)
        {
            return false;
        }

        if (floorOccupancy.IsFree(cell) == false)
        {
            return false;
        }

        if (IsWithinWallMargin(cell, roomSizeInBlocks, config.WallMarginInCells) == false)
        {
            return false;
        }

        if (HasFootprintClearance(cell, config.FootprintRadiusInCells, roomSizeInBlocks, floorOccupancy, corridorReservedSet) == false)
        {
            return false;
        }

        if (SatisfiesNeighborRules(configIndex, config, cell, placed, configs) == false)
        {
            return false;
        }

        return true;
    }

    private bool CanPlaceAtCell(
        int configIndex,
        NookPrefabConfig config,
        Vector2Int cell,
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        HashSet<Vector2Int> corridorReservedSet,
        int[,] distanceFromCorridor,
        List<PlacedNook> placed,
        IReadOnlyList<NookPrefabConfig> configs
    )
    {
        if (IsInteriorCell(cell, roomSizeInBlocks) == false)
        {
            return false;
        }

        if (corridorReservedSet.Contains(cell) == true)
        {
            return false;
        }

        if (floorOccupancy.IsFree(cell) == false)
        {
            return false;
        }

        int corridorDistance = distanceFromCorridor[cell.x, cell.y];

        if (corridorDistance == -1)
        {
            return false;
        }

        if (corridorDistance < config.MinimumDistanceFromCorridorInCells)
        {
            return false;
        }

        if (IsWithinWallMargin(cell, roomSizeInBlocks, config.WallMarginInCells) == false)
        {
            return false;
        }

        if (HasFootprintClearance(cell, config.FootprintRadiusInCells, roomSizeInBlocks, floorOccupancy, corridorReservedSet) == false)
        {
            return false;
        }

        if (SatisfiesNeighborRules(configIndex, config, cell, placed, configs) == false)
        {
            return false;
        }

        return true;
    }

    private void MarkFootprintOccupied(
        Vector2Int centerCell,
        int footprintRadiusInCells,
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy
    )
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

    private bool SatisfiesNeighborRules(int configIndex, NookPrefabConfig config, Vector2Int cell, List<PlacedNook> placed, IReadOnlyList<NookPrefabConfig> configs)
    {
        int minAny = config.MinimumDistanceToAnyNookInCells;
        int minSame = config.MinimumDistanceToSameTypeInCells;

        int radius = config.SameTypeNeighborRadiusInCells;
        int maxSame = config.MaximumSameTypeWithinNeighborRadius;

        int sameCountInRadius = 0;

        for (int index = 0; index < placed.Count; index++)
        {
            PlacedNook existing = placed[index];

            int manhattan = Mathf.Abs(existing.Cell.x - cell.x) + Mathf.Abs(existing.Cell.y - cell.y);

            if (minAny > 0 && manhattan < minAny)
            {
                return false;
            }

            if (existing.ConfigIndex == configIndex)
            {
                if (minSame > 0 && manhattan < minSame)
                {
                    return false;
                }

                if (manhattan <= radius)
                {
                    sameCountInRadius++;
                }
            }
        }

        if (maxSame == 0)
        {
            if (sameCountInRadius > 0)
            {
                return false;
            }
        }
        else
        {
            if (sameCountInRadius >= maxSame)
            {
                return false;
            }
        }

        return true;
    }

    private HashSet<Vector2Int> BuildCandidateCellSet(Vector3Int roomSizeInBlocks, RoomFloorOccupancy floorOccupancy, HashSet<Vector2Int> corridorReservedSet, int[,] distanceFromCorridor)
    {
        HashSet<Vector2Int> candidate = new HashSet<Vector2Int>();

        for (int x = 1; x <= roomSizeInBlocks.x - 2; x++)
        {
            for (int z = 1; z <= roomSizeInBlocks.z - 2; z++)
            {
                Vector2Int cell = new Vector2Int(x, z);

                if (corridorReservedSet.Contains(cell) == true)
                {
                    continue;
                }

                if (floorOccupancy.IsFree(cell) == false)
                {
                    continue;
                }

                if (distanceFromCorridor[x, z] == -1)
                {
                    continue;
                }

                candidate.Add(cell);
            }
        }

        return candidate;
    }

    private List<NookComponent> BuildComponents(
        RoomTypeProfile roomTypeProfile,
        HashSet<Vector2Int> candidateCellSet,
        HashSet<Vector2Int> obstacleFloorCellSet,
        Vector3Int roomSizeInBlocks,
        float[,] noiseValues,
        int[,] distanceFromCorridor
    )
    {
        List<NookComponent> components = new List<NookComponent>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        int minimumArea = roomTypeProfile.NookMinimumAreaInCells;

        foreach (Vector2Int startCell in candidateCellSet)
        {
            if (visited.Contains(startCell) == true)
            {
                continue;
            }

            List<Vector2Int> componentCells = new List<Vector2Int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();

            visited.Add(startCell);
            queue.Enqueue(startCell);

            Vector2Int bestCell = startCell;
            float bestCellScore = -1f;

            float sumNoise = 0f;
            float sumDistance = 0f;

            while (queue.Count > 0)
            {
                Vector2Int currentCell = queue.Dequeue();
                componentCells.Add(currentCell);

                float noiseValue = noiseValues[currentCell.x, currentCell.y];
                int corridorDistance = distanceFromCorridor[currentCell.x, currentCell.y];

                float cover = (float)CountObstacleNeighbors(currentCell, obstacleFloorCellSet) / 4f;

                float cellScore = (noiseValue * 0.65f) + (cover * 0.25f) + (Mathf.Clamp01((float)corridorDistance / 12f) * 0.10f);

                if (cellScore > bestCellScore)
                {
                    bestCellScore = cellScore;
                    bestCell = currentCell;
                }

                sumNoise += noiseValue;
                sumDistance += corridorDistance;

                EnqueueNeighbor(candidateCellSet, visited, queue, new Vector2Int(currentCell.x + 1, currentCell.y));
                EnqueueNeighbor(candidateCellSet, visited, queue, new Vector2Int(currentCell.x - 1, currentCell.y));
                EnqueueNeighbor(candidateCellSet, visited, queue, new Vector2Int(currentCell.x, currentCell.y + 1));
                EnqueueNeighbor(candidateCellSet, visited, queue, new Vector2Int(currentCell.x, currentCell.y - 1));
            }

            if (componentCells.Count < minimumArea)
            {
                continue;
            }

            float averageNoise = sumNoise / componentCells.Count;
            float averageDistance = sumDistance / componentCells.Count;

            float componentScore = (averageNoise * 0.45f) + (Mathf.Clamp01(averageDistance / 14f) * 0.25f) + (Mathf.Clamp01((float)componentCells.Count / 60f) * 0.30f);

            components.Add(new NookComponent(bestCell, componentScore, componentCells));
        }

        return components;
    }

    private void EnqueueNeighbor(HashSet<Vector2Int> candidateCellSet, HashSet<Vector2Int> visited, Queue<Vector2Int> queue, Vector2Int neighborCell)
    {
        if (candidateCellSet.Contains(neighborCell) == false)
        {
            return;
        }

        if (visited.Contains(neighborCell) == true)
        {
            return;
        }

        visited.Add(neighborCell);
        queue.Enqueue(neighborCell);
    }

    private int CompareComponentsDescending(NookComponent left, NookComponent right)
    {
        if (left.Score < right.Score)
        {
            return 1;
        }

        if (left.Score > right.Score)
        {
            return -1;
        }

        return 0;
    }

    private HashSet<Vector2Int> CreateObstacleFloorCells(HashSet<Vector2Int> occupiedFloorCells, HashSet<Vector2Int> corridorReservedSet)
    {
        HashSet<Vector2Int> obstacleFloorCellSet = new HashSet<Vector2Int>();

        foreach (Vector2Int occupiedCell in occupiedFloorCells)
        {
            if (corridorReservedSet.Contains(occupiedCell) == true)
            {
                continue;
            }

            obstacleFloorCellSet.Add(occupiedCell);
        }

        return obstacleFloorCellSet;
    }

    private int[,] ComputeDistanceFieldFromMultipleStarts(Vector3Int roomSizeInBlocks, HashSet<Vector2Int> startCells, HashSet<Vector2Int> obstacleCells)
    {
        int[,] distances = new int[roomSizeInBlocks.x, roomSizeInBlocks.z];

        for (int x = 0; x < roomSizeInBlocks.x; x++)
        {
            for (int z = 0; z < roomSizeInBlocks.z; z++)
            {
                distances[x, z] = -1;
            }
        }

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

        return distances;
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

    private bool IsWithinWallMargin(Vector2Int cell, Vector3Int roomSizeInBlocks, int wallMarginInCells)
    {
        if (wallMarginInCells <= 0)
        {
            return true;
        }

        int minimumX = 1 + wallMarginInCells;
        int maximumX = (roomSizeInBlocks.x - 2) - wallMarginInCells;

        int minimumZ = 1 + wallMarginInCells;
        int maximumZ = (roomSizeInBlocks.z - 2) - wallMarginInCells;

        if (cell.x < minimumX)
        {
            return false;
        }

        if (cell.x > maximumX)
        {
            return false;
        }

        if (cell.y < minimumZ)
        {
            return false;
        }

        if (cell.y > maximumZ)
        {
            return false;
        }

        return true;
    }

    private bool HasFootprintClearance(
        Vector2Int centerCell,
        int footprintRadiusInCells,
        Vector3Int roomSizeInBlocks,
        RoomFloorOccupancy floorOccupancy,
        HashSet<Vector2Int> corridorReservedSet
    )
    {
        if (footprintRadiusInCells <= 0)
        {
            return true;
        }

        for (int offsetX = -footprintRadiusInCells; offsetX <= footprintRadiusInCells; offsetX++)
        {
            for (int offsetZ = -footprintRadiusInCells; offsetZ <= footprintRadiusInCells; offsetZ++)
            {
                Vector2Int cell = new Vector2Int(centerCell.x + offsetX, centerCell.y + offsetZ);

                if (IsInteriorCell(cell, roomSizeInBlocks) == false)
                {
                    return false;
                }

                if (corridorReservedSet.Contains(cell) == true)
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

    private int CountObstacleNeighbors(Vector2Int cell, HashSet<Vector2Int> obstacleCells)
    {
        int count = 0;

        if (obstacleCells.Contains(new Vector2Int(cell.x + 1, cell.y)) == true)
        {
            count++;
        }

        if (obstacleCells.Contains(new Vector2Int(cell.x - 1, cell.y)) == true)
        {
            count++;
        }

        if (obstacleCells.Contains(new Vector2Int(cell.x, cell.y + 1)) == true)
        {
            count++;
        }

        if (obstacleCells.Contains(new Vector2Int(cell.x, cell.y - 1)) == true)
        {
            count++;
        }

        return count;
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

    private Vector3 GetLocalPosition(Vector2Int floorCell)
    {
        float localPositionX = (floorCell.x + 0.5f) * _blockSize;
        float localPositionZ = (floorCell.y + 0.5f) * _blockSize;
        float localPositionY = 1f * _blockSize;

        return new Vector3(localPositionX, localPositionY, localPositionZ);
    }

    private void ApplyOriginalWorldScale(Transform objectTransform)
    {
        if (objectTransform == null)
        {
            return;
        }

        Transform parentTransform = objectTransform.parent;

        if (parentTransform == null)
        {
            return;
        }

        Vector3 parentLossyScale = parentTransform.lossyScale;
        float parentScaleX = GetSafeParentScale(parentLossyScale.x);
        float parentScaleY = GetSafeParentScale(parentLossyScale.y);
        float parentScaleZ = GetSafeParentScale(parentLossyScale.z);

        Vector3 localScale = objectTransform.localScale;

        objectTransform.localScale = new Vector3(localScale.x / parentScaleX, localScale.y / parentScaleY, localScale.z / parentScaleZ);
    }

    private float GetSafeParentScale(float parentScale)
    {
        float absoluteParentScale = Mathf.Abs(parentScale);

        if (absoluteParentScale <= 0.0001f)
        {
            return 1f;
        }

        return absoluteParentScale;
    }
}
