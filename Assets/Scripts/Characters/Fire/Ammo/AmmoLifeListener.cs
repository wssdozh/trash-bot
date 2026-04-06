using System;
using UnityEngine;

public abstract class AmmoLifeListener : MonoBehaviour
{
    [SerializeField] private Ammo _ammo;

    protected Ammo Ammo => _ammo;

    public virtual bool IsLifeEndComplete => true;

    protected virtual void Awake()
    {
        if (_ammo == null)
        {
            throw new InvalidOperationException(nameof(_ammo));
        }
    }

    protected virtual void OnEnable()
    {
        _ammo.Moved += HandleAmmoMoved;
        _ammo.TargetImpacted += HandleAmmoTargetImpacted;
        _ammo.Impacted += HandleAmmoImpacted;
        _ammo.LifeEnded += HandleAmmoLifeEnded;

        OnAmmoEnabled();
    }

    protected virtual void OnDisable()
    {
        _ammo.Moved -= HandleAmmoMoved;
        _ammo.TargetImpacted -= HandleAmmoTargetImpacted;
        _ammo.Impacted -= HandleAmmoImpacted;
        _ammo.LifeEnded -= HandleAmmoLifeEnded;
    }

    private void HandleAmmoMoved(Vector3 startPoint, Vector3 endPoint)
    {
        OnAmmoMoved(startPoint, endPoint);
    }

    private void HandleAmmoTargetImpacted(Collider hitCollider)
    {
        OnAmmoTargetImpacted(hitCollider);
    }

    private void HandleAmmoImpacted()
    {
        OnAmmoImpacted();
    }

    private void HandleAmmoLifeEnded()
    {
        OnAmmoLifeEnded();
    }

    protected virtual void OnAmmoEnabled()
    {
    }

    protected virtual void OnAmmoMoved(Vector3 startPoint, Vector3 endPoint)
    {
    }

    protected virtual void OnAmmoTargetImpacted(Collider hitCollider)
    {
    }

    protected virtual void OnAmmoImpacted()
    {
    }

    protected virtual void OnAmmoLifeEnded()
    {
    }
}
