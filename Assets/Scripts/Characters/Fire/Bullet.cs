using System.Collections;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private TrailRenderer _trailRenderer;
    
    [Header("Настройки")]
    [SerializeField] private LayerMask _targetLayers;
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _lifetimeSeconds = 5f;
    [SerializeField] private float _minDamage = 3f;
    [SerializeField] private float _maxDamage = 6f;
    [SerializeField] private float _impulseStrength = 3f;

    private float _lifetimeTimer;
    private BulletSpawner _spawner;
    private Coroutine _trailRoutine;

    private void OnEnable()
    {
        _lifetimeTimer = _lifetimeSeconds;

        if (_trailRoutine == null == false)
        {
            StopCoroutine(_trailRoutine);
            _trailRoutine = null;
        }

        if (_trailRenderer == null == false)
        {
            _trailRenderer.emitting = false;
            _trailRenderer.Clear();
            _trailRoutine = StartCoroutine(EnableTrailNextFrame());
        }
    }

    private IEnumerator EnableTrailNextFrame()
    {
        yield return null;

        _trailRenderer.Clear();
        _trailRenderer.emitting = true;
        _trailRoutine = null;
    }

    public void Initialize(BulletSpawner spawner)
    {
        _spawner = spawner;
    }

    public void SetLayers(LayerMask targetLayers)
    {
        _targetLayers = targetLayers;
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

        if ((_targetLayers.value & (1 << other.gameObject.layer)) != 0)
        {   
            if (other.gameObject.TryGetComponent<Health>(out Health health))
            {
                {
                    health.Decrease(Random.Range(_minDamage, _maxDamage));
                }
            }
        
            if (other.gameObject.TryGetComponent<Rigidbody>(out Rigidbody rigidbody))
            {
                rigidbody.AddForce(transform.forward * _impulseStrength, ForceMode.Impulse);
            }
        }

        Despawn();
    }

    private void Despawn()
    {
        if (_trailRoutine == null == false)
        {
            StopCoroutine(_trailRoutine);
            _trailRoutine = null;
        }

        if (_trailRenderer == null == false)
        {
            _trailRenderer.emitting = false;
            _trailRenderer.Clear();
        }

        if (_spawner == null == false)
        {
            _spawner.Despawn(this);
            
            return;
        }

        gameObject.SetActive(false);
    }
}
