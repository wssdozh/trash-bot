using System.Collections.Generic;
using UnityEngine;

public sealed class RoomInteriorBlockFiller : MonoBehaviour
{
    private static readonly int[] s_neighborOffsetX = new int[4] { 1, -1, 0, 0 };
    private static readonly int[] s_neighborOffsetZ = new int[4] { 0, 0, 1, -1 };

    private struct NoiseCell
    {
        public Vector2Int Cell;
        public float Value;

        public NoiseCell(Vector2Int cell, float value)
        {
            Cell = cell;
            Value = value;
        }
    }

    private struct SmallPrefabSelection
    {
        public GameObject Prefab;
        public bool ApplyBlockScale;

        public SmallPrefabSelection(GameObject prefab, bool applyBlockScale)
        {
            Prefab = prefab;
            ApplyBlockScale = applyBlockScale;
        }
    }

    [SerializeField] private Transform _interiorBlocksRoot;

    [SerializeField] private GameObject _fallbackInteriorCubePrefab;

    [SerializeField] private List<WeightedPrefab> _interiorSmallPrefabs = new List<WeightedPrefab>();
    [SerializeField] private List<WeightedPrefab> _interiorLargePrefabs = new List<WeightedPrefab>();

    [SerializeField] private float _blockSize = 1f;

    [SerializeField] private bool _spawnOnlyExposedSmallCubes = true;
    [SerializeField, Min(0)] private int _minimumSolidSmallHeightInBlocks = 1;

    private readonly HashSet<Vector2Int> _reservedFloorCellSetBuffer = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> _baseCellsBuffer = new HashSet<Vector2Int>();
    private readonly List<Vector2Int> _orderedBaseCellsBuffer = new List<Vector2Int>();
    private readonly HashSet<Vector2Int> _obstacleCellsBuffer = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> _largeCubeFootprintCellsBuffer = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector2Int, SmallPrefabSelection> _smallPrefabSelectionsBuffer = new Dictionary<Vector2Int, SmallPrefabSelection>();
    private readonly List<NoiseCell> _candidateCellsBuffer = new List<NoiseCell>();
    private readonly HashSet<Vector2Int> _reachableCellsBuffer = new HashSet<Vector2Int>();
    private readonly Queue<Vector2Int> _reachableQueueBuffer = new Queue<Vector2Int>();

    public void Clear()
    {
        ClearChildren(_interiorBlocksRoot);
    }

