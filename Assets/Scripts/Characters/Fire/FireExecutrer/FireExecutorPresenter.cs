using System;
using UnityEngine;

public sealed class FireExecutorPresenter
{
    private readonly Transform _ownerTransform;
    private readonly Transform _muzzle;

    private readonly IShotStrategy _shotStrategy;
    private readonly IFireRateProvider _fireRateProvider;
    private readonly IDamageCalculator _damageCalculator;

    private LayerMask _targetLayers;
    private float _maxAimAngleDegrees;

    private bool _isFiring;
    private float _nextShotTime;
    private float _lastShotSecondsPerShot;

    private bool _hasAimPoint;
    private Vector3 _aimPoint;

    public bool HasAimPoint => _hasAimPoint;

    public Vector3 AimPoint => _aimPoint;

    public FireExecutorPresenter(
        Transform ownerTransform,
        Transform muzzle,
        IShotStrategy shotStrategy,
        IFireRateProvider fireRateProvider,
        IDamageCalculator damageCalculator,
        LayerMask targetLayers,
        float maxAimAngleDegrees)
    {
        _ownerTransform = ownerTransform;
        _muzzle = muzzle;

        _shotStrategy = shotStrategy;
        _fireRateProvider = fireRateProvider;
        _damageCalculator = damageCalculator;

        _targetLayers = targetLayers;
        _maxAimAngleDegrees = maxAimAngleDegrees;
    }

    public void OnEnable()
    {
        _nextShotTime = 0f;
        _lastShotSecondsPerShot = 0f;

        _isFiring = false;

        _hasAimPoint = false;

        _shotStrategy.Stop();
    }

    public void OnDisable()
    {
        StopFiring();

        _shotStrategy.Stop();
    }

    public void Tick(float timeSeconds)
    {
        FireShotContext context = CreateContext(timeSeconds);

        _shotStrategy.Tick(context);

        if (_isFiring == false)
        {
            return;
        }

        if (timeSeconds < _nextShotTime)
        {
            return;
        }

        if (_hasAimPoint == false)
        {
            return;
        }

        Quaternion muzzleRotation;

        if (TryGetClampedMuzzleRotation(out muzzleRotation) == false)
        {
            return;
        }

        _muzzle.rotation = muzzleRotation;

        FireShotContext shotContext = CreateContext(timeSeconds);
        shotContext.Rotation = muzzleRotation;

        bool hasStartedShot = _shotStrategy.TryStartShot(shotContext);

        if (hasStartedShot == false)
        {
            return;
        }

        float effectiveFireRatePerSecond = _fireRateProvider.GetEffectiveFireRatePerSecond();
        float secondsPerShot = 1f / effectiveFireRatePerSecond;

        _lastShotSecondsPerShot = secondsPerShot;
        _nextShotTime = timeSeconds + secondsPerShot;
    }

    public float GetFireCooldown01(float timeSeconds)
    {
        if (timeSeconds >= _nextShotTime)
        {
            return 1f;
        }

        if (_lastShotSecondsPerShot <= 0f)
        {
            return 0f;
        }

        float remainingSeconds = _nextShotTime - timeSeconds;
        float cooldown01 = 1f - (remainingSeconds / _lastShotSecondsPerShot);

        return Mathf.Clamp01(cooldown01);
    }

    public void SetAimPoint(Vector3 aimPoint)
    {
        _aimPoint = aimPoint;
        _hasAimPoint = true;
    }

    public void ClearAimPoint()
    {
        _hasAimPoint = false;
    }

    public bool TryStartFiring()
    {
        if (_isFiring)
        {
            return false;
        }

        _isFiring = true;

        return true;
    }

    public void StartFiring()
    {
        TryStartFiring();
    }

    public void StopFiring()
    {
        if (_isFiring == false)
        {
            return;
        }

        _isFiring = false;

        _shotStrategy.Stop();
    }

    public bool TryFireOnce(float timeSeconds)
    {
        if (timeSeconds < _nextShotTime)
        {
            return false;
        }

        if (_hasAimPoint == false)
        {
            return false;
        }

        Quaternion muzzleRotation;

        if (TryGetClampedMuzzleRotation(out muzzleRotation) == false)
        {
            return false;
        }

        _muzzle.rotation = muzzleRotation;

        FireShotContext context = CreateContext(timeSeconds);
        context.Rotation = muzzleRotation;

        bool hasStartedShot = _shotStrategy.TryStartShot(context);

        if (hasStartedShot == false)
        {
            return false;
        }

        float effectiveFireRatePerSecond = _fireRateProvider.GetEffectiveFireRatePerSecond();
        float secondsPerShot = 1f / effectiveFireRatePerSecond;

        _lastShotSecondsPerShot = secondsPerShot;
        _nextShotTime = timeSeconds + secondsPerShot;

        return true;
    }

    public void SetTargetLayers(LayerMask targetLayers)
    {
        _targetLayers = targetLayers;
    }

    private FireShotContext CreateContext(float timeSeconds)
    {
        FireShotContext context = new FireShotContext();

        context.TimeSeconds = timeSeconds;

        context.Position = _muzzle.position;
        context.Rotation = _muzzle.rotation;

        context.TargetLayers = _targetLayers;

        context.DamageCalculator = _damageCalculator;

        return context;
    }

    private bool TryGetClampedMuzzleRotation(out Quaternion muzzleRotation)
    {
        muzzleRotation = _muzzle.rotation;

        Vector3 desiredDirection = _aimPoint - _muzzle.position;

        if (desiredDirection.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        Vector3 upAxis = _ownerTransform.root.up;

        Vector3 flatDirection = Vector3.ProjectOnPlane(desiredDirection, upAxis);

        if (flatDirection.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        Quaternion yawRotation = Quaternion.LookRotation(flatDirection, upAxis);

        Vector3 rightAxis = yawRotation * Vector3.right;

        float pitchDegrees = Vector3.SignedAngle(flatDirection, desiredDirection, rightAxis);

        float clampedPitchDegrees = Mathf.Clamp(pitchDegrees, -_maxAimAngleDegrees, _maxAimAngleDegrees);

        Quaternion pitchRotation = Quaternion.AngleAxis(clampedPitchDegrees, Vector3.right);

        muzzleRotation = yawRotation * pitchRotation;

        return true;
    }
}
