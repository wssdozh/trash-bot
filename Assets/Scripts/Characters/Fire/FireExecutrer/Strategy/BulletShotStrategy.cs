using System;
using UnityEngine;

public sealed class BulletShotStrategy : IShotStrategy
{
    private readonly AmmoSpawner _ammoSpawner;
    private readonly FireModifierState _modifierState;
    private readonly float _minDamage;
    private readonly float _maxDamage;
    private readonly float _rocketRadiusMultiplier;

    public bool IsBusy => false;

    public BulletShotStrategy(AmmoSpawner ammoSpawner, FireModifierState modifierState, float minDamage, float maxDamage, float rocketRadiusMultiplier)
    {
        _ammoSpawner = ammoSpawner;

        if (modifierState == null)
        {
            throw new InvalidOperationException(nameof(modifierState));
        }

        _modifierState = modifierState;
        _minDamage = minDamage;
        _maxDamage = maxDamage;

        if (rocketRadiusMultiplier <= 0f)
        {
            throw new InvalidOperationException(nameof(rocketRadiusMultiplier));
        }

        _rocketRadiusMultiplier = rocketRadiusMultiplier;
    }

    public bool TryStartShot(FireShotContext context)
    {
        if (_ammoSpawner == null)
        {
            throw new InvalidOperationException(nameof(_ammoSpawner));
        }

        Ammo ammo = _ammoSpawner.Spawn(context.Position, context.Rotation, context.TargetLayers, context.IgnoredRoot);
        float damage = context.DamageCalculator.CalculateScaledDamage(_minDamage, _maxDamage);

        ammo.SetDamage(damage);
        ammo.SetSpeedMultiplier(_modifierState.ProjectileSpeedMultiplier);

        Rocket rocket = ammo as Rocket;

        if (rocket == null)
        {
            return true;
        }

        float radiusMultiplier = _modifierState.ExplosionRadiusMultiplier * _rocketRadiusMultiplier;
        rocket.SetExplosionRadiusMultiplier(radiusMultiplier);

        return true;
    }

    public void Tick(FireShotContext context)
    {
    }

    public void Stop()
    {
    }
}
