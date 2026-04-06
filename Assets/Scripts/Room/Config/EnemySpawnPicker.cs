using System;
using System.Collections.Generic;
using UnityEngine;

public static class EnemySpawnPicker
{
    private const float MinBalanceMultiplier = 0.45f;
    private const float MaxBalanceMultiplier = 1.85f;
    private const float BalanceStrength = 0.55f;

    public static EnemySpawnConfig PickSpawn(IReadOnlyList<EnemySpawnConfig> enemySpawns, System.Random random)
    {
        return PickSpawn(enemySpawns, null, random);
    }

    public static EnemySpawnConfig PickSpawn(
        IReadOnlyList<EnemySpawnConfig> enemySpawns,
        IReadOnlyList<EnemySpawnConfig> spawnedEnemySpawns,
        System.Random random
    )
    {
        if (enemySpawns == null || enemySpawns.Count == 0)
        {
            throw new InvalidOperationException(nameof(enemySpawns));
        }

        float totalWeight = GetTotalWeight(enemySpawns);

        if (totalWeight <= 0f)
        {
            throw new InvalidOperationException(nameof(enemySpawns));
        }

        float totalAdjustedWeight = 0f;

        for (int spawnIndex = 0; spawnIndex < enemySpawns.Count; spawnIndex++)
        {
            EnemySpawnConfig enemySpawn = enemySpawns[spawnIndex];
            float adjustedWeight = GetAdjustedWeight(enemySpawns, enemySpawn, spawnedEnemySpawns, totalWeight);
            totalAdjustedWeight += adjustedWeight;
        }

        if (totalAdjustedWeight <= 0f)
        {
            throw new InvalidOperationException(nameof(enemySpawns));
        }

        float randomValue = (float)random.NextDouble() * totalAdjustedWeight;
        float cumulativeWeight = 0f;

        for (int spawnIndex = 0; spawnIndex < enemySpawns.Count; spawnIndex++)
        {
            EnemySpawnConfig enemySpawn = enemySpawns[spawnIndex];
            float adjustedWeight = GetAdjustedWeight(enemySpawns, enemySpawn, spawnedEnemySpawns, totalWeight);

            if (adjustedWeight <= 0f)
            {
                continue;
            }

            cumulativeWeight += adjustedWeight;

            if (randomValue < cumulativeWeight)
            {
                return enemySpawn;
            }
        }

        throw new InvalidOperationException(nameof(enemySpawns));
    }

    private static float GetTotalWeight(IReadOnlyList<EnemySpawnConfig> enemySpawns)
    {
        float totalWeight = 0f;

        for (int spawnIndex = 0; spawnIndex < enemySpawns.Count; spawnIndex++)
        {
            EnemySpawnConfig enemySpawn = enemySpawns[spawnIndex];

            if (IsValidSpawn(enemySpawn) == false)
            {
                continue;
            }

            totalWeight += GetWeight(enemySpawn);
        }

        return totalWeight;
    }

    private static float GetAdjustedWeight(
        IReadOnlyList<EnemySpawnConfig> enemySpawns,
        EnemySpawnConfig enemySpawn,
        IReadOnlyList<EnemySpawnConfig> spawnedEnemySpawns,
        float totalWeight
    )
    {
        if (IsValidSpawn(enemySpawn) == false)
        {
            return 0f;
        }

        int weight = GetWeight(enemySpawn);

        if (spawnedEnemySpawns == null || spawnedEnemySpawns.Count == 0)
        {
            return weight;
        }

        GameObject prefab = enemySpawn.Prefab;
        float groupWeight = GetGroupWeight(enemySpawns, prefab);
        int spawnedGroupCount = GetSpawnedGroupCount(spawnedEnemySpawns, prefab);
        int spawnedCount = GetSpawnedCount(spawnedEnemySpawns);
        float expectedGroupCount = (groupWeight / totalWeight) * (spawnedCount + 1f);
        float groupGap = expectedGroupCount - spawnedGroupCount;
        float balanceMultiplier = Mathf.Clamp(1f + (groupGap * BalanceStrength), MinBalanceMultiplier, MaxBalanceMultiplier);

        return weight * balanceMultiplier;
    }

    private static float GetGroupWeight(IReadOnlyList<EnemySpawnConfig> enemySpawns, GameObject prefab)
    {
        float groupWeight = 0f;

        for (int spawnIndex = 0; spawnIndex < enemySpawns.Count; spawnIndex++)
        {
            EnemySpawnConfig enemySpawn = enemySpawns[spawnIndex];

            if (IsSameGroup(enemySpawn, prefab) == false)
            {
                continue;
            }

            groupWeight += GetWeight(enemySpawn);
        }

        return groupWeight;
    }

    private static int GetSpawnedGroupCount(IReadOnlyList<EnemySpawnConfig> spawnedEnemySpawns, GameObject prefab)
    {
        int spawnedGroupCount = 0;

        for (int spawnIndex = 0; spawnIndex < spawnedEnemySpawns.Count; spawnIndex++)
        {
            EnemySpawnConfig enemySpawn = spawnedEnemySpawns[spawnIndex];

            if (IsSameGroup(enemySpawn, prefab) == false)
            {
                continue;
            }

            spawnedGroupCount += 1;
        }

        return spawnedGroupCount;
    }

    private static int GetSpawnedCount(IReadOnlyList<EnemySpawnConfig> spawnedEnemySpawns)
    {
        int spawnedCount = 0;

        for (int spawnIndex = 0; spawnIndex < spawnedEnemySpawns.Count; spawnIndex++)
        {
            EnemySpawnConfig enemySpawn = spawnedEnemySpawns[spawnIndex];

            if (IsValidSpawn(enemySpawn) == false)
            {
                continue;
            }

            spawnedCount += 1;
        }

        return spawnedCount;
    }

    private static bool IsSameGroup(EnemySpawnConfig enemySpawn, GameObject prefab)
    {
        if (IsValidSpawn(enemySpawn) == false)
        {
            return false;
        }

        return enemySpawn.Prefab == prefab;
    }

    private static bool IsValidSpawn(EnemySpawnConfig enemySpawn)
    {
        if (enemySpawn == null)
        {
            return false;
        }

        if (enemySpawn.Prefab == null)
        {
            return false;
        }

        return true;
    }

    private static int GetWeight(EnemySpawnConfig enemySpawn)
    {
        if (enemySpawn == null)
        {
            return 0;
        }

        int weight = enemySpawn.Weight;

        if (weight < 1)
        {
            return 1;
        }

        return weight;
    }
}
