using System;
using UnityEngine;

public abstract class Ammo : MonoBehaviour
{
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

    public LayerMask TargetLayers => _targetLayers;

    protected float Damage => _damage;

    public event Action Impacted;
    public event Action LifeEnded;

    protected virtual void OnEnable()
    {
        _lifetimeTimer = _lifetimeSeconds;
        _isLifeEnded = false;

        _speed = UnityEngine.Random.Range(_minSpeed, _maxSpeed);

        _damage = 0f;
        _speedMultiplier = 1f;

        OnAmmoEnabled();
    }

    protected virtual void Update()
    {
        if (_isLifeEnded)
        {
            return;
        }

        MoveForward();

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

    private void OnTriggerEnter(Collider other)
    {
        if (_isLifeEnded)
        {
            return;
        }

        if (other.isTrigger)
        {
            return;
        }

        if (IsInTargetLayers(other.gameObject.layer))
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
        transform.Translate(Vector3.forward * _speed * _speedMultiplier * Time.deltaTime);
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
}
