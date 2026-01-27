using System;
using System.Collections;
using UnityEngine;

public abstract class FireExecutor : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private float _fireRatePerSecond = 5f;
    [SerializeField] private LayerMask _targetLayers;

    private Coroutine _firingCoroutine;
    private bool _isFiring;
    private float _nextShotTime;
    private float _lastShotSecondsPerShot;

    protected float FireRatePerSecond => _fireRatePerSecond;
    protected LayerMask TargetLayers => _targetLayers;

    protected bool HasAimPoint { get; private set; }
    protected Vector3 AimPoint { get; private set; }

    protected virtual void Awake()
    {
        if (_fireRatePerSecond <= 0f)
            throw new InvalidOperationException(nameof(_fireRatePerSecond));
    }

    protected virtual void OnEnable()
    {
        _nextShotTime = 0f;
        _lastShotSecondsPerShot = 0f;
    }

    public float GetFireCooldown01()
    {
        if (Time.time >= _nextShotTime)
            return 1f;

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
            return false;

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
            return;

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
            return false;

        float secondsPerShot = 1f / _fireRatePerSecond;

        _lastShotSecondsPerShot = secondsPerShot;
        _nextShotTime = Time.time + secondsPerShot;

        return TryFireInternal();
    }

    protected abstract bool TryFireInternal();

    private IEnumerator FiringCoroutine()
    {
        float secondsPerShot = 1f / _fireRatePerSecond;
        WaitForSeconds wait = new WaitForSeconds(secondsPerShot);

        while (_isFiring == true)
        {
            TryFire();

            yield return wait;
        }

        _firingCoroutine = null;
    }
}
