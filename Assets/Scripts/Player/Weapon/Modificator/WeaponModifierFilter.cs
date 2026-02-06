using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponTypeFilteredModifier : WeaponModifier
{
    [SerializeField] private List<WeaponType> _allowedWeaponTypes = new List<WeaponType>();

    public override void Apply(ref WeaponModifierContext context)
    {
        if (IsAllowed(context.WeaponType) == false)
        {
            return;
        }

        ApplyAllowed(ref context);
    }

    protected abstract void ApplyAllowed(ref WeaponModifierContext context);

    private bool IsAllowed(WeaponType weaponType)
    {
        if (_allowedWeaponTypes.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < _allowedWeaponTypes.Count; i++)
        {
            if (_allowedWeaponTypes[i] == weaponType)
            {
                return true;
            }
        }

        return false;
    }
}
