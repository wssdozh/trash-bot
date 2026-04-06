using System;
using UnityEngine;

public sealed class BulletFireExecutor : FireExecutor
{
    [Header("Dependencies")]
    [SerializeField] private Transform _muzzle;
    [SerializeField] private Ammo _bulletPrefab;

    [Header("Damage")]
    [SerializeField] private float _minDamage = 3f;
    [SerializeField] private float _maxDamage = 6f;

    [Header("Rocket")]
    [SerializeField] private float _rocketRadiusMultiplier = 1f;

    private AmmoSpawner _ammoSpawner;

    protected override Transform Muzzle => _muzzle;

    protected override IShotStrategy CreateShotStrategy(FireModifierState modifierState)
    {
        _ammoSpawner = SpawnerServiceLocator.Get<Ammo>(_bulletPrefab.name) as AmmoSpawner;

        if (_ammoSpawner == null)
        {
            throw new InvalidOperationException(nameof(_ammoSpawner));
        }

        return new BulletShotStrategy(_ammoSpawner, modifierState, _minDamage, _maxDamage, _rocketRadiusMultiplier);
    }
}
