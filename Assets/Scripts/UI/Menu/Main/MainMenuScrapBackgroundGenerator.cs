using System.Collections.Generic;
using UnityEngine;

public sealed class MainMenuScrapBackgroundGenerator : MonoBehaviour
{
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

    [SerializeField] private RoomNoiseProfile _noiseProfile;
    [SerializeField] private GameObject _floorPrefab;
    [SerializeField] private Vector2 _floorPrefabBaseSizeInUnits = new Vector2(10f, 10f);
    [SerializeField] private Vector3 _floorLocalOffset = Vector3.zero;
    [SerializeField] private GameObject _fallbackJunkCubePrefab;
    [SerializeField] private List<WeightedPrefab> _smallPrefabs = new List<WeightedPrefab>();
    [SerializeField] private List<WeightedPrefab> _largePrefabs = new List<WeightedPrefab>();
    [SerializeField] private Vector2Int _sizeInBlocks = new Vector2Int(28, 20);
    [SerializeField, Range(0f, 1f)] private float _blockFillPercent = 0.43f;
    [SerializeField, Range(0f, 1f)] private float _largePileAreaPercent = 0.45f;
    [SerializeField, Min(1)] private int _minimumPileHeightInBlocks = 1;
    [SerializeField, Min(1)] private int _maximumPileHeightInBlocks = 7;
    [SerializeField, Range(0.25f, 4f)] private float _heightExponent = 1.49f;
    [SerializeField] private bool _spawnOnlyExposedSmallCubes = true;
    [SerializeField, Min(0)] private int _minimumSolidSmallHeightInBlocks = 1;
    [SerializeField, Min(0.01f)] private float _blockSize = 1f;
    [SerializeField] private bool _randomYawRotation = true;
    [SerializeField, Min(1)] private int _chunkCount = 4;
    [SerializeField, Min(0f)] private float _scrollSpeed = 2.4f;
    [SerializeField, Min(0f)] private float _chunkSpacingOffsetInUnits = 0f;
    [SerializeField] private int _seed = 56;
    [SerializeField] private bool _generateOnAwake = true;

    private readonly List<NoiseCell> _candidateCellsBuffer = new List<NoiseCell>();
    private readonly HashSet<Vector2Int> _baseCellsBuffer = new HashSet<Vector2Int>();
    private readonly List<Vector2Int> _orderedBaseCellsBuffer = new List<Vector2Int>();
    private readonly HashSet<Vector2Int> _largeFootprintCellsBuffer = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector2Int, SmallPrefabSelection> _smallPrefabSelectionsBuffer = new Dictionary<Vector2Int, SmallPrefabSelection>();
    private readonly List<Transform> _chunkRootsBuffer = new List<Transform>();

    private void Awake()
    {
        if (_generateOnAwake == false)
            return;

        Generate();
    }

    private void Update()
    {
        MoveChunks();
    }

    [ContextMenu(nameof(Generate))]
    public void Generate()
    {
        if (_floorPrefab == null)
            throw new MissingReferenceException(nameof(_floorPrefab));

        if (_smallPrefabs.Count == 0 && _largePrefabs.Count == 0 && _fallbackJunkCubePrefab == null)
            throw new MissingReferenceException(nameof(_fallbackJunkCubePrefab));

        ClampSettings();
        ClearGenerated();
        CreateChunks();
    }

