using UnityEngine;

public class BulletFireExecutor : FireExecutor
{
    [SerializeField] private BulletSpawner _bulletSpawner;
    [SerializeField] private Transform _muzzle;

    protected override bool TryFireInternal()
    {
        _bulletSpawner.Spawn(_muzzle.position, _muzzle.rotation, TargetTag);
        return true;
    }
}
