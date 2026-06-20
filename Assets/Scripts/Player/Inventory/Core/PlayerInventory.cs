using System;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private InventoryDropper _inventoryDropper;
    [SerializeField] private CharacterEffects _effects;
    [SerializeField] private BerryWallet _berryWallet;

    private void Awake()
    {
        if (_inventory == null)
        {
            throw new InvalidOperationException(nameof(_inventory));
        }

        if (_inventoryDropper == null)
        {
            throw new InvalidOperationException(nameof(_inventoryDropper));
        }

        if (_effects == null)
        {
            throw new InvalidOperationException(nameof(_effects));
        }

        if (_berryWallet == null)
        {
            throw new InvalidOperationException(nameof(_berryWallet));
        }
    }

    public void Scroll(Vector2 scrollValue)
    {
        if (scrollValue.y > 0f)
        {
            _inventory.PreviousActiveSlot();
        }

        if (scrollValue.y < 0f)
        {
            _inventory.NextActiveSlot();
        }
    }

    public void SetActiveSlot(int slotIndex)
    {
        _inventory.SetActiveIndex(slotIndex);
    }

    public void DropOne()
    {
        _inventoryDropper.DropOneFromActiveSlot();
    }

    public void DropAll()
    {
        _inventoryDropper.DropAllFromActiveSlot();
    }

    public bool TryUseActiveItem()
    {
        if (_berryWallet.IsConsumeCoolingDown)
        {
            return false;
        }

        if (_berryWallet.TryConsume(_effects))
        {
            return true;
        }

        InventorySlot activeSlot = _inventory.Slots[_inventory.ActiveIndex];

        if (activeSlot.IsEmpty())
        {
            return false;
        }

        if (activeSlot.Item.Effects.Count <= 0)
        {
            return false;
        }

        UseItem(activeSlot.Item);
        _inventory.TryRemoveFromSlot(_inventory.ActiveIndex, 1);

        return true;
    }

    private void UseItem(Item item)
    {
        for (int effectIndex = 0; effectIndex < item.Effects.Count; effectIndex++)
        {
            ItemEffect effect = item.Effects[effectIndex];
            effect.Apply(_effects);
        }
    }
}
