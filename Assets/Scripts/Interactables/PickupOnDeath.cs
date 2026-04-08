using System;
using UnityEngine;

public sealed class PickupOnDeath : MonoBehaviour
{
    [SerializeField] private Health _health;
    [SerializeField] private GameObject _intactObject;
    [SerializeField] private BasePickup _pickupPrefab;
    [SerializeField] private Vector3 _spawnOffset = new Vector3(0f, 0.75f, 0f);

    private Spawner<BasePickup> _pickupSpawner;
    private bool _isDropped;

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
    }

    private void OnHealthEnded()
    {
        if (_isDropped)
        {
            return;
        }

        _isDropped = true;
        _intactObject.SetActive(false);
        SpawnPickup();
        RequestNavMeshUpdate();
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
}
