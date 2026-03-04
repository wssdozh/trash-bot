using System;
using UnityEngine;

public class DamagePopupOnHealth : MonoBehaviour
{
    [SerializeField] private Health _health;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private GameObject _prefab;

    private DamagePopupSpawner _spawner;
    private float _previousValue;

    private void Awake()
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
    }

    private void Start()
    {
        _spawner = SpawnerServiceLocator.Get<DamagePopup>(_prefab.name) as DamagePopupSpawner;

        if (_spawner == null)
        {
            throw new InvalidOperationException(nameof(_spawner));
        }
    }

    private void OnEnable()
    {
        _previousValue = _health.Value;
        _health.Changed += OnHealthChanged;
    }

    private void OnDisable()
    {
        _health.Changed -= OnHealthChanged;
    }

    private void OnHealthChanged()
    {
        float currentValue = _health.Value;
        float damageDelta = _previousValue - currentValue;
        _previousValue = currentValue;

        if (damageDelta > 0f)
        {
            _spawner.Show(damageDelta, _spawnPoint.position);
        }
    }
}