    public RoomFloorOccupancy Fill(Vector3Int roomSizeInBlocks, IReadOnlyCollection<Vector2Int> reservedFloorCells, RoomTypeProfile roomTypeProfile, System.Random random)
    {
        Clear();

        bool spawnOnlyExposedSmallCubes = _spawnOnlyExposedSmallCubes == true;

        HashSet<Vector2Int> reservedFloorCellSet = _reservedFloorCellSetBuffer;
        FillReservedFloorCells(reservedFloorCellSet, reservedFloorCells);

        float[,] noiseValues = CreateFlatNoise(roomSizeInBlocks.x, roomSizeInBlocks.z);

        if (roomTypeProfile.NoiseProfile != null)
        {
            noiseValues = roomTypeProfile.NoiseProfile.GenerateNoiseMap(roomSizeInBlocks.x, roomSizeInBlocks.z);
        }

        List<NoiseCell> candidateCells = _candidateCellsBuffer;
        FillCandidateCells(candidateCells, roomSizeInBlocks, reservedFloorCellSet, noiseValues);
        candidateCells.Sort(CompareNoiseCells);

        int targetBaseCellCount = Mathf.RoundToInt(candidateCells.Count * Mathf.Clamp01(roomTypeProfile.BlockFillPercent));
        if (targetBaseCellCount > candidateCells.Count)
        {
            targetBaseCellCount = candidateCells.Count;
        }

        HashSet<Vector2Int> baseCells = _baseCellsBuffer;
        List<Vector2Int> orderedBaseCells = _orderedBaseCellsBuffer;
        baseCells.Clear();
        orderedBaseCells.Clear();

        for (int index = 0; index < targetBaseCellCount; index++)
        {
            Vector2Int cell = candidateCells[index].Cell;
            baseCells.Add(cell);
            orderedBaseCells.Add(cell);
        }

        int targetLargeCubeCount = Mathf.RoundToInt((targetBaseCellCount * Mathf.Clamp01(roomTypeProfile.LargeCubeAreaPercent)) / 4f);
        int maximumLargeCubeCount = targetBaseCellCount / 4;

        if (targetLargeCubeCount > maximumLargeCubeCount)
        {
            targetLargeCubeCount = maximumLargeCubeCount;
        }

        HashSet<Vector2Int> obstacleCells = _obstacleCellsBuffer;
        HashSet<Vector2Int> largeCubeFootprintCells = _largeCubeFootprintCellsBuffer;
        obstacleCells.Clear();
        largeCubeFootprintCells.Clear();

        int[,] largeHeightMapInBlocks = new int[roomSizeInBlocks.x, roomSizeInBlocks.z];
        int[,] smallHeightMapInBlocks = new int[roomSizeInBlocks.x, roomSizeInBlocks.z];

        Dictionary<Vector2Int, SmallPrefabSelection> smallPrefabSelections = _smallPrefabSelectionsBuffer;
        smallPrefabSelections.Clear();

        PlaceLargeCubes(
            roomSizeInBlocks,
            roomTypeProfile,
            orderedBaseCells,
            baseCells,
            obstacleCells,
            largeCubeFootprintCells,
            targetLargeCubeCount,
            random,
            largeHeightMapInBlocks
        );

        PlaceSmallStacks(
            roomSizeInBlocks,
            roomTypeProfile,
            orderedBaseCells,
            obstacleCells,
            largeCubeFootprintCells,
            noiseValues,
            random,
            spawnOnlyExposedSmallCubes,
            smallHeightMapInBlocks,
            smallPrefabSelections
        );

        HashSet<Vector2Int> occupiedFloorCells = new HashSet<Vector2Int>();

        foreach (Vector2Int obstacleCell in obstacleCells)
        {
            occupiedFloorCells.Add(obstacleCell);
        }

        foreach (Vector2Int reservedCell in reservedFloorCellSet)
        {
            occupiedFloorCells.Add(reservedCell);
        }

        CloseDeadSpaces(
            roomSizeInBlocks,
            roomTypeProfile,
            reservedFloorCellSet,
            obstacleCells,
            occupiedFloorCells,
            noiseValues,
            random,
            spawnOnlyExposedSmallCubes,
            smallHeightMapInBlocks,
            smallPrefabSelections
        );

        if (spawnOnlyExposedSmallCubes == true)
        {
            SpawnHollowSmallCubes(
                roomSizeInBlocks,
                roomTypeProfile,
                random,
                smallHeightMapInBlocks,
                largeHeightMapInBlocks,
                smallPrefabSelections
            );
        }

        return new RoomFloorOccupancy(roomSizeInBlocks.x, roomSizeInBlocks.z, occupiedFloorCells);
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

    private void FillReservedFloorCells(HashSet<Vector2Int> targetCells, IReadOnlyCollection<Vector2Int> sourceCells)
    {
        targetCells.Clear();

        foreach (Vector2Int sourceCell in sourceCells)
        {
            targetCells.Add(sourceCell);
        }
    }

    private void FillCandidateCells(List<NoiseCell> candidateCells, Vector3Int roomSizeInBlocks, HashSet<Vector2Int> reservedFloorCellSet, float[,] noiseValues)
    {
        candidateCells.Clear();

        for (int cellX = 1; cellX <= roomSizeInBlocks.x - 2; cellX++)
        {
            for (int cellZ = 1; cellZ <= roomSizeInBlocks.z - 2; cellZ++)
            {
                Vector2Int cell = new Vector2Int(cellX, cellZ);

                if (reservedFloorCellSet.Contains(cell) == true)
                {
                    continue;
                }

                float noiseValue = noiseValues[cellX, cellZ];
                candidateCells.Add(new NoiseCell(cell, noiseValue));
            }
        }
    }

    private int CompareNoiseCells(NoiseCell left, NoiseCell right)
    {
        if (left.Value < right.Value)
        {
            return 1;
        }

        if (left.Value > right.Value)
        {
            return -1;
        }

        return 0;
    }

    private void PlaceLargeCubes(
        Vector3Int roomSizeInBlocks,
        RoomTypeProfile roomTypeProfile,
        List<Vector2Int> orderedBaseCells,
        HashSet<Vector2Int> baseCells,
        HashSet<Vector2Int> obstacleCells,
        HashSet<Vector2Int> largeCubeFootprintCells,
        int targetLargeCubeCount,
        System.Random random,
        int[,] largeHeightMapInBlocks
    )
    {
        if (targetLargeCubeCount <= 0)
        {
            return;
        }

        int maximumLargeCubeStackHeight = (roomSizeInBlocks.y - 2) / 2;
        if (maximumLargeCubeStackHeight < 1)
        {
            maximumLargeCubeStackHeight = 1;
        }

        int placedLargeCubeCount = 0;

        for (int index = 0; index < orderedBaseCells.Count; index++)
        {
            if (placedLargeCubeCount >= targetLargeCubeCount)
            {
                break;
            }

            Vector2Int anchorCell = orderedBaseCells[index];

            if (CanPlaceLargeCube(anchorCell, roomSizeInBlocks, baseCells, obstacleCells, largeCubeFootprintCells) == false)
            {
                continue;
            }

            int stackHeightInLargeCubes = random.Next(roomTypeProfile.LargeCubeStackHeightRange.x, roomTypeProfile.LargeCubeStackHeightRange.y + 1);
            if (stackHeightInLargeCubes < 1)
            {
                stackHeightInLargeCubes = 1;
            }

            if (stackHeightInLargeCubes > maximumLargeCubeStackHeight)
            {
                stackHeightInLargeCubes = maximumLargeCubeStackHeight;
            }

            bool applyBlockScale;
            GameObject prefab = GetLargeInteriorPrefab(random, out applyBlockScale);

            for (int stackIndex = 0; stackIndex < stackHeightInLargeCubes; stackIndex++)
            {
                CreateLargeCube(anchorCell, stackIndex, prefab, applyBlockScale, roomTypeProfile.RandomYawRotation, random);
            }

            int heightInBlocks = stackHeightInLargeCubes * 2;

            MarkLargeCubeFootprint(anchorCell, obstacleCells, largeCubeFootprintCells, largeHeightMapInBlocks, heightInBlocks);

            placedLargeCubeCount++;
        }
    }

    private bool CanPlaceLargeCube(
        Vector2Int anchorCell,
        Vector3Int roomSizeInBlocks,
        HashSet<Vector2Int> baseCells,
        HashSet<Vector2Int> obstacleCells,
        HashSet<Vector2Int> largeCubeFootprintCells
    )
    {
        if (anchorCell.x > roomSizeInBlocks.x - 3)
        {
            return false;
        }

        if (anchorCell.y > roomSizeInBlocks.z - 3)
        {
            return false;
        }

        Vector2Int cell00 = new Vector2Int(anchorCell.x, anchorCell.y);
        Vector2Int cell10 = new Vector2Int(anchorCell.x + 1, anchorCell.y);
        Vector2Int cell01 = new Vector2Int(anchorCell.x, anchorCell.y + 1);
        Vector2Int cell11 = new Vector2Int(anchorCell.x + 1, anchorCell.y + 1);

        if (baseCells.Contains(cell00) == false) return false;
        if (baseCells.Contains(cell10) == false) return false;
        if (baseCells.Contains(cell01) == false) return false;
        if (baseCells.Contains(cell11) == false) return false;

        if (obstacleCells.Contains(cell00) == true) return false;
        if (obstacleCells.Contains(cell10) == true) return false;
        if (obstacleCells.Contains(cell01) == true) return false;
        if (obstacleCells.Contains(cell11) == true) return false;

        if (largeCubeFootprintCells.Contains(cell00) == true) return false;
        if (largeCubeFootprintCells.Contains(cell10) == true) return false;
        if (largeCubeFootprintCells.Contains(cell01) == true) return false;
        if (largeCubeFootprintCells.Contains(cell11) == true) return false;

        return true;
    }

    private void MarkLargeCubeFootprint(
        Vector2Int anchorCell,
        HashSet<Vector2Int> obstacleCells,
        HashSet<Vector2Int> largeCubeFootprintCells,
        int[,] largeHeightMapInBlocks,
        int heightInBlocks
    )
    {
        Vector2Int cell00 = new Vector2Int(anchorCell.x, anchorCell.y);
        Vector2Int cell10 = new Vector2Int(anchorCell.x + 1, anchorCell.y);
        Vector2Int cell01 = new Vector2Int(anchorCell.x, anchorCell.y + 1);
        Vector2Int cell11 = new Vector2Int(anchorCell.x + 1, anchorCell.y + 1);

        obstacleCells.Add(cell00);
        obstacleCells.Add(cell10);
        obstacleCells.Add(cell01);
        obstacleCells.Add(cell11);

        largeCubeFootprintCells.Add(cell00);
        largeCubeFootprintCells.Add(cell10);
        largeCubeFootprintCells.Add(cell01);
        largeCubeFootprintCells.Add(cell11);

        SetLargeHeight(largeHeightMapInBlocks, cell00, heightInBlocks);
        SetLargeHeight(largeHeightMapInBlocks, cell10, heightInBlocks);
        SetLargeHeight(largeHeightMapInBlocks, cell01, heightInBlocks);
        SetLargeHeight(largeHeightMapInBlocks, cell11, heightInBlocks);
    }

    private void SetLargeHeight(int[,] largeHeightMapInBlocks, Vector2Int cell, int heightInBlocks)
    {
        int existing = largeHeightMapInBlocks[cell.x, cell.y];

        if (heightInBlocks > existing)
        {
            largeHeightMapInBlocks[cell.x, cell.y] = heightInBlocks;
        }
    }

    private void PlaceSmallStacks(
        Vector3Int roomSizeInBlocks,
        RoomTypeProfile roomTypeProfile,
        List<Vector2Int> orderedBaseCells,
        HashSet<Vector2Int> obstacleCells,
        HashSet<Vector2Int> largeCubeFootprintCells,
        float[,] noiseValues,
        System.Random random,
        bool spawnOnlyExposedSmallCubes,
        int[,] smallHeightMapInBlocks,
        Dictionary<Vector2Int, SmallPrefabSelection> smallPrefabSelections
    )
    {
        int maximumAllowedStackHeightInBlocks = roomSizeInBlocks.y - 2;
        if (maximumAllowedStackHeightInBlocks < 1)
        {
            maximumAllowedStackHeightInBlocks = 1;
        }

        int minimumHeight = roomTypeProfile.MinimumStackHeightInBlocks;
        if (minimumHeight < 1)
        {
            minimumHeight = 1;
        }

        int maximumHeight = roomTypeProfile.MaximumStackHeightInBlocks;
        if (maximumHeight < minimumHeight)
        {
            maximumHeight = minimumHeight;
        }

        if (maximumHeight > maximumAllowedStackHeightInBlocks)
        {
            maximumHeight = maximumAllowedStackHeightInBlocks;
        }

        float heightExponent = roomTypeProfile.HeightExponent;
        if (heightExponent < 0.01f)
        {
            heightExponent = 0.01f;
        }

        for (int index = 0; index < orderedBaseCells.Count; index++)
        {
            Vector2Int cell = orderedBaseCells[index];

            if (largeCubeFootprintCells.Contains(cell) == true)
            {
                continue;
            }

            if (obstacleCells.Contains(cell) == true)
            {
                continue;
            }

            float noiseValue = noiseValues[cell.x, cell.y];
            float shapedValue = Mathf.Pow(noiseValue, heightExponent);

            int stackHeight = Mathf.RoundToInt(Mathf.Lerp(minimumHeight, maximumHeight, shapedValue));

            if (stackHeight < 1)
            {
                stackHeight = 1;
            }

            if (stackHeight > maximumAllowedStackHeightInBlocks)
            {
                stackHeight = maximumAllowedStackHeightInBlocks;
            }

            obstacleCells.Add(cell);

            bool applyBlockScale;
            GameObject prefab = GetSmallInteriorPrefab(random, out applyBlockScale);

            if (spawnOnlyExposedSmallCubes == true)
            {
                smallHeightMapInBlocks[cell.x, cell.y] = stackHeight;
                smallPrefabSelections[cell] = new SmallPrefabSelection(prefab, applyBlockScale);
                continue;
            }

            for (int layerIndex = 0; layerIndex < stackHeight; layerIndex++)
            {
                CreateSmallCube(cell, layerIndex, prefab, applyBlockScale, roomTypeProfile.RandomYawRotation, random);
            }
        }
    }

    private void CloseDeadSpaces(
        Vector3Int roomSizeInBlocks,
        RoomTypeProfile roomTypeProfile,
        HashSet<Vector2Int> reservedFloorCells,
        HashSet<Vector2Int> obstacleCells,
        HashSet<Vector2Int> occupiedFloorCells,
        float[,] noiseValues,
        System.Random random,
        bool spawnOnlyExposedSmallCubes,
        int[,] smallHeightMapInBlocks,
        Dictionary<Vector2Int, SmallPrefabSelection> smallPrefabSelections
    )
    {
        HashSet<Vector2Int> reachableCells = ComputeReachableCells(roomSizeInBlocks, reservedFloorCells, obstacleCells);

        int maximumAllowedStackHeightInBlocks = roomSizeInBlocks.y - 2;
        if (maximumAllowedStackHeightInBlocks < 1)
        {
            maximumAllowedStackHeightInBlocks = 1;
        }

        int minimumHeight = roomTypeProfile.MinimumStackHeightInBlocks;
        if (minimumHeight < 1)
        {
            minimumHeight = 1;
        }

        int maximumHeight = roomTypeProfile.MaximumStackHeightInBlocks;
        if (maximumHeight < minimumHeight)
        {
            maximumHeight = minimumHeight;
        }

        if (maximumHeight > maximumAllowedStackHeightInBlocks)
        {
            maximumHeight = maximumAllowedStackHeightInBlocks;
        }

        float heightExponent = roomTypeProfile.HeightExponent;
        if (heightExponent < 0.01f)
        {
            heightExponent = 0.01f;
        }

        for (int cellX = 1; cellX <= roomSizeInBlocks.x - 2; cellX++)
        {
            for (int cellZ = 1; cellZ <= roomSizeInBlocks.z - 2; cellZ++)
            {
                Vector2Int cell = new Vector2Int(cellX, cellZ);

                if (reservedFloorCells.Contains(cell) == true)
                {
                    continue;
                }

                if (obstacleCells.Contains(cell) == true)
                {
                    continue;
                }

                if (reachableCells.Contains(cell) == true)
                {
                    continue;
                }

                float noiseValue = noiseValues[cell.x, cell.y];
                float shapedValue = Mathf.Pow(noiseValue, heightExponent);

                int stackHeight = Mathf.RoundToInt(Mathf.Lerp(minimumHeight, maximumHeight, shapedValue));

                if (stackHeight < 1)
                {
                    stackHeight = 1;
                }

                if (stackHeight > maximumAllowedStackHeightInBlocks)
                {
                    stackHeight = maximumAllowedStackHeightInBlocks;
                }

                obstacleCells.Add(cell);
                occupiedFloorCells.Add(cell);

                bool applyBlockScale;
                GameObject prefab = GetSmallInteriorPrefab(random, out applyBlockScale);

                if (spawnOnlyExposedSmallCubes == true)
                {
                    smallHeightMapInBlocks[cell.x, cell.y] = stackHeight;
                    smallPrefabSelections[cell] = new SmallPrefabSelection(prefab, applyBlockScale);
                    continue;
                }

                for (int layerIndex = 0; layerIndex < stackHeight; layerIndex++)
                {
                    CreateSmallCube(cell, layerIndex, prefab, applyBlockScale, roomTypeProfile.RandomYawRotation, random);
                }
            }
        }
    }

    private HashSet<Vector2Int> ComputeReachableCells(Vector3Int roomSizeInBlocks, HashSet<Vector2Int> reservedFloorCells, HashSet<Vector2Int> obstacleCells)
    {
        HashSet<Vector2Int> reachableCells = _reachableCellsBuffer;
        Queue<Vector2Int> queue = _reachableQueueBuffer;
        reachableCells.Clear();
        queue.Clear();

        foreach (Vector2Int startCell in reservedFloorCells)
        {
            if (IsInteriorCell(startCell, roomSizeInBlocks) == false)
            {
                continue;
            }

            if (obstacleCells.Contains(startCell) == true)
            {
                continue;
            }

            if (reachableCells.Contains(startCell) == true)
            {
                continue;
            }

            reachableCells.Add(startCell);
            queue.Enqueue(startCell);
        }

        while (queue.Count > 0)
        {
            Vector2Int currentCell = queue.Dequeue();

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

                if (reachableCells.Contains(neighborCell) == true)
                {
                    continue;
                }

                reachableCells.Add(neighborCell);
                queue.Enqueue(neighborCell);
            }
        }

        return reachableCells;
    }

