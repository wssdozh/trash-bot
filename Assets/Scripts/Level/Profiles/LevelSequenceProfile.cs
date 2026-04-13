using UnityEngine;

[CreateAssetMenu(menuName = "Levels/Level Sequence Profile", fileName = "LevelSequenceProfile")]
public sealed class LevelSequenceProfile : ScriptableObject
{
    [Header("Профили комнат")]
    [SerializeField] private RoomTypeProfile _firstCombatProfile;

    [Header("Основной путь")]
    [Tooltip("Сколько Combat-комнат будет на основном пути между Start и Boss.\nX — минимум, Y — максимум.")]
    [SerializeField] private Vector2Int _mainCombatCountRange = new Vector2Int(2, 6);

    [Tooltip("Разрешить Treasure-комнаты на основном пути (между Combat).")]
    [SerializeField] private bool _allowTreasureOnMainPath = true;

    [Tooltip("Соотношение Treasure к Combat на основном пути.\nНапример 0.35 при 6 Combat даст примерно 2 Treasure.")]
    [SerializeField, Range(0f, 2f)] private float _mainTreasurePerCombatRatio = 0.35f;

    [Tooltip("Жёсткое ограничение количества Treasure на основном пути.\nX — минимум, Y — максимум.")]
    [SerializeField] private Vector2Int _mainTreasureCountClamp = new Vector2Int(0, 3);

    [Tooltip("Минимальная дистанция (в комнатах) между Treasure на основном пути.\n0 — могут стоять рядом.")]
    [SerializeField, Min(0)] private int _mainTreasureMinimumSpacing = 1;

    [Header("Ветвления")]
    [Tooltip("Общее количество веток в уровне.\nX — минимум, Y — максимум.")]
    [SerializeField] private Vector2Int _totalBranchCountRange = new Vector2Int(0, 2);

    [Tooltip("Длина ветки (количество комнат в цепочке).\nX — минимум, Y — максимум.")]
    [SerializeField] private Vector2Int _branchLengthRange = new Vector2Int(1, 3);

    [Tooltip("Максимальная глубина ветвлений (ветка от ветки).\n1 — только ветки от основного пути.\n2 — ветка может породить подветку.\n3+ — ещё глубже.")]
    [SerializeField, Range(1, 6)] private int _maximumBranchDepth = 2;

    [Tooltip("Шанс того, что в ветке появится дополнительное ветвление (если бюджет веток ещё есть).")]
    [SerializeField, Range(0f, 1f)] private float _nestedBranchChance = 0.35f;

    [Tooltip("Можно ли начинать ветку от Start-комнаты.")]
    [SerializeField] private bool _allowBranchesFromStart = false;

    [Tooltip("Можно ли начинать ветку от Boss-комнаты.")]
    [SerializeField] private bool _allowBranchesFromBoss = false;

    [Header("Treasure на ветках")]
    [Tooltip("Разрешить Treasure внутри веток (не только в конце).")]
    [SerializeField] private bool _allowTreasureInsideBranches = true;

    [Tooltip("Шанс того, что очередная комната ветки будет Treasure (если включено).")]
    [SerializeField, Range(0f, 1f)] private float _branchTreasureChance = 0.35f;

    [Tooltip("Делать конечную комнату ветки Treasure.")]
    [SerializeField] private bool _branchTerminalIsTreasure = true;

    [Header("Ограничения дверей")]
    [Tooltip("Максимум исходящих соединений из одной комнаты.\nДолжно укладываться в лимит дверей комнаты (обычно до 4).")]
    [SerializeField, Range(1, 3)] private int _maximumChildrenPerRoom = 2;

    public RoomTypeProfile FirstCombatProfile => _firstCombatProfile;

    public Vector2Int MainCombatCountRange => _mainCombatCountRange;

    public bool AllowTreasureOnMainPath => _allowTreasureOnMainPath;
    public float MainTreasurePerCombatRatio => _mainTreasurePerCombatRatio;
    public Vector2Int MainTreasureCountClamp => _mainTreasureCountClamp;
    public int MainTreasureMinimumSpacing => _mainTreasureMinimumSpacing;

    public Vector2Int TotalBranchCountRange => _totalBranchCountRange;
    public Vector2Int BranchLengthRange => _branchLengthRange;
    public int MaximumBranchDepth => _maximumBranchDepth;
    public float NestedBranchChance => _nestedBranchChance;

    public bool AllowBranchesFromStart => _allowBranchesFromStart;
    public bool AllowBranchesFromBoss => _allowBranchesFromBoss;

    public bool AllowTreasureInsideBranches => _allowTreasureInsideBranches;
    public float BranchTreasureChance => _branchTreasureChance;
    public bool BranchTerminalIsTreasure => _branchTerminalIsTreasure;

    public int MaximumChildrenPerRoom => _maximumChildrenPerRoom;
}
