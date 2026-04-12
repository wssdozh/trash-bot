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
        _inventory.InventoryChanged += OnInventoryChanged;
        _inventory.ActiveIndexChanged += OnActiveIndexChanged;

        ApplyToCurrentWeapon();
    }

    private void OnDisable()
    {
        _weaponHolder.Changed -= ApplyToCurrentWeapon;
        _weaponModifierStack.Changed -= ApplyToCurrentWeapon;
        _inventory.InventoryChanged -= OnInventoryChanged;
        _inventory.ActiveIndexChanged -= OnActiveIndexChanged;
    }

    public WeaponModifierContext BuildCurrentContext()
    {
        WeaponModifierContext context = new WeaponModifierContext();
        context.SetDefaults();
        context.WeaponType = GetCurrentWeaponType();

        Item item = GetCurrentItem();

        if (item != null)
        {
            ApplyModifiers(item.WeaponModifiers, ref context);
        }

        ApplyModifiers(_weaponModifierStack.Modifiers, ref context);

        return context;
    }

    private void ApplyToCurrentWeapon()
    {
        FireExecutor fireExecutor = _weaponHolder.FireExecutor;

        if (fireExecutor == null)
        {
            return;
        }

        WeaponModifierContext context = BuildCurrentContext();

        fireExecutor.ApplyModifierContext(context);
    }

    private void OnInventoryChanged()
    {
        ApplyToCurrentWeapon();
    }

    private void OnActiveIndexChanged(int activeIndex)
    {
        ApplyToCurrentWeapon();
    }

    private Item GetCurrentItem()
    {
        IReadOnlyList<InventorySlot> slots = _inventory.Slots;
        int activeIndex = _inventory.ActiveIndex;

        if (activeIndex < 0)
        {
            return null;
        }

        if (activeIndex >= slots.Count)
        {
            return null;
        }

        InventorySlot activeSlot = slots[activeIndex];

        if (activeSlot.IsEmpty())
        {
            return null;
        }

        Item item = activeSlot.Item;

        if (item == null)
        {
            return null;
        }

        return item;
    }

    private WeaponType GetCurrentWeaponType()
    {
        Item item = GetCurrentItem();

        if (item == null)
        {
            return WeaponType.None;
        }

        return item.WeaponType;
    }

    private void ApplyModifiers(IReadOnlyList<WeaponModifier> modifiers, ref WeaponModifierContext context)
    {
        if (modifiers == null)
        {
            return;
        }

        for (int i = 0; i < modifiers.Count; i++)
        {
            WeaponModifier modifier = modifiers[i];

            if (modifier == null)
            {
                continue;
            }

            modifier.Apply(ref context);
        }
    }
}