    [ContextMenu(nameof(ClearGenerated))]
    public void ClearGenerated()
    {
        _chunkRootsBuffer.Clear();

        int childCount = transform.childCount;

        for (int childIndex = childCount - 1; childIndex >= 0; childIndex--)
        {
            Transform childTransform = transform.GetChild(childIndex);

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

    private void OnValidate()
    {
        ClampSettings();
    }

    private void ClampSettings()
    {
        _sizeInBlocks.x = Mathf.Max(1, _sizeInBlocks.x);
        _sizeInBlocks.y = Mathf.Max(1, _sizeInBlocks.y);
        _minimumPileHeightInBlocks = Mathf.Max(1, _minimumPileHeightInBlocks);
        _maximumPileHeightInBlocks = Mathf.Max(_minimumPileHeightInBlocks, _maximumPileHeightInBlocks);
        _minimumSolidSmallHeightInBlocks = Mathf.Max(0, _minimumSolidSmallHeightInBlocks);
        _blockSize = Mathf.Max(0.01f, _blockSize);
        _heightExponent = Mathf.Max(0.25f, _heightExponent);
        _floorPrefabBaseSizeInUnits.x = Mathf.Max(0.01f, _floorPrefabBaseSizeInUnits.x);
        _floorPrefabBaseSizeInUnits.y = Mathf.Max(0.01f, _floorPrefabBaseSizeInUnits.y);
        _chunkCount = Mathf.Max(1, _chunkCount);
        _scrollSpeed = Mathf.Max(0f, _scrollSpeed);
    }

    private void CreateChunks()
    {
        float chunkSpanInUnits = GetChunkSpanInUnits();

        for (int chunkIndex = 0; chunkIndex < _chunkCount; chunkIndex++)
        {
            GameObject chunkObject = new GameObject("Chunk_" + chunkIndex);
            Transform chunkRoot = chunkObject.transform;
            chunkRoot.SetParent(transform, false);
            chunkRoot.localPosition = new Vector3(0f, 0f, chunkIndex * chunkSpanInUnits);
            chunkRoot.localRotation = Quaternion.identity;
            chunkRoot.localScale = Vector3.one;
            _chunkRootsBuffer.Add(chunkRoot);

            int chunkSeed = _seed + (chunkIndex * 9973);
            System.Random random = new System.Random(chunkSeed);
            float[,] noiseValues = CreateNoiseMap(chunkSeed);

            CreateFloor(chunkRoot);
            PlaceJunk(chunkRoot, noiseValues, random);
        }
    }

    private void MoveChunks()
    {
        if (_chunkRootsBuffer.Count == 0)
            return;

        if (_scrollSpeed <= 0f)
            return;

        float delta = _scrollSpeed * Time.unscaledDeltaTime;

        for (int chunkIndex = 0; chunkIndex < _chunkRootsBuffer.Count; chunkIndex++)
        {
            Transform chunkRoot = _chunkRootsBuffer[chunkIndex];
            Vector3 localPosition = chunkRoot.localPosition;
            localPosition.z -= delta;
            chunkRoot.localPosition = localPosition;
        }

        if (_chunkRootsBuffer.Count > 1)
        {
            WrapChunks(GetChunkSpanInUnits());
        }
    }

    private void WrapChunks(float chunkSpanInUnits)
    {
        float backmostZ = GetBackmostChunkZ();

        for (int chunkIndex = 0; chunkIndex < _chunkRootsBuffer.Count; chunkIndex++)
        {
            Transform chunkRoot = _chunkRootsBuffer[chunkIndex];
            float chunkZ = chunkRoot.localPosition.z;

            if (chunkZ > -chunkSpanInUnits)
            {
                continue;
            }

            Vector3 localPosition = chunkRoot.localPosition;
            localPosition.z = backmostZ + chunkSpanInUnits;
            chunkRoot.localPosition = localPosition;
            backmostZ = localPosition.z;
        }
    }

    private float GetBackmostChunkZ()
    {
        Transform firstChunkRoot = _chunkRootsBuffer[0];
        float backmostZ = firstChunkRoot.localPosition.z;

        for (int chunkIndex = 1; chunkIndex < _chunkRootsBuffer.Count; chunkIndex++)
        {
            Transform chunkRoot = _chunkRootsBuffer[chunkIndex];
            float chunkZ = chunkRoot.localPosition.z;

            if (chunkZ > backmostZ)
            {
                backmostZ = chunkZ;
            }
        }

        return backmostZ;
    }

    private float[,] CreateNoiseMap(int seed)
    {
        if (_noiseProfile == null)
        {
            return CreateFlatNoise();
        }

        try
        {
            _noiseProfile.SetRuntimeSeed(seed);
            return _noiseProfile.GenerateNoiseMap(_sizeInBlocks.x, _sizeInBlocks.y);
        }
        finally
        {
            _noiseProfile.ClearRuntimeSeed();
        }
    }

    private float[,] CreateFlatNoise()
    {
        float[,] values = new float[_sizeInBlocks.x, _sizeInBlocks.y];

        for (int cellX = 0; cellX < _sizeInBlocks.x; cellX++)
        {
            for (int cellZ = 0; cellZ < _sizeInBlocks.y; cellZ++)
            {
                values[cellX, cellZ] = 0.5f;
            }
        }

        return values;
    }

    private void CreateFloor(Transform chunkRoot)
    {
        GameObject floorInstance = Instantiate(_floorPrefab);
        PrepareChunkInstance(floorInstance, chunkRoot);

        float widthInUnits = _sizeInBlocks.x * _blockSize;
        float depthInUnits = _sizeInBlocks.y * _blockSize;
        float scaleX = widthInUnits / _floorPrefabBaseSizeInUnits.x;
        float scaleZ = depthInUnits / _floorPrefabBaseSizeInUnits.y;

        floorInstance.transform.localPosition = _floorLocalOffset;
        floorInstance.transform.localRotation = Quaternion.identity;
        floorInstance.transform.localScale = new Vector3(scaleX, 1f, scaleZ);
    }

    private void PlaceJunk(Transform chunkRoot, float[,] noiseValues, System.Random random)
    {
        List<NoiseCell> candidateCells = _candidateCellsBuffer;
        candidateCells.Clear();

        for (int cellX = 0; cellX < _sizeInBlocks.x; cellX++)
        {
            for (int cellZ = 0; cellZ < _sizeInBlocks.y; cellZ++)
            {
                Vector2Int cell = new Vector2Int(cellX, cellZ);
                candidateCells.Add(new NoiseCell(cell, noiseValues[cellX, cellZ]));
            }
        }

        candidateCells.Sort(CompareNoiseCells);

        int targetBaseCellCount = Mathf.RoundToInt(candidateCells.Count * Mathf.Clamp01(_blockFillPercent));

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

        HashSet<Vector2Int> largeFootprintCells = _largeFootprintCellsBuffer;
        largeFootprintCells.Clear();

        int[,] largeHeightMapInBlocks = new int[_sizeInBlocks.x, _sizeInBlocks.y];
        int[,] smallHeightMapInBlocks = new int[_sizeInBlocks.x, _sizeInBlocks.y];

        Dictionary<Vector2Int, SmallPrefabSelection> smallPrefabSelections = _smallPrefabSelectionsBuffer;
        smallPrefabSelections.Clear();

        PlaceLargePiles(chunkRoot, orderedBaseCells, baseCells, largeFootprintCells, noiseValues, random, largeHeightMapInBlocks);
        PlaceSmallPiles(chunkRoot, orderedBaseCells, largeFootprintCells, noiseValues, random, smallHeightMapInBlocks, smallPrefabSelections);

        if (_spawnOnlyExposedSmallCubes == true)
        {
            SpawnHollowSmallPiles(chunkRoot, random, smallHeightMapInBlocks, largeHeightMapInBlocks, smallPrefabSelections);
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

    private void PlaceLargePiles(
        Transform chunkRoot,
        List<Vector2Int> orderedBaseCells,
        HashSet<Vector2Int> baseCells,
        HashSet<Vector2Int> largeFootprintCells,
        float[,] noiseValues,
        System.Random random,
        int[,] largeHeightMapInBlocks
    )
    {
        int targetLargePileCount = Mathf.RoundToInt((orderedBaseCells.Count * Mathf.Clamp01(_largePileAreaPercent)) / 4f);
        int maximumLargePileCount = orderedBaseCells.Count / 4;

        if (targetLargePileCount > maximumLargePileCount)
        {
            targetLargePileCount = maximumLargePileCount;
        }

        int placedLargePileCount = 0;

        for (int index = 0; index < orderedBaseCells.Count; index++)
        {
            if (placedLargePileCount >= targetLargePileCount)
            {
                break;
            }

            Vector2Int anchorCell = orderedBaseCells[index];

            if (CanPlaceLargePile(anchorCell, baseCells, largeFootprintCells) == false)
            {
                continue;
            }

            float shapedNoise = GetShapedNoise(noiseValues[anchorCell.x, anchorCell.y]);
            int heightInBlocks = EvaluatePileHeightInBlocks(shapedNoise);
            int stackHeightInLargeCubes = Mathf.Max(1, Mathf.CeilToInt(heightInBlocks * 0.5f));

            bool applyBlockScale;
            GameObject prefab = GetLargeJunkPrefab(random, out applyBlockScale);

            for (int stackIndex = 0; stackIndex < stackHeightInLargeCubes; stackIndex++)
            {
                CreateLargeCube(chunkRoot, anchorCell, stackIndex, prefab, applyBlockScale, random);
            }

            MarkLargeFootprint(anchorCell, largeFootprintCells, largeHeightMapInBlocks, stackHeightInLargeCubes * 2);
            placedLargePileCount += 1;
        }
    }

    private bool CanPlaceLargePile(Vector2Int anchorCell, HashSet<Vector2Int> baseCells, HashSet<Vector2Int> largeFootprintCells)
    {
        if (anchorCell.x >= _sizeInBlocks.x - 1)
        {
            return false;
        }

        if (anchorCell.y >= _sizeInBlocks.y - 1)
        {
            return false;
        }

        Vector2Int cell00 = new Vector2Int(anchorCell.x, anchorCell.y);
        Vector2Int cell10 = new Vector2Int(anchorCell.x + 1, anchorCell.y);
        Vector2Int cell01 = new Vector2Int(anchorCell.x, anchorCell.y + 1);
        Vector2Int cell11 = new Vector2Int(anchorCell.x + 1, anchorCell.y + 1);

        if (baseCells.Contains(cell00) == false)
            return false;

        if (baseCells.Contains(cell10) == false)
            return false;

        if (baseCells.Contains(cell01) == false)
            return false;

        if (baseCells.Contains(cell11) == false)
            return false;

        if (largeFootprintCells.Contains(cell00) == true)
            return false;

        if (largeFootprintCells.Contains(cell10) == true)
            return false;

        if (largeFootprintCells.Contains(cell01) == true)
            return false;

        if (largeFootprintCells.Contains(cell11) == true)
            return false;

        return true;
    }

    private void MarkLargeFootprint(
        Vector2Int anchorCell,
        HashSet<Vector2Int> largeFootprintCells,
        int[,] largeHeightMapInBlocks,
        int heightInBlocks
    )
    {
        Vector2Int cell00 = new Vector2Int(anchorCell.x, anchorCell.y);
        Vector2Int cell10 = new Vector2Int(anchorCell.x + 1, anchorCell.y);
        Vector2Int cell01 = new Vector2Int(anchorCell.x, anchorCell.y + 1);
        Vector2Int cell11 = new Vector2Int(anchorCell.x + 1, anchorCell.y + 1);

        largeFootprintCells.Add(cell00);
        largeFootprintCells.Add(cell10);
        largeFootprintCells.Add(cell01);
        largeFootprintCells.Add(cell11);

        largeHeightMapInBlocks[cell00.x, cell00.y] = heightInBlocks;
        largeHeightMapInBlocks[cell10.x, cell10.y] = heightInBlocks;
        largeHeightMapInBlocks[cell01.x, cell01.y] = heightInBlocks;
        largeHeightMapInBlocks[cell11.x, cell11.y] = heightInBlocks;
    }

    private void PlaceSmallPiles(
        Transform chunkRoot,
        List<Vector2Int> orderedBaseCells,
        HashSet<Vector2Int> largeFootprintCells,
        float[,] noiseValues,
        System.Random random,
        int[,] smallHeightMapInBlocks,
        Dictionary<Vector2Int, SmallPrefabSelection> smallPrefabSelections
    )
    {
        for (int index = 0; index < orderedBaseCells.Count; index++)
        {
            Vector2Int cell = orderedBaseCells[index];

            if (largeFootprintCells.Contains(cell) == true)
            {
                continue;
            }

            float shapedNoise = GetShapedNoise(noiseValues[cell.x, cell.y]);
            int stackHeight = EvaluatePileHeightInBlocks(shapedNoise);

            bool applyBlockScale;
            GameObject prefab = GetSmallJunkPrefab(random, out applyBlockScale);

            if (_spawnOnlyExposedSmallCubes == true)
            {
                smallHeightMapInBlocks[cell.x, cell.y] = stackHeight;
                smallPrefabSelections[cell] = new SmallPrefabSelection(prefab, applyBlockScale);
                continue;
            }

            for (int layerIndex = 0; layerIndex < stackHeight; layerIndex++)
            {
                CreateSmallCube(chunkRoot, cell, layerIndex, prefab, applyBlockScale, random);
            }
        }
    }

    private void SpawnHollowSmallPiles(
        Transform chunkRoot,
        System.Random random,
        int[,] smallHeightMapInBlocks,
        int[,] largeHeightMapInBlocks,
        Dictionary<Vector2Int, SmallPrefabSelection> smallPrefabSelections
    )
    {
        for (int cellX = 0; cellX < _sizeInBlocks.x; cellX++)
        {
            for (int cellZ = 0; cellZ < _sizeInBlocks.y; cellZ++)
            {
                int smallHeight = smallHeightMapInBlocks[cellX, cellZ];

                if (smallHeight <= 0)
                {
                    continue;
                }

                Vector2Int cell = new Vector2Int(cellX, cellZ);
                SmallPrefabSelection selection;

                if (smallPrefabSelections.TryGetValue(cell, out selection) == false)
                {
                    bool applyBlockScale;
                    GameObject prefab = GetSmallJunkPrefab(random, out applyBlockScale);
                    selection = new SmallPrefabSelection(prefab, applyBlockScale);
                }

                int minimumSolidHeight = _minimumSolidSmallHeightInBlocks;

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
                        shouldSpawn = IsSmallCubeLayerExposed(cellX, cellZ, layerIndex, smallHeight, smallHeightMapInBlocks, largeHeightMapInBlocks);
                    }

                    if (shouldSpawn == false)
                    {
                        continue;
                    }

                    CreateSmallCube(chunkRoot, cell, layerIndex, selection.Prefab, selection.ApplyBlockScale, random);
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
        int[,] largeHeightMapInBlocks
    )
    {
        if (layerIndex == smallHeight - 1)
        {
            return true;
        }

        int rightHeight = GetCombinedHeight(cellX + 1, cellZ, smallHeightMapInBlocks, largeHeightMapInBlocks);

        if (rightHeight <= layerIndex)
        {
            return true;
        }

        int leftHeight = GetCombinedHeight(cellX - 1, cellZ, smallHeightMapInBlocks, largeHeightMapInBlocks);

        if (leftHeight <= layerIndex)
        {
            return true;
        }

        int upHeight = GetCombinedHeight(cellX, cellZ + 1, smallHeightMapInBlocks, largeHeightMapInBlocks);

        if (upHeight <= layerIndex)
        {
            return true;
        }

        int downHeight = GetCombinedHeight(cellX, cellZ - 1, smallHeightMapInBlocks, largeHeightMapInBlocks);

        if (downHeight <= layerIndex)
        {
            return true;
        }

        return false;
    }

    private int GetCombinedHeight(int cellX, int cellZ, int[,] smallHeightMapInBlocks, int[,] largeHeightMapInBlocks)
    {
        if (cellX < 0)
            return 0;

        if (cellX >= _sizeInBlocks.x)
            return 0;

        if (cellZ < 0)
            return 0;

        if (cellZ >= _sizeInBlocks.y)
            return 0;

        int height = smallHeightMapInBlocks[cellX, cellZ];
        int largeHeight = largeHeightMapInBlocks[cellX, cellZ];

        if (largeHeight > height)
        {
            height = largeHeight;
        }

        return height;
    }

    private float GetShapedNoise(float noiseValue)
    {
        float clampedValue = Mathf.Clamp01(noiseValue);
        return Mathf.Pow(clampedValue, _heightExponent);
    }

    private int EvaluatePileHeightInBlocks(float shapedNoise)
    {
        int stackHeight = Mathf.RoundToInt(Mathf.Lerp(_minimumPileHeightInBlocks, _maximumPileHeightInBlocks, shapedNoise));

        if (stackHeight < 1)
        {
            stackHeight = 1;
        }

        return stackHeight;
    }

    private GameObject GetSmallJunkPrefab(System.Random random, out bool applyBlockScale)
    {
        if (_smallPrefabs.Count == 0)
        {
            if (_fallbackJunkCubePrefab == null)
                throw new MissingReferenceException(nameof(_fallbackJunkCubePrefab));

            applyBlockScale = true;
            return _fallbackJunkCubePrefab;
        }

        WeightedPrefab weightedPrefab = PickWeightedPrefab(_smallPrefabs, random);
        applyBlockScale = false;

        return weightedPrefab.Prefab;
    }

    private GameObject GetLargeJunkPrefab(System.Random random, out bool applyBlockScale)
    {
        if (_largePrefabs.Count == 0)
        {
            if (_fallbackJunkCubePrefab == null)
                throw new MissingReferenceException(nameof(_fallbackJunkCubePrefab));

            applyBlockScale = true;
            return _fallbackJunkCubePrefab;
        }

        WeightedPrefab weightedPrefab = PickWeightedPrefab(_largePrefabs, random);
        applyBlockScale = false;

        return weightedPrefab.Prefab;
    }

    private WeightedPrefab PickWeightedPrefab(IReadOnlyList<WeightedPrefab> weightedPrefabs, System.Random random)
    {
        int totalWeight = 0;

        for (int prefabIndex = 0; prefabIndex < weightedPrefabs.Count; prefabIndex++)
        {
            WeightedPrefab weightedPrefab = weightedPrefabs[prefabIndex];

            if (weightedPrefab == null)
            {
                continue;
            }

            if (weightedPrefab.Prefab == null)
            {
                continue;
            }

            int weight = weightedPrefab.Weight;

            if (weight < 1)
            {
                weight = 1;
            }

            totalWeight += weight;
        }

        if (totalWeight <= 0)
            throw new System.InvalidOperationException(nameof(weightedPrefabs));

        int randomValue = random.Next(0, totalWeight);
        int cumulativeWeight = 0;

        for (int prefabIndex = 0; prefabIndex < weightedPrefabs.Count; prefabIndex++)
        {
            WeightedPrefab weightedPrefab = weightedPrefabs[prefabIndex];

            if (weightedPrefab == null)
            {
                continue;
            }

            if (weightedPrefab.Prefab == null)
            {
                continue;
            }

            int weight = weightedPrefab.Weight;

            if (weight < 1)
            {
                weight = 1;
            }

            cumulativeWeight += weight;

            if (randomValue < cumulativeWeight)
            {
                return weightedPrefab;
            }
        }

        throw new System.InvalidOperationException(nameof(weightedPrefabs));
    }

    private void CreateSmallCube(Transform chunkRoot, Vector2Int cell, int layerIndex, GameObject prefab, bool applyBlockScale, System.Random random)
    {
        GameObject instance = Instantiate(prefab);
        PrepareChunkInstance(instance, chunkRoot);
        float localPositionY = GetFloorTopY() + ((layerIndex + 0.5f) * _blockSize);

        instance.transform.localPosition = new Vector3(
            GetCellLocalX(cell.x),
            localPositionY,
            GetCellLocalZ(cell.y)
        );

        ApplyRotation(instance.transform, random);

        if (applyBlockScale == true)
        {
            instance.transform.localScale = new Vector3(_blockSize, _blockSize, _blockSize);
        }
    }

    private void CreateLargeCube(Transform chunkRoot, Vector2Int anchorCell, int stackIndex, GameObject prefab, bool applyBlockScale, System.Random random)
    {
        GameObject instance = Instantiate(prefab);
        PrepareChunkInstance(instance, chunkRoot);

        float localPositionX = GetCellLocalX(anchorCell.x) + (_blockSize * 0.5f);
        float localPositionZ = GetCellLocalZ(anchorCell.y) + (_blockSize * 0.5f);
        float localPositionY = GetFloorTopY() + _blockSize + (stackIndex * 2f * _blockSize);

        instance.transform.localPosition = new Vector3(localPositionX, localPositionY, localPositionZ);

        ApplyRotation(instance.transform, random);

        if (applyBlockScale == true)
        {
            float largeScale = 2f * _blockSize;
            instance.transform.localScale = new Vector3(largeScale, largeScale, largeScale);
        }
    }

    private void ApplyRotation(Transform targetTransform, System.Random random)
    {
        if (_randomYawRotation == false)
        {
            targetTransform.localRotation = Quaternion.identity;
            return;
        }

        int rotationIndex = random.Next(0, 4);
        targetTransform.localRotation = Quaternion.Euler(0f, rotationIndex * 90f, 0f);
    }

    private void PrepareChunkInstance(GameObject instance, Transform chunkRoot)
    {
        instance.transform.SetParent(chunkRoot, false);
        ClearStaticFlags(instance.transform);
    }

    private void ClearStaticFlags(Transform rootTransform)
    {
        rootTransform.gameObject.isStatic = false;

        int childCount = rootTransform.childCount;

        for (int childIndex = 0; childIndex < childCount; childIndex++)
        {
            Transform childTransform = rootTransform.GetChild(childIndex);
            ClearStaticFlags(childTransform);
        }
    }

    private float GetFloorTopY()
    {
        return _floorLocalOffset.y;
    }

    private float GetCellLocalX(int cellX)
    {
        float halfWidth = (_sizeInBlocks.x - 1) * _blockSize * 0.5f;
        return -halfWidth + (cellX * _blockSize);
    }

    private float GetCellLocalZ(int cellZ)
    {
        float halfDepth = (_sizeInBlocks.y - 1) * _blockSize * 0.5f;
        return -halfDepth + (cellZ * _blockSize);
    }

    private float GetChunkSpanInUnits()
    {
        float depthInUnits = _sizeInBlocks.y * _blockSize;

        return depthInUnits + _chunkSpacingOffsetInUnits;
    }
}
