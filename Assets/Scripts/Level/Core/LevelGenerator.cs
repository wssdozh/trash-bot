using System;
using UnityEngine;

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
                return;

        }

        Clear();
        Debug.LogWarning("LevelGenerator: generation failed after all attempts.");
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        _generationContext.ClearData();

        LevelGeneratorUtility.DestroyChildren(_roomsRoot);
        LevelGeneratorUtility.DestroyChildren(_corridorsRoot);
    }

    private bool TryGenerateOnce(System.Random random)
    {
        LevelTreasureRatio treasureRatio = new LevelTreasureRatio(_treasurePerCombatNumerator, _treasurePerCombatDenominator);

        LevelNode root = _planBuilder.BuildPlan(_generationContext, random, _levelSequenceProfile, treasureRatio);

        if (_roomShellInstantiator.InstantiateRoomsShellOnly(_generationContext, random, _roomsRoot, _roomPrefabLibrary, _maximumRoomRegenerateAttempts) == false)
            return false;

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

        _roomFinalizer.FinalizeInteriors(_generationContext);

        return true;
    }
}
