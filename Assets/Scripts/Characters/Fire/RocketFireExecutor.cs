using System;
using UnityEngine;

public sealed class RocketFireExecutor : FireExecutor
{
    [Header("Зависимости")]
    [SerializeField] private Transform _muzzle;
    [SerializeField] private Ammo _rocketPrefab;

    [Header("Урон")]
    [SerializeField] private float _minDamage = 8f;
    [SerializeField] private float _maxDamage = 12f;

    private AmmoSpawner _ammoSpawner;

    protected override Transform Muzzle => _muzzle;

    protected override IShotStrategy CreateShotStrategy(FireModifierState modifierState)
    {

        _ammoSpawner = SpawnerServiceLocator.Get<Ammo>(_rocketPrefab.name) as AmmoSpawner;

        if (_ammoSpawner == null)
        {
            throw new InvalidOperationException(nameof(_ammoSpawner));
        }

        return new RocketShotStrategy(_ammoSpawner, modifierState, _minDamage, _maxDamage);

    }
}
