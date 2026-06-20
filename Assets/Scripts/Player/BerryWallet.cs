using System;
using UnityEngine;

public sealed class BerryWallet : MonoBehaviour
{
    private const float DefaultConsumeCooldownSeconds = 3f;

    [SerializeField] private Item _item;
    [SerializeField] private int _berries;
    [SerializeField] private float _consumeCooldownSeconds = DefaultConsumeCooldownSeconds;

    private float _cooldownStartedAt = -DefaultConsumeCooldownSeconds;

    public event Action<int> BerriesChanged;

    public int Berries => _berries;
    public float CooldownProgress01
    {
        get
        {
            if (_consumeCooldownSeconds <= 0f)
            {
                return 1f;
            }

            float cooldownElapsed = Time.time - _cooldownStartedAt;

            return Mathf.Clamp01(cooldownElapsed / _consumeCooldownSeconds);
        }
    }

    public bool CanConsume => _berries > 0 && CooldownProgress01 >= 1f;
    public bool IsConsumeCoolingDown => CooldownProgress01 < 1f;

    private void Awake()
    {
        if (_item == null)
        {
            throw new InvalidOperationException(nameof(_item));
        }

        if (_consumeCooldownSeconds < 0f)
        {
            throw new InvalidOperationException(nameof(_consumeCooldownSeconds));
        }
    }

    public bool TryConsume(CharacterEffects effects)
    {
        if (effects == null)
        {
            throw new InvalidOperationException(nameof(effects));
        }

        if (CanConsume == false)
        {
            return false;
        }

        for (int effectIndex = 0; effectIndex < _item.Effects.Count; effectIndex++)
        {
            ItemEffect effect = _item.Effects[effectIndex];
            effect.Apply(effects);
        }

        _berries--;
        _cooldownStartedAt = Time.time;
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
