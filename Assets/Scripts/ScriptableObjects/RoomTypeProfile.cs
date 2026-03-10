using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Rooms/Room Type Profile", fileName = "RoomTypeProfile")]
public sealed class RoomTypeProfile : ScriptableObject
{
    [Header("Основное")]
    [Tooltip("Тип комнаты. Используется генератором/логикой для выбора поведения комнаты.")]
    [SerializeField] private RoomType _roomType;

    [Tooltip("Профиль шума: формирует пятна/кучи препятствий и распределение высоты.")]
    [SerializeField] private RoomNoiseProfile _noiseProfile;

    [Header("Заполнение препятствиями")]
    [Tooltip("Доля внутренних клеток пола, занятых препятствиями (0..1).")]
    [SerializeField, Range(0f, 1f)] private float _blockFillPercent = 0.45f;

    [Tooltip("Доля занятой площади, которая должна приходиться на крупные блоки 2×2 (0..1).")]
    [SerializeField, Range(0f, 1f)] private float _largeCubeAreaPercent = 0.3f;

    [Header("Высота препятствий")]
    [Tooltip("Минимальная высота столбика 1×1 (в блоках).")]
    [SerializeField, Min(1)] private int _minimumStackHeightInBlocks = 1;

    [Tooltip("Максимальная высота столбика 1×1 (в блоках).")]
    [SerializeField, Min(1)] private int _maximumStackHeightInBlocks = 8;

    [Tooltip("Степень преобразования шума в высоту.")]
    [SerializeField, Range(0.25f, 4f)] private float _heightExponent = 1.4f;

    [Header("Крупные блоки 2×2")]
    [Tooltip("Диапазон количества ярусов по вертикали для блоков 2×2.\nX — минимум, Y — максимум.")]
    [SerializeField] private Vector2Int _largeCubeStackHeightRange = new Vector2Int(1, 2);

    [Tooltip("Поворачивать блоки по оси Y на 0/90/180/270 для разнообразия.")]
    [SerializeField] private bool _randomYawRotation = true;

    [Header("Враги")]
    [Tooltip("Сколько врагов спавнить в комнате.\nX — минимум, Y — максимум.")]
    [SerializeField] private Vector2Int _enemySpawnCountRange = new Vector2Int(0, 0);

    [Tooltip("Набор префабов врагов с весами.")]
    [SerializeField] private List<WeightedPrefab> _enemyPrefabs = new List<WeightedPrefab>();

    [Tooltip("Индивидуальная высота спавна для конкретных префабов врагов.")]
    [SerializeField] private List<EnemySpawnHeight> _enemySpawnHeights = new List<EnemySpawnHeight>();

    [Header("Ресурсы (обычные объекты)")]
    [Tooltip("Сколько объектов/ресурсов спавнить.\nX — минимум, Y — максимум.")]
    [SerializeField] private Vector2Int _objectSpawnCountRange = new Vector2Int(0, 0);

    [Tooltip("Набор префабов ресурсов/объектов с весами.")]
    [SerializeField] private List<WeightedPrefab> _objectPrefabs = new List<WeightedPrefab>();

    [Header("Закоулки (POI)")]
    [Tooltip("Префабы закоулков с индивидуальными настройками (count, гарант, footprint, дистанции, соседство).")]
    [SerializeField] private List<NookPrefabConfig> _nookPrefabs = new List<NookPrefabConfig>();

    [Tooltip("Минимальный размер связного кармана (в клетках), чтобы считать его закоулком.\nМелкие щели игнорируются.")]
    [SerializeField, Min(1)] private int _nookMinimumAreaInCells = 8;

    public RoomTypeProfile CreateRuntimeInstance(RoomNoiseProfile noiseProfileOverride)
    {
        RoomTypeProfile copy = Instantiate(this);
        copy._noiseProfile = noiseProfileOverride;
        copy.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
        return copy;
    }

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
    public IReadOnlyList<EnemySpawnHeight> EnemySpawnHeights => _enemySpawnHeights;

    public Vector2Int ObjectSpawnCountRange => _objectSpawnCountRange;
    public IReadOnlyList<WeightedPrefab> ObjectPrefabs => _objectPrefabs;

    public IReadOnlyList<NookPrefabConfig> NookPrefabs => _nookPrefabs;
    public int NookMinimumAreaInCells => _nookMinimumAreaInCells;

    public float GetEnemySpawnHeight(GameObject prefab, float defaultSpawnHeight)
    {
        if (prefab == null)
        {
            return defaultSpawnHeight;
        }

        for (int index = 0; index < _enemySpawnHeights.Count; index++)
        {
            EnemySpawnHeight enemySpawnHeight = _enemySpawnHeights[index];

            if (enemySpawnHeight == null)
            {
                continue;
            }

            if (enemySpawnHeight.Prefab != prefab)
            {
                continue;
            }

            return enemySpawnHeight.SpawnHeight;
        }

        return defaultSpawnHeight;
    }

    public bool HasGuaranteedNookDemand
    {
        get
        {
            for (int index = 0; index < _nookPrefabs.Count; index++)
            {
                NookPrefabConfig config = _nookPrefabs[index];

                if (config == null)
                {
                    continue;
                }

                if (config.Guaranteed == true)
                {
                    return true;
                }

                if (config.CountRange.x > 0)
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
                NookPrefabConfig config = _nookPrefabs[index];

                if (config == null)
                {
                    continue;
                }

                if (config.FootprintRadiusInCells > best)
                {
                    best = config.FootprintRadiusInCells;
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
                NookPrefabConfig config = _nookPrefabs[index];

                if (config == null)
                {
                    continue;
                }

                if (config.WallMarginInCells > best)
                {
                    best = config.WallMarginInCells;
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
                NookPrefabConfig config = _nookPrefabs[index];

                if (config == null)
                {
                    continue;
                }

                if (config.MinimumDistanceFromCorridorInCells > best)
                {
                    best = config.MinimumDistanceFromCorridorInCells;
                }
            }

            return best;
        }
    }
}
