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
        _ammo.Impacted += HandleAmmoImpacted;
        _ammo.LifeEnded += HandleAmmoLifeEnded;

        OnAmmoEnabled();
    }

    protected virtual void OnDisable()
    {
        _ammo.Impacted -= HandleAmmoImpacted;
        _ammo.LifeEnded -= HandleAmmoLifeEnded;
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

    protected virtual void OnAmmoImpacted()
    {
    }

    protected virtual void OnAmmoLifeEnded()
    {
    }
}
