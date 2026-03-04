using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class RoomInteriorClusterMeshCombiner
{
    private struct SubMeshSource
    {
        public MeshFilter MeshFilter;
        public int SubMeshIndex;

        public SubMeshSource(MeshFilter meshFilter, int subMeshIndex)
        {
            MeshFilter = meshFilter;
            SubMeshIndex = subMeshIndex;
        }
    }

    private readonly Transform _combinedRoot;
    private readonly RoomInteriorChunkRootCompositionProfile _chunkRootCompositionProfile;

    private readonly bool _createMeshCollider;
    private readonly bool _meshColliderConvex;
    private readonly bool _meshColliderIsTrigger;

    public RoomInteriorClusterMeshCombiner(
        Transform combinedRoot,
        RoomInteriorChunkRootCompositionProfile chunkRootCompositionProfile,
        bool createMeshCollider,
        bool meshColliderConvex,
        bool meshColliderIsTrigger
    )
    {
        _combinedRoot = combinedRoot;
        _chunkRootCompositionProfile = chunkRootCompositionProfile;

        _createMeshCollider = createMeshCollider;
        _meshColliderConvex = meshColliderConvex;
        _meshColliderIsTrigger = meshColliderIsTrigger;
    }

    public void CreateClusterRoot(
        int clusterIndex,
        HashSet<int> objectIndices,
        List<RoomInteriorBlockRange> blockRanges,
        HashSet<int> combinedSourceObjectIds
    )
    {
        bool clusterIsStatic = ComputeClusterIsStatic(objectIndices, blockRanges);

        Dictionary<Material, List<SubMeshSource>> materialGroups = new Dictionary<Material, List<SubMeshSource>>();

        foreach (int index in objectIndices)
        {
            RoomInteriorBlockRange range = blockRanges[index];

            MeshFilter meshFilter = range.MeshFilter;

            if (meshFilter == null)
            {
                continue;
            }

            MeshRenderer meshRenderer = range.MeshRenderer;

            if (meshRenderer == null)
            {
                continue;
            }

            Mesh sharedMesh = meshFilter.sharedMesh;

            if (sharedMesh == null)
            {
                continue;
            }

            Material[] materials = meshRenderer.sharedMaterials;

            if (materials == null)
            {
                continue;
            }

            if (materials.Length == 0)
            {
                continue;
            }

            int subMeshCount = sharedMesh.subMeshCount;

            if (subMeshCount <= 0)
            {
                continue;
            }

            int usedSubMeshCount = Mathf.Min(subMeshCount, materials.Length);

            for (int subMeshIndex = 0; subMeshIndex < usedSubMeshCount; subMeshIndex++)
            {
                Material material = materials[subMeshIndex];

                if (material == null)
                {
                    continue;
                }

                List<SubMeshSource> list;

                bool has = materialGroups.TryGetValue(material, out list);

                if (has == false)
                {
                    list = new List<SubMeshSource>();
                    materialGroups.Add(material, list);
                }

                list.Add(new SubMeshSource(meshFilter, subMeshIndex));
            }

            combinedSourceObjectIds.Add(meshFilter.gameObject.GetInstanceID());
        }

        if (materialGroups.Count == 0)
        {
            return;
        }

        List<Material> combinedMaterials = new List<Material>();
        List<Mesh> groupMeshes = new List<Mesh>();

        foreach (KeyValuePair<Material, List<SubMeshSource>> pair in materialGroups)
        {
            Mesh groupMesh = CreateGroupMesh(pair.Value);

            if (groupMesh == null)
            {
                continue;
            }

            combinedMaterials.Add(pair.Key);
            groupMeshes.Add(groupMesh);
        }

        if (groupMeshes.Count == 0)
        {
            DestroyGroupMeshes(groupMeshes);

            return;
        }

        CombineInstance[] finalCombine = new CombineInstance[groupMeshes.Count];

        for (int meshIndex = 0; meshIndex < groupMeshes.Count; meshIndex++)
        {
            CombineInstance combineInstance = new CombineInstance();
            combineInstance.mesh = groupMeshes[meshIndex];
            combineInstance.subMeshIndex = 0;
            combineInstance.transform = Matrix4x4.identity;

            finalCombine[meshIndex] = combineInstance;
        }

        Mesh finalMesh = new Mesh();
        finalMesh.indexFormat = IndexFormat.UInt32;
        finalMesh.CombineMeshes(finalCombine, false, false);

        GameObject rootObject = new GameObject("Chunk_" + clusterIndex);
        rootObject.transform.SetParent(_combinedRoot, false);
        rootObject.isStatic = true;

        if (_createMeshCollider == true)
        {
            MeshCollider meshCollider = rootObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = finalMesh;
            meshCollider.convex = _meshColliderConvex;
            meshCollider.isTrigger = _meshColliderIsTrigger;
        }

        GameObject staticVisualObject = new GameObject("chunk (static)");
        staticVisualObject.transform.SetParent(rootObject.transform, false);
        staticVisualObject.isStatic = true;

        MeshFilter staticMeshFilter = staticVisualObject.AddComponent<MeshFilter>();
        MeshRenderer staticMeshRenderer = staticVisualObject.AddComponent<MeshRenderer>();

        staticMeshFilter.sharedMesh = finalMesh;
        staticMeshRenderer.sharedMaterials = combinedMaterials.ToArray();

        GameObject notStaticVisualObject = new GameObject("chunk (not static)");
        notStaticVisualObject.transform.SetParent(rootObject.transform, false);
        notStaticVisualObject.isStatic = false;

        MeshFilter notStaticMeshFilter = notStaticVisualObject.AddComponent<MeshFilter>();
        MeshRenderer notStaticMeshRenderer = notStaticVisualObject.AddComponent<MeshRenderer>();

        notStaticMeshFilter.sharedMesh = finalMesh;
        notStaticMeshRenderer.sharedMaterials = combinedMaterials.ToArray();

        bool startWithStaticActive = clusterIsStatic;

        _chunkRootCompositionProfile.Compose(rootObject, staticVisualObject, notStaticVisualObject, startWithStaticActive);

        DestroyGroupMeshes(groupMeshes);
    }

    private bool ComputeClusterIsStatic(HashSet<int> objectIndices, List<RoomInteriorBlockRange> blockRanges)
    {
        bool isStatic = true;

        foreach (int index in objectIndices)
        {
            RoomInteriorBlockRange range = blockRanges[index];

            MeshFilter meshFilter = range.MeshFilter;

            if (meshFilter == null)
            {
                continue;
            }

            if (meshFilter.gameObject.isStatic == false)
            {
                isStatic = false;

                break;
            }
        }

        return isStatic;
    }

    private Mesh CreateGroupMesh(List<SubMeshSource> subMeshSources)
    {
        int combineCount = 0;

        for (int index = 0; index < subMeshSources.Count; index++)
        {
            SubMeshSource source = subMeshSources[index];
            MeshFilter meshFilter = source.MeshFilter;

            if (meshFilter == null)
            {
                continue;
            }

            Mesh sharedMesh = meshFilter.sharedMesh;

            if (sharedMesh == null)
            {
                continue;
            }

            if (source.SubMeshIndex < 0 || source.SubMeshIndex >= sharedMesh.subMeshCount)
            {
                continue;
            }

            combineCount++;
        }

        if (combineCount <= 0)
        {
            return null;
        }

        CombineInstance[] combineInstances = new CombineInstance[combineCount];

        int combineIndex = 0;

        Matrix4x4 worldToLocal = _combinedRoot.worldToLocalMatrix;

        for (int index = 0; index < subMeshSources.Count; index++)
        {
            SubMeshSource source = subMeshSources[index];
            MeshFilter meshFilter = source.MeshFilter;

            if (meshFilter == null)
            {
                continue;
            }

            Mesh sharedMesh = meshFilter.sharedMesh;

            if (sharedMesh == null)
            {
                continue;
            }

            if (source.SubMeshIndex < 0 || source.SubMeshIndex >= sharedMesh.subMeshCount)
            {
                continue;
            }

            CombineInstance combineInstance = new CombineInstance();
            combineInstance.mesh = sharedMesh;
            combineInstance.subMeshIndex = source.SubMeshIndex;
            combineInstance.transform = worldToLocal * meshFilter.transform.localToWorldMatrix;

            combineInstances[combineIndex] = combineInstance;
            combineIndex++;
        }

        Mesh groupMesh = new Mesh();
        groupMesh.indexFormat = IndexFormat.UInt32;
        groupMesh.CombineMeshes(combineInstances, true, true);

        return groupMesh;
    }

    private void DestroyGroupMeshes(List<Mesh> groupMeshes)
    {
        for (int index = 0; index < groupMeshes.Count; index++)
        {
            Mesh mesh = groupMeshes[index];

            if (mesh == null)
            {
                continue;
            }

            if (Application.isPlaying == false)
            {
                Object.DestroyImmediate(mesh);
            }
            else
            {
                Object.Destroy(mesh);
            }
        }
    }
}
