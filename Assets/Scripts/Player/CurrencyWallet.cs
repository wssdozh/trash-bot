using System;
using UnityEngine;

public sealed class CurrencyWallet : MonoBehaviour
{
    [SerializeField] private int _coins;

    public int Coins => _coins;

    public event Action<int> CoinsChanged;

    public bool CanSpend(int amount)
    {
        if (amount < 0)
        {
            throw new InvalidOperationException(nameof(amount));
        }

        return _coins >= amount;
    }

    public bool TrySpend(int amount)
    {
        if (amount < 0)
        {
            throw new InvalidOperationException(nameof(amount));
        }

        if (CanSpend(amount) == false)
        {
            return false;
        }

        _coins -= amount;
        InvokeCoinsChanged();

        return true;
    }

    public void Add(int amount)
    {
        if (amount < 0)
        {
            throw new InvalidOperationException(nameof(amount));
        }

        _coins += amount;
        InvokeCoinsChanged();
    }

    private void InvokeCoinsChanged()
    {
        if (CoinsChanged != null)
        {
            CoinsChanged.Invoke(_coins);
        }
    }
}
