using System.Collections.Generic;
using UnityEngine;

internal static class LevelGeneratorUtility
{
    public static int RandomRangeInclusive(System.Random random, Vector2Int range)
    {
        int min = range.x;
        int max = range.y;

        if (max < min)
            max = min;

        if (max == min)
            return min;

        return random.Next(min, max + 1);
    }

    public static int NextAnyInt(System.Random random)
    {
        int a = random.Next();
        int b = random.Next();

        return a ^ (b << 1);
    }

    public static void Shuffle<T>(List<T> list, System.Random random)
    {
        for (int index = list.Count - 1; index > 0; index--)
        {

            int swapIndex = random.Next(0, index + 1);

            T value = list[index];

            list[index] = list[swapIndex];
            list[swapIndex] = value;

        }
    }

    public static void DestroyChildren(Transform parent)
    {
        if (parent == null)
            return;

        for (int index = parent.childCount - 1; index >= 0; index--)
        {

            Transform child = parent.GetChild(index);

            if (Application.isPlaying == true)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }

        }
    }
}
