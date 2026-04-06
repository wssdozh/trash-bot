using System;
using UnityEngine;

public abstract class Ammo : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private Rigidbody _rigidbody;

    [Header("Настройки")]
    [SerializeField] private LayerMask _targetLayers;
    [SerializeField] private float _minSpeed = 8f;
    [SerializeField] private float _maxSpeed = 12f;
    [SerializeField] private float _lifetimeSeconds = 5f;

    private float _speed;
    private float _lifetimeTimer;
    private bool _isLifeEnded;

    private float _damage;
    private float _speedMultiplier = 1f;
    private Transform _ignoredRoot;

    public LayerMask TargetLayers => _targetLayers;

    protected float Damage => _damage;

    public event Action Impacted;
    public event Action LifeEnded;

    private void Awake()
    {
        if (_rigidbody == null)
        {
            throw new InvalidOperationException(nameof(_rigidbody));
        }
    }

    protected virtual void OnEnable()
    {
        _lifetimeTimer = _lifetimeSeconds;
        _isLifeEnded = false;

        _speed = UnityEngine.Random.Range(_minSpeed, _maxSpeed);

        _damage = 0f;
        _speedMultiplier = 1f;

        OnAmmoEnabled();
    }

    private void FixedUpdate()
    {
        if (_isLifeEnded)
        {
            return;
        }

        MoveForward();
    }

    protected virtual void Update()
    {
        if (_isLifeEnded)
        {
            return;
        }

        _lifetimeTimer -= Time.deltaTime;

        if (_lifetimeTimer <= 0f)
        {
            EndLife();
        }
    }

    public void SetLayers(LayerMask targetLayers)
    {
        _targetLayers = targetLayers;
    }

    public void SetDamage(float damage)
    {
        if (damage <= 0f)
        {
            throw new InvalidOperationException(nameof(damage));
        }

        _damage = damage;
    }

    public void SetSpeedMultiplier(float speedMultiplier)
    {
        if (speedMultiplier <= 0f)
        {
            throw new InvalidOperationException(nameof(speedMultiplier));
        }

        _speedMultiplier = speedMultiplier;
    }

    public void SetIgnoredRoot(Transform ignoredRoot)
    {
        _ignoredRoot = ignoredRoot;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isLifeEnded)
        {
            return;
        }

        if (IsIgnoredRoot(other))
        {
            return;
        }

        bool isTargetLayer = IsInTargetLayers(other.gameObject.layer);

        if (other.isTrigger)
        {
            return;
        }

        if (isTargetLayer)
        {
            if (_damage <= 0f)
            {
                throw new InvalidOperationException(nameof(_damage));
            }

            OnHitTarget(other);
        }

        Action impacted = Impacted;

        if (impacted != null)
        {
            impacted.Invoke();
        }

        EndLife();
    }

    protected virtual void OnAmmoEnabled()
    {
    }

    protected virtual void MoveForward()
    {
        Vector3 moveOffset = transform.forward * _speed * _speedMultiplier * Time.fixedDeltaTime;
        Vector3 nextPosition = _rigidbody.position + moveOffset;

        _rigidbody.MovePosition(nextPosition);
    }

    protected abstract void OnHitTarget(Collider other);

    protected virtual void OnLifeEnding()
    {
    }

    protected void EndLife()
    {
        if (_isLifeEnded)
        {
            return;
        }

        _isLifeEnded = true;

        OnLifeEnding();

        Action lifeEnded = LifeEnded;

        if (lifeEnded != null)
        {
            lifeEnded.Invoke();
        }
    }

    private bool IsInTargetLayers(int layer)
    {
        return (_targetLayers.value & (1 << layer)) != 0;
    }

    private bool IsIgnoredRoot(Collider other)
    {
        if (_ignoredRoot == null)
        {
            return false;
        }

        if (other.transform.IsChildOf(_ignoredRoot))
        {
            return true;
        }

        Rigidbody attachedRigidbody = other.attachedRigidbody;

        if (attachedRigidbody == null)
        {
            return false;
        }

        return attachedRigidbody.transform.IsChildOf(_ignoredRoot);
    }
}
