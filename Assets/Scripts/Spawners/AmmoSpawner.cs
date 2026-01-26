using UnityEngine;

public class AmmoSpawner : Spawner<Ammo>, IAmmoSpawner
{
    protected override void Awake()
    {
        base.Awake();

        for (int i = 0; i < PoolSize; i++)
        {
            Ammo bullet = Pool.Get();
            Pool.Release(bullet);
        }
    }

    public Ammo Spawn(Vector3 position, Quaternion rotation, LayerMask targetLayers)
    {
        Ammo bullet = Pool.Get();
        bullet.transform.SetPositionAndRotation(position, rotation);
        bullet.SetLayers(targetLayers);
        bullet.GetComponent<AmmoReturner>().Initialize(this);

        return bullet;
    }

    public override Ammo Spawn(Vector3 position)
    {
        return Spawn(position, Quaternion.identity, default);
    }

    public override void Despawn(Ammo bullet)
    {
        Pool.Release(bullet);
    }

    protected override void ActionOnRelease(Ammo bullet)
    {
        bullet.gameObject.SetActive(false);
    }
}