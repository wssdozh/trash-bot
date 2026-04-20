using System.Collections;
using System;
using UnityEngine;

public sealed class PickupOnDeath : MonoBehaviour
{
    private const float MinSinkDistance = 0.5f;

    [SerializeField] private Health _health;
    [SerializeField] private GameObject _intactObject;
    [SerializeField] private BasePickup _pickupPrefab;
    [SerializeField] private Vector3 _spawnOffset = new Vector3(0f, 0.75f, 0f);
    [SerializeField] private bool _useSinkAnimation = true;
    [SerializeField, Min(0f)] private float _sinkDelay = 0.15f;
    [SerializeField, Min(0.01f)] private float _sinkDuration = 1.15f;
    [SerializeField, Min(0.1f)] private float _sinkDistanceMultiplier = 1.35f;
    [SerializeField, Min(0f)] private float _sinkTiltMaxAngle = 10f;

    private Spawner<BasePickup> _pickupSpawner;
    private bool _isDropped;
    private Coroutine _sinkCoroutine;

    private void Awake()
    {
        if (_health == null)
        {
            throw new InvalidOperationException(nameof(_health));
        }

        if (_intactObject == null)
        {
            throw new InvalidOperationException(nameof(_intactObject));
        }

        if (_pickupPrefab == null)
        {
            throw new InvalidOperationException(nameof(_pickupPrefab));
        }

        if (_sinkDelay < 0f)
        {
            throw new InvalidOperationException(nameof(_sinkDelay));
        }

        if (_sinkDuration <= 0f)
        {
            throw new InvalidOperationException(nameof(_sinkDuration));
        }

        if (_sinkDistanceMultiplier <= 0f)
        {
            throw new InvalidOperationException(nameof(_sinkDistanceMultiplier));
        }
    }

    private void Start()
    {
        _pickupSpawner = SpawnerServiceLocator.Get<BasePickup>(_pickupPrefab.name);
    }

    private void OnEnable()
    {
        _health.Ended += OnHealthEnded;
    }

    private void OnDisable()
    {
        _health.Ended -= OnHealthEnded;

        if (_sinkCoroutine != null)
        {
            StopCoroutine(_sinkCoroutine);
            _sinkCoroutine = null;
        }
    }

    private void OnHealthEnded()
    {
        if (_isDropped)
        {
            return;
        }

        _isDropped = true;
        DisableColliders();
        DisableRigidbodies();
        SpawnPickup();
        RequestNavMeshUpdate();

        if (_useSinkAnimation)
        {
            _sinkCoroutine = StartCoroutine(SinkCoroutine());

            return;
        }

        Destroy(gameObject);
    }

    private void SpawnPickup()
    {
        Vector3 spawnPosition = GetSpawnPosition();
        BasePickup pickup = _pickupSpawner.Spawn(spawnPosition);
        pickup.transform.rotation = _pickupPrefab.transform.rotation;
        pickup.SetAmount(_pickupPrefab.Amount);

        if (pickup.TryGetComponent<Rigidbody>(out Rigidbody rigidbody))
        {
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }
    }

    private Vector3 GetSpawnPosition()
    {
        Vector3 horizontalOffset = new Vector3(_spawnOffset.x, 0f, _spawnOffset.z);

        return _intactObject.transform.position + horizontalOffset + Vector3.up * _spawnOffset.y;
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

    private IEnumerator SinkCoroutine()
    {
        float delayTimer = 0f;

        while (delayTimer < _sinkDelay)
        {
            delayTimer += Time.deltaTime;

            yield return null;
        }

        Transform sinkTransform = _intactObject.transform;
        Vector3 startPoint = sinkTransform.position;
        Vector3 endPoint = startPoint + (Vector3.down * GetSinkDistance());
        Quaternion startRotation = sinkTransform.rotation;
        Quaternion endRotation = GetSinkRotation(startRotation);
        float sinkTimer = 0f;

        while (sinkTimer < _sinkDuration)
        {
            sinkTimer += Time.deltaTime;

            float sinkProgress = Mathf.Clamp01(sinkTimer / _sinkDuration);
            sinkTransform.position = Vector3.Lerp(startPoint, endPoint, sinkProgress);
            sinkTransform.rotation = Quaternion.Slerp(startRotation, endRotation, sinkProgress);

            yield return null;
        }

        Destroy(gameObject);
    }

    private void DisableColliders()
    {
        Collider[] colliders = _intactObject.GetComponentsInChildren<Collider>(true);
        int colliderIndex = 0;

        while (colliderIndex < colliders.Length)
        {
            Collider collider = colliders[colliderIndex];

            if (collider != null)
            {
                collider.enabled = false;
            }

            colliderIndex++;
        }
    }

    private void DisableRigidbodies()
    {
        Rigidbody[] rigidbodies = _intactObject.GetComponentsInChildren<Rigidbody>(true);
        int rigidbodyIndex = 0;

        while (rigidbodyIndex < rigidbodies.Length)
        {
            Rigidbody rigidbody = rigidbodies[rigidbodyIndex];

            if (rigidbody != null)
            {
                if (rigidbody.isKinematic == false)
                {
                    rigidbody.linearVelocity = Vector3.zero;
                    rigidbody.angularVelocity = Vector3.zero;
                }

                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
            }

            rigidbodyIndex++;
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

    private float GetSinkDistance()
    {
        Bounds bounds;
        bool hasBounds = TryGetSinkBounds(out bounds);

        if (hasBounds == false)
        {
            return MinSinkDistance;
        }

        return Mathf.Max(bounds.size.y * _sinkDistanceMultiplier, MinSinkDistance);
    }

    private bool TryGetSinkBounds(out Bounds bounds)
    {
        Renderer[] renderers = _intactObject.GetComponentsInChildren<Renderer>(true);
        int rendererIndex = 0;
        bool hasBounds = false;
        bounds = default;

        while (rendererIndex < renderers.Length)
        {
            Renderer renderer = renderers[rendererIndex];

            if (renderer != null)
            {
                if (hasBounds == false)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            rendererIndex++;
        }

        return hasBounds;
    }
}