    private bool IsInteriorCell(Vector2Int cell, Vector3Int roomSizeInBlocks)
    {
        if (cell.x < 1) return false;
        if (cell.x > roomSizeInBlocks.x - 2) return false;
        if (cell.y < 1) return false;
        if (cell.y > roomSizeInBlocks.z - 2) return false;

        return true;
    }

    private void SpawnHollowSmallCubes(
        Vector3Int roomSizeInBlocks,
        RoomTypeProfile roomTypeProfile,
        System.Random random,
        int[,] smallHeightMapInBlocks,
        int[,] largeHeightMapInBlocks,
        Dictionary<Vector2Int, SmallPrefabSelection> smallPrefabSelections
    )
    {
        int width = roomSizeInBlocks.x;
        int depth = roomSizeInBlocks.z;

        for (int cellX = 1; cellX <= width - 2; cellX++)
        {
            for (int cellZ = 1; cellZ <= depth - 2; cellZ++)
            {
                int smallHeight = smallHeightMapInBlocks[cellX, cellZ];

                if (smallHeight <= 0)
                {
                    continue;
                }

                Vector2Int cell = new Vector2Int(cellX, cellZ);

                SmallPrefabSelection selection;

                bool hasSelection = smallPrefabSelections.TryGetValue(cell, out selection);

                if (hasSelection == false)
                {
                    bool applyBlockScale;
                    GameObject prefab = GetSmallInteriorPrefab(random, out applyBlockScale);
                    selection = new SmallPrefabSelection(prefab, applyBlockScale);
                }

                int minimumSolidHeight = _minimumSolidSmallHeightInBlocks;

                if (minimumSolidHeight < 0)
                {
                    minimumSolidHeight = 0;
                }

                if (minimumSolidHeight > smallHeight)
                {
                    minimumSolidHeight = smallHeight;
                }

                for (int layerIndex = 0; layerIndex < smallHeight; layerIndex++)
                {
                    bool shouldSpawn = false;

                    if (layerIndex < minimumSolidHeight)
                    {
                        shouldSpawn = true;
                    }
                    else
                    {
                        shouldSpawn = IsSmallCubeLayerExposed(cellX, cellZ, layerIndex, smallHeight, smallHeightMapInBlocks, largeHeightMapInBlocks, roomSizeInBlocks);
                    }

                    if (shouldSpawn == false)
                    {
                        continue;
                    }

                    CreateSmallCube(cell, layerIndex, selection.Prefab, selection.ApplyBlockScale, roomTypeProfile.RandomYawRotation, random);
                }
            }
        }
    }

