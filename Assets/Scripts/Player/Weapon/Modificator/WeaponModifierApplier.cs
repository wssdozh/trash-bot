using System.Collections.Generic;
using UnityEngine;

public sealed class WeaponModifierApplier : MonoBehaviour
{
    [SerializeField] private WeaponHolder _weaponHolder;
    [SerializeField] private WeaponModifierStack _weaponModifierStack;
    [SerializeField] private Inventory _inventory;

    private void OnEnable()
    {
        _weaponHolder.Changed += ApplyToCurrentWeapon;
        _weaponModifierStack.Changed += ApplyToCurrentWeapon;

        ApplyToCurrentWeapon();
    }

    private void OnDisable()
    {
        _weaponHolder.Changed -= ApplyToCurrentWeapon;
        _weaponModifierStack.Changed -= ApplyToCurrentWeapon;
    }

    private void ApplyToCurrentWeapon()
    {
        FireExecutor fireExecutor = _weaponHolder.FireExecutor;

        if (fireExecutor == null)
        {
            return;
        }

        WeaponModifierContext context = WeaponModifierContext.CreateDefault();

        context.WeaponType = GetCurrentWeaponType();

        IReadOnlyList<WeaponModifier> modifiers = _weaponModifierStack.Modifiers;

        for (int i = 0; i < modifiers.Count; i++)
        {
            WeaponModifier modifier = modifiers[i];

            if (modifier == null)
            {
                continue;
            }

            modifier.Apply(ref context);
        }

        fireExecutor.SetFireRateMultiplier(context.FireRateMultiplier);
        fireExecutor.SetDamageMultiplier(context.DamageMultiplier);
    }

    private WeaponType GetCurrentWeaponType()
    {
        InventorySlot activeSlot = _inventory.Slots[_inventory.ActiveIndex];

        if (activeSlot.IsEmpty())
        {
            return WeaponType.None;
        }

        Item item = activeSlot.Item;

        if (item == null)
        {
            return WeaponType.None;
        }

        return item.WeaponType;
    }
}
