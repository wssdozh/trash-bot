using System;
using UnityEngine;

public sealed class RocketShotStrategy : IShotStrategy
{
    private readonly AmmoSpawner _ammoSpawner;
    private readonly FireModifierState _modifierState;

    private readonly float _minDamage;
    private readonly float _maxDamage;

    public bool IsBusy => false;

    public RocketShotStrategy(AmmoSpawner ammoSpawner, FireModifierState modifierState, float minDamage, float maxDamage)
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

        Ammo ammo = _ammoSpawner.Spawn(context.Position, context.Rotation, context.TargetLayers, context.IgnoredRoot);

        Rocket rocket = ammo as Rocket;

        if (rocket == null)
        {
            throw new InvalidOperationException(nameof(rocket));
        }

        float damage = context.DamageCalculator.CalculateScaledDamage(_minDamage, _maxDamage);

        rocket.SetDamage(damage);
        rocket.SetSpeedMultiplier(_modifierState.ProjectileSpeedMultiplier);
        rocket.SetExplosionRadiusMultiplier(_modifierState.ExplosionRadiusMultiplier);

        return true;

    }

    public void Tick(FireShotContext context)
    {
    }

    public void Stop()
    {
    }
}
