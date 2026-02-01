using System;
using DG.Tweening;
using UnityEngine;

public sealed class PickupIdleParticles : PickupIdleBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private ParticleSystem _particleSystem;

    [Header("Настройки")]
    [SerializeField] private float _randomStartDelaySeconds = 0.25f;
    [SerializeField] private bool _clearOnStop = true;

    private Tween _startDelayTween;

    private void Awake()
    {
        if (_particleSystem == null)

            throw new InvalidOperationException(nameof(_particleSystem));
    }

    private void OnDestroy()
    {
        StopParticles();
    }

    protected override void OnIdleActivated()
    {
        StartParticles();
    }

    protected override void OnIdleDeactivated()
    {
        StopParticles();
    }

    private void StartParticles()
    {
        StopParticles();

        float startDelaySeconds = 0f;

        if (_randomStartDelaySeconds > 0f)

            startDelaySeconds = UnityEngine.Random.Range(0f, _randomStartDelaySeconds);


        _startDelayTween = DOVirtual.DelayedCall(startDelaySeconds, PlayNow);
        _startDelayTween.SetId(this);
        _startDelayTween.SetLink(_particleSystem.gameObject, LinkBehaviour.KillOnDestroy);
    }

    private void StopParticles()
    {
        DOTween.Kill(this);

        _startDelayTween = null;


        if (_clearOnStop == true)
        {
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            return;
        }

        _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void PlayNow()
    {
        _particleSystem.Play(true);
    }
}
