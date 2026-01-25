using UnityEngine;

public class BulletFireExecutor : FireExecutor
{
    [SerializeField] private Transform _muzzle;
    [SerializeField] private Bullet _bulletPrefab;

    private BulletSpawner _bulletSpawner;

    private void Start()
    {
        _bulletSpawner = SpawnerServiceLocator.Get<Bullet>(_bulletPrefab.name) as BulletSpawner;
    }

    protected override bool TryFireInternal()
    {
        if (HasAimPoint)
        {
            Vector3 direction = AimPoint - _muzzle.position;
            
            if (direction.sqrMagnitude > 0.0001f)
                _muzzle.rotation = Quaternion.LookRotation(direction);
        }

        if (_bulletSpawner != null)
            _bulletSpawner.Spawn(_muzzle.position, _muzzle.rotation, TargetLayers);

        return true;
    }
}