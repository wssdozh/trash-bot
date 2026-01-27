using System;
using System.Collections;
using UnityEngine;

public sealed class AmmoTrailRenderer : AmmoLifeListener
{
    [Header("Зависимости")]
    [SerializeField] private TrailRenderer _trailRenderer;

    private Coroutine _trailRoutine;

    public float TrailTimeSeconds => _trailRenderer.time;

    protected override void Awake()
    {
        base.Awake();

        if (_trailRenderer == null)
        {
            throw new InvalidOperationException(nameof(_trailRenderer));
        }
    }

    protected override void OnAmmoEnabled()
    {
        StopTrailRoutineIfRunning();

        _trailRenderer.emitting = false;
        _trailRenderer.Clear();

        _trailRoutine = StartCoroutine(EnableTrailNextFrame());
    }

    protected override void OnAmmoLifeEnded()
    {
        StopTrailRoutineIfRunning();

        _trailRenderer.emitting = false;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        StopTrailRoutineIfRunning();

        _trailRenderer.emitting = false;
        _trailRenderer.Clear();
    }

    private IEnumerator EnableTrailNextFrame()
    {
        yield return null;

        _trailRenderer.Clear();
        _trailRenderer.emitting = true;
        _trailRoutine = null;
    }

    private void StopTrailRoutineIfRunning()
    {
        if (_trailRoutine == null)
        {
            return;
        }

        StopCoroutine(_trailRoutine);
        _trailRoutine = null;
    }
}
