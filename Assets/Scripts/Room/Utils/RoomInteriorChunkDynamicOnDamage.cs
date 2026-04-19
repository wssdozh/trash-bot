using System.Collections;
using UnityEngine;

public sealed class RoomInteriorChunkDynamicOnDamage : MonoBehaviour
{
    private const float MinSinkDistance = 0.5f;

    [SerializeField, Min(0f)] private float _sinkDelay = 0.15f;
    [SerializeField, Min(0.01f)] private float _sinkDuration = 1.15f;
    [SerializeField, Min(0.1f)] private float _sinkDistanceMultiplier = 1.35f;
    [SerializeField, Min(0f)] private float _sinkTiltMaxAngle = 10f;

    private ChunkVariantSwitcherBase _chunkVariantSwitcher;
    private GameObject _staticObject;
    private GameObject _notStaticObject;
    private Health _health;

    private MeshCollider _rootCollider;
    private MeshCollider _staticCollider;
    private MeshCollider _notStaticCollider;

    private Rigidbody _staticRigidbody;
    private Rigidbody _notStaticRigidbody;

    private FeedbackGroup _notStaticFeedbackGroup;
    private Vector3 _staticInitialLocalPosition;
    private Quaternion _staticInitialLocalRotation;
    private Vector3 _notStaticInitialLocalPosition;
    private Quaternion _notStaticInitialLocalRotation;

    private bool _dynamicEnabled;
    private bool _isSubscribed;
    private bool _isSinking;

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
        bool wasDynamicEnabled = _dynamicEnabled;

        EnableDynamic();

