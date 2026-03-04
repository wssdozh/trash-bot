using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Levels/Room Prefab Library", fileName = "LevelRoomPrefabLibrary")]
public sealed class LevelRoomPrefabLibrary : ScriptableObject
{
    [Header("Пулы префабов по типам")]
    [SerializeField] private List<WeightedRoomGeneratorPrefab> _startRooms = new List<WeightedRoomGeneratorPrefab>();
    [SerializeField] private List<WeightedRoomGeneratorPrefab> _combatRooms = new List<WeightedRoomGeneratorPrefab>();
    [SerializeField] private List<WeightedRoomGeneratorPrefab> _treasureRooms = new List<WeightedRoomGeneratorPrefab>();
    [SerializeField] private List<WeightedRoomGeneratorPrefab> _bossRooms = new List<WeightedRoomGeneratorPrefab>();

    public RoomGenerator Pick(RoomType roomType, System.Random random)
    {
        List<WeightedRoomGeneratorPrefab> list = GetList(roomType);

        if (list == null || list.Count == 0)
        {
            throw new System.InvalidOperationException(nameof(list));
        }

        int totalWeight = 0;

        for (int index = 0; index < list.Count; index++)
        {
            WeightedRoomGeneratorPrefab entry = list[index];

            if (entry == null)
            {
                continue;
            }

            if (entry.Prefab == null)
            {
                continue;
            }

            int weight = entry.Weight;

            if (weight < 1)
            {
                weight = 1;
            }

            totalWeight += weight;
        }

        if (totalWeight <= 0)
        {
            RoomGenerator fallbackPrefab;

            if (TryPickFirstAvailable(list, out fallbackPrefab) == true)
            {
                Debug.LogWarning($"LevelRoomPrefabLibrary: pool '{roomType}' has no valid weighted entries, using fallback '{fallbackPrefab.name}'.");
                return fallbackPrefab;
            }

            if (TryPickAnyPoolFallback(out fallbackPrefab) == true)
            {
                Debug.LogWarning($"LevelRoomPrefabLibrary: pool '{roomType}' has no valid prefabs, using cross-pool fallback '{fallbackPrefab.name}'.");
                return fallbackPrefab;
            }

            throw new System.InvalidOperationException($"No valid RoomGenerator prefab found for roomType '{roomType}'.");
        }

        int roll = random.Next(0, totalWeight);
        int cursor = 0;

        for (int index = 0; index < list.Count; index++)
        {
            WeightedRoomGeneratorPrefab entry = list[index];

            if (entry == null)
            {
                continue;
            }

            RoomGenerator prefab = entry.Prefab;

            if (prefab == null)
            {
                continue;
            }

            int weight = entry.Weight;

            if (weight < 1)
            {
                weight = 1;
            }

            cursor += weight;

            if (roll < cursor)
            {
                return prefab;
            }
        }

        throw new System.InvalidOperationException(nameof(roomType));
    }

    private bool TryPickAnyPoolFallback(out RoomGenerator prefab)
    {
        if (TryPickFirstAvailable(_startRooms, out prefab) == true)
        {
            return true;
        }

        if (TryPickFirstAvailable(_combatRooms, out prefab) == true)
        {
            return true;
        }

        if (TryPickFirstAvailable(_treasureRooms, out prefab) == true)
        {
            return true;
        }

        if (TryPickFirstAvailable(_bossRooms, out prefab) == true)
        {
            return true;
        }

        prefab = null;
        return false;
    }

    private static bool TryPickFirstAvailable(List<WeightedRoomGeneratorPrefab> list, out RoomGenerator prefab)
    {
        if (list == null)
        {
            prefab = null;
            return false;
        }

        for (int index = 0; index < list.Count; index++)
        {
            WeightedRoomGeneratorPrefab entry = list[index];

            if (entry == null)
            {
                continue;
            }

            RoomGenerator candidate = entry.Prefab;

            if (candidate == null)
            {
                continue;
            }

            prefab = candidate;
            return true;
        }

        prefab = null;
        return false;
    }

    private List<WeightedRoomGeneratorPrefab> GetList(RoomType roomType)
    {
        if (roomType == RoomType.Start)
        {
            return _startRooms;
        }

        if (roomType == RoomType.Combat)
        {
            return _combatRooms;
        }

        if (roomType == RoomType.Treasure)
        {
            return _treasureRooms;
        }

        return _bossRooms;
    }
}
