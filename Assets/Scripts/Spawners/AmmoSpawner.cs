using System;
using System.Collections.Generic;
using UnityEngine;

public class AmmoSpawner : Spawner<Ammo>, IAmmoSpawner
{
    private readonly Dictionary<int, AmmoReturner> _returnersByAmmoId = new Dictionary<int, AmmoReturner>(32);

    protected override void Awake()
    {
        base.Awake();

        for (int i = 0; i < PoolSize; i++)
        {
            Ammo ammo = Pool.Get();
            Pool.Release(ammo);
        }
    }

    public Ammo Spawn(Vector3 position, Quaternion rotation, LayerMask targetLayers, Transform ignoredRoot)
    {
        Ammo ammo = Pool.Get();
        ammo.transform.SetPositionAndRotation(position, rotation);
        ammo.SetLayers(targetLayers);
        ammo.SetIgnoredRoot(ignoredRoot);
        AmmoReturner ammoReturner = GetAmmoReturner(ammo);
        ammoReturner.Initialize(this);
        ammo.gameObject.SetActive(true);

        return ammo;
    }

    public override Ammo Spawn(Vector3 position)
    {
        return Spawn(position, Quaternion.identity, default, null);
    }

    public override void Despawn(Ammo ammo)
    {
        Pool.Release(ammo);
    }

    protected override void ActionOnGet(Ammo ammo)
    {
    }

    protected override void ActionOnRelease(Ammo ammo)
    {
        ammo.gameObject.SetActive(false);
    }

    private AmmoReturner GetAmmoReturner(Ammo ammo)
    {
        int ammoId = ammo.GetInstanceID();

        if (_returnersByAmmoId.TryGetValue(ammoId, out AmmoReturner cachedReturner))
        {
            if (cachedReturner != null)
            {
                return cachedReturner;
            }
        }

        AmmoReturner ammoReturner = ammo.GetComponent<AmmoReturner>();

        if (ammoReturner == null)
        {
            throw new InvalidOperationException(nameof(ammoReturner));
        }

        _returnersByAmmoId[ammoId] = ammoReturner;

        return ammoReturner;
    }
}
