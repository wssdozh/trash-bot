using System;
using System.Collections;
using UnityEngine;

public sealed class AmmoParticleSystem : AmmoLifeListener
{
    [Header("Зависимости")]
    [SerializeField] private ParticleSystem _particleSystem;

    private Coroutine _playRoutine;

    public bool IsAlive => _particleSystem.IsAlive(true);

    protected override void Awake()
    {
        base.Awake();

        if (_particleSystem == null)
        {
            throw new InvalidOperationException(nameof(_particleSystem));
        }
    }

    protected override void OnAmmoEnabled()
    {
        StopPlayRoutineIfRunning();

        _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        _playRoutine = StartCoroutine(PlayNextFrame());
    }

    private IEnumerator PlayNextFrame()
    {
        yield return null;

        _particleSystem.Play(true);
        _playRoutine = null;
    }

    protected override void OnAmmoLifeEnded()
    {
        StopPlayRoutineIfRunning();

        _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        StopPlayRoutineIfRunning();

        _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void StopPlayRoutineIfRunning()
    {
        if (_playRoutine == null)
        {
            return;
        }

        StopCoroutine(_playRoutine);
        _playRoutine = null;
    }
}
