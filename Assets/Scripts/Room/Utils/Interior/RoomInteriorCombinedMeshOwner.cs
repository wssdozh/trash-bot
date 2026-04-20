using UnityEngine;

public sealed class RoomInteriorCombinedMeshOwner : MonoBehaviour
{
    private Mesh _mesh;
    private MeshCollider _meshCollider;
    private MeshFilter _staticMeshFilter;
    private MeshFilter _notStaticMeshFilter;
    private bool _isInitialized;

    public void Initialize(Mesh mesh, MeshCollider meshCollider, MeshFilter staticMeshFilter, MeshFilter notStaticMeshFilter)
    {
        if (mesh == null)
        {
            throw new MissingReferenceException(nameof(mesh));
        }

        _mesh = mesh;
        _meshCollider = meshCollider;
        _staticMeshFilter = staticMeshFilter;
        _notStaticMeshFilter = notStaticMeshFilter;
        _isInitialized = true;
    }

    private void OnDestroy()
    {
        if (_isInitialized == false)
        {
            return;
        }

        ClearMeshReferences();
        DestroyMesh();
        _isInitialized = false;
    }

    private void ClearMeshReferences()
    {
        if (_meshCollider != null)
        {
            _meshCollider.sharedMesh = null;
        }

        if (_staticMeshFilter != null)
        {
            _staticMeshFilter.sharedMesh = null;
        }

        if (_notStaticMeshFilter != null)
        {
            _notStaticMeshFilter.sharedMesh = null;
        }
    }

    private void DestroyMesh()
    {
        if (_mesh == null)
        {
            return;
        }

        if (Application.isPlaying == false)
        {
            DestroyImmediate(_mesh);
        }
        else
        {
            Destroy(_mesh);
        }

        _mesh = null;
    }
}
