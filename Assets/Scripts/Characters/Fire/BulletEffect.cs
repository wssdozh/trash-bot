using UnityEngine;

class BulletEffect : MonoBehaviour
{
    [SerializeField] private BulletReturner _bulletReturner;
    [SerializeField] private ParticleEffect _particlePrefab;

    private Spawner<ParticleEffect> _particleEffectSpawner;

    private void Start()
    {
        _particleEffectSpawner = SpawnerServiceLocator.Get<ParticleEffect>(_particlePrefab.name);
    }

    private void OnEnable()
    {
        _bulletReturner.Return += Play;
    }

    private void OnDisable()
    {
        _bulletReturner.Return -= Play;
    }

    private void Play()
    {
        if (_particleEffectSpawner != null)
            _particleEffectSpawner.Spawn(transform.position);
    }
}