using UnityEngine;

public sealed class RoomInteriorChunkDynamicOnDamage : MonoBehaviour
{
    private ChunkVariantSwitcherBase _chunkVariantSwitcher;
    private GameObject _staticObject;
    private GameObject _notStaticObject;
    private Health _health;

    private MeshCollider _rootCollider;
    private MeshCollider _staticCollider;
    private BoxCollider _notStaticCollider;

    private Rigidbody _staticRigidbody;
    private Rigidbody _notStaticRigidbody;

    private bool _dynamicEnabled;
    private bool _isSubscribed;

    public void Initialize(ChunkVariantSwitcherBase chunkVariantSwitcher)
    {
        _chunkVariantSwitcher = chunkVariantSwitcher;
    }

    public void Initialize(ChunkVariantSwitcherBase chunkVariantSwitcher, GameObject staticObject, GameObject notStaticObject, Health health)
    {
        _chunkVariantSwitcher = chunkVariantSwitcher;
        _staticObject = staticObject;
        _notStaticObject = notStaticObject;
        _health = health;

        ConfigureVariants();
        Subscribe();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void ApplyDamage()
    {
        EnableDynamic();
    }

    public void ApplyDamage(int damage)
    {
        EnableDynamic();
    }

    public void ApplyDamage(float damage)
    {
        EnableDynamic();
    }

    private void OnHealthDecreased()
    {
        EnableDynamic();
    }

    private void OnHealthEnded()
    {
        if (_dynamicEnabled == false)
        {
            EnableDynamic();
        }

        RequestNavMeshUpdate();
        Destroy(gameObject);
    }

    private void EnableDynamic()
    {
        if (_chunkVariantSwitcher == null)
        {
            throw new MissingReferenceException(nameof(_chunkVariantSwitcher));
        }

        if (_dynamicEnabled == true)
        {
            return;
        }

        _dynamicEnabled = true;

        ApplyState();
    }

    private void ConfigureVariants()
    {
        if (_staticObject == null)
        {
            throw new MissingReferenceException(nameof(_staticObject));
        }

        if (_notStaticObject == null)
        {
            throw new MissingReferenceException(nameof(_notStaticObject));
        }

        if (_health == null)
        {
            throw new MissingReferenceException(nameof(_health));
        }

        _rootCollider = GetComponent<MeshCollider>();
        _staticCollider = EnsureMeshCollider(_staticObject, false);
        _notStaticCollider = EnsureBoxCollider(_notStaticObject);

        _staticRigidbody = EnsureRigidbody(_staticObject);
        _notStaticRigidbody = EnsureRigidbody(_notStaticObject);

        _dynamicEnabled = _notStaticObject.activeSelf;

        ApplyState();
    }

    private void ApplyState()
    {
        if (_chunkVariantSwitcher == null)
        {
            throw new MissingReferenceException(nameof(_chunkVariantSwitcher));
        }

        if (_dynamicEnabled)
        {
            ApplyDynamicState();

            return;
        }

        ApplyStaticState();
    }

    private void ApplyStaticState()
    {
        _chunkVariantSwitcher.SetUseStatic(true);

        gameObject.isStatic = true;

        if (_rootCollider != null)
        {
            _rootCollider.enabled = false;
        }

        if (_staticCollider != null)
        {
            _staticCollider.enabled = true;
        }

        if (_notStaticCollider != null)
        {
            _notStaticCollider.enabled = false;
        }

        if (_staticRigidbody != null)
        {
            if (_staticRigidbody.isKinematic == false)
            {
                _staticRigidbody.linearVelocity = Vector3.zero;
                _staticRigidbody.angularVelocity = Vector3.zero;
            }

            _staticRigidbody.isKinematic = true;
            _staticRigidbody.useGravity = false;
        }

        if (_notStaticRigidbody != null)
        {
            if (_notStaticRigidbody.isKinematic == false)
            {
                _notStaticRigidbody.linearVelocity = Vector3.zero;
                _notStaticRigidbody.angularVelocity = Vector3.zero;
            }

            _notStaticRigidbody.isKinematic = true;
            _notStaticRigidbody.useGravity = false;
        }
    }

    private void ApplyDynamicState()
    {
        _chunkVariantSwitcher.SetUseStatic(false);

        gameObject.isStatic = false;

        if (_rootCollider != null)
        {
            _rootCollider.enabled = false;
        }

        if (_staticCollider != null)
        {
            _staticCollider.enabled = false;
        }

        if (_notStaticCollider != null)
        {
            _notStaticCollider.enabled = true;
        }

        if (_staticRigidbody != null)
        {
            if (_staticRigidbody.isKinematic == false)
            {
                _staticRigidbody.linearVelocity = Vector3.zero;
                _staticRigidbody.angularVelocity = Vector3.zero;
            }

            _staticRigidbody.isKinematic = true;
            _staticRigidbody.useGravity = false;
        }

        if (_notStaticRigidbody != null)
        {
            _notStaticRigidbody.isKinematic = false;
            _notStaticRigidbody.useGravity = true;
            _notStaticRigidbody.WakeUp();
        }
    }

    private void Subscribe()
    {
        if (_isSubscribed == true)
        {
            return;
        }

        if (_health == null)
        {
            return;
        }

        _health.Decreased += OnHealthDecreased;
        _health.Ended += OnHealthEnded;
        _isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (_isSubscribed == false)
        {
            return;
        }

        if (_health == null)
        {
            _isSubscribed = false;

            return;
        }

        _health.Decreased -= OnHealthDecreased;
        _health.Ended -= OnHealthEnded;
        _isSubscribed = false;
    }

    private MeshCollider EnsureMeshCollider(GameObject targetObject, bool isConvex)
    {
        MeshCollider meshCollider = targetObject.GetComponent<MeshCollider>();

        if (meshCollider == null)
        {
            meshCollider = targetObject.AddComponent<MeshCollider>();
        }

        MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();

        if (meshFilter == null)
        {
            throw new MissingReferenceException(nameof(meshFilter));
        }

        if (meshFilter.sharedMesh == null)
        {
            throw new MissingReferenceException(nameof(meshFilter.sharedMesh));
        }

        meshCollider.sharedMesh = meshFilter.sharedMesh;
        meshCollider.convex = isConvex;
        meshCollider.isTrigger = false;

        return meshCollider;
    }

    private BoxCollider EnsureBoxCollider(GameObject targetObject)
    {
        BoxCollider boxCollider = targetObject.GetComponent<BoxCollider>();

        if (boxCollider == null)
        {
            boxCollider = targetObject.AddComponent<BoxCollider>();
        }

        MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();

        if (meshFilter == null)
        {
            throw new MissingReferenceException(nameof(meshFilter));
        }

        if (meshFilter.sharedMesh == null)
        {
            throw new MissingReferenceException(nameof(meshFilter.sharedMesh));
        }

        Bounds bounds = meshFilter.sharedMesh.bounds;

        boxCollider.center = bounds.center;
        boxCollider.size = bounds.size;
        boxCollider.isTrigger = false;

        return boxCollider;
    }

    private Rigidbody EnsureRigidbody(GameObject targetObject)
    {
        Rigidbody rigidbody = targetObject.GetComponent<Rigidbody>();

        if (rigidbody == null)
        {
            rigidbody = targetObject.AddComponent<Rigidbody>();
        }

        return rigidbody;
    }

    private void RequestNavMeshUpdate()
    {
        LevelRuntimeNavMesh levelRuntimeNavMesh = GetComponentInParent<LevelRuntimeNavMesh>();

        if (levelRuntimeNavMesh == null)
        {
            return;
        }

        levelRuntimeNavMesh.RequestUpdate();
    }
}