    private bool IsSmallCubeLayerExposed(
        int cellX,
        int cellZ,
        int layerIndex,
        int smallHeight,
        int[,] smallHeightMapInBlocks,
        int[,] largeHeightMapInBlocks,
        Vector3Int roomSizeInBlocks
    )
    {
        if (layerIndex == smallHeight - 1)
        {
            return true;
        }

        int rightHeight = GetCombinedHeight(cellX + 1, cellZ, smallHeightMapInBlocks, largeHeightMapInBlocks, roomSizeInBlocks);
        if (rightHeight <= layerIndex)
        {
            return true;
        }

        int leftHeight = GetCombinedHeight(cellX - 1, cellZ, smallHeightMapInBlocks, largeHeightMapInBlocks, roomSizeInBlocks);
        if (leftHeight <= layerIndex)
        {
            return true;
        }

        int upHeight = GetCombinedHeight(cellX, cellZ + 1, smallHeightMapInBlocks, largeHeightMapInBlocks, roomSizeInBlocks);
        if (upHeight <= layerIndex)
        {
            return true;
        }

        int downHeight = GetCombinedHeight(cellX, cellZ - 1, smallHeightMapInBlocks, largeHeightMapInBlocks, roomSizeInBlocks);
        if (downHeight <= layerIndex)
        {
            return true;
        }

        return false;
    }

