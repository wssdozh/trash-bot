using UnityEngine;

public sealed class ParticleEffectSpawner : Spawner<ParticleEffect>
{
    protected override void Awake()
    {
        base.Awake();

        for (int i = 0; i < PoolSize; i++)
        {
            ParticleEffect effect = Pool.Get();
            Pool.Release(effect);
        }
    }

    public override ParticleEffect Spawn(Vector3 position)
    {
        ParticleEffect effect = Pool.Get();
        effect.transform.position = position;
        effect.Initialize(this);
        effect.Play();

        return effect;
    }

    public override void Despawn(ParticleEffect effect)
    {
        Pool.Release(effect);
    }

    protected override void ActionOnRelease(ParticleEffect effect)
    {
        effect.StopAndClear();
        effect.gameObject.SetActive(false);
    }
}