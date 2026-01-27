using System;
using System.Collections;
using UnityEngine;

public class ShotgunFireExecutor : FireExecutor
{
    [Header("Зависимости")]
    [SerializeField] private Transform _muzzle;
    [SerializeField] private Ammo _pelletPrefab;

    [Header("Настройки")]
    [SerializeField] private int _minPelletsPerShot = 6;
    [SerializeField] private int _maxPelletsPerShot = 10;
    [SerializeField] private float _spreadAngleDegrees = 6f;
    [SerializeField] private float _pelletIntervalSeconds = 0.01f;

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

    private void OnDisable()
    {
        if (_burstCoroutine != null)
        {
            StopCoroutine(_burstCoroutine);
            _burstCoroutine = null;
        }
    }

    protected override bool TryFireInternal()
    {
        if (HasAimPoint == true)
        {
            Vector3 direction = AimPoint - _muzzle.position;

            if (direction.sqrMagnitude > 0.0001f)
            {
                _muzzle.rotation = Quaternion.LookRotation(direction);
            }
        }

        int pelletsPerShot = UnityEngine.Random.Range(_minPelletsPerShot, _maxPelletsPerShot + 1);

        if (_burstCoroutine != null)
        {
            StopCoroutine(_burstCoroutine);
            _burstCoroutine = null;
        }

        _burstCoroutine = StartCoroutine(BurstCoroutine(pelletsPerShot));

        return true;
    }

    private IEnumerator BurstCoroutine(int pelletsPerShot)
    {
        int pelletIndex = 0;

        while (pelletIndex < pelletsPerShot)
        {
            Vector3 muzzlePosition = _muzzle.position;
            Quaternion muzzleRotation = _muzzle.rotation;
            Quaternion pelletRotation = GetPelletRotation(muzzleRotation);

            _pelletSpawner.Spawn(muzzlePosition, pelletRotation, TargetLayers);

            pelletIndex++;

            if (pelletIndex >= pelletsPerShot)
            {
                break;
            }

            if (_pelletIntervalSeconds > 0f)
            {
                yield return new WaitForSeconds(_pelletIntervalSeconds);

                continue;
            }

            yield return null;
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