    private int GetCombinedHeight(
        int cellX,
        int cellZ,
        int[,] smallHeightMapInBlocks,
        int[,] largeHeightMapInBlocks,
        Vector3Int roomSizeInBlocks
    )
    {
        if (cellX < 1 || cellX > roomSizeInBlocks.x - 2)
        {
            return 0;
        }

        if (cellZ < 1 || cellZ > roomSizeInBlocks.z - 2)
        {
            return 0;
        }

        int height = smallHeightMapInBlocks[cellX, cellZ];
        int largeHeight = largeHeightMapInBlocks[cellX, cellZ];

        if (largeHeight > height)
        {
            height = largeHeight;
        }

        return height;
    }

    private GameObject GetSmallInteriorPrefab(System.Random random, out bool applyBlockScale)
    {
        if (_interiorSmallPrefabs.Count == 0)
        {
            applyBlockScale = true;
            return _fallbackInteriorCubePrefab;
        }

        applyBlockScale = false;
        return WeightedPrefabPicker.PickPrefab(_interiorSmallPrefabs, random);
    }

    private GameObject GetLargeInteriorPrefab(System.Random random, out bool applyBlockScale)
    {
        if (_interiorLargePrefabs.Count == 0)
        {
            applyBlockScale = true;
            return _fallbackInteriorCubePrefab;
        }

        applyBlockScale = false;
        return WeightedPrefabPicker.PickPrefab(_interiorLargePrefabs, random);
    }

