using UnityEngine;

public class BulletSpawner : Spawner<Bullet>
{
    [SerializeField] private BulletSpawnerRef _link;

    protected override void Awake()
    {
        base.Awake();

        for (int i = 0; i < PoolSize; i++)
        {
            Bullet bullet = Pool.Get();
            Pool.Release(bullet);
        }

        if (_link == null == false)
        {
            _link.Set(this);
        }
    }

    private void OnDestroy()
    {
        if (_link == null == false)
        {
            if (_link.Value == this)
            {
                _link.Clear();
            }
        }
    }

    public Bullet Spawn(Vector3 position, Quaternion rotation, string targetTag)
    {
        Bullet bullet = Pool.Get();
        bullet.transform.SetPositionAndRotation(position, rotation);
        bullet.SetTag(targetTag);
        bullet.GetComponent<BulletReturner>().Initialize(this);
        bullet.gameObject.SetActive(true);
        return bullet;
    }

    public void Despawn(Bullet bullet)
    {
        Pool.Release(bullet);
    }

    protected override void ActionOnRelease(Bullet bullet)
    {
        bullet.gameObject.SetActive(false);
    }
}
