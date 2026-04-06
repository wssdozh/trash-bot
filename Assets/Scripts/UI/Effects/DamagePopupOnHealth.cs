using System;
using UnityEngine;

public class DamagePopupOnHealth : MonoBehaviour
{
    [SerializeField] private Health _health;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private GameObject _prefab;

    private DamagePopupSpawner _spawner;
    private float _previousValue;
    private bool _isSubscribed;

    private void Start()
    {
        if (_health == null)
        {
            throw new InvalidOperationException(nameof(_health));
        }

        if (_spawnPoint == null)
        {
            throw new InvalidOperationException(nameof(_spawnPoint));
        }

        if (_prefab == null)
        {
            throw new InvalidOperationException(nameof(_prefab));
        }

        _spawner = SpawnerServiceLocator.Get<DamagePopup>(_prefab.name) as DamagePopupSpawner;

        if (_spawner == null)
        {
            throw new InvalidOperationException(nameof(_spawner));
        }
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Initialize(Health health, Transform spawnPoint)
    {
        _health = health;
        _spawnPoint = spawnPoint;

        if (isActiveAndEnabled == false)
        {
            return;
        }

        Subscribe();
    }

    private void Subscribe()
    {
        if (_health == null)
        {
            return;
        }

        if (_isSubscribed)
        {
            return;
        }

        _previousValue = _health.Value;
        _health.Changed += OnHealthChanged;
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

        _health.Changed -= OnHealthChanged;
        _isSubscribed = false;
    }

    private void OnHealthChanged()
    {
        if (_spawner == null)
        {
            return;
        }

        float currentValue = _health.Value;
        float damageDelta = _previousValue - currentValue;
        _previousValue = currentValue;

        if (damageDelta > 0f)
        {
            _spawner.Show(damageDelta, _spawnPoint.position);
        }
    }
}
