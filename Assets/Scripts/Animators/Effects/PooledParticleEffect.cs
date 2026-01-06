using UnityEngine;

public sealed class PooledParticleEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem _particleSystem;

    private ParticleEffectSpawner _particleEffectSpawner;

    private void Awake()
    {
        ParticleSystem.MainModule mainModule = _particleSystem.main;
        mainModule.stopAction = ParticleSystemStopAction.Callback;
    }

    public void Initialize(ParticleEffectSpawner particleEffectSpawner)
    {
        _particleEffectSpawner = particleEffectSpawner;
    }

    public void Play()
    {
        _particleSystem.Play(true);
    }

    public void StopAndClear()
    {
        _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnParticleSystemStopped()
    {
        if (_particleEffectSpawner == null == false)
        {
            _particleEffectSpawner.Despawn(this);
        }
    }
}
