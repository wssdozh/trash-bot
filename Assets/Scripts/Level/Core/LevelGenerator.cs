using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LevelGenerator : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private Transform _roomsRoot;
    [SerializeField] private Transform _corridorsRoot;

    [Header("Profiles")]
    [SerializeField] private LevelSequenceProfile _levelSequenceProfile;
    [SerializeField] private LevelRoomPrefabLibrary _roomPrefabLibrary;

    [Header("Builders")]
    [SerializeField] private LevelCorridorBuilder _corridorBuilder;

    [Header("Seed")]
    [SerializeField] private int _levelSeed = 1;

    [Header("Runtime")]
    [SerializeField] private bool _generateOnAwake = true;
    [SerializeField] private bool _randomSeedOnAwake = true;
    [SerializeField, Min(1)] private int _runtimeAttempts = 8;
    [SerializeField, Min(0)] private int _seedMin = 0;
    [SerializeField, Min(0)] private int _seedMax = int.MaxValue;
    [SerializeField] private bool _removeLegacyEnvironment = true;

    [Header("Room Runtime")]
    [SerializeField] private bool _streamRooms = true;
    [SerializeField] private Transform _player;
    [SerializeField, Min(0f)] private float _enemyBorderGap = 0.35f;
    [SerializeField, Min(0f)] private float _roomEnableDistance = 35f;
    [SerializeField, Min(0f)] private float _roomDisableDistance = 45f;
    [SerializeField, Min(0f)] private float _roomStreamDelay = 0.5f;

    [Header("Attempts")]
    [SerializeField, Min(1)] private int _generationAttempts = 3;
    [SerializeField, Min(0)] private int _maximumRoomRegenerateAttempts = 8;

    [Header("Corridors")]
    [SerializeField, Min(0)] private int _corridorMinimumLengthInBlocks = 6;
    [SerializeField, Min(0)] private int _corridorMaximumLengthInBlocks = 30;
    [SerializeField, Min(0)] private int _corridorLengthJitterInBlocks = 8;
    [SerializeField, Min(1)] private int _corridorWidthInBlocks = 3;
    [SerializeField, Min(0)] private int _corridorCollisionExtraWidthInBlocks = 1;
    [SerializeField, Min(0)] private int _corridorIgnoreEndsInBlocks = 2;

    [Header("Collisions")]
    [SerializeField, Min(0)] private int _roomSpacingPaddingInBlocks = 3;
    [SerializeField] private bool _disallowCorridorIntersections = true;

    [Header("Layout")]
    [SerializeField] private bool _randomizeRootRotation = true;

    [Header("Grid Snap")]
    [SerializeField] private bool _snapRoomsToGrid = true;
    [SerializeField, Min(0.01f)] private float _roomGridStepInBlocks = 0.5f;

    [Header("Treasure Ratio")]
    [SerializeField, Min(0)] private int _treasurePerCombatNumerator = 1;
    [SerializeField, Min(1)] private int _treasurePerCombatDenominator = 2;

    private readonly LevelGenerationContext _generationContext = new LevelGenerationContext(
        nodeCapacity: 128,
        edgeCapacity: 256,
        placedRoomCapacity: 128,
        corridorCapacity: 256
    );

    private readonly LevelPlanBuilder _planBuilder = new LevelPlanBuilder();
    private readonly LevelRoomShellInstantiator _roomShellInstantiator = new LevelRoomShellInstantiator();
    private readonly LevelRoomPlacer _roomPlacer = new LevelRoomPlacer();
    private readonly LevelCorridorExecutor _corridorExecutor = new LevelCorridorExecutor();
    private readonly LevelRoomFinalizer _roomFinalizer = new LevelRoomFinalizer();

    public bool HasGeneratedLevel => _generationContext.PlacedRooms.Count > 0;

    public event Action GenerationCompleted;

    private void Awake()
    {
        if (_generateOnAwake == false)
        {
            return;
        }

        CleanupLegacyEnvironment();

        TryGenerateRuntime();
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        if (_roomsRoot == null)
            throw new InvalidOperationException(nameof(_roomsRoot));

        if (_corridorsRoot == null)
            throw new InvalidOperationException(nameof(_corridorsRoot));

        if (_levelSequenceProfile == null)
            throw new InvalidOperationException(nameof(_levelSequenceProfile));

        if (_roomPrefabLibrary == null)
            throw new InvalidOperationException(nameof(_roomPrefabLibrary));

        if (_corridorBuilder == null)
            throw new InvalidOperationException(nameof(_corridorBuilder));

        int attempts = Mathf.Clamp(_generationAttempts, 1, 64);

        for (int attemptIndex = 0; attemptIndex < attempts; attemptIndex++)
        {

            Clear();

            int attemptSeed = _levelSeed + (attemptIndex * 10007);
            System.Random random = new System.Random(attemptSeed);

            if (TryGenerateOnce(random) == true)
            {
                if (Application.isPlaying == true)
                {
                    InvokeGenerationCompletedEvent();
                }

                BuildRuntimeNavMesh();

                if (Application.isPlaying == true)
                {
                    ConfigureRoomStreaming();
                }

                return;
            }

        }

        Clear();
        Debug.LogWarning("LevelGenerator: generation failed after all attempts.");
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        _generationContext.ClearData();
        ClearRuntimeNavMesh();
        ClearRoomStreaming();

        LevelGeneratorUtility.DestroyChildren(_roomsRoot);
        LevelGeneratorUtility.DestroyChildren(_corridorsRoot);
    }

    private bool TryGenerateOnce(System.Random random)
    {
        LevelTreasureRatio treasureRatio = new LevelTreasureRatio(_treasurePerCombatNumerator, _treasurePerCombatDenominator);

        LevelNode root = _planBuilder.BuildPlan(_generationContext, random, _levelSequenceProfile, treasureRatio);

        if (_roomShellInstantiator.InstantiateRoomsShellOnly(
            _generationContext,
            random,
            _roomsRoot,
            _roomPrefabLibrary,
            _levelSequenceProfile,
            _maximumRoomRegenerateAttempts
        ) == false)
        {
            return false;
        }

        LevelPlacementSettings placementSettings = new LevelPlacementSettings(
            randomizeRootRotation: _randomizeRootRotation,
            snapRoomsToGrid: _snapRoomsToGrid,
            roomGridStepInBlocks: _roomGridStepInBlocks,
            roomSpacingPaddingInBlocks: _roomSpacingPaddingInBlocks,
            disallowCorridorIntersections: _disallowCorridorIntersections,
            corridorMinimumLengthInBlocks: _corridorMinimumLengthInBlocks,
            corridorMaximumLengthInBlocks: _corridorMaximumLengthInBlocks,
            corridorLengthJitterInBlocks: _corridorLengthJitterInBlocks,
            corridorWidthInBlocks: _corridorWidthInBlocks,
            corridorCollisionExtraWidthInBlocks: _corridorCollisionExtraWidthInBlocks,
            corridorIgnoreEndsInBlocks: _corridorIgnoreEndsInBlocks
        );

        if (_roomPlacer.PlaceAllRooms(_generationContext, root, random, _corridorBuilder, placementSettings) == false)
            return false;

        if (_corridorExecutor.BuildCorridors(_generationContext, _corridorsRoot, _corridorBuilder) == false)
            return false;

        _roomFinalizer.FinalizeInteriors(_generationContext, _enemyBorderGap);

        return true;
    }

    private void TryGenerateRuntime()
    {
        int attempts = Mathf.Clamp(_runtimeAttempts, 1, 64);

        for (int attemptIndex = 0; attemptIndex < attempts; attemptIndex++)
        {

            if (_randomSeedOnAwake == true)
            {
                _levelSeed = GetRandomSeed();
            }

            Generate();

            if (_roomsRoot.childCount > 0)
            {
                return;
            }

        }

        throw new InvalidOperationException(nameof(_levelSeed));
    }

    private void InvokeGenerationCompletedEvent()
    {
        Action generationCompleted = GenerationCompleted;

        if (generationCompleted == null)
        {
            return;
        }

        generationCompleted.Invoke();
    }

    private void BuildRuntimeNavMesh()
    {
        LevelRuntimeNavMesh levelRuntimeNavMesh = GetOrCreateRuntimeNavMesh();
        levelRuntimeNavMesh.RequestBuild();
    }

    private void ConfigureRoomStreaming()
    {
        LevelRoomStreamer levelRoomStreamer = GetComponent<LevelRoomStreamer>();

        if (_streamRooms == false)
        {
            if (levelRoomStreamer != null)
            {
                levelRoomStreamer.ClearRooms();
                levelRoomStreamer.enabled = false;
            }

            return;
        }

        RoomRuntimeState[] roomRuntimeStates = CollectRoomStates();

        if (roomRuntimeStates.Length == 0)
        {
            if (levelRoomStreamer != null)
            {
                levelRoomStreamer.ClearRooms();
                levelRoomStreamer.enabled = false;
            }

            return;
        }

        if (levelRoomStreamer == null)
        {
            levelRoomStreamer = gameObject.AddComponent<LevelRoomStreamer>();
        }

        levelRoomStreamer.Setup(_player, _roomEnableDistance, _roomDisableDistance, _roomStreamDelay, roomRuntimeStates);
    }

    private void ClearRuntimeNavMesh()
    {
        LevelRuntimeNavMesh levelRuntimeNavMesh = GetComponent<LevelRuntimeNavMesh>();

        if (levelRuntimeNavMesh == null)
        {
            return;
        }

        levelRuntimeNavMesh.ClearData();
    }

    private void ClearRoomStreaming()
    {
        LevelRoomStreamer levelRoomStreamer = GetComponent<LevelRoomStreamer>();

        if (levelRoomStreamer == null)
        {
            return;
        }

        levelRoomStreamer.ClearRooms();
        levelRoomStreamer.enabled = false;
    }

    private LevelRuntimeNavMesh GetOrCreateRuntimeNavMesh()
    {
        LevelRuntimeNavMesh levelRuntimeNavMesh = GetComponent<LevelRuntimeNavMesh>();

        if (levelRuntimeNavMesh != null)
        {
            return levelRuntimeNavMesh;
        }

        return gameObject.AddComponent<LevelRuntimeNavMesh>();
    }

    private RoomRuntimeState[] CollectRoomStates()
    {
        RoomRuntimeState[] roomRuntimeStates = new RoomRuntimeState[_generationContext.PlacedRooms.Count];
        int count = 0;

        for (int index = 0; index < _generationContext.PlacedRooms.Count; index++)
        {
            PlacedRoomInfo placedRoomInfo = _generationContext.PlacedRooms[index];

            if (placedRoomInfo == null)
            {
                continue;
            }

            if (placedRoomInfo.Node == null)
            {
                continue;
            }

            if (placedRoomInfo.Node.RoomInstance == null)
            {
                continue;
            }

            RoomRuntimeState roomRuntimeState = placedRoomInfo.Node.RoomInstance.GetComponent<RoomRuntimeState>();

            if (roomRuntimeState == null)
            {
                continue;
            }

            roomRuntimeStates[count] = roomRuntimeState;
            count += 1;
        }

        if (count == roomRuntimeStates.Length)
        {
            return roomRuntimeStates;
        }

        RoomRuntimeState[] compactStates = new RoomRuntimeState[count];

        for (int index = 0; index < count; index++)
        {
            compactStates[index] = roomRuntimeStates[index];
        }

        return compactStates;
    }

    private int GetRandomSeed()
    {
        int minSeed = Mathf.Max(_seedMin, 0);
        int maxSeed = Mathf.Max(_seedMax, minSeed);

        if (maxSeed == int.MaxValue)
        {

            if (minSeed == int.MaxValue)
            {
                return int.MaxValue;
            }

            return UnityEngine.Random.Range(minSeed, int.MaxValue);

        }

        return UnityEngine.Random.Range(minSeed, maxSeed + 1);
    }

    private void CleanupLegacyEnvironment()
    {
        if (_removeLegacyEnvironment == false)
        {
            return;
        }

        Scene activeScene = gameObject.scene;
        GameObject[] rootObjects = activeScene.GetRootGameObjects();

        for (int rootIndex = rootObjects.Length - 1; rootIndex >= 0; rootIndex--)
        {

            GameObject rootObject = rootObjects[rootIndex];

            if (rootObject == gameObject)
            {
                continue;
            }

            bool isWorldRoot = string.Equals(rootObject.name, "--------WORLD-------------", StringComparison.Ordinal);
            bool isPropsRoot = string.Equals(rootObject.name, "--------PROPS--------", StringComparison.Ordinal);

            if (isWorldRoot == false && isPropsRoot == false)
            {
                continue;
            }

            Destroy(rootObject);

        }
    }

    public Vector3 GetStartRoomCenter()
    {
        PlacedRoomInfo startRoomInfo = FindStartRoomInfo();

        if (startRoomInfo == null)
        {
            throw new InvalidOperationException(nameof(startRoomInfo));
        }

        if (startRoomInfo.Node == null)
        {
            throw new InvalidOperationException(nameof(startRoomInfo.Node));
        }

        if (startRoomInfo.Node.RoomInstance == null)
        {
            throw new InvalidOperationException(nameof(startRoomInfo.Node.RoomInstance));
        }

        return ResolveStartRoomCenter(startRoomInfo);
    }

    private PlacedRoomInfo FindStartRoomInfo()
    {
        for (int roomIndex = 0; roomIndex < _generationContext.PlacedRooms.Count; roomIndex++)
        {

            PlacedRoomInfo placedRoomInfo = _generationContext.PlacedRooms[roomIndex];

            if (placedRoomInfo == null)
            {
                continue;
            }

            LevelNode levelNode = placedRoomInfo.Node;

            if (levelNode == null)
            {
                continue;
            }

            if (levelNode.RoomType == RoomType.Start)
            {
                return placedRoomInfo;
            }

        }

        return null;
    }

    private Vector3 ResolveStartRoomCenter(PlacedRoomInfo startRoomInfo)
    {
        Vector3 roomCenterPosition = startRoomInfo.SolidBounds.center;
        Transform roomTransform = startRoomInfo.Node.RoomInstance.transform;
        Transform floorRoot = roomTransform.Find("FloorBlockRoot");

        if (floorRoot == null)
        {
            roomCenterPosition.y = startRoomInfo.SolidBounds.min.y;

            return roomCenterPosition;
        }

        roomCenterPosition.y = ResolveCenterHeight(floorRoot, roomCenterPosition);

        return roomCenterPosition;
    }

    private float ResolveCenterHeight(Transform floorRoot, Vector3 roomCenterPosition)
    {
        if (floorRoot.childCount == 0)
        {
            return floorRoot.position.y;
        }

        Transform nearestFloorTransform = floorRoot.GetChild(0);
        float nearestSqrDistance = float.MaxValue;

        for (int childIndex = 0; childIndex < floorRoot.childCount; childIndex++)
        {

            Transform floorBlockTransform = floorRoot.GetChild(childIndex);
            Vector3 floorBlockPosition = floorBlockTransform.position;

            float distanceX = floorBlockPosition.x - roomCenterPosition.x;
            float distanceZ = floorBlockPosition.z - roomCenterPosition.z;
            float sqrDistance = (distanceX * distanceX) + (distanceZ * distanceZ);

            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearestFloorTransform = floorBlockTransform;
            }

        }

        return nearestFloorTransform.position.y;
    }
}
