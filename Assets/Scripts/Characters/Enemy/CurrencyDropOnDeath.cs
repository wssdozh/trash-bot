using System;
using UnityEngine;

public sealed class CurrencyDropOnDeath : MonoBehaviour
{
    [SerializeField] private Health _health;
    [SerializeField] private CurrencyPickup _pickupPrefab;
    [SerializeField] private int _dropCountMin = 2;
    [SerializeField] private int _dropCountMax = 4;
    [SerializeField] private int _amountPerCoin = 1;
    [SerializeField] private Vector3 _spawnOffset = new Vector3(0f, 0.75f, 0f);
    [SerializeField] private float _forceXMin = -1.4f;
    [SerializeField] private float _forceXMax = 1.4f;
    [SerializeField] private float _forceYMin = 3.2f;
    [SerializeField] private float _forceYMax = 4.4f;
    [SerializeField] private float _forceZMin = -1.4f;
    [SerializeField] private float _forceZMax = 1.4f;

    private Spawner<BasePickup> _pickupSpawner;
    private bool _isDropped;

    private void Awake()
    {
        if (_health == null)
        {
            throw new InvalidOperationException(nameof(_health));
        }

        if (_pickupPrefab == null)
        {
            throw new InvalidOperationException(nameof(_pickupPrefab));
        }

        if (_dropCountMin <= 0)
        {
            throw new InvalidOperationException(nameof(_dropCountMin));
        }

        if (_dropCountMax < _dropCountMin)
        {
            throw new InvalidOperationException(nameof(_dropCountMax));
        }

        if (_amountPerCoin <= 0)
        {
            throw new InvalidOperationException(nameof(_amountPerCoin));
        }
    }

    private void Start()
    {
        _pickupSpawner = SpawnerServiceLocator.Get<BasePickup>(_pickupPrefab.name);
    }

    private void OnEnable()
    {
        _isDropped = false;
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

        SpawnCoins();
    }

    private void SpawnCoins()
    {
        int dropCount = UnityEngine.Random.Range(_dropCountMin, _dropCountMax + 1);

        for (int i = 0; i < dropCount; i++)
        {
            Vector3 spawnPosition = transform.TransformPoint(_spawnOffset);
            BasePickup pickup = _pickupSpawner.Spawn(spawnPosition);
            Rigidbody rigidbody = pickup.GetComponent<Rigidbody>();
            Vector3 impulse = GetImpulse();
            Vector3 worldImpulse = transform.TransformDirection(impulse);

            pickup.transform.rotation = _pickupPrefab.transform.rotation;
            pickup.SetAmount(_amountPerCoin);

            if (rigidbody != null)
            {
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                rigidbody.AddForce(worldImpulse, ForceMode.Impulse);
            }
        }
    }

    private Vector3 GetImpulse()
    {
        float forceX = UnityEngine.Random.Range(_forceXMin, _forceXMax);
        float forceY = UnityEngine.Random.Range(_forceYMin, _forceYMax);
        float forceZ = UnityEngine.Random.Range(_forceZMin, _forceZMax);

        return new Vector3(forceX, forceY, forceZ);
    }
}
