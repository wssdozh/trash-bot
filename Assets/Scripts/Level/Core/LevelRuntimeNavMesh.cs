using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class LevelRuntimeNavMesh : MonoBehaviour
{
    [SerializeField] private int _agentTypeId;
    [SerializeField, Min(16)] private int _tileSize = 64;
    [SerializeField, Min(0.01f)] private float _voxelSize = 0.05f;
    [SerializeField, Min(0f)] private float _minRegionArea;
    [SerializeField, Min(0f)] private float _buildDelay = 0.15f;
    [SerializeField, Min(0f)] private float _updateDelay = 0.05f;
    [SerializeField] private NavMeshCollectGeometry _editorGeometry = NavMeshCollectGeometry.RenderMeshes;
    [SerializeField] private NavMeshCollectGeometry _runtimeGeometry = NavMeshCollectGeometry.PhysicsColliders;

    private NavMeshSurface _navMeshSurface;
    private AsyncOperation _updateOperation;
    private bool _hasPendingBuild;
    private bool _hasPendingUpdate;
    private int _buildRequestFrame;
    private float _buildRequestTime;
    private float _updateRequestTime;

    private void Awake()
    {
        EnsureSurface();
        RefreshLoopState();
    }

    private void LateUpdate()
    {
        if (_hasPendingBuild)
        {
            if (_updateOperation != null && _updateOperation.isDone == false)
            {
                return;
            }

            if (Time.frameCount <= _buildRequestFrame)
            {
                return;
            }

            float timeSinceBuildRequest = Time.unscaledTime - _buildRequestTime;

            if (timeSinceBuildRequest < _buildDelay)
            {
                return;
            }

            BuildNow();

            return;
        }

        if (_hasPendingUpdate == false)
        {
            RefreshLoopState();

            return;
        }

        if (_updateOperation != null && _updateOperation.isDone == false)
        {
            return;
        }

        float timeSinceRequest = Time.unscaledTime - _updateRequestTime;

        if (timeSinceRequest < _updateDelay)
        {
            return;
        }

        UpdateNow();
        RefreshLoopState();
    }

    public void RequestBuild()
    {
        EnsureSurface();

        if (Application.isPlaying == false)
        {
            BuildNow();

            return;
        }

        _hasPendingBuild = true;
        _hasPendingUpdate = false;
        _buildRequestFrame = Time.frameCount;
        _buildRequestTime = Time.unscaledTime;
        RefreshLoopState();
    }

    public void BuildNow()
    {
        EnsureSurface();
        ApplySurfaceSettings();

        _hasPendingBuild = false;
        _hasPendingUpdate = false;
        _updateOperation = null;
        _navMeshSurface.BuildNavMesh();
        RefreshLoopState();
    }

    public void RequestUpdate()
    {
        EnsureSurface();
        ApplySurfaceSettings();

        if (Application.isPlaying == false)
        {
            BuildNow();

            return;
        }

        if (_hasPendingBuild == true)
        {
            return;
        }

        _updateRequestTime = Time.unscaledTime;
        _hasPendingUpdate = true;
        RefreshLoopState();
    }

    public void ClearData()
    {
        if (_navMeshSurface == null)
        {
            return;
        }

        _hasPendingBuild = false;
        _hasPendingUpdate = false;
        _updateOperation = null;

        _navMeshSurface.RemoveData();
        _navMeshSurface.navMeshData = null;
        RefreshLoopState();
    }

    private void UpdateNow()
    {
        EnsureSurface();
        ApplySurfaceSettings();

        _hasPendingUpdate = false;

        if (_navMeshSurface.navMeshData == null)
        {
            BuildNow();

            return;
        }

        _updateOperation = _navMeshSurface.UpdateNavMesh(_navMeshSurface.navMeshData);
        RefreshLoopState();
    }

    private void EnsureSurface()
    {
        if (_navMeshSurface != null)
        {
            return;
        }

        _navMeshSurface = GetComponent<NavMeshSurface>();

        if (_navMeshSurface == null)
        {
            _navMeshSurface = gameObject.AddComponent<NavMeshSurface>();
        }

        ApplySurfaceSettings();
    }

    private void ApplySurfaceSettings()
    {
        if (_navMeshSurface == null)
        {
            return;
        }

        NavMeshCollectGeometry geometry = _editorGeometry;

        if (Application.isPlaying)
        {
            geometry = _runtimeGeometry;
        }

        _navMeshSurface.agentTypeID = _agentTypeId;
        _navMeshSurface.collectObjects = CollectObjects.Children;
        _navMeshSurface.useGeometry = geometry;
        _navMeshSurface.ignoreNavMeshAgent = true;
        _navMeshSurface.ignoreNavMeshObstacle = true;
        _navMeshSurface.overrideTileSize = true;
        _navMeshSurface.tileSize = _tileSize;
        _navMeshSurface.overrideVoxelSize = true;
        _navMeshSurface.voxelSize = _voxelSize;
        _navMeshSurface.minRegionArea = _minRegionArea;
    }

    private void RefreshLoopState()
    {
        if (Application.isPlaying == false)
        {
            return;
        }

        bool isLoopNeeded = _hasPendingBuild || _hasPendingUpdate;

        if (_updateOperation != null && _updateOperation.isDone == false)
        {
            isLoopNeeded = true;
        }

        enabled = isLoopNeeded;
    }

}
