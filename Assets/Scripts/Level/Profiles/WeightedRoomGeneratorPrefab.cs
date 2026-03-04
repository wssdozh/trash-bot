using UnityEngine;

[System.Serializable]
public sealed class WeightedRoomGeneratorPrefab
{
    [SerializeField] private RoomGenerator _prefab;
    [SerializeField, Min(1)] private int _weight = 1;

    public RoomGenerator Prefab => _prefab;
    public int Weight => _weight;
}
