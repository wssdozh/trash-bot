using System.Collections.Generic;
using UnityEngine;

public sealed class RoomInteriorHiddenBlockCuller : MonoBehaviour
{
    private struct BlockVoxelRange
    {
        public GameObject GameObject;
        public int MinX;
        public int MaxX;
        public int MinY;
        public int MaxY;
        public int MinZ;
        public int MaxZ;

        public BlockVoxelRange(GameObject gameObject, int minX, int maxX, int minY, int maxY, int minZ, int maxZ)
        {
            GameObject = gameObject;
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
            MinZ = minZ;
            MaxZ = maxZ;
        }
    }

    [SerializeField] private Transform _interiorBlocksRoot;
    [SerializeField] private float _blockSize = 1f;

    [SerializeField] private bool _destroyHiddenObjects = true;
    [SerializeField] private bool _disableHiddenObjects = false;

    [SerializeField] private bool _treatFloorAsSolid = true;
    [SerializeField] private int _floorSolidYIndex = 0;

    [SerializeField] private bool _includeInactive = false;

    [SerializeField] private Transform _ignoreRoot;

    [ContextMenu("Cull Hidden Interior Blocks")]
    public void CullHidden()
    {
        if (_interiorBlocksRoot == null)
        {
            return;
        }

        List<BlockVoxelRange> blockRanges = new List<BlockVoxelRange>();
        HashSet<Vector3Int> occupiedVoxels = new HashSet<Vector3Int>();

        int globalMinX = int.MaxValue;
        int globalMaxX = int.MinValue;

        int globalMinZ = int.MaxValue;
        int globalMaxZ = int.MinValue;

        CollectBlockRanges(blockRanges, ref globalMinX, ref globalMaxX, ref globalMinZ, ref globalMaxZ);

        if (blockRanges.Count == 0)
        {
            return;
        }

        FillOccupiedVoxels(occupiedVoxels, blockRanges);

        if (_treatFloorAsSolid == true)
        {
            FillFloorVoxels(occupiedVoxels, globalMinX, globalMaxX, globalMinZ, globalMaxZ, _floorSolidYIndex);
        }

        List<GameObject> hiddenObjects = new List<GameObject>();

        for (int rangeIndex = 0; rangeIndex < blockRanges.Count; rangeIndex++)
        {
            BlockVoxelRange range = blockRanges[rangeIndex];

            if (IsRangeExposed(range, occupiedVoxels) == false)
            {
                hiddenObjects.Add(range.GameObject);
            }
        }

        for (int index = 0; index < hiddenObjects.Count; index++)
        {
            GameObject hiddenObject = hiddenObjects[index];

            if (_disableHiddenObjects == true)
            {
                hiddenObject.SetActive(false);
                continue;
            }

            if (_destroyHiddenObjects == true)
            {
                DestroyGameObject(hiddenObject);
            }
        }
    }

    private void CollectBlockRanges(List<BlockVoxelRange> blockRanges, ref int globalMinX, ref int globalMaxX, ref int globalMinZ, ref int globalMaxZ)
    {
        Renderer[] renderers = _interiorBlocksRoot.GetComponentsInChildren<Renderer>(_includeInactive);

        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];

            if (renderer == null)
            {
                continue;
            }

            if (_includeInactive == false && renderer.enabled == false)
            {
                continue;
            }

            Transform transform = renderer.transform;

            if (_ignoreRoot != null && transform.IsChildOf(_ignoreRoot) == true)
            {
                continue;
            }

            GameObject gameObject = transform.gameObject;

            Bounds bounds = GetCombinedRendererBounds(gameObject);

            Vector3 localMin;
            Vector3 localMax;

            GetLocalBounds(_interiorBlocksRoot, bounds, out localMin, out localMax);

            int minX;
            int maxX;
            int minY;
            int maxY;
            int minZ;
            int maxZ;

            ComputeVoxelRange(localMin, localMax, out minX, out maxX, out minY, out maxY, out minZ, out maxZ);

            if (minX < globalMinX) globalMinX = minX;
            if (maxX > globalMaxX) globalMaxX = maxX;

            if (minZ < globalMinZ) globalMinZ = minZ;
            if (maxZ > globalMaxZ) globalMaxZ = maxZ;

