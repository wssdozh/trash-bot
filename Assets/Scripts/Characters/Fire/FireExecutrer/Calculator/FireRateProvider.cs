using System;

public sealed class FireRateProvider : IFireRateProvider
{
    private readonly float _baseFireRatePerSecond;
    private readonly FireModifierState _modifierState;

    public FireRateProvider(float baseFireRatePerSecond, FireModifierState modifierState)
    {
        if (baseFireRatePerSecond <= 0f)
        {
            throw new InvalidOperationException(nameof(baseFireRatePerSecond));
        }

        if (modifierState == null)
        {
            throw new InvalidOperationException(nameof(modifierState));
        }

        _baseFireRatePerSecond = baseFireRatePerSecond;
        _modifierState = modifierState;
    }

    public float GetEffectiveFireRatePerSecond()
    {
        float effectiveFireRatePerSecond = _baseFireRatePerSecond * _modifierState.FireRateMultiplier;

        if (effectiveFireRatePerSecond <= 0f)
        {
            throw new InvalidOperationException(nameof(effectiveFireRatePerSecond));
        }

        return effectiveFireRatePerSecond;
    }
}
