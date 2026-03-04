using System.Collections.Generic;
using UnityEngine;

public sealed class RoomInteriorVoxelClusterer
{
    public List<HashSet<int>> BuildClusters(List<RoomInteriorBlockRange> blockRanges)
    {
        Dictionary<Vector3Int, List<int>> voxelToObjectIndices = new Dictionary<Vector3Int, List<int>>();

        FillVoxelMap(blockRanges, voxelToObjectIndices);

        List<HashSet<int>> clusters = BuildClustersFromVoxels(voxelToObjectIndices, blockRanges);

        EnsureAllObjectsClustered(clusters, blockRanges);

        return clusters;
    }

    private void FillVoxelMap(List<RoomInteriorBlockRange> blockRanges, Dictionary<Vector3Int, List<int>> voxelToObjectIndices)
    {
        for (int rangeIndex = 0; rangeIndex < blockRanges.Count; rangeIndex++)
        {
            RoomInteriorBlockRange range = blockRanges[rangeIndex];

            for (int x = range.MinX; x <= range.MaxX; x++)
            {
                for (int y = range.MinY; y <= range.MaxY; y++)
                {
                    for (int z = range.MinZ; z <= range.MaxZ; z++)
                    {
                        Vector3Int voxel = new Vector3Int(x, y, z);

                        List<int> objectIndices;

                        bool has = voxelToObjectIndices.TryGetValue(voxel, out objectIndices);

                        if (has == false)
                        {
                            objectIndices = new List<int>();
                            voxelToObjectIndices.Add(voxel, objectIndices);
                        }

                        objectIndices.Add(rangeIndex);
                    }
                }
            }
        }
    }

    private List<HashSet<int>> BuildClustersFromVoxels(Dictionary<Vector3Int, List<int>> voxelToObjectIndices, List<RoomInteriorBlockRange> blockRanges)
    {
        List<HashSet<int>> clusters = new List<HashSet<int>>();

        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        Queue<Vector3Int> queue = new Queue<Vector3Int>();

        List<Vector3Int> voxels = new List<Vector3Int>();

        foreach (KeyValuePair<Vector3Int, List<int>> pair in voxelToObjectIndices)
        {
            voxels.Add(pair.Key);
        }

        for (int voxelIndex = 0; voxelIndex < voxels.Count; voxelIndex++)
        {
            Vector3Int startVoxel = voxels[voxelIndex];

            if (visited.Contains(startVoxel) == true)
            {
                continue;
            }

            List<int> startVoxelObjectIndices;

            bool startExists = voxelToObjectIndices.TryGetValue(startVoxel, out startVoxelObjectIndices);

            if (startExists == false)
            {
                continue;
            }

            if (startVoxelObjectIndices == null || startVoxelObjectIndices.Count == 0)
            {
                continue;
            }

            int startObjectIndex = startVoxelObjectIndices[0];
            Vector2Int requiredChunkKey = blockRanges[startObjectIndex].ChunkKey;

            HashSet<int> objectIndices = new HashSet<int>();

            visited.Add(startVoxel);
            queue.Enqueue(startVoxel);

            while (queue.Count > 0)
            {
                Vector3Int current = queue.Dequeue();

                AddVoxelObjects(current, requiredChunkKey, voxelToObjectIndices, blockRanges, objectIndices);

                EnqueueNeighbor(new Vector3Int(current.x + 1, current.y, current.z), requiredChunkKey, voxelToObjectIndices, blockRanges, visited, queue);
                EnqueueNeighbor(new Vector3Int(current.x - 1, current.y, current.z), requiredChunkKey, voxelToObjectIndices, blockRanges, visited, queue);

                EnqueueNeighbor(new Vector3Int(current.x, current.y + 1, current.z), requiredChunkKey, voxelToObjectIndices, blockRanges, visited, queue);
                EnqueueNeighbor(new Vector3Int(current.x, current.y - 1, current.z), requiredChunkKey, voxelToObjectIndices, blockRanges, visited, queue);

                EnqueueNeighbor(new Vector3Int(current.x, current.y, current.z + 1), requiredChunkKey, voxelToObjectIndices, blockRanges, visited, queue);
                EnqueueNeighbor(new Vector3Int(current.x, current.y, current.z - 1), requiredChunkKey, voxelToObjectIndices, blockRanges, visited, queue);
            }

            clusters.Add(objectIndices);
        }

        return clusters;
    }

