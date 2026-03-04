using System.Collections.Generic;
using UnityEngine;

public static class WeightedPrefabPicker
{
    public static GameObject PickPrefab(IReadOnlyList<WeightedPrefab> weightedPrefabs, System.Random random)
    {
        int totalWeight = 0;

        for (int prefabIndex = 0; prefabIndex < weightedPrefabs.Count; prefabIndex++)
        {
            totalWeight += weightedPrefabs[prefabIndex].Weight;
        }

        int randomValue = random.Next(0, totalWeight);
        int cumulativeWeight = 0;

        for (int prefabIndex = 0; prefabIndex < weightedPrefabs.Count; prefabIndex++)
        {
            cumulativeWeight += weightedPrefabs[prefabIndex].Weight;

            if (randomValue < cumulativeWeight)
            {
                return weightedPrefabs[prefabIndex].Prefab;
            }
        }

        return weightedPrefabs[weightedPrefabs.Count - 1].Prefab;
    }
}
