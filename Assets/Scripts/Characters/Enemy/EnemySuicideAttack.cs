using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemySuicideAttack : MonoBehaviour
{
    private const int HitBufferSize = 32;

    private readonly Collider[] _hitBuffer = new Collider[HitBufferSize];

    [Header("Dependencies")]
    [SerializeField] private Health _health;
    [SerializeField] private GameObject _fxPrefab;

    [Header("Fuse")]
    [SerializeField] private float _startDistance = 1.25f;
    [SerializeField] private float _delay = 0.6f;

    [Header("Explosion")]
    [SerializeField] private LayerMask _damageMask;
    [SerializeField] private float _damage = 24f;
    [SerializeField] private float _radius = 2.25f;
    [SerializeField] private float _impulse = 3.5f;
    [SerializeField] private float _up = 0.35f;
    [SerializeField] private float _fxLife = 5f;

    private bool _isActive;
    private float _delayTimer;

    public bool IsActive => _isActive;

    private void Awake()
    {
        if (_health == null)
        {
            throw new InvalidOperationException(nameof(_health));
        }

        if (_startDistance <= 0f)
        {
            throw new InvalidOperationException(nameof(_startDistance));
        }

        if (_delay < 0f)
        {
            throw new InvalidOperationException(nameof(_delay));
        }

        if (_damageMask.value == 0)
        {
            throw new InvalidOperationException(nameof(_damageMask));
        }

        if (_damage <= 0f)
        {
            throw new InvalidOperationException(nameof(_damage));
        }

        if (_radius <= 0f)
        {
            throw new InvalidOperationException(nameof(_radius));
        }

        if (_impulse < 0f)
        {
            throw new InvalidOperationException(nameof(_impulse));
        }

        if (_up < 0f)
        {
            throw new InvalidOperationException(nameof(_up));
        }

        if (_fxLife < 0f)
        {
            throw new InvalidOperationException(nameof(_fxLife));
        }
    }

    private void OnEnable()
    {
        ResetState();
    }

    public bool IsStartNeeded(float distance)
    {
        if (_isActive)
        {
            return false;
        }

        if (distance > _startDistance)
        {
            return false;
        }

        return true;
    }

    public void StartFuse()
    {
        if (_isActive)
        {
            return;
        }

        _isActive = true;
        _delayTimer = _delay;
    }

    public void Tick()
    {
        if (_isActive == false)
        {
            return;
        }

        if (_delayTimer > 0f)
        {
            _delayTimer -= Time.fixedDeltaTime;
        }

        if (_delayTimer > 0f)
        {
            return;
        }

        Explode();
    }

    public void ResetState()
    {
        _isActive = false;
        _delayTimer = 0f;
    }

    private void Explode()
    {
        if (_health.Value <= _health.MinValue)
        {
            return;
        }

        ResetState();

        Vector3 explosionPoint = transform.position;
        SpawnFx(explosionPoint);

        int hitCount = Physics.OverlapSphereNonAlloc(
            explosionPoint,
            _radius,
            _hitBuffer,
            _damageMask,
            QueryTriggerInteraction.Ignore);
        int hitIndex = 0;

        while (hitIndex < hitCount)
        {
            Collider hitCollider = _hitBuffer[hitIndex];
            _hitBuffer[hitIndex] = null;

            if (CanHit(hitCollider))
            {
                ApplyExplosion(hitCollider, explosionPoint);
            }

            hitIndex += 1;
        }

        _health.Decrease(_health.MaxValue);
    }

    private void ApplyExplosion(Collider hitCollider, Vector3 explosionPoint)
    {
        Vector3 hitPoint = GetClosestPoint(hitCollider, explosionPoint);
        float distance = Vector3.Distance(explosionPoint, hitPoint);
        float distance01 = Mathf.Clamp01(distance / _radius);
        float damage01 = 1f - distance01;

        if (damage01 > 0f)
        {
            Health targetHealth = hitCollider.GetComponentInParent<Health>();

            if (targetHealth != null)
            {
                float finalDamage = _damage * damage01;

                if (finalDamage > 0f)
                {
                    targetHealth.Decrease(finalDamage);
                }
            }
        }

        Rigidbody targetRigidbody = hitCollider.attachedRigidbody;

        if (targetRigidbody == null)
        {
            return;
        }

        targetRigidbody.AddExplosionForce(_impulse, explosionPoint, _radius, _up, ForceMode.Impulse);
    }

    private Vector3 GetClosestPoint(Collider hitCollider, Vector3 point)
    {
        if (hitCollider is BoxCollider
            || hitCollider is SphereCollider
            || hitCollider is CapsuleCollider)
        {
            return hitCollider.ClosestPoint(point);
        }

        MeshCollider meshCollider = hitCollider as MeshCollider;

        if (meshCollider != null && meshCollider.convex)
        {
            return hitCollider.ClosestPoint(point);
        }

        return hitCollider.bounds.ClosestPoint(point);
    }

    private bool CanHit(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return false;
        }

        if (hitCollider.transform.IsChildOf(transform))
        {
            return false;
        }

        Rigidbody attachedRigidbody = hitCollider.attachedRigidbody;

        if (attachedRigidbody == null)
        {
            return true;
        }

        if (attachedRigidbody.transform.IsChildOf(transform))
        {
            return false;
        }

        return true;
    }

    private void SpawnFx(Vector3 explosionPoint)
    {
        if (_fxPrefab == null)
        {
            return;
        }

        GameObject fxObject = Instantiate(_fxPrefab, explosionPoint, Quaternion.identity);

        if (_fxLife > 0f)
        {
            Destroy(fxObject, _fxLife);
        }
    }
}
