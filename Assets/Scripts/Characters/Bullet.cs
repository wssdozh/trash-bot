using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _lifetimeSeconds = 5f;
    [SerializeField] private string _targetTag = "Enemy";
    [SerializeField] private float _damage = 1f;

    private float _lifetimeTimer;
    private BulletSpawner _spawner;

    private void OnEnable()
    {
        _lifetimeTimer = _lifetimeSeconds;
    }

    public void Initialize(BulletSpawner spawner, string targetTag)
    {
        _spawner = spawner;
        _targetTag = targetTag;
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);

        _lifetimeTimer -= Time.deltaTime;
        if (_lifetimeTimer <= 0f)
        {
            Despawn();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger == true)
        {
            return;
        }

        if (other.gameObject.TryGetComponent<Health>(out Health health))
        {
            if (other.gameObject.CompareTag(_targetTag))
            {
                health.Decrease(_damage);
            }
        }

        Despawn();
    }

    private void Despawn()
    {
        if (_spawner == null == false)
        {
            _spawner.Despawn(this);
            return;
        }

        gameObject.SetActive(false);
    }
}
