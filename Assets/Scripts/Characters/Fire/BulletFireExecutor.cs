using System;
using UnityEngine;

public sealed class BulletFireExecutor : FireExecutor
{
    [Header("Зависимости")]
    [SerializeField] private Transform _muzzle;
    [SerializeField] private Ammo _bulletPrefab;

    [Header("Урон")]
    [SerializeField] private float _minDamage = 3f;
    [SerializeField] private float _maxDamage = 6f;

    private AmmoSpawner _bulletSpawner;

    private void Start()
    {
        _bulletSpawner = SpawnerServiceLocator.Get<Ammo>(_bulletPrefab.name) as AmmoSpawner;

        if (_bulletSpawner == null)
        {
            throw new InvalidOperationException(nameof(_bulletSpawner));
        }
    }

    protected override bool TryFireInternal()
    {
        if (HasAimPoint == false)
        {
            return false;
        }

        if (_bulletSpawner == null)
        {
            throw new InvalidOperationException(nameof(_bulletSpawner));
        }

        RotateMuzzleToAimPoint(_muzzle);

        Ammo bullet = _bulletSpawner.Spawn(_muzzle.position, _muzzle.rotation, TargetLayers);

        float damage = CalculateScaledDamage(_minDamage, _maxDamage);

        bullet.SetDamage(damage);

        return true;
    }
}
