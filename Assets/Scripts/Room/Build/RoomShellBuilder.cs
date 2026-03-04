using System.Collections.Generic;
using UnityEngine;

public sealed class RoomShellBuilder : MonoBehaviour
{
    private enum FloorBuildMode
    {
        TiledBlocks,
        SinglePrefab
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

    [SerializeField] private Transform _fenceSegmentsRoot;
    [SerializeField] private GameObject _fenceSegmentPrefab;

    [SerializeField] private Transform _doorMarkersRoot;
    [SerializeField] private GameObject _doorMarkerPrefab;

    [SerializeField] private float _blockSize = 1f;
    [SerializeField] private bool _ceilingEnabled = false;

    [SerializeField] private bool _postsEnabled = true;

    [SerializeField, Min(4)] private int _totalPostCount = 12;
    [SerializeField, Min(0)] private int _minimumPostDistanceInCells = 2;

    [SerializeField, Min(1)] private int _postHeightInBlocks = 5;
    [SerializeField, Min(1)] private int _segmentHeightInBlocks = 2;

    [SerializeField] private bool _postPivotAtBase = false;
    [SerializeField] private bool _segmentPivotAtBase = false;

    [SerializeField, Min(0f)] private float _segmentEndGapInBlocks = 0.5f;

    [SerializeField] private float _floorSurfaceYOffset = 0f;

    public void BuildShell(Vector3Int roomSizeInBlocks, IReadOnlyList<RoomDoorPlan> doorPlans)
    {
        Clear();

        float floorSurfaceY = GetFloorSurfaceY(roomSizeInBlocks);

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

        AddCornerPosts(postCells, roomSizeInBlocks);
        AddDoorEdgePosts(postCells, roomSizeInBlocks, doorPlans, northDoorMask, southDoorMask, eastDoorMask, westDoorMask);

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

        if (_postsEnabled == true)
        {
            CreatePosts(postCells, floorSurfaceY);
        }

        CreateSegmentsForSide(DoorSide.North, roomSizeInBlocks, postCells, northDoorMask, floorSurfaceY);
        CreateSegmentsForSide(DoorSide.South, roomSizeInBlocks, postCells, southDoorMask, floorSurfaceY);
        CreateSegmentsForSide(DoorSide.East, roomSizeInBlocks, postCells, eastDoorMask, floorSurfaceY);
        CreateSegmentsForSide(DoorSide.West, roomSizeInBlocks, postCells, westDoorMask, floorSurfaceY);
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
                AddDoorEdgePostsOnHorizontalWall(postCells, roomSizeInBlocks, doorPlan, northDoorMask, roomSizeInBlocks.z - 1);
            }

            if (doorPlan.Side == DoorSide.South)
            {
                AddDoorEdgePostsOnHorizontalWall(postCells, roomSizeInBlocks, doorPlan, southDoorMask, 0);
            }

            if (doorPlan.Side == DoorSide.East)
            {
                AddDoorEdgePostsOnVerticalWall(postCells, roomSizeInBlocks, doorPlan, eastDoorMask, roomSizeInBlocks.x - 1);
            }

            if (doorPlan.Side == DoorSide.West)
            {
                AddDoorEdgePostsOnVerticalWall(postCells, roomSizeInBlocks, doorPlan, westDoorMask, 0);
            }
        }
    }

    private void AddDoorEdgePostsOnHorizontalWall(HashSet<Vector2Int> postCells, Vector3Int roomSizeInBlocks, RoomDoorPlan doorPlan, bool[] doorMask, int wallZ)
    {
        int width = roomSizeInBlocks.x;

        int leftCell = doorPlan.OpeningOffset - 1;
        int rightCell = doorPlan.OpeningOffset + doorPlan.OpeningWidthInBlocks;

        if (leftCell >= 0 && leftCell < width && doorMask[leftCell] == false)
        {
            postCells.Add(new Vector2Int(leftCell, wallZ));
        }

        if (rightCell >= 0 && rightCell < width && doorMask[rightCell] == false)
        {
            postCells.Add(new Vector2Int(rightCell, wallZ));
        }
    }

    private void AddDoorEdgePostsOnVerticalWall(HashSet<Vector2Int> postCells, Vector3Int roomSizeInBlocks, RoomDoorPlan doorPlan, bool[] doorMask, int wallX)
    {
        int depth = roomSizeInBlocks.z;

        int bottomCell = doorPlan.OpeningOffset - 1;
        int topCell = doorPlan.OpeningOffset + doorPlan.OpeningWidthInBlocks;

        if (bottomCell >= 0 && bottomCell < depth && doorMask[bottomCell] == false)
        {
            postCells.Add(new Vector2Int(wallX, bottomCell));
        }

        if (topCell >= 0 && topCell < depth && doorMask[topCell] == false)
        {
            postCells.Add(new Vector2Int(wallX, topCell));
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

    private void CreatePosts(HashSet<Vector2Int> postCells, float floorSurfaceY)
    {
        foreach (Vector2Int cell in postCells)
        {
            CreatePost(cell, floorSurfaceY);
        }
    }

    private void CreateSegmentsForSide(DoorSide side, Vector3Int roomSizeInBlocks, HashSet<Vector2Int> postCells, bool[] doorMask, float floorSurfaceY)
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

            CreateFenceSegment(side, roomSizeInBlocks, a, b, floorSurfaceY);
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

    private void CreatePost(Vector2Int wallCell, float floorSurfaceY)
    {
        GameObject postInstance = Instantiate(_fencePostPrefab, _fencePostsRoot);

        float localPositionX = (wallCell.x + 0.5f) * _blockSize;
        float localPositionZ = (wallCell.y + 0.5f) * _blockSize;
        float localPositionY = GetFenceElementLocalPositionY(floorSurfaceY, _postHeightInBlocks, _postPivotAtBase);

        postInstance.transform.localPosition = new Vector3(localPositionX, localPositionY, localPositionZ);
        postInstance.transform.localRotation = Quaternion.identity;

        float scaleX = _blockSize;
        float scaleY = _postHeightInBlocks * _blockSize;
        float scaleZ = _blockSize;

        postInstance.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
    }

    private void CreateFenceSegment(DoorSide side, Vector3Int roomSizeInBlocks, int indexA, int indexB, float floorSurfaceY)
    {
        int lengthInCells = Mathf.Abs(indexB - indexA);

        if (lengthInCells <= 0)
        {
            return;
        }

        Vector3 positionA = GetFenceAnchorPosition(side, roomSizeInBlocks, indexA);
        Vector3 positionB = GetFenceAnchorPosition(side, roomSizeInBlocks, indexB);

        Vector3 segmentPosition = (positionA + positionB) * 0.5f;

        float localPositionY = GetFenceElementLocalPositionY(floorSurfaceY, _segmentHeightInBlocks, _segmentPivotAtBase);
        segmentPosition.y = localPositionY;

        float endGapInUnits = 0f;

        if (_postsEnabled == true)
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

        Quaternion rotation = Quaternion.identity;

        if (side == DoorSide.East || side == DoorSide.West)
        {
            rotation = Quaternion.Euler(0f, 90f, 0f);
        }

        segmentInstance.transform.localRotation = rotation;

        float heightScale = _segmentHeightInBlocks * _blockSize;
        float thicknessScale = _blockSize;

        segmentInstance.transform.localScale = new Vector3(segmentLengthInUnits, heightScale, thicknessScale);
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
        doorMarker.Initialize(doorPlan.Side, doorPlan.Role, doorPlan.OpeningWidthInBlocks);
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
}

