using UnityEngine;

public sealed class ParticleEffectSpawner : Spawner<PooledParticleEffect>
{
    [SerializeField] private ParticleSystemSpawnerRef _link;

    protected override void Awake()
    {
        base.Awake();

        for (int i = 0; i < PoolSize; i++)
        {
            PooledParticleEffect pooledParticleEffect = Pool.Get();
            Pool.Release(pooledParticleEffect);
        }

        if (_link == null == false)
        {
            _link.Set(this);
        }
    }

    private void OnDestroy()
    {
        if (_link == null == false)
        {
            if (_link.Value == this)
            {
                _link.Clear();
            }
        }
    }

    public PooledParticleEffect Spawn(Vector3 position, Quaternion rotation)
    {
        PooledParticleEffect pooledParticleEffect = Pool.Get();
        pooledParticleEffect.transform.SetPositionAndRotation(position, rotation);
        pooledParticleEffect.Initialize(this);
        pooledParticleEffect.Play();
        return pooledParticleEffect;
    }

    public void Despawn(PooledParticleEffect pooledParticleEffect)
    {
        Pool.Release(pooledParticleEffect);
    }

    protected override void ActionOnRelease(PooledParticleEffect pooledParticleEffect)
    {
        pooledParticleEffect.StopAndClear();
        pooledParticleEffect.gameObject.SetActive(false);
    }
}
