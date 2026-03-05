using System.Collections.Generic;
using UnityEngine;

public static class WeightedPrefabPicker
{
    public static GameObject PickPrefab(IReadOnlyList<WeightedPrefab> weightedPrefabs, System.Random random)
    {
        if (weightedPrefabs == null || weightedPrefabs.Count == 0)
            throw new System.InvalidOperationException(nameof(weightedPrefabs));

        int totalWeight = 0;

        for (int prefabIndex = 0; prefabIndex < weightedPrefabs.Count; prefabIndex++)
        {
            WeightedPrefab weightedPrefab = weightedPrefabs[prefabIndex];

            if (weightedPrefab == null)
                continue;

            GameObject prefab = weightedPrefab.Prefab;

            if (prefab == null)
                continue;

            int weight = weightedPrefab.Weight;

            if (weight < 1)
                weight = 1;

            totalWeight += weight;
        }

        if (totalWeight <= 0)
            throw new System.InvalidOperationException(nameof(weightedPrefabs));

        int randomValue = random.Next(0, totalWeight);
        int cumulativeWeight = 0;

        for (int prefabIndex = 0; prefabIndex < weightedPrefabs.Count; prefabIndex++)
        {
            WeightedPrefab weightedPrefab = weightedPrefabs[prefabIndex];

            if (weightedPrefab == null)
                continue;

            GameObject prefab = weightedPrefab.Prefab;

            if (prefab == null)
                continue;

            int weight = weightedPrefab.Weight;

            if (weight < 1)
                weight = 1;

            cumulativeWeight += weight;

            if (randomValue < cumulativeWeight)
                return prefab;
        }

        throw new System.InvalidOperationException(nameof(weightedPrefabs));
    }
}
