using System;
using UnityEngine;

public sealed class BerryPickup : BaseAnimatedPickup
{
    private BerryWallet _berryWallet;

    protected override void OnEnable()
    {
        base.OnEnable();
        _berryWallet = null;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _berryWallet = null;
    }

    public override bool TryCollect(GameObject player, Inventory inventory)
    {
        _berryWallet = player.GetComponent<BerryWallet>();

        if (_berryWallet == null)
        {
            _berryWallet = player.GetComponentInParent<BerryWallet>();
        }

        if (_berryWallet == null)
        {
            _berryWallet = player.GetComponentInChildren<BerryWallet>();
        }

        if (_berryWallet == null)
        {
            throw new InvalidOperationException(nameof(_berryWallet));
        }

        Pickup(player);

        return true;
    }

    protected override void OnConsumed(GameObject player)
    {
        if (_berryWallet == null)
        {
            throw new InvalidOperationException(nameof(_berryWallet));
        }

        _berryWallet.Add(Amount);
        _berryWallet = null;

        base.OnConsumed(player);
    }
}
