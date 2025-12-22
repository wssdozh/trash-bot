using System.Collections;
using UnityEngine;

public abstract class FireExecutor : MonoBehaviour
{
    [SerializeField] private float _fireRatePerSecond = 5f;
    [SerializeField] private float _delayFire = 0.12f;
    [SerializeField] private string _targetTag = "Enemy";

    private Coroutine _firingCoroutine;
    private bool _isFiring;
    private Vector3 _aimPoint;
    private bool _hasAimPoint;

    protected float FireRatePerSecond => _fireRatePerSecond;
    protected string TargetTag => _targetTag;
    protected bool HasAimPoint
    {
        get { return _hasAimPoint; }
    }

    protected Vector3 AimPoint
    {
        get { return _aimPoint; }
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

    public void SetTargetTag(string targetTag)
    {
        _targetTag = targetTag;
    }

    public bool TryFire()
    {
        return TryFireInternal();
    }

    protected abstract bool TryFireInternal();

    private IEnumerator FiringCoroutine()
    {
        yield return new WaitForSeconds(_delayFire);

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
