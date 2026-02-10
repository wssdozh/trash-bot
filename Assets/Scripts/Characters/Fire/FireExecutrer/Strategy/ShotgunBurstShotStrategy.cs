using System;
using UnityEngine;

public sealed class ShotgunBurstShotStrategy : IShotStrategy
{
    private readonly AmmoSpawner _ammoSpawner;
    private readonly FireModifierState _modifierState;

    private readonly int _minPelletsPerShot;
    private readonly int _maxPelletsPerShot;

    private readonly float _spreadAngleDegrees;
    private readonly float _pelletIntervalSeconds;

    private readonly float _minPelletDamage;
    private readonly float _maxPelletDamage;

    private bool _isBursting;
    private int _pelletsRemaining;
    private float _nextPelletTimeSeconds;

    public bool IsBusy => _isBursting;

    public ShotgunBurstShotStrategy(
        AmmoSpawner ammoSpawner,
        FireModifierState modifierState,
        int minPelletsPerShot,
        int maxPelletsPerShot,
        float spreadAngleDegrees,
        float pelletIntervalSeconds,
        float minPelletDamage,
        float maxPelletDamage)
    {
        _ammoSpawner = ammoSpawner;

        if (modifierState == null)
        {
            throw new InvalidOperationException(nameof(modifierState));
        }

        _modifierState = modifierState;

        _minPelletsPerShot = minPelletsPerShot;
        _maxPelletsPerShot = maxPelletsPerShot;

        _spreadAngleDegrees = spreadAngleDegrees;
        _pelletIntervalSeconds = pelletIntervalSeconds;

        _minPelletDamage = minPelletDamage;
        _maxPelletDamage = maxPelletDamage;
    }

    public bool TryStartShot(FireShotContext context)
    {

        if (_isBursting == true)
        {
            return false;
        }

        int pelletsPerShot = UnityEngine.Random.Range(_minPelletsPerShot, _maxPelletsPerShot + 1) + _modifierState.PelletBonus;

        if (pelletsPerShot < 1)
        {
            pelletsPerShot = 1;
        }

        _isBursting = true;
        _pelletsRemaining = pelletsPerShot;

        SpawnPellet(context);

        _pelletsRemaining--;

        _nextPelletTimeSeconds = context.TimeSeconds + _pelletIntervalSeconds;

        return true;

    }

    public void Tick(FireShotContext context)
    {

        if (_isBursting == false)
        {
            return;
        }

        if (_pelletsRemaining <= 0)
        {
            _isBursting = false;

            return;
        }

        if (_pelletIntervalSeconds <= 0f)
        {

            while (_pelletsRemaining > 0)
            {

                SpawnPellet(context);

                _pelletsRemaining--;

            }

            _isBursting = false;

            return;

        }

        if (context.TimeSeconds < _nextPelletTimeSeconds)
        {
            return;
        }

        while (context.TimeSeconds >= _nextPelletTimeSeconds && _pelletsRemaining > 0)
        {

            SpawnPellet(context);

            _pelletsRemaining--;

            _nextPelletTimeSeconds += _pelletIntervalSeconds;

        }

        if (_pelletsRemaining <= 0)
        {
            _isBursting = false;
        }

    }

    public void Stop()
    {
        _isBursting = false;
        _pelletsRemaining = 0;
    }

    private void SpawnPellet(FireShotContext context)
    {
        float spreadAngle = _spreadAngleDegrees * _modifierState.SpreadMultiplier;

        float yawDegrees = UnityEngine.Random.Range(-spreadAngle, spreadAngle);
        float pitchDegrees = UnityEngine.Random.Range(-spreadAngle, spreadAngle);

        Quaternion spreadRotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);

        Quaternion pelletRotation = context.Rotation * spreadRotation;

        Ammo pellet = _ammoSpawner.Spawn(context.Position, pelletRotation, context.TargetLayers);

        float damage = context.DamageCalculator.CalculateScaledDamage(_minPelletDamage, _maxPelletDamage);

        pellet.SetDamage(damage);
        pellet.SetSpeedMultiplier(_modifierState.ProjectileSpeedMultiplier);
    }
}