            blockRanges.Add(new BlockVoxelRange(gameObject, minX, maxX, minY, maxY, minZ, maxZ));
        }

        if (globalMinX == int.MaxValue) globalMinX = 0;
        if (globalMaxX == int.MinValue) globalMaxX = 0;

        if (globalMinZ == int.MaxValue) globalMinZ = 0;
        if (globalMaxZ == int.MinValue) globalMaxZ = 0;
    }

    private Bounds GetCombinedRendererBounds(GameObject gameObject)
    {
        Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>(_includeInactive);

        Bounds bounds = renderers[0].bounds;

        for (int index = 1; index < renderers.Length; index++)
        {
            bounds.Encapsulate(renderers[index].bounds);
        }

        return bounds;
    }

    private void GetLocalBounds(Transform rootTransform, Bounds worldBounds, out Vector3 localMin, out Vector3 localMax)
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

            if (local.x < minX) minX = local.x;
            if (local.y < minY) minY = local.y;
            if (local.z < minZ) minZ = local.z;

            if (local.x > maxX) maxX = local.x;
            if (local.y > maxY) maxY = local.y;
            if (local.z > maxZ) maxZ = local.z;
        }

        localMin = new Vector3(minX, minY, minZ);
        localMax = new Vector3(maxX, maxY, maxZ);
    }

    private void ComputeVoxelRange(Vector3 localMin, Vector3 localMax, out int minX, out int maxX, out int minY, out int maxY, out int minZ, out int maxZ)
    {
        float inverse = 1f / _blockSize;

        int minXIndex = Mathf.RoundToInt(localMin.x * inverse);
        int minYIndex = Mathf.RoundToInt(localMin.y * inverse);
        int minZIndex = Mathf.RoundToInt(localMin.z * inverse);

        int maxXExclusive = Mathf.RoundToInt(localMax.x * inverse);
        int maxYExclusive = Mathf.RoundToInt(localMax.y * inverse);
        int maxZExclusive = Mathf.RoundToInt(localMax.z * inverse);

        int sizeX = maxXExclusive - minXIndex;
        int sizeY = maxYExclusive - minYIndex;
        int sizeZ = maxZExclusive - minZIndex;

        if (sizeX < 1) sizeX = 1;
        if (sizeY < 1) sizeY = 1;
        if (sizeZ < 1) sizeZ = 1;

        minX = minXIndex;
        minY = minYIndex;
        minZ = minZIndex;

        maxX = minXIndex + sizeX - 1;
        maxY = minYIndex + sizeY - 1;
        maxZ = minZIndex + sizeZ - 1;
    }

    private void FillOccupiedVoxels(HashSet<Vector3Int> occupiedVoxels, List<BlockVoxelRange> blockRanges)
    {
        for (int rangeIndex = 0; rangeIndex < blockRanges.Count; rangeIndex++)
        {
            BlockVoxelRange range = blockRanges[rangeIndex];

            for (int x = range.MinX; x <= range.MaxX; x++)
            {
                for (int y = range.MinY; y <= range.MaxY; y++)
                {
                    for (int z = range.MinZ; z <= range.MaxZ; z++)
                    {
                        occupiedVoxels.Add(new Vector3Int(x, y, z));
                    }
                }
            }
        }
    }

    private void FillFloorVoxels(HashSet<Vector3Int> occupiedVoxels, int minX, int maxX, int minZ, int maxZ, int floorY)
    {
        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                occupiedVoxels.Add(new Vector3Int(x, floorY, z));
            }
        }
    }

    private bool IsRangeExposed(BlockVoxelRange range, HashSet<Vector3Int> occupiedVoxels)
    {
        for (int x = range.MinX; x <= range.MaxX; x++)
        {
            for (int y = range.MinY; y <= range.MaxY; y++)
            {
                for (int z = range.MinZ; z <= range.MaxZ; z++)
                {
                    Vector3Int voxel = new Vector3Int(x, y, z);

                    if (IsVoxelExposed(voxel, occupiedVoxels) == true)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool IsVoxelExposed(Vector3Int voxel, HashSet<Vector3Int> occupiedVoxels)
    {
        Vector3Int right = new Vector3Int(voxel.x + 1, voxel.y, voxel.z);
        Vector3Int left = new Vector3Int(voxel.x - 1, voxel.y, voxel.z);
        Vector3Int up = new Vector3Int(voxel.x, voxel.y + 1, voxel.z);
        Vector3Int down = new Vector3Int(voxel.x, voxel.y - 1, voxel.z);
        Vector3Int forward = new Vector3Int(voxel.x, voxel.y, voxel.z + 1);
        Vector3Int back = new Vector3Int(voxel.x, voxel.y, voxel.z - 1);

        if (occupiedVoxels.Contains(right) == false) return true;
        if (occupiedVoxels.Contains(left) == false) return true;
        if (occupiedVoxels.Contains(up) == false) return true;
        if (occupiedVoxels.Contains(down) == false) return true;
        if (occupiedVoxels.Contains(forward) == false) return true;
        if (occupiedVoxels.Contains(back) == false) return true;

        return false;
    }

    private void DestroyGameObject(GameObject gameObject)
    {
        if (Application.isPlaying == false)
        {
            DestroyImmediate(gameObject);
            return;
        }

        Destroy(gameObject);
    }
}
