using System;
using System.Collections.Generic;
using UnityEngine;

public class PickupSpawner : Spawner<BasePickup>
{
    private readonly Dictionary<int, PickupReturner> _returnersByPickupId = new Dictionary<int, PickupReturner>(16);

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
        pickup.transform.SetParent(null, true);
        pickup.transform.position = position;
        PickupReturner pickupReturner = GetPickupReturner(pickup);
        pickupReturner.SetSpawner(this);
        pickup.gameObject.SetActive(true);

        return pickup;
    }

    public override void Despawn(BasePickup pickup)
    {
        Pool.Release(pickup);
    }

    protected override void ActionOnGet(BasePickup pickup)
    {
    }

    protected override void ActionOnRelease(BasePickup pickup)
    {
        pickup.transform.SetParent(transform, true);
        pickup.gameObject.SetActive(false);
    }

    private PickupReturner GetPickupReturner(BasePickup pickup)
    {
        int pickupId = pickup.GetInstanceID();

        if (_returnersByPickupId.TryGetValue(pickupId, out PickupReturner cachedReturner))
        {
            if (cachedReturner != null)
            {
                return cachedReturner;
            }
        }

        PickupReturner pickupReturner = pickup.GetComponent<PickupReturner>();

        if (pickupReturner == null)
        {
            throw new InvalidOperationException(nameof(pickupReturner));
        }

        _returnersByPickupId[pickupId] = pickupReturner;

        return pickupReturner;
    }
}
