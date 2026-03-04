using UnityEngine;

internal static class LevelCorridorBoundsBuilder
{
    public static bool TryBuildCorridorCollisionBounds(Vector3 fromDoor, Vector3 toDoor, int corridorWidthInBlocks, LevelCorridorBuilder corridorBuilder, LevelPlacementSettings placementSettings, out Bounds bounds)
    {
        float blockSize = corridorBuilder.BlockSize;
        float extra = placementSettings.CorridorCollisionExtraWidthInBlocks * blockSize;
        float halfWidth = (corridorWidthInBlocks * blockSize * 0.5f) + extra;
        float ignoreEnds = placementSettings.CorridorIgnoreEndsInBlocks * blockSize;

        Vector3 from = new Vector3(fromDoor.x, 0f, fromDoor.z);
        Vector3 to = new Vector3(toDoor.x, 0f, toDoor.z);

        float dx = Mathf.Abs(from.x - to.x);
        float dz = Mathf.Abs(from.z - to.z);

        if (dx >= dz)
        {

            if (dz > 0.001f)
            {
                bounds = new Bounds(Vector3.zero, Vector3.zero);
                return false;
            }

            float minX = Mathf.Min(from.x, to.x) + ignoreEnds;
            float maxX = Mathf.Max(from.x, to.x) - ignoreEnds;

            if (maxX <= minX)
            {
                bounds = new Bounds(Vector3.zero, Vector3.zero);
                return false;
            }

            bounds = new Bounds(
                new Vector3((minX + maxX) * 0.5f, 0f, from.z),
                new Vector3(maxX - minX, 1f, halfWidth * 2f)
            );

            return true;

        }

        if (dx > 0.001f)
        {
            bounds = new Bounds(Vector3.zero, Vector3.zero);
            return false;
        }

        float minZ = Mathf.Min(from.z, to.z) + ignoreEnds;
        float maxZ = Mathf.Max(from.z, to.z) - ignoreEnds;

        if (maxZ <= minZ)
        {
            bounds = new Bounds(Vector3.zero, Vector3.zero);
            return false;
        }

        bounds = new Bounds(
            new Vector3(from.x, 0f, (minZ + maxZ) * 0.5f),
            new Vector3(halfWidth * 2f, 1f, maxZ - minZ)
        );

        return true;
    }

    public static bool IsStrictlyStraight(Vector3 from, Vector3 to)
    {
        float dx = Mathf.Abs(from.x - to.x);
        float dz = Mathf.Abs(from.z - to.z);

        if (dx < 0.001f)
            return true;

        if (dz < 0.001f)
            return true;

        return false;
    }
}
