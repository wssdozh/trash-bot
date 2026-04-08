using System;
using UnityEngine;

public sealed class CurrencyPickup : BaseAnimatedPickup
{
    private CurrencyWallet _currencyWallet;

    protected override void OnEnable()
    {
        base.OnEnable();
        _currencyWallet = null;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _currencyWallet = null;
    }

    public override bool TryCollect(GameObject player, Inventory inventory)
    {
        _currencyWallet = player.GetComponent<CurrencyWallet>();

        if (_currencyWallet == null)
        {
            _currencyWallet = player.GetComponentInParent<CurrencyWallet>();
        }

        if (_currencyWallet == null)
        {
            _currencyWallet = player.GetComponentInChildren<CurrencyWallet>();
        }

        if (_currencyWallet == null)
        {
            throw new InvalidOperationException(nameof(_currencyWallet));
        }

        Pickup(player);

        return true;
    }

    protected override void OnConsumed(GameObject player)
    {
        if (_currencyWallet == null)
        {
            throw new InvalidOperationException(nameof(_currencyWallet));
        }

        _currencyWallet.Add(Amount);
        _currencyWallet = null;

        base.OnConsumed(player);
    }
}
