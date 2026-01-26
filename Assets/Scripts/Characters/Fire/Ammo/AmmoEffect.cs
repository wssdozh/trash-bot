using UnityEngine;

class AmmoEffect : MonoBehaviour
{
    [SerializeField] private AmmoReturner _bulletReturner;
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
        Debug.Log("опоп");

        if (_particleEffectSpawner != null)
        {
            _particleEffectSpawner.Spawn(transform.position);
        }
    }
}
