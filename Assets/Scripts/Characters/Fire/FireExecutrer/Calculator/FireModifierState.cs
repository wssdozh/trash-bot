using System;
using UnityEngine;

public sealed class FireModifierState
{
    private WeaponModifierContext _context;

    public float FireRateMultiplier => _context.FireRateMultiplier;
    public float DamageMultiplier => _context.DamageMultiplier;

    public float SpreadMultiplier => _context.SpreadMultiplier;
    public int PelletBonus => _context.PelletBonus;

    public float ProjectileSpeedMultiplier => _context.ProjectileSpeedMultiplier;
    public float ExplosionRadiusMultiplier => _context.ExplosionRadiusMultiplier;

    public float CriticalChance01 => Mathf.Clamp01(_context.CriticalChance01);
    public float CriticalDamageMultiplier => _context.CriticalDamageMultiplier;

    public void SetContext(WeaponModifierContext context)
    {

        if (context.FireRateMultiplier <= 0f)
        {
            throw new InvalidOperationException(nameof(context.FireRateMultiplier));
        }

        if (context.DamageMultiplier <= 0f)
        {
            throw new InvalidOperationException(nameof(context.DamageMultiplier));
        }

        if (context.SpreadMultiplier <= 0f)
        {
            throw new InvalidOperationException(nameof(context.SpreadMultiplier));
        }

        if (context.ProjectileSpeedMultiplier <= 0f)
        {
            throw new InvalidOperationException(nameof(context.ProjectileSpeedMultiplier));
        }

        if (context.ExplosionRadiusMultiplier <= 0f)
        {
            throw new InvalidOperationException(nameof(context.ExplosionRadiusMultiplier));
        }

        if (context.CriticalDamageMultiplier < 1f)
        {
            throw new InvalidOperationException(nameof(context.CriticalDamageMultiplier));
        }

        _context = context;

    }
}
