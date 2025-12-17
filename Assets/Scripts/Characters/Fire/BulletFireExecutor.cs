using UnityEngine;

public class BulletFireExecutor : FireExecutor
{
    [SerializeField] private BulletSpawnerRef _bulletSpawnerRef;
    [SerializeField] private Transform _muzzle;

    protected override bool TryFireInternal()
    {
        _bulletSpawnerRef.Value.Spawn(_muzzle.position, _muzzle.rotation, TargetTag);
        return true;
    }
}
