using System.Collections.Generic;
using UnityEngine;

public static class RoomInteriorBlockRangeCollector
{
    public static void Collect(
        Transform interiorBlocksRoot,
        Transform combinedRoot,
        float blockSize,
        int chunkSizeInCells,
        bool includeInactive,
        List<RoomInteriorBlockRange> blockRanges
    )
    {
        MeshFilter[] meshFilters = interiorBlocksRoot.GetComponentsInChildren<MeshFilter>(includeInactive);

        int chunkSize = chunkSizeInCells;

        if (chunkSize < 2)
        {
            chunkSize = 2;
        }

        for (int index = 0; index < meshFilters.Length; index++)
        {
            MeshFilter meshFilter = meshFilters[index];

            if (meshFilter == null)
            {
                continue;
            }

            if (combinedRoot != null && meshFilter.transform.IsChildOf(combinedRoot) == true)
            {
                continue;
            }

            MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();

            if (meshRenderer == null)
            {
                continue;
            }

            Mesh sharedMesh = meshFilter.sharedMesh;

            if (sharedMesh == null)
            {
                continue;
            }

            Bounds bounds = meshRenderer.bounds;

            Vector3 localMin;
            Vector3 localMax;

            GetLocalBounds(interiorBlocksRoot, bounds, out localMin, out localMax);

            int minX;
            int maxX;
            int minY;
            int maxY;
            int minZ;
            int maxZ;

            ComputeVoxelRange(localMin, localMax, blockSize, out minX, out maxX, out minY, out maxY, out minZ, out maxZ);

            Vector3 localPosition = interiorBlocksRoot.InverseTransformPoint(meshFilter.transform.position);

            int centerCellX = Mathf.FloorToInt(localPosition.x / blockSize);
            int centerCellZ = Mathf.FloorToInt(localPosition.z / blockSize);

            int chunkX = Mathf.FloorToInt((float)centerCellX / (float)chunkSize);
            int chunkZ = Mathf.FloorToInt((float)centerCellZ / (float)chunkSize);

            Vector2Int chunkKey = new Vector2Int(chunkX, chunkZ);

            RoomInteriorBlockRange range = new RoomInteriorBlockRange(meshFilter, meshRenderer, minX, maxX, minY, maxY, minZ, maxZ, chunkKey);
            blockRanges.Add(range);
        }
    }

    private static void GetLocalBounds(Transform rootTransform, Bounds worldBounds, out Vector3 localMin, out Vector3 localMax)
    {
        Vector3 worldCenter = worldBounds.center;
        Vector3 worldExtents = worldBounds.extents;

        Vector3[] corners = new Vector3[8];

        corners[0] = worldCenter + new Vector3(-worldExtents.x, -worldExtents.y, -worldExtents.z);
        corners[1] = worldCenter + new Vector3(-worldExtents.x, -worldExtents.y, worldExtents.z);
        corners[2] = worldCenter + new Vector3(-worldExtents.x, worldExtents.y, -worldExtents.z);
        corners[3] = worldCenter + new Vector3(-worldExtents.x, worldExtents.y, worldExtents.z);

        corners[4] = worldCenter + new Vector3(worldExtents.x, -worldExtents.y, -worldExtents.z);
        corners[5] = worldCenter + new Vector3(worldExtents.x, -worldExtents.y, worldExtents.z);
        corners[6] = worldCenter + new Vector3(worldExtents.x, worldExtents.y, -worldExtents.z);
        corners[7] = worldCenter + new Vector3(worldExtents.x, worldExtents.y, worldExtents.z);

        Vector3 firstLocal = rootTransform.InverseTransformPoint(corners[0]);

        float minX = firstLocal.x;
        float minY = firstLocal.y;
        float minZ = firstLocal.z;

        float maxX = firstLocal.x;
        float maxY = firstLocal.y;
        float maxZ = firstLocal.z;

        for (int index = 1; index < 8; index++)
        {
            Vector3 local = rootTransform.InverseTransformPoint(corners[index]);

            if (local.x < minX)
            {
                minX = local.x;
            }

            if (local.y < minY)
            {
                minY = local.y;
            }

            if (local.z < minZ)
            {
                minZ = local.z;
            }

            if (local.x > maxX)
            {
                maxX = local.x;
            }

            if (local.y > maxY)
            {
                maxY = local.y;
            }

            if (local.z > maxZ)
            {
                maxZ = local.z;
            }
        }

        localMin = new Vector3(minX, minY, minZ);
        localMax = new Vector3(maxX, maxY, maxZ);
    }

    private static void ComputeVoxelRange(
        Vector3 localMin,
        Vector3 localMax,
        float blockSize,
        out int minX,
        out int maxX,
        out int minY,
        out int maxY,
        out int minZ,
        out int maxZ
    )
    {
        float inverse = 1f / blockSize;

        int minXIndex = Mathf.FloorToInt(localMin.x * inverse);
        int minYIndex = Mathf.FloorToInt(localMin.y * inverse);
        int minZIndex = Mathf.FloorToInt(localMin.z * inverse);

        int maxXExclusive = Mathf.CeilToInt(localMax.x * inverse);
        int maxYExclusive = Mathf.CeilToInt(localMax.y * inverse);
        int maxZExclusive = Mathf.CeilToInt(localMax.z * inverse);

        minX = minXIndex;
        minY = minYIndex;
        minZ = minZIndex;

        maxX = maxXExclusive - 1;
        maxY = maxYExclusive - 1;
        maxZ = maxZExclusive - 1;

        if (maxX < minX)
        {
            maxX = minX;
        }

        if (maxY < minY)
        {
            maxY = minY;
        }

        if (maxZ < minZ)
        {
            maxZ = minZ;
        }
    }
}

public struct RoomInteriorBlockRange
{
    public MeshFilter MeshFilter;
    public MeshRenderer MeshRenderer;

    public int MinX;
    public int MaxX;

    public int MinY;
    public int MaxY;

    public int MinZ;
    public int MaxZ;

    public Vector2Int ChunkKey;

    public RoomInteriorBlockRange(
        MeshFilter meshFilter,
        MeshRenderer meshRenderer,
        int minX,
        int maxX,
        int minY,
        int maxY,
        int minZ,
        int maxZ,
        Vector2Int chunkKey
    )
    {
        MeshFilter = meshFilter;
        MeshRenderer = meshRenderer;

        MinX = minX;
        MaxX = maxX;

        MinY = minY;
        MaxY = maxY;

        MinZ = minZ;
        MaxZ = maxZ;

        ChunkKey = chunkKey;
    }
}
