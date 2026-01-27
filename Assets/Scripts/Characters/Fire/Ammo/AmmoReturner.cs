using System;
using System.Collections;
using UnityEngine;

public class AmmoReturner : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private Ammo _ammo;

    private IAmmoSpawner _spawner;
    private Coroutine _returnRoutine;

    public Ammo Ammo => _ammo;

    public event Action Return;

    public void Initialize(IAmmoSpawner spawner)
    {
        _spawner = spawner;
    }

    private void OnEnable()
    {
        _ammo.LifeEnded += OnLifeEnded;
    }

    private void OnDisable()
    {
        _ammo.LifeEnded -= OnLifeEnded;

        StopReturnRoutineIfRunning();
    }

    private void OnLifeEnded()
    {
        StopReturnRoutineIfRunning();

        _returnRoutine = StartCoroutine(ReturnAfterEffectsEnded());
    }

    private IEnumerator ReturnAfterEffectsEnded()
    {
        float trailTimeSeconds = 0f;

        AmmoTrailRenderer ammoTrailRenderer = GetComponent<AmmoTrailRenderer>();

        if (ammoTrailRenderer == null == false)
        {
            trailTimeSeconds = ammoTrailRenderer.TrailTimeSeconds;
        }

        if (trailTimeSeconds > 0f)
        {
            yield return new WaitForSeconds(trailTimeSeconds);
        }

        AmmoParticleSystem ammoParticleSystem = GetComponent<AmmoParticleSystem>();

        if (ammoParticleSystem == null == false)
        {
            while (ammoParticleSystem.IsAlive == true)
            {
                yield return null;
            }
        }

        _returnRoutine = null;

        ReturnNow();
    }

    private void ReturnNow()
    {
        Action returnAction = Return;

        if (returnAction == null == false)
        {
            returnAction.Invoke();
        }

        if (_spawner == null == false)
        {
            _spawner.Despawn(_ammo);

            return;
        }

        _ammo.gameObject.SetActive(false);
    }

    private void StopReturnRoutineIfRunning()
    {
        if (_returnRoutine == null)
        {
            return;
        }

        StopCoroutine(_returnRoutine);
        _returnRoutine = null;
    }
}
