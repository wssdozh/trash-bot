using System;
using System.Collections.Generic;
using UnityEngine;

public static class EnemySpawnPicker
{
    public static EnemySpawnConfig PickSpawn(IReadOnlyList<EnemySpawnConfig> enemySpawns, System.Random random)
    {
        if (enemySpawns == null || enemySpawns.Count == 0)
        {
            throw new InvalidOperationException(nameof(enemySpawns));
        }

        int totalWeight = 0;

        for (int spawnIndex = 0; spawnIndex < enemySpawns.Count; spawnIndex++)
        {
            EnemySpawnConfig enemySpawn = enemySpawns[spawnIndex];

            if (enemySpawn == null)
            {
                continue;
            }

            GameObject prefab = enemySpawn.Prefab;

            if (prefab == null)
            {
                continue;
            }

            int weight = enemySpawn.Weight;

            if (weight < 1)
            {
                weight = 1;
            }

            totalWeight += weight;
        }

        if (totalWeight <= 0)
        {
            throw new InvalidOperationException(nameof(enemySpawns));
        }

        int randomValue = random.Next(0, totalWeight);
        int cumulativeWeight = 0;

        for (int spawnIndex = 0; spawnIndex < enemySpawns.Count; spawnIndex++)
        {
            EnemySpawnConfig enemySpawn = enemySpawns[spawnIndex];

            if (enemySpawn == null)
            {
                continue;
            }

            GameObject prefab = enemySpawn.Prefab;

            if (prefab == null)
            {
                continue;
            }

            int weight = enemySpawn.Weight;

            if (weight < 1)
            {
                weight = 1;
            }

            cumulativeWeight += weight;

            if (randomValue < cumulativeWeight)
            {
                return enemySpawn;
            }
        }

        throw new InvalidOperationException(nameof(enemySpawns));
    }
}
