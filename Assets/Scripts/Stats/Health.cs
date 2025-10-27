using UnityEngine;

public class Health : Stat
{
    [SerializeField] private bool _autoRegen = true;
    [SerializeField] private float _regenPerSecond = 2f;
    [SerializeField] private float _regenDelay = 3f;

    private float _sinceLastDamage;

    private void Update()
    {
        if (_autoRegen == false || _value >= _maxValue)
            return;

        _sinceLastDamage += Time.unscaledDeltaTime;

        if (_sinceLastDamage >= _regenDelay)
        {
            Increase(_regenPerSecond * Time.unscaledDeltaTime);
        }
    }

    public override void Decrease(float amount)
    {
        base.Decrease(amount);
        _sinceLastDamage = 0f;
    }
}
