using System;
using UnityEngine;

public abstract class Stat : MonoBehaviour
{
    [SerializeField] protected float _minValue = 0f;
    [SerializeField] protected float _maxValue = 100f;
    [SerializeField] protected float _value = 100f;

    public event Action Changed;
    public event Action Increased;
    public event Action Decreased;
    public event Action Ended;

    public float MinValue => _minValue;
    public float MaxValue => _maxValue;
    public float Value => _value;
    public float Normalized => (_maxValue <= _minValue) ? 0f : Mathf.InverseLerp(_minValue, _maxValue, _value);

    public virtual void Increase(float amount)
    {
        if (amount <= 0f || _value >= _maxValue)
            return;

        SetValue(Mathf.Min(_value + amount, _maxValue));
        Increased?.Invoke();
        Changed?.Invoke();
    }

    public virtual void Decrease(float amount)
    {
        if (amount <= 0f || _value <= _minValue)
            return;

        SetValue(Mathf.Max(_value - amount, _minValue));
        Decreased?.Invoke();
        Changed?.Invoke();

        if (_value <= _minValue)
            Ended?.Invoke();
    }

    public virtual void SetValue(float newValue)
    {
        _value = Mathf.Clamp(newValue, _minValue, _maxValue);
    }

    public virtual void SetMaxValue(float newMaxValue)
    {
        if (newMaxValue < _minValue)
        {
            throw new InvalidOperationException(nameof(newMaxValue));
        }

        if (Mathf.Abs(_maxValue - newMaxValue) <= Mathf.Epsilon)
        {
            return;
        }

        float normalized = Normalized;

        _maxValue = newMaxValue;
        _value = Mathf.Lerp(_minValue, _maxValue, normalized);
        Changed?.Invoke();
    }

    public virtual void Fill()
    {
        if (_value >= _maxValue)
        {
            return;
        }

        _value = _maxValue;
        Increased?.Invoke();
        Changed?.Invoke();
    }
}
