using UnityEngine;

public class PickupSpawner : Spawner<BasePickup>
{
    protected override void Awake()
    {
        base.Awake();

        for (int i = 0; i < PoolSize; i++)
        {
            BasePickup pickup = Pool.Get();
            Pool.Release(pickup);
        }
    }

    public override BasePickup Spawn(Vector3 position)
    {
        BasePickup pickup = Pool.Get();
        pickup.transform.position = position;
        pickup.GetComponent<PickupReturner>().SetSpawner(this);

        return pickup;
    }

    public override void Despawn(BasePickup pickup)
    {
        Pool.Release(pickup);
    }
}