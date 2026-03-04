using System.Collections.Generic;
using UnityEngine;

public sealed class RoomGenerator : MonoBehaviour
{
    [SerializeField] private Vector3Int _roomSizeInBlocks = new Vector3Int(12, 7, 12);
    [SerializeField] private int _randomSeed = 1;

    [Header("Door Roles")]
    [SerializeField] private bool _entranceDoorEnabled = true;
    [SerializeField] private bool _exitDoorEnabled = true;

    [SerializeField] private RoomTypeProfile _roomTypeProfile;

    [SerializeField] private RoomDoorPlanner _roomDoorPlanner;
    [SerializeField] private RoomShellBuilder _roomShellBuilder;
    [SerializeField] private RoomPassagePlanner _roomPassagePlanner;
    [SerializeField] private RoomInteriorBlockFiller _roomInteriorBlockFiller;
    [SerializeField] private RoomContentSpawner _roomContentSpawner;
    [SerializeField] private RoomNookSpawner _roomNookSpawner;

    [SerializeField] private bool _combineInteriorChunks = true;
    [SerializeField] private RoomInteriorChunkCombiner _roomInteriorChunkCombiner;

    private readonly List<RoomDoorPlan> _cachedDoorPlans = new List<RoomDoorPlan>(8);
    private readonly List<RoomDoorMarker> _doorMarkers = new List<RoomDoorMarker>(8);

    private bool _hasRuntimeSeed = false;
    private int _runtimeSeed = 1;

    private bool _hasRuntimeDoorCountOverride = false;
    private int _runtimeMinimumDoorCount = 0;
    private int _runtimeMaximumDoorCount = 0;

    private bool _hasGeneratedShell = false;
    private int _cachedSeed = 1;

    public IReadOnlyList<RoomDoorMarker> DoorMarkers => _doorMarkers;
    public bool HasGeneratedShell => _hasGeneratedShell;

    public void SetRuntimeSeed(int seed)
    {
        _runtimeSeed = seed;
        _hasRuntimeSeed = true;
    }

    public void ClearRuntimeSeed()
    {
        _hasRuntimeSeed = false;
    }

    public void SetDoorRolesEnabled(bool entranceDoorEnabled, bool exitDoorEnabled)
    {
        _entranceDoorEnabled = entranceDoorEnabled;
        _exitDoorEnabled = exitDoorEnabled;
    }

    public void SetRuntimeDoorCountRange(int minimumDoorCount, int maximumDoorCount)
    {
        _runtimeMinimumDoorCount = minimumDoorCount;
        _runtimeMaximumDoorCount = maximumDoorCount;
        _hasRuntimeDoorCountOverride = true;
    }

    public void ClearRuntimeDoorCountRange()
    {
        _runtimeMinimumDoorCount = 0;
        _runtimeMaximumDoorCount = 0;
        _hasRuntimeDoorCountOverride = false;
    }

    [ContextMenu("Generate (Full)")]
    public void Generate()
    {
        GenerateShellOnly();
        GenerateInteriorFromShell();
    }

    [ContextMenu("Generate (Shell Only)")]
    public void GenerateShellOnly()
    {
        if (_roomTypeProfile == null)
        {
            return;
        }

        int seed = _hasRuntimeSeed == true ? _runtimeSeed : _randomSeed;
        _cachedSeed = seed;

        System.Random random = new System.Random(seed);

        Clear();

        RoomNoiseProfile noiseProfile = _roomTypeProfile.NoiseProfile;

        if (noiseProfile != null)
        {
            noiseProfile.SetRuntimeSeed(seed);
        }

        List<RoomDoorPlan> doorPlans;

        if (_hasRuntimeDoorCountOverride == true)
        {
            doorPlans = _roomDoorPlanner.CreateDoorPlans(
                _roomSizeInBlocks,
                _roomTypeProfile,
                _entranceDoorEnabled,
                _exitDoorEnabled,
                _runtimeMinimumDoorCount,
                _runtimeMaximumDoorCount,
                random
            );
        }
        else
        {
            doorPlans = _roomDoorPlanner.CreateDoorPlans(
                _roomSizeInBlocks,
                _roomTypeProfile,
                _entranceDoorEnabled,
                _exitDoorEnabled,
                random
            );
        }

        _cachedDoorPlans.Clear();

        for (int index = 0; index < doorPlans.Count; index++)
        {
            _cachedDoorPlans.Add(doorPlans[index]);
        }

        _roomShellBuilder.BuildShell(_roomSizeInBlocks, _cachedDoorPlans);
        CreateDoorMarkers();

        if (noiseProfile != null)
        {
            noiseProfile.ClearRuntimeSeed();
        }

        _hasGeneratedShell = true;
    }

    [ContextMenu("Generate (Interior From Shell)")]
    public void GenerateInteriorFromShell()
    {
        if (_roomTypeProfile == null)
        {
            return;
        }

        if (_hasGeneratedShell == false)
        {
            GenerateShellOnly();

            if (_hasGeneratedShell == false)
            {
                return;
            }
        }

        RoomNoiseProfile noiseProfile = _roomTypeProfile.NoiseProfile;

        if (noiseProfile != null)
        {
            noiseProfile.SetRuntimeSeed(_cachedSeed);
        }

        System.Random random = new System.Random(_cachedSeed);

        HashSet<Vector2Int> reservedFloorCells = _roomPassagePlanner.CreateReservedFloorCells(_roomSizeInBlocks, _cachedDoorPlans, _roomTypeProfile);
        RoomFloorOccupancy floorOccupancy = _roomInteriorBlockFiller.Fill(_roomSizeInBlocks, reservedFloorCells, _roomTypeProfile, random);

        if (_combineInteriorChunks == true && _roomInteriorChunkCombiner != null)
        {
            _roomInteriorChunkCombiner.ClearCombined();
            _roomInteriorChunkCombiner.Combine();
        }

        _roomContentSpawner.Spawn(_roomTypeProfile, _roomSizeInBlocks, floorOccupancy, reservedFloorCells, _cachedDoorPlans, random);

        bool hasGuaranteed = _roomPassagePlanner.TryGetGuaranteedNookCell(out Vector2Int guaranteedCell);

        _roomNookSpawner.Spawn(
            _roomTypeProfile,
            _roomSizeInBlocks,
            floorOccupancy,
            reservedFloorCells,
            hasGuaranteed,
            guaranteedCell,
            random
        );

        if (noiseProfile != null)
        {
            noiseProfile.ClearRuntimeSeed();
        }
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        _hasGeneratedShell = false;
        _cachedDoorPlans.Clear();
        _doorMarkers.Clear();

        if (_roomShellBuilder != null)
        {
            _roomShellBuilder.Clear();
        }

        if (_roomInteriorBlockFiller != null)
        {
            _roomInteriorBlockFiller.Clear();
        }

        if (_roomContentSpawner != null)
        {
            _roomContentSpawner.Clear();
        }

        if (_roomNookSpawner != null)
        {
            _roomNookSpawner.Clear();
        }

        if (_combineInteriorChunks == true && _roomInteriorChunkCombiner != null)
        {
            _roomInteriorChunkCombiner.ClearCombined();
        }
    }

    public void SetSeed(int seed)
    {
        _randomSeed = seed;
    }

    private void CreateDoorMarkers()
    {
        _doorMarkers.Clear();

        RoomDoorMarker[] markers = GetComponentsInChildren<RoomDoorMarker>(true);

        for (int index = 0; index < markers.Length; index++)
        {
            RoomDoorMarker marker = markers[index];

            if (marker == null)
            {
                continue;
            }

            _doorMarkers.Add(marker);
        }
    }
}
