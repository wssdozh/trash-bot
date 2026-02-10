using System;
using UnityEngine;

public sealed class ShotgunFireExecutor : FireExecutor
{
    [Header("Зависимости")]
    [SerializeField] private Transform _muzzle;
    [SerializeField] private Ammo _pelletPrefab;

    [Header("Настройки")]
    [SerializeField] private int _minPelletsPerShot = 6;
    [SerializeField] private int _maxPelletsPerShot = 10;
    [SerializeField] private float _spreadAngleDegrees = 6f;
    [SerializeField] private float _pelletIntervalSeconds = 0.01f;

    [Header("Урон")]
    [SerializeField] private float _minPelletDamage = 1f;
    [SerializeField] private float _maxPelletDamage = 2f;

    private AmmoSpawner _ammoSpawner;

    protected override Transform Muzzle => _muzzle;

    protected override IShotStrategy CreateShotStrategy(FireModifierState modifierState)
    {

        _ammoSpawner = SpawnerServiceLocator.Get<Ammo>(_pelletPrefab.name) as AmmoSpawner;

        if (_ammoSpawner == null)
        {
            throw new InvalidOperationException(nameof(_ammoSpawner));
        }

        return new ShotgunBurstShotStrategy(
            _ammoSpawner,
            modifierState,
            _minPelletsPerShot,
            _maxPelletsPerShot,
            _spreadAngleDegrees,
            _pelletIntervalSeconds,
            _minPelletDamage,
            _maxPelletDamage);

    }
}
