using UnityEngine;

public class BulletSpawner : Spawner<Bullet>
{
    protected override void Awake()
    {
        base.Awake();

        for (int i = 0; i < PoolSize; i++)
        {
            Bullet bullet = Pool.Get();
            Pool.Release(bullet);
        }
    }

    public Bullet Spawn(Vector3 position, Quaternion rotation, LayerMask targetLayers)
    {
        Bullet bullet = Pool.Get();
        bullet.transform.SetPositionAndRotation(position, rotation);
        bullet.SetLayers(targetLayers);
        bullet.GetComponent<BulletReturner>().Initialize(this);

        return bullet;
    }

    public override Bullet Spawn(Vector3 position)
    {
        return Spawn(position, Quaternion.identity, default);
    }

    public override void Despawn(Bullet bullet)
    {
        Pool.Release(bullet);
    }

    protected override void ActionOnRelease(Bullet bullet)
    {
        bullet.gameObject.SetActive(false);
    }
}