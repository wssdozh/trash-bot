using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Rooms/Room Type Profile", fileName = "RoomTypeProfile")]
public sealed class RoomTypeProfile : ScriptableObject
{
    [Header("Main")]
    [SerializeField] private RoomType _roomType;
    [SerializeField] private RoomNoiseProfile _noiseProfile;

    [Header("Fill")]
    [SerializeField, Range(0f, 1f)] private float _blockFillPercent = 0.45f;
    [SerializeField, Range(0f, 1f)] private float _largeCubeAreaPercent = 0.3f;

    [Header("Height")]
    [SerializeField, Min(1)] private int _minimumStackHeightInBlocks = 1;
    [SerializeField, Min(1)] private int _maximumStackHeightInBlocks = 8;
    [SerializeField, Range(0.25f, 4f)] private float _heightExponent = 1.4f;

    [Header("Large Cubes")]
    [SerializeField] private Vector2Int _largeCubeStackHeightRange = new Vector2Int(1, 2);
    [SerializeField] private bool _randomYawRotation = true;

    [Header("Enemies")]
    [SerializeField] private Vector2Int _enemySpawnCountRange = new Vector2Int(0, 0);
    [SerializeField] private List<WeightedPrefab> _enemyPrefabs = new List<WeightedPrefab>();
    [SerializeField, HideInInspector] private List<EnemySpawnHeight> _enemySpawnHeights = new List<EnemySpawnHeight>();

    [Header("Objects")]
    [SerializeField] private Vector2Int _objectSpawnCountRange = new Vector2Int(0, 0);
    [SerializeField] private List<WeightedPrefab> _objectPrefabs = new List<WeightedPrefab>();

    [Header("Nooks")]
    [SerializeField] private List<NookPrefabConfig> _nookPrefabs = new List<NookPrefabConfig>();
    [SerializeField, Min(1)] private int _nookMinimumAreaInCells = 8;

    public RoomType RoomType => _roomType;
    public RoomNoiseProfile NoiseProfile => _noiseProfile;
    public float BlockFillPercent => _blockFillPercent;
    public float LargeCubeAreaPercent => _largeCubeAreaPercent;
    public int MinimumStackHeightInBlocks => _minimumStackHeightInBlocks;
    public int MaximumStackHeightInBlocks => _maximumStackHeightInBlocks;
    public float HeightExponent => _heightExponent;
    public Vector2Int LargeCubeStackHeightRange => _largeCubeStackHeightRange;
    public bool RandomYawRotation => _randomYawRotation;
    public Vector2Int EnemySpawnCountRange => _enemySpawnCountRange;
    public IReadOnlyList<WeightedPrefab> EnemyPrefabs => _enemyPrefabs;
    public Vector2Int ObjectSpawnCountRange => _objectSpawnCountRange;
    public IReadOnlyList<WeightedPrefab> ObjectPrefabs => _objectPrefabs;
    public IReadOnlyList<NookPrefabConfig> NookPrefabs => _nookPrefabs;
    public int NookMinimumAreaInCells => _nookMinimumAreaInCells;

    private void OnValidate()
    {
        MigrateEnemySpawnHeights();
    }

    public RoomTypeProfile CreateRuntimeInstance(RoomNoiseProfile noiseProfileOverride)
    {
        RoomTypeProfile copy = Instantiate(this);
        copy._noiseProfile = noiseProfileOverride;
        copy.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;

        return copy;
    }

    public float GetEnemySpawnHeight(GameObject prefab, float defaultSpawnHeight)
    {
        MigrateEnemySpawnHeights();

        if (prefab == null)
        {
            return defaultSpawnHeight;
        }

        for (int index = 0; index < _enemyPrefabs.Count; index++)
        {
            WeightedPrefab weightedPrefab = _enemyPrefabs[index];

            if (weightedPrefab == null)
            {
                continue;
            }

            if (weightedPrefab.Prefab != prefab)
            {
                continue;
            }

            return weightedPrefab.SpawnHeight;
        }

        return defaultSpawnHeight;
    }

    public bool HasGuaranteedNookDemand
    {
        get
        {
            for (int index = 0; index < _nookPrefabs.Count; index++)
            {
                NookPrefabConfig nookPrefabConfig = _nookPrefabs[index];

                if (nookPrefabConfig == null)
                {
                    continue;
                }

                if (nookPrefabConfig.Guaranteed)
                {
                    return true;
                }

                if (nookPrefabConfig.CountRange.x > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public int MaximumNookFootprintRadiusInCells
    {
        get
        {
            int best = 0;

            for (int index = 0; index < _nookPrefabs.Count; index++)
            {
                NookPrefabConfig nookPrefabConfig = _nookPrefabs[index];

                if (nookPrefabConfig == null)
                {
                    continue;
                }

                if (nookPrefabConfig.FootprintRadiusInCells > best)
                {
                    best = nookPrefabConfig.FootprintRadiusInCells;
                }
            }

            return best;
        }
    }

    public int MaximumNookWallMarginInCells
    {
        get
        {
            int best = 0;

            for (int index = 0; index < _nookPrefabs.Count; index++)
            {
                NookPrefabConfig nookPrefabConfig = _nookPrefabs[index];

                if (nookPrefabConfig == null)
                {
                    continue;
                }

                if (nookPrefabConfig.WallMarginInCells > best)
                {
                    best = nookPrefabConfig.WallMarginInCells;
                }
            }

            return best;
        }
    }

    public int MaximumNookMinimumDistanceFromCorridorInCells
    {
        get
        {
            int best = 0;

            for (int index = 0; index < _nookPrefabs.Count; index++)
            {
                NookPrefabConfig nookPrefabConfig = _nookPrefabs[index];

                if (nookPrefabConfig == null)
                {
                    continue;
                }

                if (nookPrefabConfig.MinimumDistanceFromCorridorInCells > best)
                {
                    best = nookPrefabConfig.MinimumDistanceFromCorridorInCells;
                }
            }

            return best;
        }
    }

    private void MigrateEnemySpawnHeights()
    {
        if (_enemySpawnHeights == null)
        {
            return;
        }

        if (_enemySpawnHeights.Count == 0)
        {
            return;
        }

        for (int index = 0; index < _enemySpawnHeights.Count; index++)
        {
            EnemySpawnHeight enemySpawnHeight = _enemySpawnHeights[index];

            if (enemySpawnHeight == null)
            {
                continue;
            }

            GameObject prefab = enemySpawnHeight.Prefab;

            if (prefab == null)
            {
                continue;
            }

            WeightedPrefab weightedPrefab = FindEnemyPrefab(prefab);

            if (weightedPrefab == null)
            {
                continue;
            }

            weightedPrefab.SetSpawnHeight(enemySpawnHeight.SpawnHeight);
        }

        _enemySpawnHeights.Clear();
    }

    private WeightedPrefab FindEnemyPrefab(GameObject prefab)
    {
        for (int index = 0; index < _enemyPrefabs.Count; index++)
        {
            WeightedPrefab weightedPrefab = _enemyPrefabs[index];

            if (weightedPrefab == null)
            {
                continue;
            }

            if (weightedPrefab.Prefab == prefab)
            {
                return weightedPrefab;
            }
        }

        return null;
    }
}
