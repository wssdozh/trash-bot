using System;
using System.Collections;
using UnityEngine;

public sealed class ShotgunFireExecutor : FireExecutor
{
    [Header("Зависимости")]
    [SerializeField] private Transform _muzzle;
    [SerializeField] private Ammo _pelletPrefab;

    [Header("Пеллеты")]
    [SerializeField] private int _minPelletsPerShot = 6;
    [SerializeField] private int _maxPelletsPerShot = 10;
    [SerializeField] private float _spreadAngleDegrees = 6f;
    [SerializeField] private float _pelletIntervalSeconds = 0.01f;

    [Header("Урон")]
    [SerializeField] private float _minPelletDamage = 1f;
    [SerializeField] private float _maxPelletDamage = 2f;

    private AmmoSpawner _pelletSpawner;
    private Coroutine _burstCoroutine;

    private void Start()
    {
        _pelletSpawner = SpawnerServiceLocator.Get<Ammo>(_pelletPrefab.name) as AmmoSpawner;

        if (_pelletSpawner == null)
        {
            throw new InvalidOperationException(nameof(_pelletSpawner));
        }
    }

    protected override bool TryFireInternal()
    {
        if (HasAimPoint == false)
        {
            return false;
        }

        if (_pelletSpawner == null)
        {
            throw new InvalidOperationException(nameof(_pelletSpawner));
        }

        if (_burstCoroutine != null)
        {
            return false;
        }

        RotateMuzzleToAimPoint(_muzzle);

        _burstCoroutine = StartCoroutine(BurstCoroutine());

        return true;
    }

    private IEnumerator BurstCoroutine()
    {
        int pelletsPerShot = UnityEngine.Random.Range(_minPelletsPerShot, _maxPelletsPerShot + 1);

        WaitForSeconds wait = new WaitForSeconds(_pelletIntervalSeconds);

        for (int i = 0; i < pelletsPerShot; i++)
        {

            Quaternion pelletRotation = GetPelletRotation(_muzzle.rotation);

            Ammo pellet = _pelletSpawner.Spawn(_muzzle.position, pelletRotation, TargetLayers);

            float damage = CalculateScaledDamage(_minPelletDamage, _maxPelletDamage);

            pellet.SetDamage(damage);

            yield return wait;

        }

        _burstCoroutine = null;
    }

    private Quaternion GetPelletRotation(Quaternion muzzleRotation)
    {
        float yawDegrees = UnityEngine.Random.Range(-_spreadAngleDegrees, _spreadAngleDegrees);
        float pitchDegrees = UnityEngine.Random.Range(-_spreadAngleDegrees, _spreadAngleDegrees);

        Quaternion spreadRotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);

        return muzzleRotation * spreadRotation;
    }
}