        if (wasDynamicEnabled == false)
        {
            PlayDynamicFeedback();
        }
    }

    private void OnHealthEnded()
    {
        if (_isSinking)
            return;

        if (_dynamicEnabled == false)
            EnableDynamic();

        RequestNavMeshUpdate();
        StartSink();
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
            throw new MissingReferenceException(nameof(_staticObject));

        if (_notStaticObject == null)
            throw new MissingReferenceException(nameof(_notStaticObject));

        if (_health == null)
            throw new MissingReferenceException(nameof(_health));

        _rootCollider = GetComponent<MeshCollider>();
        _staticCollider = EnsureMeshCollider(_staticObject, false);
        _notStaticCollider = EnsureMeshCollider(_notStaticObject, false);

        _staticRigidbody = EnsureRigidbody(_staticObject);
        _notStaticRigidbody = EnsureRigidbody(_notStaticObject);
        _notStaticFeedbackGroup = GetFeedbackGroup(_notStaticObject);
        _staticInitialLocalPosition = _staticObject.transform.localPosition;
        _staticInitialLocalRotation = _staticObject.transform.localRotation;
        _notStaticInitialLocalPosition = _notStaticObject.transform.localPosition;
        _notStaticInitialLocalRotation = _notStaticObject.transform.localRotation;

        _dynamicEnabled = _notStaticObject.activeSelf;

        ApplyState();
    }

    private void ApplyState()
    {
        if (_chunkVariantSwitcher == null)
            throw new MissingReferenceException(nameof(_chunkVariantSwitcher));

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
            if (_notStaticRigidbody.isKinematic == false)
            {
                _notStaticRigidbody.linearVelocity = Vector3.zero;
                _notStaticRigidbody.angularVelocity = Vector3.zero;
            }

            _notStaticRigidbody.isKinematic = true;
            _notStaticRigidbody.useGravity = false;
        }
    }

    private void Subscribe()
    {
        if (_isSubscribed == true)
            return;

        if (_health == null)
            return;

        _health.Decreased += OnHealthDecreased;
        _health.Ended += OnHealthEnded;
        _isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (_isSubscribed == false)
            return;

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
            meshCollider = targetObject.AddComponent<MeshCollider>();

        MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();

        if (meshFilter == null)
            throw new MissingReferenceException(nameof(meshFilter));

        if (meshFilter.sharedMesh == null)
            throw new MissingReferenceException(nameof(meshFilter.sharedMesh));

        meshCollider.sharedMesh = meshFilter.sharedMesh;
        meshCollider.convex = isConvex;
        meshCollider.isTrigger = false;

        return meshCollider;
    }

    private Rigidbody EnsureRigidbody(GameObject targetObject)
    {
        Rigidbody rigidbody = targetObject.GetComponent<Rigidbody>();

        if (rigidbody == null)
            rigidbody = targetObject.AddComponent<Rigidbody>();

        return rigidbody;
    }

    private FeedbackGroup GetFeedbackGroup(GameObject targetObject)
    {
        if (targetObject == null)
            return null;

        return targetObject.GetComponentInChildren<FeedbackGroup>(true);
    }

    private void PlayDynamicFeedback()
    {
        if (_notStaticFeedbackGroup == null)
            return;

        _notStaticFeedbackGroup.Play();
    }

    private void RequestNavMeshUpdate()
    {
        LevelRuntimeNavMesh levelRuntimeNavMesh = GetComponentInParent<LevelRuntimeNavMesh>();

        if (levelRuntimeNavMesh == null)
            return;

        levelRuntimeNavMesh.RequestUpdate();
    }

    private void StartSink()
    {
        _isSinking = true;
        Unsubscribe();
        StopDynamicFeedback();
        ResetVisualTransforms();
        DisableColliders();
        DisableRigidbodies();
        StartCoroutine(SinkCoroutine());
    }

    private IEnumerator SinkCoroutine()
    {
        float delayTimer = 0f;

        while (delayTimer < _sinkDelay)
        {
            delayTimer += Time.deltaTime;

            yield return null;
        }

        Vector3 startPoint = transform.position;
        Vector3 endPoint = startPoint + (Vector3.down * GetSinkDistance());
        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = GetSinkRotation(startRotation);
        float sinkTimer = 0f;

        while (sinkTimer < _sinkDuration)
        {
            sinkTimer += Time.deltaTime;

            float sinkProgress = Mathf.Clamp01(sinkTimer / _sinkDuration);
            transform.position = Vector3.Lerp(startPoint, endPoint, sinkProgress);
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, sinkProgress);

            yield return null;
        }

        Destroy(gameObject);
    }

    private void DisableColliders()
    {
        if (_rootCollider != null)
            _rootCollider.enabled = false;

        if (_staticCollider != null)
            _staticCollider.enabled = false;

        if (_notStaticCollider != null)
            _notStaticCollider.enabled = false;
    }

    private void DisableRigidbodies()
    {
        if (_staticRigidbody != null)
        {
            if (_staticRigidbody.isKinematic == false)
            {
                _staticRigidbody.linearVelocity = Vector3.zero;
                _staticRigidbody.angularVelocity = Vector3.zero;
            }

            _staticRigidbody.useGravity = false;
            _staticRigidbody.isKinematic = true;
        }

        if (_notStaticRigidbody != null)
        {
            if (_notStaticRigidbody.isKinematic == false)
            {
                _notStaticRigidbody.linearVelocity = Vector3.zero;
                _notStaticRigidbody.angularVelocity = Vector3.zero;
            }

            _notStaticRigidbody.useGravity = false;
            _notStaticRigidbody.isKinematic = true;
        }
    }

    private Quaternion GetSinkRotation(Quaternion startRotation)
    {
        if (_sinkTiltMaxAngle <= 0f)
        {
            return startRotation;
        }

        Vector3 startEulerAngles = startRotation.eulerAngles;
        float xTilt = UnityEngine.Random.Range(-_sinkTiltMaxAngle, _sinkTiltMaxAngle);
        float zTilt = UnityEngine.Random.Range(-_sinkTiltMaxAngle, _sinkTiltMaxAngle);
        Vector3 endEulerAngles = new Vector3(startEulerAngles.x + xTilt, startEulerAngles.y, startEulerAngles.z + zTilt);

        return Quaternion.Euler(endEulerAngles);
    }

    private void StopDynamicFeedback()
    {
        if (_notStaticFeedbackGroup == null)
            return;

        _notStaticFeedbackGroup.Stop();
    }

    private void ResetVisualTransforms()
    {
        if (_staticObject != null)
        {
            _staticObject.transform.localPosition = _staticInitialLocalPosition;
            _staticObject.transform.localRotation = _staticInitialLocalRotation;
        }

        if (_notStaticObject != null)
        {
            _notStaticObject.transform.localPosition = _notStaticInitialLocalPosition;
            _notStaticObject.transform.localRotation = _notStaticInitialLocalRotation;
        }
    }

    private float GetSinkDistance()
    {
        Bounds bounds;
        bool hasBounds = TryGetVisualBounds(out bounds);

        if (hasBounds == false)
            return MinSinkDistance;

        return Mathf.Max(bounds.size.y * _sinkDistanceMultiplier, MinSinkDistance);
    }

    private bool TryGetVisualBounds(out Bounds bounds)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        int rendererIndex = 0;
        bool hasBounds = false;
        bounds = default;

        while (rendererIndex < renderers.Length)
        {
            Renderer renderer = renderers[rendererIndex];
            rendererIndex += 1;

            if (renderer == null)
                continue;

            if (renderer.enabled == false)
                continue;

            if (hasBounds == false)
            {
                bounds = renderer.bounds;
                hasBounds = true;

                continue;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        return hasBounds;
    }
}
