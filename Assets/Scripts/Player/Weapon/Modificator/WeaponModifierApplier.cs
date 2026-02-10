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

        fireExecutor.ApplyModifierContext(context);
    }

    private WeaponType GetCurrentWeaponType()
    {
        IReadOnlyList<InventorySlot> slots = _inventory.Slots;
        int activeIndex = _inventory.ActiveIndex;

        if (activeIndex < 0 || activeIndex >= slots.Count)
        {
            return WeaponType.None;
        }

        InventorySlot activeSlot = slots[activeIndex];

        if (activeSlot.IsEmpty() == true)
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
