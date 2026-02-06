using System;
using System.Collections;
using UnityEngine;

public sealed class AmmoParticleSystem : AmmoLifeListener
{
    [Header("Зависимости")]
    [SerializeField] private ParticleSystem _particleSystem;

    private Coroutine _playRoutine;
    private bool _isLifeEndStarted;

    public override bool IsLifeEndComplete
    {
        get
        {
            if (_isLifeEndStarted == false)
            {
                return true;
            }

            return _particleSystem.IsAlive(true) == false;
        }
    }

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

        _isLifeEndStarted = false;

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

        _isLifeEndStarted = true;

        _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        StopPlayRoutineIfRunning();

        _isLifeEndStarted = false;

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
