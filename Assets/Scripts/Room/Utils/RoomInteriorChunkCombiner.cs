using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RoomInteriorChunkCombiner : MonoBehaviour
{
    [SerializeField] private Transform _interiorBlocksRoot;
    [SerializeField] private Transform _combinedRoot;

    [SerializeField] private RoomInteriorChunkRootCompositionProfile _chunkRootCompositionProfile;

    [SerializeField] private float _blockSize = 1f;

    [SerializeField, Min(2)] private int _chunkSizeInCells = 12;

    [SerializeField] private bool _includeInactive = false;

    [SerializeField] private bool _disableSourceRenderers = true;
    [SerializeField] private bool _disableSourceColliders = false;

    [SerializeField] private bool _createMeshCollider = true;
    [SerializeField] private bool _meshColliderConvex = false;
    [SerializeField] private bool _meshColliderIsTrigger = false;

    [ContextMenu("Combine")]
    public void Combine()
    {
        if (_interiorBlocksRoot == null)
        {
            throw new MissingReferenceException(nameof(_interiorBlocksRoot));
        }

        if (_chunkRootCompositionProfile == null)
        {
            throw new MissingReferenceException(nameof(_chunkRootCompositionProfile));
        }

        EnsureCombinedRoot();

        ClearCombined();

        List<RoomInteriorBlockRange> blockRanges = new List<RoomInteriorBlockRange>();

        RoomInteriorBlockRangeCollector.Collect(
            _interiorBlocksRoot,
            _combinedRoot,
            _blockSize,
            _chunkSizeInCells,
            _includeInactive,
            blockRanges
        );

        RoomInteriorVoxelClusterer clusterer = new RoomInteriorVoxelClusterer();

        List<HashSet<int>> clusters = clusterer.BuildClusters(blockRanges);

        HashSet<int> combinedSourceObjectIds = new HashSet<int>();

        RoomInteriorClusterMeshCombiner meshCombiner = new RoomInteriorClusterMeshCombiner(
            _combinedRoot,
            _chunkRootCompositionProfile,
            _createMeshCollider,
            _meshColliderConvex,
            _meshColliderIsTrigger
        );

        for (int clusterIndex = 0; clusterIndex < clusters.Count; clusterIndex++)
        {
            HashSet<int> objectIndices = clusters[clusterIndex];

            meshCombiner.CreateClusterRoot(clusterIndex, objectIndices, blockRanges, combinedSourceObjectIds);
        }

        MeshFilter[] meshFilters = _interiorBlocksRoot.GetComponentsInChildren<MeshFilter>(_includeInactive);

        if (_disableSourceRenderers == true)
        {
            RoomInteriorSourceDisabler.DisableRenderers(meshFilters, _combinedRoot, combinedSourceObjectIds);
        }

        if (_disableSourceColliders == true)
        {
            RoomInteriorSourceDisabler.DisableColliders(meshFilters, _combinedRoot, combinedSourceObjectIds);
        }
    }

    [ContextMenu("Clear Combined")]
    public void ClearCombined()
    {
        if (_combinedRoot == null)
        {
            return;
        }

        int childCount = _combinedRoot.childCount;

        for (int childIndex = childCount - 1; childIndex >= 0; childIndex--)
        {
            Transform childTransform = _combinedRoot.GetChild(childIndex);

            if (Application.isPlaying == false)
            {
                DestroyImmediate(childTransform.gameObject);
            }
            else
            {
                Destroy(childTransform.gameObject);
            }
        }
    }

    private void EnsureCombinedRoot()
    {
        if (_combinedRoot != null)
        {
            return;
        }

        GameObject combined = new GameObject("__InteriorCombined");
        combined.transform.SetParent(_interiorBlocksRoot, false);

        _combinedRoot = combined.transform;
    }
}
