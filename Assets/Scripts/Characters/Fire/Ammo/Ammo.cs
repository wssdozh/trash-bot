using System;
using UnityEngine;

public abstract class Ammo : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private LayerMask _targetLayers;
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _lifetimeSeconds = 5f;

    private float _lifetimeTimer;
    private bool _isLifeEnded;

    public LayerMask TargetLayers => _targetLayers;

    public event Action Impacted;
    public event Action LifeEnded;

    protected virtual void OnEnable()
    {
        _lifetimeTimer = _lifetimeSeconds;
        _isLifeEnded = false;

        OnAmmoEnabled();
    }

    protected virtual void Update()
    {
        if (_isLifeEnded == true)
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

    private void OnTriggerEnter(Collider other)
    {
        if (_isLifeEnded == true)
        {
            return;
        }

        if (other.isTrigger == true)
        {
            return;
        }

        if (IsInTargetLayers(other.gameObject.layer))
        {
            OnHitTarget(other);
        }

        Action impacted = Impacted;

        if (impacted == null == false)
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
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }

    protected abstract void OnHitTarget(Collider other);

    protected virtual void OnLifeEnding()
    {
    }

    protected void EndLife()
    {
        if (_isLifeEnded == true)
        {
            return;
        }

        _isLifeEnded = true;

        OnLifeEnding();

        Action lifeEnded = LifeEnded;

        if (lifeEnded == null == false)
        {
            lifeEnded.Invoke();
        }
    }

    private bool IsInTargetLayers(int layer)
    {
        return (_targetLayers.value & (1 << layer)) != 0;
    }
}
