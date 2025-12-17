using UnityEngine;

public class BulletFireExecutor : FireExecutor
{
    [SerializeField] private BulletSpawnerRef _bulletSpawnerRef;
    [SerializeField] private Transform _muzzle;

    protected override bool TryFireInternal()
    {
        if (HasAimPoint == true)
        {
            Vector3 direction = AimPoint - _muzzle.position;

            if (direction.sqrMagnitude > 0.0001f)
            {
                _muzzle.rotation = Quaternion.LookRotation(direction);
            }
        }

        _bulletSpawnerRef.Value.Spawn(_muzzle.position, _muzzle.rotation, TargetTag);

        return true;
    }
}
