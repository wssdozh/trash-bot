using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RoomShellBuilder : MonoBehaviour
{
    private const float FenceFaceYawOffset = -90f;
    private const float FencePostFloorLiftInBlocks = 0.5f;
    private const float MinVisualSize = 0.0001f;

    private struct FenceVisualMetrics
    {
        public Bounds LocalBounds;
        public bool LengthAlongX;
        public float SourceLength;

        public FenceVisualMetrics(Bounds localBounds)
        {
            LocalBounds = localBounds;
            LengthAlongX = localBounds.size.x > localBounds.size.z;
            SourceLength = LengthAlongX == true ? localBounds.size.x : localBounds.size.z;
        }
    }

    private enum FloorBuildMode
    {
        TiledBlocks,
        SinglePrefab
    }

    private enum FencePostPlacement
    {
        None,
        CornersAndDoors,
        EveryFenceSegment
    }

    [SerializeField] private Transform _floorBlocksRoot;
    [SerializeField] private GameObject _floorBlockPrefab;

    [SerializeField] private FloorBuildMode _floorBuildMode = FloorBuildMode.TiledBlocks;

    [SerializeField] private GameObject _floorPrefab;
    [SerializeField] private bool _autoScaleFloorPrefab = true;
    [SerializeField] private Vector2 _floorPrefabBaseSizeInUnits = new Vector2(10f, 10f);
    [SerializeField] private Vector3 _floorPrefabLocalOffset = Vector3.zero;
    [SerializeField] private Vector3 _floorPrefabScaleMultiplier = Vector3.one;

    [SerializeField] private Transform _fencePostsRoot;
    [SerializeField] private GameObject _fencePostPrefab;
    [SerializeField] private FencePostPlacement _postPlacement = FencePostPlacement.CornersAndDoors;
    [SerializeField] private GameObject _fencePostVisualPrefab;
    [SerializeField] private string _fencePostVisualResourcePath;
    [SerializeField, Min(0.01f)] private float _fencePostVisualHeightMultiplier = 0.825f;
    [SerializeField, Min(0.01f)] private float _fencePostVisualThicknessMultiplier = 1f;
    [SerializeField] private float _fencePostVisualYawOffset = 0f;

    [SerializeField] private Transform _fenceSegmentsRoot;
    [SerializeField] private GameObject _fenceSegmentPrefab;
    [SerializeField] private List<GameObject> _fenceSegmentVisualPrefabs = new List<GameObject>();
    [SerializeField] private List<string> _fenceSegmentVisualResourcePaths = new List<string>();
    [SerializeField] private bool _hideFenceBlockRenderersWhenVisualsAssigned = true;
    [SerializeField, Min(0.01f)] private float _fenceSegmentVisualHeightMultiplier = 1f;
    [SerializeField, Min(1f)] private float _fenceSegmentVisualMaximumStretch = 1.35f;
    [SerializeField] private float _fenceSegmentVisualYawOffset = 0f;

    [SerializeField] private Transform _doorMarkersRoot;
    [SerializeField] private GameObject _doorMarkerPrefab;

    [SerializeField] private float _blockSize = 1f;
    [SerializeField] private bool _ceilingEnabled = false;

    [SerializeField, Min(4)] private int _totalPostCount = 12;
    [SerializeField, Min(0)] private int _minimumPostDistanceInCells = 2;

    [SerializeField, Min(1)] private int _postHeightInBlocks = 5;
    [SerializeField, Min(1)] private int _segmentHeightInBlocks = 2;

    [SerializeField] private bool _postPivotAtBase = false;
    [SerializeField] private bool _segmentPivotAtBase = false;

    [SerializeField, Min(0f)] private float _segmentEndGapInBlocks = 0.5f;

    [SerializeField] private float _floorSurfaceYOffset = 0f;

    private readonly List<GameObject> _resolvedFenceSegmentVisualPrefabs = new List<GameObject>();
    private GameObject _resolvedFencePostVisualPrefab;

    public float BlockSize => _blockSize;
    public int PostHeightInBlocks => _postHeightInBlocks;
    public Material FenceMaterial => GetFenceMaterial();

    public void BuildShell(Vector3Int roomSizeInBlocks, IReadOnlyList<RoomDoorPlan> doorPlans)
    {
        Clear();

        float floorSurfaceY = GetFloorSurfaceY(roomSizeInBlocks);

        ResolveFenceSegmentVisualPrefabs();
        ResolveFencePostVisualPrefab();
        BuildFloor(roomSizeInBlocks);
        BuildFence(roomSizeInBlocks, doorPlans, floorSurfaceY);
        BuildDoorMarkers(roomSizeInBlocks, doorPlans, floorSurfaceY);
    }

    public void Clear()
    {
        ClearChildren(_floorBlocksRoot);
        ClearChildren(_fencePostsRoot);
        ClearChildren(_fenceSegmentsRoot);
        ClearChildren(_doorMarkersRoot);
    }

    private void BuildFloor(Vector3Int roomSizeInBlocks)
    {
        if (_floorBuildMode == FloorBuildMode.TiledBlocks)
        {
            BuildFloorTiled(roomSizeInBlocks);
            return;
        }

        BuildFloorPrefab(roomSizeInBlocks);
    }

    private void BuildFloorTiled(Vector3Int roomSizeInBlocks)
    {
        int roomWidthInBlocks = roomSizeInBlocks.x;
        int roomDepthInBlocks = roomSizeInBlocks.z;

        for (int widthIndex = 0; widthIndex < roomWidthInBlocks; widthIndex++)
        {
            for (int depthIndex = 0; depthIndex < roomDepthInBlocks; depthIndex++)
            {
                Vector3Int floorCoordinate = new Vector3Int(widthIndex, 0, depthIndex);
                CreateFloorBlock(floorCoordinate);

                if (_ceilingEnabled == true)
                {
                    Vector3Int ceilingCoordinate = new Vector3Int(widthIndex, roomSizeInBlocks.y - 1, depthIndex);
                    CreateFloorBlock(ceilingCoordinate);
                }
            }
        }
    }

    private void BuildFloorPrefab(Vector3Int roomSizeInBlocks)
    {
        GameObject floorInstance = Instantiate(_floorPrefab, _floorBlocksRoot);

        float roomWidthInUnits = roomSizeInBlocks.x * _blockSize;
        float roomDepthInUnits = roomSizeInBlocks.z * _blockSize;

        float localPositionX = (roomWidthInUnits * 0.5f) + _floorPrefabLocalOffset.x;
        float localPositionY = _floorPrefabLocalOffset.y;
        float localPositionZ = (roomDepthInUnits * 0.5f) + _floorPrefabLocalOffset.z;

        floorInstance.transform.localPosition = new Vector3(localPositionX, localPositionY, localPositionZ);
        floorInstance.transform.localRotation = Quaternion.identity;

        if (_autoScaleFloorPrefab == false)
        {
            floorInstance.transform.localScale = _floorPrefabScaleMultiplier;
            return;
        }

        float baseWidth = _floorPrefabBaseSizeInUnits.x;
        float baseDepth = _floorPrefabBaseSizeInUnits.y;

        if (baseWidth < 0.0001f)
        {
            baseWidth = 0.0001f;
        }

        if (baseDepth < 0.0001f)
        {
            baseDepth = 0.0001f;
        }

        float scaleX = (roomWidthInUnits / baseWidth) * _floorPrefabScaleMultiplier.x;
        float scaleZ = (roomDepthInUnits / baseDepth) * _floorPrefabScaleMultiplier.z;
        float scaleY = _floorPrefabScaleMultiplier.y;

        floorInstance.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
    }

    private float GetFloorSurfaceY(Vector3Int roomSizeInBlocks)
    {
        float baseSurfaceY = 0f;

        if (_floorBuildMode == FloorBuildMode.TiledBlocks)
        {
            baseSurfaceY = _blockSize;
        }
        else
        {
            baseSurfaceY = _floorPrefabLocalOffset.y;
        }

        return baseSurfaceY + _floorSurfaceYOffset;
    }

    private void BuildFence(Vector3Int roomSizeInBlocks, IReadOnlyList<RoomDoorPlan> doorPlans, float floorSurfaceY)
    {
        bool[] northDoorMask = BuildDoorMask(DoorSide.North, roomSizeInBlocks, doorPlans);
        bool[] southDoorMask = BuildDoorMask(DoorSide.South, roomSizeInBlocks, doorPlans);
        bool[] eastDoorMask = BuildDoorMask(DoorSide.East, roomSizeInBlocks, doorPlans);
        bool[] westDoorMask = BuildDoorMask(DoorSide.West, roomSizeInBlocks, doorPlans);

        HashSet<Vector2Int> postCells = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, Vector2> postVisualPositions = new Dictionary<Vector2Int, Vector2>();

        AddCornerPosts(postCells, roomSizeInBlocks);
        AddDoorEdgePosts(postCells, postVisualPositions, roomSizeInBlocks, doorPlans, northDoorMask, southDoorMask, eastDoorMask, westDoorMask);

        if (_postPlacement == FencePostPlacement.EveryFenceSegment)
        {
            List<Vector2Int> perimeterCandidates = BuildPerimeterCandidates(roomSizeInBlocks, northDoorMask, southDoorMask, eastDoorMask, westDoorMask, postCells);
            int requiredPostCount = postCells.Count;
            int desiredPostCount = _totalPostCount;

            if (desiredPostCount < requiredPostCount)
            {
                desiredPostCount = requiredPostCount;
            }

            int additionalNeeded = desiredPostCount - requiredPostCount;

            if (additionalNeeded > 0 && perimeterCandidates.Count > 0)
            {
                int maximumAddable = perimeterCandidates.Count;

                if (additionalNeeded > maximumAddable)
                {
                    additionalNeeded = maximumAddable;
                }

                AddDistributedPosts(postCells, perimeterCandidates, additionalNeeded, _minimumPostDistanceInCells);
            }
        }

        if (ShouldCreateFencePosts() == true)
        {
            CreatePosts(postCells, postVisualPositions, roomSizeInBlocks, floorSurfaceY);
        }

        CreateSegmentsForSide(DoorSide.North, roomSizeInBlocks, postCells, postVisualPositions, northDoorMask, floorSurfaceY);
        CreateSegmentsForSide(DoorSide.South, roomSizeInBlocks, postCells, postVisualPositions, southDoorMask, floorSurfaceY);
        CreateSegmentsForSide(DoorSide.East, roomSizeInBlocks, postCells, postVisualPositions, eastDoorMask, floorSurfaceY);
        CreateSegmentsForSide(DoorSide.West, roomSizeInBlocks, postCells, postVisualPositions, westDoorMask, floorSurfaceY);
    }

    private void BuildDoorMarkers(Vector3Int roomSizeInBlocks, IReadOnlyList<RoomDoorPlan> doorPlans, float floorSurfaceY)
    {
        for (int doorIndex = 0; doorIndex < doorPlans.Count; doorIndex++)
        {
            CreateDoorMarker(doorPlans[doorIndex], roomSizeInBlocks, floorSurfaceY);
        }
    }

    private void AddCornerPosts(HashSet<Vector2Int> postCells, Vector3Int roomSizeInBlocks)
    {
        int maxX = roomSizeInBlocks.x - 1;
        int maxZ = roomSizeInBlocks.z - 1;

        postCells.Add(new Vector2Int(0, 0));
        postCells.Add(new Vector2Int(maxX, 0));
        postCells.Add(new Vector2Int(maxX, maxZ));
        postCells.Add(new Vector2Int(0, maxZ));
    }

    private void AddDoorEdgePosts(
        HashSet<Vector2Int> postCells,
        Dictionary<Vector2Int, Vector2> postVisualPositions,
        Vector3Int roomSizeInBlocks,
        IReadOnlyList<RoomDoorPlan> doorPlans,
        bool[] northDoorMask,
        bool[] southDoorMask,
        bool[] eastDoorMask,
        bool[] westDoorMask
    )
    {
        for (int doorIndex = 0; doorIndex < doorPlans.Count; doorIndex++)
        {
            RoomDoorPlan doorPlan = doorPlans[doorIndex];

            if (doorPlan.Side == DoorSide.North)
            {
                AddDoorEdgePostsOnHorizontalWall(postCells, postVisualPositions, roomSizeInBlocks, doorPlan, northDoorMask, roomSizeInBlocks.z - 1);
            }

            if (doorPlan.Side == DoorSide.South)
            {
                AddDoorEdgePostsOnHorizontalWall(postCells, postVisualPositions, roomSizeInBlocks, doorPlan, southDoorMask, 0);
            }

            if (doorPlan.Side == DoorSide.East)
            {
                AddDoorEdgePostsOnVerticalWall(postCells, postVisualPositions, roomSizeInBlocks, doorPlan, eastDoorMask, roomSizeInBlocks.x - 1);
            }

            if (doorPlan.Side == DoorSide.West)
            {
                AddDoorEdgePostsOnVerticalWall(postCells, postVisualPositions, roomSizeInBlocks, doorPlan, westDoorMask, 0);
            }
        }
    }

    private void AddDoorEdgePostsOnHorizontalWall(HashSet<Vector2Int> postCells, Dictionary<Vector2Int, Vector2> postVisualPositions, Vector3Int roomSizeInBlocks, RoomDoorPlan doorPlan, bool[] doorMask, int wallZ)
    {
        int width = roomSizeInBlocks.x;

        int leftCell = doorPlan.OpeningOffset - 1;
        int rightCell = doorPlan.OpeningOffset + doorPlan.OpeningWidthInBlocks;
        float wallPositionZ = (wallZ + 0.5f) * _blockSize;

        if (leftCell >= 0 && leftCell < width && doorMask[leftCell] == false)
        {
            Vector2Int cell = new Vector2Int(leftCell, wallZ);
            postCells.Add(cell);
            postVisualPositions[cell] = new Vector2(doorPlan.OpeningOffset * _blockSize, wallPositionZ);
        }

        if (rightCell >= 0 && rightCell < width && doorMask[rightCell] == false)
        {
            Vector2Int cell = new Vector2Int(rightCell, wallZ);
            postCells.Add(cell);
            postVisualPositions[cell] = new Vector2((doorPlan.OpeningOffset + doorPlan.OpeningWidthInBlocks) * _blockSize, wallPositionZ);
        }
    }

    private void AddDoorEdgePostsOnVerticalWall(HashSet<Vector2Int> postCells, Dictionary<Vector2Int, Vector2> postVisualPositions, Vector3Int roomSizeInBlocks, RoomDoorPlan doorPlan, bool[] doorMask, int wallX)
    {
        int depth = roomSizeInBlocks.z;

        int bottomCell = doorPlan.OpeningOffset - 1;
        int topCell = doorPlan.OpeningOffset + doorPlan.OpeningWidthInBlocks;
        float wallPositionX = (wallX + 0.5f) * _blockSize;

        if (bottomCell >= 0 && bottomCell < depth && doorMask[bottomCell] == false)
        {
            Vector2Int cell = new Vector2Int(wallX, bottomCell);
            postCells.Add(cell);
            postVisualPositions[cell] = new Vector2(wallPositionX, doorPlan.OpeningOffset * _blockSize);
        }

        if (topCell >= 0 && topCell < depth && doorMask[topCell] == false)
        {
            Vector2Int cell = new Vector2Int(wallX, topCell);
            postCells.Add(cell);
            postVisualPositions[cell] = new Vector2(wallPositionX, (doorPlan.OpeningOffset + doorPlan.OpeningWidthInBlocks) * _blockSize);
        }
    }

    private List<Vector2Int> BuildPerimeterCandidates(
        Vector3Int roomSizeInBlocks,
        bool[] northDoorMask,
        bool[] southDoorMask,
        bool[] eastDoorMask,
        bool[] westDoorMask,
        HashSet<Vector2Int> existingPosts
    )
    {
        int width = roomSizeInBlocks.x;
        int depth = roomSizeInBlocks.z;

        List<Vector2Int> candidates = new List<Vector2Int>();

        int southZ = 0;
        for (int x = 0; x < width; x++)
        {
            if (southDoorMask.Length == width && southDoorMask[x] == true)
            {
                continue;
            }

            Vector2Int cell = new Vector2Int(x, southZ);

            if (existingPosts.Contains(cell) == false)
            {
                candidates.Add(cell);
            }
        }

        int eastX = width - 1;
        for (int z = 1; z < depth; z++)
        {
            if (eastDoorMask.Length == depth && eastDoorMask[z] == true)
            {
                continue;
            }

            Vector2Int cell = new Vector2Int(eastX, z);

            if (existingPosts.Contains(cell) == false)
            {
                candidates.Add(cell);
            }
        }

        int northZ = depth - 1;
        for (int x = width - 2; x >= 0; x--)
        {
            if (northDoorMask.Length == width && northDoorMask[x] == true)
            {
                continue;
            }

            Vector2Int cell = new Vector2Int(x, northZ);

            if (existingPosts.Contains(cell) == false)
            {
                candidates.Add(cell);
            }
        }

        int westX = 0;
        for (int z = depth - 2; z >= 1; z--)
        {
            if (westDoorMask.Length == depth && westDoorMask[z] == true)
            {
                continue;
            }

            Vector2Int cell = new Vector2Int(westX, z);

            if (existingPosts.Contains(cell) == false)
            {
                candidates.Add(cell);
            }
        }

        return candidates;
    }

    private void AddDistributedPosts(HashSet<Vector2Int> postCells, List<Vector2Int> candidates, int additionalNeeded, int minimumDistanceInCells)
    {
        HashSet<int> usedIndices = new HashSet<int>();

        int candidateCount = candidates.Count;

        for (int index = 0; index < additionalNeeded; index++)
        {
            float step = (float)candidateCount / (float)additionalNeeded;
            float position = (index + 0.5f) * step;

            int desiredIndex = Mathf.FloorToInt(position);

            if (desiredIndex < 0)
            {
                desiredIndex = 0;
            }

            if (desiredIndex > candidateCount - 1)
            {
                desiredIndex = candidateCount - 1;
            }

            int foundIndex = FindNearestValidCandidateIndex(desiredIndex, candidates, usedIndices, postCells, minimumDistanceInCells);

            if (foundIndex == -1)
            {
                continue;
            }

            usedIndices.Add(foundIndex);
            postCells.Add(candidates[foundIndex]);
        }
    }

    private int FindNearestValidCandidateIndex(int desiredIndex, List<Vector2Int> candidates, HashSet<int> usedIndices, HashSet<Vector2Int> postCells, int minimumDistanceInCells)
    {
        int count = candidates.Count;

        for (int delta = 0; delta < count; delta++)
        {
            int forwardIndex = desiredIndex + delta;

            while (forwardIndex >= count)
            {
                forwardIndex -= count;
            }

            if (IsCandidateIndexValid(forwardIndex, candidates, usedIndices, postCells, minimumDistanceInCells) == true)
            {
                return forwardIndex;
            }

            if (delta == 0)
            {
                continue;
            }

            int backwardIndex = desiredIndex - delta;

            while (backwardIndex < 0)
            {
                backwardIndex += count;
            }

            if (IsCandidateIndexValid(backwardIndex, candidates, usedIndices, postCells, minimumDistanceInCells) == true)
            {
                return backwardIndex;
            }
        }

        return -1;
    }

    private bool IsCandidateIndexValid(int index, List<Vector2Int> candidates, HashSet<int> usedIndices, HashSet<Vector2Int> postCells, int minimumDistanceInCells)
    {
        if (usedIndices.Contains(index) == true)
        {
            return false;
        }

        Vector2Int cell = candidates[index];

        if (IsFarEnoughFromPosts(cell, postCells, minimumDistanceInCells) == false)
        {
            return false;
        }

        return true;
    }

    private bool IsFarEnoughFromPosts(Vector2Int cell, HashSet<Vector2Int> postCells, int minimumDistanceInCells)
    {
        if (minimumDistanceInCells <= 0)
        {
            return true;
        }

        foreach (Vector2Int existingCell in postCells)
        {
            int distance = Mathf.Abs(existingCell.x - cell.x) + Mathf.Abs(existingCell.y - cell.y);

            if (distance < minimumDistanceInCells)
            {
                return false;
            }
        }

        return true;
    }

    private void CreatePosts(HashSet<Vector2Int> postCells, Dictionary<Vector2Int, Vector2> postVisualPositions, Vector3Int roomSizeInBlocks, float floorSurfaceY)
    {
        foreach (Vector2Int cell in postCells)
        {
            CreatePost(cell, postVisualPositions, roomSizeInBlocks, floorSurfaceY);
        }
    }

    private void CreateSegmentsForSide(DoorSide side, Vector3Int roomSizeInBlocks, HashSet<Vector2Int> postCells, Dictionary<Vector2Int, Vector2> postVisualPositions, bool[] doorMask, float floorSurfaceY)
    {
        List<int> indices = CollectPostIndicesForSide(side, roomSizeInBlocks, postCells);

        if (indices.Count < 2)
        {
            return;
        }

        indices.Sort();

        for (int i = 0; i < indices.Count - 1; i++)
        {
            int a = indices[i];
            int b = indices[i + 1];

            if (HasDoorBetween(a, b, doorMask) == true)
            {
                continue;
            }

            CreateFenceSegment(side, roomSizeInBlocks, postVisualPositions, a, b, floorSurfaceY);
        }
    }

    private List<int> CollectPostIndicesForSide(DoorSide side, Vector3Int roomSizeInBlocks, HashSet<Vector2Int> postCells)
    {
        List<int> indices = new List<int>();

        int maxX = roomSizeInBlocks.x - 1;
        int maxZ = roomSizeInBlocks.z - 1;

        foreach (Vector2Int cell in postCells)
        {
            if (side == DoorSide.North && cell.y == maxZ)
            {
                indices.Add(cell.x);
            }

            if (side == DoorSide.South && cell.y == 0)
            {
                indices.Add(cell.x);
            }

            if (side == DoorSide.East && cell.x == maxX)
            {
                indices.Add(cell.y);
            }

            if (side == DoorSide.West && cell.x == 0)
            {
                indices.Add(cell.y);
            }
        }

        return indices;
    }

    private bool HasDoorBetween(int a, int b, bool[] doorMask)
    {
        int min = a;
        int max = b;

        if (min > max)
        {
            int temp = min;
            min = max;
            max = temp;
        }

        for (int i = min + 1; i <= max - 1; i++)
        {
            if (i < 0 || i >= doorMask.Length)
            {
                continue;
            }

            if (doorMask[i] == true)
            {
                return true;
            }
        }

        return false;
    }

    private bool[] BuildDoorMask(DoorSide side, Vector3Int roomSizeInBlocks, IReadOnlyList<RoomDoorPlan> doorPlans)
    {
        int length = GetSideLength(side, roomSizeInBlocks);
        bool[] mask = new bool[length];

        for (int doorIndex = 0; doorIndex < doorPlans.Count; doorIndex++)
        {
            RoomDoorPlan doorPlan = doorPlans[doorIndex];

            if (doorPlan.Side != side)
            {
                continue;
            }

            for (int i = 0; i < doorPlan.OpeningWidthInBlocks; i++)
            {
                int index = doorPlan.OpeningOffset + i;

                if (index < 0 || index >= length)
                {
                    continue;
                }

                mask[index] = true;
            }
        }

        return mask;
    }

    private int GetSideLength(DoorSide side, Vector3Int roomSizeInBlocks)
    {
        if (side == DoorSide.North || side == DoorSide.South)
        {
            return roomSizeInBlocks.x;
        }

        return roomSizeInBlocks.z;
    }

    private float GetFenceElementLocalPositionY(float floorSurfaceY, int heightInBlocks, bool pivotAtBase)
    {
        float heightInUnits = heightInBlocks * _blockSize;

        if (pivotAtBase == true)
        {
            return floorSurfaceY;
        }

        return floorSurfaceY + (heightInUnits * 0.5f);
    }

    private void CreatePost(Vector2Int wallCell, Dictionary<Vector2Int, Vector2> postVisualPositions, Vector3Int roomSizeInBlocks, float floorSurfaceY)
    {
        GameObject postInstance = Instantiate(_fencePostPrefab, _fencePostsRoot);

        float localPositionX = (wallCell.x + 0.5f) * _blockSize;
        float localPositionZ = (wallCell.y + 0.5f) * _blockSize;
        float visualPositionX = localPositionX;
        float visualPositionZ = localPositionZ;
        float localPositionY = GetFenceElementLocalPositionY(floorSurfaceY, _postHeightInBlocks, _postPivotAtBase);
        localPositionY += FencePostFloorLiftInBlocks * _blockSize;
        Vector3 inwardDirection = GetFencePostInwardDirection(wallCell, roomSizeInBlocks);

        Vector2 visualPosition;
        bool hasVisualPosition = postVisualPositions.TryGetValue(wallCell, out visualPosition);

        if (hasVisualPosition == true)
        {
            visualPositionX = visualPosition.x;
            visualPositionZ = visualPosition.y;
        }

        postInstance.transform.localPosition = new Vector3(localPositionX, localPositionY, localPositionZ);
        postInstance.transform.localRotation = GetFenceFaceRotation(inwardDirection);

        float scaleX = _blockSize;
        float scaleY = _postHeightInBlocks * _blockSize;
        float scaleZ = _blockSize;

        Vector3 targetSize = new Vector3(scaleX, scaleY, scaleZ);
        postInstance.transform.localScale = targetSize;

        if (_resolvedFencePostVisualPrefab != null)
        {
            HideBlockRenderers(postInstance);
            CreateFencePostVisual(visualPositionX, visualPositionZ, inwardDirection, floorSurfaceY);
        }
    }

    private void CreateFencePostVisual(float localPositionX, float localPositionZ, Vector3 inwardDirection, float floorSurfaceY)
    {
        GameObject visualInstance = Instantiate(_resolvedFencePostVisualPrefab, _fencePostsRoot);

        Bounds localBounds;
        bool hasBounds = TryGetLocalRendererBounds(visualInstance.transform, out localBounds);

        if (hasBounds == false)
        {
            throw new InvalidOperationException(nameof(Renderer));
        }

        Vector3 targetSize = GetFencePostVisualTargetSize();
        float sourceThickness = Mathf.Max(localBounds.size.x, localBounds.size.z);
        float maximumStretch = GetFenceVisualMaximumStretch();
        float horizontalScale = GetVisualAxisScale(targetSize.x, sourceThickness);
        float verticalScale = GetVisualAxisScale(targetSize.y, localBounds.size.y);
        float minimumHorizontalScale = verticalScale / maximumStretch;
        float maximumHorizontalScale = verticalScale * maximumStretch;

        if (horizontalScale < minimumHorizontalScale)
        {
            horizontalScale = minimumHorizontalScale;
        }

        if (horizontalScale > maximumHorizontalScale)
        {
            horizontalScale = maximumHorizontalScale;
        }

        float visualHeight = localBounds.size.y * verticalScale;
        Vector3 axisScale = new Vector3(
            horizontalScale,
            verticalScale,
            horizontalScale
        );
        Quaternion visualRotation = GetFenceFaceRotation(inwardDirection) * Quaternion.Euler(0f, _fencePostVisualYawOffset, 0f);
        Vector3 localPosition = new Vector3(
            localPositionX,
            GetFenceVisualLocalPositionY(floorSurfaceY, visualHeight, _postPivotAtBase),
            localPositionZ
        );

        visualInstance.transform.localScale = Vector3.Scale(visualInstance.transform.localScale, axisScale);
        visualInstance.transform.localRotation = visualRotation;
        visualInstance.transform.localPosition = localPosition - (visualRotation * Vector3.Scale(localBounds.center, axisScale));
    }

    private Vector3 GetFencePostVisualTargetSize()
    {
        float heightMultiplier = _fencePostVisualHeightMultiplier;

        if (heightMultiplier < MinVisualSize)
        {
            heightMultiplier = MinVisualSize;
        }

        float thicknessMultiplier = _fencePostVisualThicknessMultiplier;

        if (thicknessMultiplier < MinVisualSize)
        {
            thicknessMultiplier = MinVisualSize;
        }

        float thickness = _blockSize * thicknessMultiplier;
        float height = _segmentHeightInBlocks * _blockSize * heightMultiplier;

        return new Vector3(thickness, height, thickness);
    }

    private void CreateFenceSegment(DoorSide side, Vector3Int roomSizeInBlocks, Dictionary<Vector2Int, Vector2> postVisualPositions, int indexA, int indexB, float floorSurfaceY)
    {
        int lengthInCells = Mathf.Abs(indexB - indexA);

        if (lengthInCells <= 0)
        {
            return;
        }

        Vector3 positionA = GetFenceAnchorPosition(side, roomSizeInBlocks, postVisualPositions, indexA);
        Vector3 positionB = GetFenceAnchorPosition(side, roomSizeInBlocks, postVisualPositions, indexB);

        Vector3 segmentPosition = (positionA + positionB) * 0.5f;

        float localPositionY = GetFenceElementLocalPositionY(floorSurfaceY, _segmentHeightInBlocks, _segmentPivotAtBase);
        segmentPosition.y = localPositionY;

        float endGapInUnits = 0f;

        if (ShouldKeepSegmentEndGap() == true)
        {
            endGapInUnits = _segmentEndGapInBlocks * _blockSize;
        }

        float distanceBetweenPostCentersInUnits = lengthInCells * _blockSize;
        float segmentLengthInUnits = distanceBetweenPostCentersInUnits - (endGapInUnits * 2f);

        if (segmentLengthInUnits <= 0.001f)
        {
            return;
        }

        GameObject segmentInstance = Instantiate(_fenceSegmentPrefab, _fenceSegmentsRoot);

        segmentInstance.transform.localPosition = segmentPosition;

        segmentInstance.transform.localRotation = GetFenceSegmentRotation(side);

        float heightScale = _segmentHeightInBlocks * _blockSize;
        float thicknessScale = _blockSize;

        Vector3 targetSize = new Vector3(thicknessScale, heightScale, segmentLengthInUnits);
        segmentInstance.transform.localScale = targetSize;

        GameObject visualPrefab = GetFenceSegmentVisualPrefab(side, indexA, indexB);

        if (visualPrefab != null)
        {
            Vector3 visualTargetSize = GetFenceSegmentVisualTargetSize(targetSize);
            Vector3 visualPosition = segmentInstance.transform.localPosition;
            visualPosition.y = GetFenceVisualLocalPositionY(floorSurfaceY, visualTargetSize.y, _segmentPivotAtBase);

            HideBlockRenderers(segmentInstance);
            CreateFenceVisuals(visualPrefab, _fenceSegmentsRoot, visualPosition, segmentInstance.transform.localRotation, visualTargetSize);
        }
    }

    private Vector3 GetFenceSegmentVisualTargetSize(Vector3 targetSize)
    {
        float heightMultiplier = _fenceSegmentVisualHeightMultiplier;

        if (heightMultiplier < MinVisualSize)
        {
            heightMultiplier = MinVisualSize;
        }

        return new Vector3(targetSize.x, targetSize.y * heightMultiplier, targetSize.z);
    }

    private float GetFenceVisualLocalPositionY(float floorSurfaceY, float heightInUnits, bool pivotAtBase)
    {
        if (pivotAtBase == true)
        {
            return floorSurfaceY;
        }

        return floorSurfaceY + (heightInUnits * 0.5f);
    }

    private GameObject GetFenceSegmentVisualPrefab(DoorSide side, int indexA, int indexB)
    {
        if (_resolvedFenceSegmentVisualPrefabs.Count == 0)
        {
            return null;
        }

        int sideIndex = (int)side;
        int index = Mathf.Abs((sideIndex * 31) + (indexA * 17) + indexB);
        index %= _resolvedFenceSegmentVisualPrefabs.Count;

        return _resolvedFenceSegmentVisualPrefabs[index];
    }

    private void ResolveFenceSegmentVisualPrefabs()
    {
        _resolvedFenceSegmentVisualPrefabs.Clear();

        for (int prefabIndex = 0; prefabIndex < _fenceSegmentVisualPrefabs.Count; prefabIndex++)
        {
            GameObject prefab = _fenceSegmentVisualPrefabs[prefabIndex];

            if (prefab == null)
            {
                continue;
            }

            _resolvedFenceSegmentVisualPrefabs.Add(prefab);
        }

        for (int pathIndex = 0; pathIndex < _fenceSegmentVisualResourcePaths.Count; pathIndex++)
        {
            string resourcePath = _fenceSegmentVisualResourcePaths[pathIndex];

            if (string.IsNullOrWhiteSpace(resourcePath) == true)
            {
                throw new InvalidOperationException(nameof(_fenceSegmentVisualResourcePaths));
            }

            GameObject prefab = Resources.Load<GameObject>(resourcePath);

            if (prefab == null)
            {
                throw new InvalidOperationException(nameof(_fenceSegmentVisualResourcePaths));
            }

            _resolvedFenceSegmentVisualPrefabs.Add(prefab);
        }
    }

    private void ResolveFencePostVisualPrefab()
    {
        _resolvedFencePostVisualPrefab = _fencePostVisualPrefab;

        if (_resolvedFencePostVisualPrefab != null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_fencePostVisualResourcePath) == true)
        {
            return;
        }

        _resolvedFencePostVisualPrefab = Resources.Load<GameObject>(_fencePostVisualResourcePath);

        if (_resolvedFencePostVisualPrefab == null)
        {
            throw new InvalidOperationException(nameof(_fencePostVisualResourcePath));
        }
    }

    private bool ShouldCreateFencePosts()
    {
        return _postPlacement != FencePostPlacement.None;
    }

    private bool ShouldKeepSegmentEndGap()
    {
        if (ShouldCreateFencePosts() == false)
        {
            return false;
        }

        return true;
    }

    private void CreateFenceVisuals(GameObject visualPrefab, Transform rootTransform, Vector3 localPosition, Quaternion localRotation, Vector3 targetSize)
    {
        GameObject firstVisualInstance = Instantiate(visualPrefab, rootTransform);

        Bounds localBounds;
        bool hasBounds = TryGetLocalRendererBounds(firstVisualInstance.transform, out localBounds);

        if (hasBounds == false)
        {
            throw new InvalidOperationException(nameof(Renderer));
        }

        FenceVisualMetrics metrics = new FenceVisualMetrics(localBounds);
        float uniformScale = GetFenceVisualUniformScale(metrics, targetSize);
        int pieceCount = GetFenceVisualPieceCount(metrics, targetSize.z, uniformScale);
        float pieceLength = targetSize.z / pieceCount;
        Vector3 pieceTargetSize = new Vector3(targetSize.x, targetSize.y, pieceLength);
        float startOffset = targetSize.z * -0.5f;

        for (int pieceIndex = 0; pieceIndex < pieceCount; pieceIndex++)
        {
            GameObject visualInstance = firstVisualInstance;

            if (pieceIndex > 0)
            {
                visualInstance = Instantiate(visualPrefab, rootTransform);
            }

            float pieceOffset = startOffset + (pieceLength * (pieceIndex + 0.5f));
            Vector3 piecePosition = localPosition + (localRotation * new Vector3(0f, 0f, pieceOffset));
            ConfigureFenceVisual(visualInstance, piecePosition, localRotation, pieceTargetSize, metrics, uniformScale);
        }
    }

    private int GetFenceVisualPieceCount(FenceVisualMetrics metrics, float targetLength, float uniformScale)
    {
        float sourceLength = metrics.SourceLength;

        if (sourceLength < MinVisualSize)
        {
            return 1;
        }

        float maximumStretch = GetFenceVisualMaximumStretch();
        float maximumPieceLength = sourceLength * maximumStretch;

        if (maximumPieceLength < MinVisualSize)
        {
            return 1;
        }

        float uniformPieceLength = sourceLength * uniformScale;

        if (uniformPieceLength < MinVisualSize)
        {
            uniformPieceLength = maximumPieceLength;
        }

        int pieceCount = Mathf.Max(1, Mathf.RoundToInt(targetLength / uniformPieceLength));
        float pieceScale = GetVisualAxisScale(targetLength / pieceCount, sourceLength);

        if (pieceScale > maximumStretch)
        {
            pieceCount = Mathf.CeilToInt(targetLength / maximumPieceLength);
        }

        return Mathf.Max(1, pieceCount);
    }

    private void ConfigureFenceVisual(GameObject visualInstance, Vector3 localPosition, Quaternion localRotation, Vector3 targetSize, FenceVisualMetrics metrics, float uniformScale)
    {
        Vector3 baseScale = visualInstance.transform.localScale;
        Vector3 axisScale = GetFenceVisualAxisScale(metrics.LocalBounds.size, targetSize, metrics.LengthAlongX, uniformScale);
        Vector3 visualScale = new Vector3(
            baseScale.x * axisScale.x,
            baseScale.y * axisScale.y,
            baseScale.z * axisScale.z
        );
        Quaternion axisRotation = GetFenceVisualAxisRotation(metrics.LengthAlongX);
        Quaternion visualRotation = localRotation * axisRotation * Quaternion.Euler(0f, _fenceSegmentVisualYawOffset, 0f);

        visualInstance.transform.localScale = visualScale;
        visualInstance.transform.localRotation = visualRotation;
        visualInstance.transform.localPosition = localPosition - (visualRotation * Vector3.Scale(metrics.LocalBounds.center, axisScale));
    }

    private Vector3 GetFenceVisualAxisScale(Vector3 sourceSize, Vector3 targetSize, bool lengthAlongX, float uniformScale)
    {
        if (lengthAlongX == true)
        {
            return new Vector3(
                GetVisualAxisScale(targetSize.z, sourceSize.x),
                uniformScale,
                uniformScale
            );
        }

        return new Vector3(
            uniformScale,
            uniformScale,
            GetVisualAxisScale(targetSize.z, sourceSize.z)
        );
    }

    private Quaternion GetFenceVisualAxisRotation(bool lengthAlongX)
    {
        if (lengthAlongX == true)
        {
            return Quaternion.Euler(0f, -90f, 0f);
        }

        return Quaternion.identity;
    }

    private bool TryGetLocalRendererBounds(Transform rootTransform, out Bounds localBounds)
    {
        Renderer[] renderers = rootTransform.GetComponentsInChildren<Renderer>(true);
        localBounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;

        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];

            if (renderer == null)
            {
                continue;
            }

            Bounds rendererBounds = renderer.bounds;
            EncapsulateWorldPoint(rootTransform, rendererBounds.min, ref localBounds, ref hasBounds);
            EncapsulateWorldPoint(rootTransform, rendererBounds.max, ref localBounds, ref hasBounds);
            EncapsulateWorldPoint(rootTransform, new Vector3(rendererBounds.min.x, rendererBounds.min.y, rendererBounds.max.z), ref localBounds, ref hasBounds);
            EncapsulateWorldPoint(rootTransform, new Vector3(rendererBounds.min.x, rendererBounds.max.y, rendererBounds.min.z), ref localBounds, ref hasBounds);
            EncapsulateWorldPoint(rootTransform, new Vector3(rendererBounds.max.x, rendererBounds.min.y, rendererBounds.min.z), ref localBounds, ref hasBounds);
            EncapsulateWorldPoint(rootTransform, new Vector3(rendererBounds.min.x, rendererBounds.max.y, rendererBounds.max.z), ref localBounds, ref hasBounds);
            EncapsulateWorldPoint(rootTransform, new Vector3(rendererBounds.max.x, rendererBounds.min.y, rendererBounds.max.z), ref localBounds, ref hasBounds);
            EncapsulateWorldPoint(rootTransform, new Vector3(rendererBounds.max.x, rendererBounds.max.y, rendererBounds.min.z), ref localBounds, ref hasBounds);
        }

        return hasBounds;
    }

    private void EncapsulateWorldPoint(Transform rootTransform, Vector3 worldPoint, ref Bounds localBounds, ref bool hasBounds)
    {
        Vector3 localPoint = rootTransform.InverseTransformPoint(worldPoint);

        if (hasBounds == false)
        {
            localBounds = new Bounds(localPoint, Vector3.zero);
            hasBounds = true;
            return;
        }

        localBounds.Encapsulate(localPoint);
    }

    private float GetVisualAxisScale(float targetSize, float sourceSize)
    {
        if (sourceSize < MinVisualSize)
        {
            return 1f;
        }

        return targetSize / sourceSize;
    }

    private float GetFenceVisualUniformScale(FenceVisualMetrics metrics, Vector3 targetSize)
    {
        float scale = GetVisualAxisScale(targetSize.y, metrics.LocalBounds.size.y);
        float maximumStretch = GetFenceVisualMaximumStretch();

        if (scale > maximumStretch)
        {
            return maximumStretch;
        }

        return scale;
    }

    private float GetFenceVisualMaximumStretch()
    {
        float maximumStretch = _fenceSegmentVisualMaximumStretch;

        if (maximumStretch < 1f)
        {
            maximumStretch = 1f;
        }

        return maximumStretch;
    }

    private void HideBlockRenderers(GameObject blockInstance)
    {
        if (_hideFenceBlockRenderersWhenVisualsAssigned == false)
        {
            return;
        }

        Renderer[] renderers = blockInstance.GetComponentsInChildren<Renderer>(true);

        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];

            if (renderer == null)
            {
                continue;
            }

            renderer.enabled = false;
        }
    }

    private Quaternion GetFenceSegmentRotation(DoorSide side)
    {
        Vector3 inwardDirection = GetFenceInwardDirection(side);

        return GetFenceFaceRotation(inwardDirection);
    }

    private Quaternion GetFenceFaceRotation(Vector3 inwardDirection)
    {
        return Quaternion.LookRotation(inwardDirection, Vector3.up) * Quaternion.Euler(0f, FenceFaceYawOffset, 0f);
    }

    private Vector3 GetFencePostInwardDirection(Vector2Int wallCell, Vector3Int roomSizeInBlocks)
    {
        int maxX = roomSizeInBlocks.x - 1;
        int maxZ = roomSizeInBlocks.z - 1;
        Vector3 inwardDirection = Vector3.zero;

        if (wallCell.y == maxZ)
        {
            inwardDirection += GetFenceInwardDirection(DoorSide.North);
        }

        if (wallCell.y == 0)
        {
            inwardDirection += GetFenceInwardDirection(DoorSide.South);
        }

        if (wallCell.x == maxX)
        {
            inwardDirection += GetFenceInwardDirection(DoorSide.East);
        }

        if (wallCell.x == 0)
        {
            inwardDirection += GetFenceInwardDirection(DoorSide.West);
        }

        return inwardDirection.normalized;
    }

    private Vector3 GetFenceInwardDirection(DoorSide side)
    {
        if (side == DoorSide.North)
        {
            return Vector3.back;
        }

        if (side == DoorSide.South)
        {
            return Vector3.forward;
        }

        if (side == DoorSide.East)
        {
            return Vector3.left;
        }

        return Vector3.right;
    }

    private Vector3 GetFenceAnchorPosition(DoorSide side, Vector3Int roomSizeInBlocks, int alongIndex)
    {
        int maxX = roomSizeInBlocks.x - 1;
        int maxZ = roomSizeInBlocks.z - 1;

        int x = 0;
        int z = 0;

        if (side == DoorSide.North)
        {
            x = alongIndex;
            z = maxZ;
        }

        if (side == DoorSide.South)
        {
            x = alongIndex;
            z = 0;
        }

        if (side == DoorSide.East)
        {
            x = maxX;
            z = alongIndex;
        }

        if (side == DoorSide.West)
        {
            x = 0;
            z = alongIndex;
        }

        float localPositionX = (x + 0.5f) * _blockSize;
        float localPositionZ = (z + 0.5f) * _blockSize;

        return new Vector3(localPositionX, 0f, localPositionZ);
    }

    private Vector3 GetFenceAnchorPosition(DoorSide side, Vector3Int roomSizeInBlocks, Dictionary<Vector2Int, Vector2> postVisualPositions, int alongIndex)
    {
        Vector2Int cell = GetFenceAnchorCell(side, roomSizeInBlocks, alongIndex);

        Vector2 visualPosition;
        bool hasVisualPosition = postVisualPositions.TryGetValue(cell, out visualPosition);

        if (hasVisualPosition == true)
        {
            return new Vector3(visualPosition.x, 0f, visualPosition.y);
        }

        return GetFenceAnchorPosition(side, roomSizeInBlocks, alongIndex);
    }

    private Vector2Int GetFenceAnchorCell(DoorSide side, Vector3Int roomSizeInBlocks, int alongIndex)
    {
        int maxX = roomSizeInBlocks.x - 1;
        int maxZ = roomSizeInBlocks.z - 1;

        if (side == DoorSide.North)
        {
            return new Vector2Int(alongIndex, maxZ);
        }

        if (side == DoorSide.South)
        {
            return new Vector2Int(alongIndex, 0);
        }

        if (side == DoorSide.East)
        {
            return new Vector2Int(maxX, alongIndex);
        }

        return new Vector2Int(0, alongIndex);
    }

    private void CreateFloorBlock(Vector3Int blockCoordinate)
    {
        GameObject blockInstance = Instantiate(_floorBlockPrefab, _floorBlocksRoot);

        float localPositionX = (blockCoordinate.x + 0.5f) * _blockSize;
        float localPositionY = (blockCoordinate.y + 0.5f) * _blockSize;
        float localPositionZ = (blockCoordinate.z + 0.5f) * _blockSize;

        blockInstance.transform.localPosition = new Vector3(localPositionX, localPositionY, localPositionZ);
        blockInstance.transform.localRotation = Quaternion.identity;
        blockInstance.transform.localScale = new Vector3(_blockSize, _blockSize, _blockSize);
    }

    private void CreateDoorMarker(RoomDoorPlan doorPlan, Vector3Int roomSizeInBlocks, float floorSurfaceY)
    {
        GameObject markerInstance = Instantiate(_doorMarkerPrefab, _doorMarkersRoot);

        float openingCenterOffset = doorPlan.OpeningOffset + (doorPlan.OpeningWidthInBlocks * 0.5f);

        float markerLocalPositionX = 0f;
        float markerLocalPositionZ = 0f;

        if (doorPlan.Side == DoorSide.North)
        {
            markerLocalPositionX = openingCenterOffset * _blockSize;
            markerLocalPositionZ = (roomSizeInBlocks.z - 1.5f) * _blockSize;
        }

        if (doorPlan.Side == DoorSide.South)
        {
            markerLocalPositionX = openingCenterOffset * _blockSize;
            markerLocalPositionZ = 1.5f * _blockSize;
        }

        if (doorPlan.Side == DoorSide.East)
        {
            markerLocalPositionX = (roomSizeInBlocks.x - 1.5f) * _blockSize;
            markerLocalPositionZ = openingCenterOffset * _blockSize;
        }

        if (doorPlan.Side == DoorSide.West)
        {
            markerLocalPositionX = 1.5f * _blockSize;
            markerLocalPositionZ = openingCenterOffset * _blockSize;
        }

        float markerLocalPositionY = floorSurfaceY;

        markerInstance.transform.localPosition = new Vector3(markerLocalPositionX, markerLocalPositionY, markerLocalPositionZ);
        markerInstance.transform.localRotation = Quaternion.identity;
        markerInstance.transform.localScale = Vector3.one;

        RoomDoorMarker doorMarker = markerInstance.GetComponent<RoomDoorMarker>();
        doorMarker.Initialize(doorPlan.Side, doorPlan.Role, doorPlan.OpeningWidthInBlocks, doorPlan.OpeningHeightInBlocks);
    }

    private void ClearChildren(Transform rootTransform)
    {
        int childCount = rootTransform.childCount;

        for (int childIndex = childCount - 1; childIndex >= 0; childIndex--)
        {
            Transform childTransform = rootTransform.GetChild(childIndex);
            DestroyGameObject(childTransform.gameObject);
        }
    }

    private void DestroyGameObject(GameObject gameObject)
    {
        if (Application.isPlaying == false)
        {
            DestroyImmediate(gameObject);
            return;
        }

        Destroy(gameObject);
    }

    private Material GetFenceMaterial()
    {
        if (_fenceSegmentPrefab == null)
        {
            return null;
        }

        Renderer fenceRenderer = _fenceSegmentPrefab.GetComponent<Renderer>();

        if (fenceRenderer == null)
        {
            return null;
        }

        return fenceRenderer.sharedMaterial;
    }
}

