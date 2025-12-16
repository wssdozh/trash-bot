using System.Collections;
using UnityEngine;

public class FireExecutor : MonoBehaviour
{
    [SerializeField] private BulletSpawner _bulletSpawner;
    [SerializeField] private Transform _muzzle;
    [SerializeField] private float _fireRatePerSecond = 5f;
    [SerializeField] private string _targetTag = "Enemy";

    private Coroutine _firingCoroutine;
    private bool _isFiring;

    public void StartFiring()
    {
        if (_isFiring == true)
        {
            return;
        }

        _isFiring = true;
        _firingCoroutine = StartCoroutine(FiringCoroutine());
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
        _bulletSpawner.Spawn(_muzzle.position, _muzzle.rotation, _targetTag);
        return true;
    }

    private IEnumerator FiringCoroutine()
    {
        while (_isFiring == true)
        {
            TryFire();

            float secondsPerShot = 1f / _fireRatePerSecond;
            yield return new WaitForSeconds(secondsPerShot);
        }

        _firingCoroutine = null;
    }
}
