using UnityEngine;

public class PickupSpawner : Spawner<BasePickup>
{
    [SerializeField] private PickupSpawnerRef _link;

    protected override void Awake()
    {
        base.Awake();

        for (int i = 0; i < PoolSize; i++)
        {
            BasePickup pickup = Pool.Get();
            Pool.Release(pickup);
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

    public BasePickup Spawn(Vector3 position)
    {
        BasePickup pickup = Pool.Get();
        pickup.transform.position = position;
        pickup.gameObject.SetActive(true);
        pickup.GetComponent<PickupReturner>().Initialize(this);
        return pickup;
    }

    public void Despawn(BasePickup pickup)
    {
        Pool.Release(pickup);
    }

    protected override void ActionOnRelease(BasePickup pickup)
    {
        pickup.gameObject.SetActive(false);
    }
}
