using UnityEngine;

class BulletEffect : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private BulletReturner _bulletReturner;
    [SerializeField] private SpawnerRef<ParticleEffectSpawner> _spawnerRef;

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
        _spawnerRef.Value.Spawn(transform.position, transform.rotation);
    }
}