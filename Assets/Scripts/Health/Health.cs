using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float _minValue = 0f;
    [SerializeField] private float _maxValue = 100f;
    [SerializeField] private float _value = 100f;

    public event Action Changed;
    public event Action Healed;
    public event Action Damaged;
    public event Action Ended;

    public float MinValue => _minValue;
    public float MaxValue => _maxValue;
    public float Value => _value;

    public void Increase(float amount)
    {
        if (amount <= 0f || _value >= _maxValue)
            return;

        _value = Mathf.Min(_value + amount, _maxValue);

        Healed?.Invoke();
        Changed?.Invoke();
    }

    public void Decrease(float amount)
    {
        if (amount <= 0f || _value <= _minValue)
            return;

        _value = Mathf.Max(_value - amount, _minValue);

        Damaged?.Invoke();
        Changed?.Invoke();

        if (_value <= _minValue)
            Ended?.Invoke();
    }
}