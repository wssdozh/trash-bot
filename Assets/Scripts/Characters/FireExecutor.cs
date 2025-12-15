using UnityEngine;

public class FireExecutor : MonoBehaviour
{
    [SerializeField] private BulletSpawner _bulletSpawner;
    [SerializeField] private Transform _muzzle;
    [SerializeField] private float _fireRatePerSecond = 5f;
    [SerializeField] private string _targetTag = "Enemy";

    private float _cooldownSeconds;
    private bool _isFiring;

    private void Update()
    {
        if (_isFiring == false)
        {
            return;
        }

        _cooldownSeconds -= Time.deltaTime;
        TryFire();
    }

    public void StartFiring()
    {
        _isFiring = true;
    }

    public void StopFiring()
    {
        _isFiring = false;
    }

    public void SetTargetTag(string targetTag)
    {
        _targetTag = targetTag;
    }

    public bool TryFire()
    {
        if (_cooldownSeconds > 0f)
        {
            return false;
        }

        float secondsPerShot = 1f / _fireRatePerSecond;
        _cooldownSeconds = secondsPerShot;

        _bulletSpawner.Spawn(_muzzle.position, _muzzle.rotation, _targetTag);
        return true;
    }
}