    private void AddVoxelObjects(
        Vector3Int voxel,
        Vector2Int requiredChunkKey,
        Dictionary<Vector3Int, List<int>> voxelToObjectIndices,
        List<RoomInteriorBlockRange> blockRanges,
        HashSet<int> objectIndices
    )
    {
        List<int> voxelObjectIndices;

        bool exists = voxelToObjectIndices.TryGetValue(voxel, out voxelObjectIndices);

        if (exists == false)
        {
            return;
        }

        if (voxelObjectIndices == null)
        {
            return;
        }

        for (int index = 0; index < voxelObjectIndices.Count; index++)
        {
            int objectIndex = voxelObjectIndices[index];

            if (blockRanges[objectIndex].ChunkKey != requiredChunkKey)
            {
                continue;
            }

            objectIndices.Add(objectIndex);
        }
    }

    private void EnqueueNeighbor(
        Vector3Int neighbor,
        Vector2Int requiredChunkKey,
        Dictionary<Vector3Int, List<int>> voxelToObjectIndices,
        List<RoomInteriorBlockRange> blockRanges,
        HashSet<Vector3Int> visited,
        Queue<Vector3Int> queue
    )
    {
        List<int> objectIndices;

        bool exists = voxelToObjectIndices.TryGetValue(neighbor, out objectIndices);

        if (exists == false)
        {
            return;
        }

        bool hasRequiredChunkKey = false;

        if (objectIndices != null)
        {
            for (int index = 0; index < objectIndices.Count; index++)
            {
                int objectIndex = objectIndices[index];

                if (blockRanges[objectIndex].ChunkKey != requiredChunkKey)
                {
                    continue;
                }

                hasRequiredChunkKey = true;

                break;
            }
        }

        if (hasRequiredChunkKey == false)
        {
            return;
        }

        if (visited.Add(neighbor) == false)
        {
            return;
        }

        queue.Enqueue(neighbor);
    }

    private void EnsureAllObjectsClustered(List<HashSet<int>> clusters, List<RoomInteriorBlockRange> blockRanges)
    {
        HashSet<int> clusteredObjectIndices = new HashSet<int>();
        Dictionary<Vector2Int, int> chunkKeyToClusterIndex = new Dictionary<Vector2Int, int>();

        for (int clusterIndex = 0; clusterIndex < clusters.Count; clusterIndex++)
        {
            HashSet<int> objectIndices = clusters[clusterIndex];

            int firstObjectIndex = -1;

            foreach (int objectIndex in objectIndices)
            {
                clusteredObjectIndices.Add(objectIndex);

                if (firstObjectIndex < 0)
                {
                    firstObjectIndex = objectIndex;
                }
            }

            if (firstObjectIndex < 0)
            {
                continue;
            }

            Vector2Int chunkKey = blockRanges[firstObjectIndex].ChunkKey;

            if (chunkKeyToClusterIndex.ContainsKey(chunkKey) == false)
            {
                chunkKeyToClusterIndex.Add(chunkKey, clusterIndex);
            }
        }

        for (int rangeIndex = 0; rangeIndex < blockRanges.Count; rangeIndex++)
        {
            if (clusteredObjectIndices.Contains(rangeIndex) == true)
            {
                continue;
            }

            Vector2Int chunkKey = blockRanges[rangeIndex].ChunkKey;

            int clusterIndex;

            bool hasCluster = chunkKeyToClusterIndex.TryGetValue(chunkKey, out clusterIndex);

            if (hasCluster == true)
            {
                clusters[clusterIndex].Add(rangeIndex);

                continue;
            }

            HashSet<int> newCluster = new HashSet<int>();
            newCluster.Add(rangeIndex);

            clusters.Add(newCluster);

            chunkKeyToClusterIndex.Add(chunkKey, clusters.Count - 1);
        }
    }
}
