using System.Collections;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _lifetimeSeconds = 5f;
    [SerializeField] private string _targetTag = "Enemy";
    [SerializeField] private float _damage = 1f;
    [SerializeField] private TrailRenderer _trailRenderer;

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

    public void SetTag(string targetTag)
    {
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