    private void CreateSmallCube(Vector2Int cell, int layerIndex, GameObject prefab, bool applyBlockScale, bool randomYawRotation, System.Random random)
    {
        GameObject instance = Instantiate(prefab, _interiorBlocksRoot);

        float localPositionX = (cell.x + 0.5f) * _blockSize;
        float localPositionZ = (cell.y + 0.5f) * _blockSize;

        float floorTopLocalPositionY = 1f * _blockSize;
        float localPositionY = floorTopLocalPositionY + ((layerIndex + 0.5f) * _blockSize);

        instance.transform.localPosition = new Vector3(localPositionX, localPositionY, localPositionZ);

        if (randomYawRotation == true)
        {
            int rotationIndex = random.Next(0, 4);
            instance.transform.localRotation = Quaternion.Euler(0f, rotationIndex * 90f, 0f);
        }
        else
        {
            instance.transform.localRotation = Quaternion.identity;
        }

        if (applyBlockScale == true)
        {
            instance.transform.localScale = new Vector3(_blockSize, _blockSize, _blockSize);
        }
    }

    private void CreateLargeCube(Vector2Int anchorCell, int stackIndex, GameObject prefab, bool applyBlockScale, bool randomYawRotation, System.Random random)
    {
        GameObject instance = Instantiate(prefab, _interiorBlocksRoot);

        float centerLocalPositionX = (anchorCell.x + 1f) * _blockSize;
        float centerLocalPositionZ = (anchorCell.y + 1f) * _blockSize;

        float floorTopLocalPositionY = 1f * _blockSize;
        float centerLocalPositionY = floorTopLocalPositionY + (1f * _blockSize) + (stackIndex * 2f * _blockSize);

        instance.transform.localPosition = new Vector3(centerLocalPositionX, centerLocalPositionY, centerLocalPositionZ);

        if (randomYawRotation == true)
        {
            int rotationIndex = random.Next(0, 4);
            instance.transform.localRotation = Quaternion.Euler(0f, rotationIndex * 90f, 0f);
        }
        else
        {
            instance.transform.localRotation = Quaternion.identity;
        }

        if (applyBlockScale == true)
        {
            float cubeLocalScale = 2f * _blockSize;
            instance.transform.localScale = new Vector3(cubeLocalScale, cubeLocalScale, cubeLocalScale);
        }
    }
}
