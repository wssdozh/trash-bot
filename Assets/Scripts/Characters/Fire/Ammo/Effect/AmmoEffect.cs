using System;
using UnityEngine;

public sealed class AmmoEffect : MonoBehaviour
{
    [SerializeField] private AmmoReturner _bulletReturner;
    [SerializeField] private ParticleEffect _particlePrefab;

    private Spawner<ParticleEffect> _particleEffectSpawner;

    private void Awake()
    {
        if (_bulletReturner == null)
        {
            throw new InvalidOperationException(nameof(_bulletReturner));
        }

        if (_particlePrefab == null)
        {
            throw new InvalidOperationException(nameof(_particlePrefab));
        }
    }

    private void Start()
    {
        _particleEffectSpawner = SpawnerServiceLocator.Get<ParticleEffect>(_particlePrefab.name);
    }

    private void OnEnable()
    {
        _bulletReturner.Ammo.Impacted += Play;
    }

    private void OnDisable()
    {
        _bulletReturner.Ammo.Impacted -= Play;
    }

    private void Play()
    {
        if (_particleEffectSpawner == null)
        {
            return;
        }

        _particleEffectSpawner.Spawn(transform.position);
    }
}
