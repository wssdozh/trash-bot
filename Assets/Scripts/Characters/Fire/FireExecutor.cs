using System;
using System.Collections;
using UnityEngine;

public abstract class FireExecutor : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private float _fireRatePerSecond = 5f;
    [SerializeField] private LayerMask _targetLayers;
    [SerializeField] private float _maxAimAngleDegrees = 30f;

    private Coroutine _firingCoroutine;
    private bool _isFiring;
    private float _nextShotTime;
    private float _lastShotSecondsPerShot;

    private float _fireRateMultiplier = 1f;
    private float _damageMultiplier = 1f;

    protected float DamageMultiplier => _damageMultiplier;
    protected float FireRatePerSecond => _fireRatePerSecond;
    protected LayerMask TargetLayers => _targetLayers;

    protected bool HasAimPoint { get; private set; }
    protected Vector3 AimPoint { get; private set; }

    protected virtual void Awake()
    {
        if (_fireRatePerSecond <= 0f)
        {
            throw new InvalidOperationException(nameof(_fireRatePerSecond));
        }

        if (_maxAimAngleDegrees <= 0f)
        {
            throw new InvalidOperationException(nameof(_maxAimAngleDegrees));
        }
    }

    protected virtual void OnEnable()
    {
        _nextShotTime = 0f;
        _lastShotSecondsPerShot = 0f;

        _fireRateMultiplier = 1f;
        _damageMultiplier = 1f;
    }

    public float GetFireCooldown01()
    {
        if (Time.time >= _nextShotTime)
        {
            return 1f;
        }

        if (_lastShotSecondsPerShot <= 0f)
        {
            return 0f;
        }

        float remainingSeconds = _nextShotTime - Time.time;
        float cooldown01 = 1f - (remainingSeconds / _lastShotSecondsPerShot);

        return Mathf.Clamp01(cooldown01);
    }

    public void SetAimPoint(Vector3 aimPoint)
    {
        AimPoint = aimPoint;
        HasAimPoint = true;
    }

    public void ClearAimPoint()
    {
        HasAimPoint = false;
    }

    public bool TryStartFiring()
    {
        if (_isFiring == true)
        {
            return false;
        }

        _isFiring = true;
        _firingCoroutine = StartCoroutine(FiringCoroutine());

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

        if (_firingCoroutine != null)
        {
            StopCoroutine(_firingCoroutine);
            _firingCoroutine = null;
        }
    }

    public void SetTargetLayers(LayerMask targetLayers)
    {
        _targetLayers = targetLayers;
    }

    public bool TryFire()
    {
        if (Time.time < _nextShotTime)
        {
            return false;
        }

        float effectiveFireRatePerSecond = GetEffectiveFireRatePerSecond();

        float secondsPerShot = 1f / effectiveFireRatePerSecond;

        bool hasFired = TryFireInternal();

        if (hasFired == false)
        {
            return false;
        }

        _lastShotSecondsPerShot = secondsPerShot;
        _nextShotTime = Time.time + secondsPerShot;

        return true;
    }

    protected abstract bool TryFireInternal();

    protected float GetEffectiveFireRatePerSecond()
    {
        float effectiveFireRatePerSecond = _fireRatePerSecond * _fireRateMultiplier;

        if (effectiveFireRatePerSecond <= 0f)
        {
            throw new InvalidOperationException(nameof(effectiveFireRatePerSecond));
        }

        return effectiveFireRatePerSecond;
    }

    protected float CalculateScaledDamage(float minDamage, float maxDamage)
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
        float scaledDamage = baseDamage * _damageMultiplier;

        return scaledDamage;
    }

    protected void RotateMuzzleToAimPoint(Transform muzzle)
    {
        if (HasAimPoint == false)
        {
            return;
        }

        Vector3 desiredDirection = AimPoint - muzzle.position;

        if (desiredDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector3 upAxis = transform.up;

        Vector3 flatDirection = Vector3.ProjectOnPlane(desiredDirection, upAxis);

        if (flatDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion yawRotation = Quaternion.LookRotation(flatDirection, upAxis);

        Vector3 rightAxis = yawRotation * Vector3.right;

        float pitchDegrees = Vector3.SignedAngle(flatDirection, desiredDirection, rightAxis);

        float clampedPitchDegrees = Mathf.Clamp(pitchDegrees, -_maxAimAngleDegrees, _maxAimAngleDegrees);

        Quaternion pitchRotation = Quaternion.AngleAxis(clampedPitchDegrees, Vector3.right);

        muzzle.rotation = yawRotation * pitchRotation;
    }

    private IEnumerator FiringCoroutine()
    {
        while (_isFiring == true)
        {

            TryFire();

            yield return null;

        }

        _firingCoroutine = null;
    }

    public void SetFireRateMultiplier(float fireRateMultiplier)
    {
        if (fireRateMultiplier <= 0f)
        {
            throw new InvalidOperationException(nameof(fireRateMultiplier));
        }

        _fireRateMultiplier = fireRateMultiplier;
    }

    public void SetDamageMultiplier(float damageMultiplier)
    {
        if (damageMultiplier <= 0f)
        {
            throw new InvalidOperationException(nameof(damageMultiplier));
        }

        _damageMultiplier = damageMultiplier;
    }
}
