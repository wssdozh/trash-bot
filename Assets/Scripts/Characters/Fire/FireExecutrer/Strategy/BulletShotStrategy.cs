using System;
using UnityEngine;

public sealed class BulletShotStrategy : IShotStrategy
{
    private readonly AmmoSpawner _ammoSpawner;
    private readonly FireModifierState _modifierState;

    private readonly float _minDamage;
    private readonly float _maxDamage;

    public bool IsBusy => false;

    public BulletShotStrategy(AmmoSpawner ammoSpawner, FireModifierState modifierState, float minDamage, float maxDamage)
    {
        _ammoSpawner = ammoSpawner;

        if (modifierState == null)
        {
            throw new InvalidOperationException(nameof(modifierState));
        }

        _modifierState = modifierState;

        _minDamage = minDamage;
        _maxDamage = maxDamage;
    }

    public bool TryStartShot(FireShotContext context)
    {
        if (_ammoSpawner == null)
        {
            throw new InvalidOperationException(nameof(_ammoSpawner));
        }

        Ammo ammo = _ammoSpawner.Spawn(context.Position, context.Rotation, context.TargetLayers);

        float damage = context.DamageCalculator.CalculateScaledDamage(_minDamage, _maxDamage);

        ammo.SetDamage(damage);
        ammo.SetSpeedMultiplier(_modifierState.ProjectileSpeedMultiplier);

        return true;
    }

    public void Tick(FireShotContext context)
    {
    }

    public void Stop()
    {
    }
}
