using System;
using UnityEngine;

public sealed class FireDamageCalculator : IDamageCalculator
{
    private readonly FireModifierState _modifierState;

    public FireDamageCalculator(FireModifierState modifierState)
    {
        if (modifierState == null)
        {
            throw new InvalidOperationException(nameof(modifierState));
        }

        _modifierState = modifierState;
    }

    public float CalculateScaledDamage(float minDamage, float maxDamage)
    {

        if (minDamage <= 0f)
        {
            throw new InvalidOperationException(nameof(minDamage));
        }

        if (maxDamage < minDamage)
        {
            throw new InvalidOperationException(nameof(maxDamage));
        }

        float baseDamage = UnityEngine.Random.Range(minDamage, maxDamage);

        float scaledDamage = baseDamage * _modifierState.DamageMultiplier;

        bool isCritical = UnityEngine.Random.value <= _modifierState.CriticalChance01;

        if (isCritical == true)
        {
            scaledDamage *= _modifierState.CriticalDamageMultiplier;
        }

        return scaledDamage;

    }
}
