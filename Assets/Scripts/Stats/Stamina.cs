using UnityEngine;


public class Stamina : Stat
{
    [SerializeField] private bool _autoRegen = true;
    [SerializeField] private float _regenPerSecond = 5f;
    [SerializeField] private float _regenDelay = 1.0f;

    private float _sinceLastSpend;

    private void Update()
    {
        if (_autoRegen == false || _value >= _maxValue)
            return;

        _sinceLastSpend += Time.unscaledDeltaTime;

        if (_sinceLastSpend >= _regenDelay)
        {
            Increase(_regenPerSecond * Time.unscaledDeltaTime);
        }
    }

    public override void Decrease(float amount)
    {
        base.Decrease(amount);
        _sinceLastSpend = 0f;
    }
}