using System;
using UnityEngine;

[Serializable]
public sealed class NookPrefabConfig
{
    [Header("Префаб и вес")]
    [Tooltip("Префаб закоулка (POI), который будет ставиться в подходящих карманах/местах.")]
    [SerializeField] private GameObject _prefab;

    [Tooltip("Вес для случайного выбора.\nЧем больше вес — тем чаще этот закоулок выбирается (когда есть выбор).")]
    [SerializeField, Min(1)] private int _weight = 1;

    [Header("Количество на комнате")]
    [Tooltip("Если включено — этот закоулок должен появиться минимум 1 раз (если физически найдётся место).\nЕсли места нет — будет пропущен.")]
    [SerializeField] private bool _guaranteed = false;

    [Tooltip("Сколько экземпляров этого закоулка может появиться в комнате.\nX — минимум, Y — максимум.\nЕсли Guaranteed включён — минимум автоматически становится >= 1.")]
    [SerializeField] private Vector2Int _countRange = new Vector2Int(0, 1);

    [Header("Требования к позиции")]
    [Tooltip("Минимальная дистанция от коридора/тропы до точки спавна (в клетках).\nЧем больше — тем дальше игрок должен отклониться от основного маршрута.")]
    [SerializeField, Min(0)] private int _minimumDistanceFromCorridorInCells = 4;

    [Tooltip("Отступ от стен (в клетках), чтобы префаб не торчал за пределы комнаты.\nПолезно, если pivot/габариты у префаба не 1×1.")]
    [SerializeField, Min(0)] private int _wallMarginInCells = 2;

    [Tooltip("Требуемая свободная площадка вокруг точки (радиус в клетках).\n0 — только сама клетка.\n1 — нужна зона 3×3.\n2 — нужна зона 5×5.")]
    [SerializeField, Min(0)] private int _footprintRadiusInCells = 1;

    [Header("Соседство с другими закоулками")]
    [Tooltip("Минимальная дистанция до ЛЮБОГО другого закоулка (в клетках).\n0 — можно ставить вплотную.")]
    [SerializeField, Min(0)] private int _minimumDistanceToAnyNookInCells = 2;

    [Tooltip("Минимальная дистанция до закоулка ТОГО ЖЕ ТИПА (этого же конфига) (в клетках).\n0 — одинаковые могут стоять рядом.")]
    [SerializeField, Min(0)] private int _minimumDistanceToSameTypeInCells = 4;

    [Tooltip("Радиус (в клетках), внутри которого считается количество одинаковых закоулков рядом.")]
    [SerializeField, Min(1)] private int _sameTypeNeighborRadiusInCells = 6;

    [Tooltip("Сколько одинаковых (уже поставленных) допускается в этом радиусе.\n0 — запрещает иметь одинаковые рядом вообще.\n1 — допускает максимум 1 одинаковый рядом и т.д.")]
    [SerializeField, Min(0)] private int _maximumSameTypeWithinNeighborRadius = 1;

    [Header("Выбор точки внутри кармана")]
    [Tooltip("Ограничение разброса внутри кармана от «лучшей» точки.\n0 — можно по всему карману.\n3 — компактнее рядом с центром кармана.")]
    [SerializeField, Min(0)] private int _scatterRadiusInCells = 3;

    public GameObject Prefab => _prefab;
    public int Weight => _weight;

    public bool Guaranteed => _guaranteed;
    public Vector2Int CountRange => _countRange;

    public int MinimumDistanceFromCorridorInCells => _minimumDistanceFromCorridorInCells;
    public int WallMarginInCells => _wallMarginInCells;
    public int FootprintRadiusInCells => _footprintRadiusInCells;

    public int MinimumDistanceToAnyNookInCells => _minimumDistanceToAnyNookInCells;
    public int MinimumDistanceToSameTypeInCells => _minimumDistanceToSameTypeInCells;

    public int SameTypeNeighborRadiusInCells => _sameTypeNeighborRadiusInCells;
    public int MaximumSameTypeWithinNeighborRadius => _maximumSameTypeWithinNeighborRadius;

    public int ScatterRadiusInCells => _scatterRadiusInCells;
}
