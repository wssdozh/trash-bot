using System;
using UnityEngine;

public sealed class BerryWallet : MonoBehaviour
{
    [SerializeField] private Item _item;
    [SerializeField] private int _berries;

    public int Berries => _berries;

    public event Action<int> BerriesChanged;

    private void Awake()
    {
        if (_item == null)
        {
            throw new InvalidOperationException(nameof(_item));
        }
    }

    public bool TryConsume(CharacterEffects effects)
    {
        if (effects == null)
        {
            throw new InvalidOperationException(nameof(effects));
        }

        if (_berries <= 0)
        {
            return false;
        }

        for (int effectIndex = 0; effectIndex < _item.Effects.Count; effectIndex++)
        {
            ItemEffect effect = _item.Effects[effectIndex];
            effect.Apply(effects);
        }

        _berries--;
        InvokeBerriesChanged();

        return true;
    }

    public void Add(int amount)
    {
        if (amount < 0)
        {
            throw new InvalidOperationException(nameof(amount));
        }

        _berries += amount;
        InvokeBerriesChanged();
    }

    private void InvokeBerriesChanged()
    {
        if (BerriesChanged != null)
        {
            BerriesChanged.Invoke(_berries);
        }
    }
}
