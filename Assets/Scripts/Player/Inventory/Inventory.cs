using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] private int _capacity = 16;
    [SerializeField] private List<InventorySlot> _slots;
    [SerializeField] private int _activeIndex;

    public IReadOnlyList<InventorySlot> Slots
    {
        get { return _slots; }
    }

    public int ActiveIndex
    {
        get { return _activeIndex; }
    }

    public event Action InventoryChanged;
    public event Action<int> ActiveIndexChanged;

    private void Awake()
    {
        if (_slots == null)
        {
            _slots = new List<InventorySlot>();
        }

        if (_slots.Count != _capacity)
        {
            _slots.Clear();

            for (int i = 0; i < _capacity; i++)
            {
                InventorySlot slot = new InventorySlot();
                _slots.Add(slot);
            }
        }

        if (_activeIndex < 0 || _activeIndex >= _slots.Count)
        {
            _activeIndex = 0;
        }
    }

    public bool TryAddItem(Item item, int amount)
    {
        if (item == null)
        {
            return false;
        }

        if (amount <= 0)
        {
            return false;
        }

        int remainingAmount = amount;

        for (int i = 0; i < _slots.Count; i++)
        {
            InventorySlot slot = _slots[i];

            if (slot.CanStack(item) == false)
            {
                continue;
            }

            int freeSpace = item.MaxStack - slot.Amount;
            int addAmount = remainingAmount;

            if (addAmount > freeSpace)
            {
                addAmount = freeSpace;
            }

            slot.Amount += addAmount;
            remainingAmount -= addAmount;

            if (remainingAmount <= 0)
            {
                InvokeInventoryChanged();
                return true;
            }
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            InventorySlot slot = _slots[i];

            if (slot.IsEmpty() == false)
            {
                continue;
            }

            int addAmount = remainingAmount;

            if (item.IsStackable && addAmount > item.MaxStack)
            {
                addAmount = item.MaxStack;
            }

            slot.SetItem(item, addAmount);
            remainingAmount -= addAmount;

            if (remainingAmount <= 0)
            {
                SelectWeaponSlot(item, i);
                InvokeInventoryChanged();
                return true;
            }
        }

        if (remainingAmount < amount)
        {
            InvokeInventoryChanged();
        }

        return false;
    }

    public bool TryUseItem(int slotIndex)
    {
        if (slotIndex < 0)
        {
            return false;
        }

        if (slotIndex >= _slots.Count)
        {
            return false;
        }

        InventorySlot slot = _slots[slotIndex];

        if (slot.IsEmpty())
        {
            return false;
        }

        ConsumeFromSlot(slot, 1);
        InvokeInventoryChanged();

        return true;
    }

    private void ConsumeFromSlot(InventorySlot slot, int amount)
    {
        if (slot.Item.IsStackable)
        {
            slot.Amount -= amount;

            if (slot.Amount <= 0)
            {
                slot.Clear();
            }

            return;
        }

        slot.Clear();
    }

    public void SetActiveIndex(int index)
    {
        if (_slots.Count == 0)
        {
            return;
        }

        if (index < 0)
        {
            index = _slots.Count - 1;
        }

        if (index >= _slots.Count)
        {
            index = 0;
        }

        if (_activeIndex == index)
        {
            return;
        }

        _activeIndex = index;

        if (ActiveIndexChanged != null)
        {
            ActiveIndexChanged?.Invoke(_activeIndex);
        }
    }

    public bool TryRemoveFromSlot(int slotIndex, int amount)
    {
        if (slotIndex < 0)
        {
            return false;
        }

        if (slotIndex >= _slots.Count)
        {
            return false;
        }

        if (amount <= 0)
        {
            return false;
        }

        InventorySlot slot = _slots[slotIndex];

        if (slot.IsEmpty())
        {
            return false;
        }

        if (slot.Item.IsStackable)
        {
            if (amount > slot.Amount)
            {
                amount = slot.Amount;
            }

            slot.Amount -= amount;

            if (slot.Amount <= 0)
            {
                slot.Clear();
            }
        }
        else
        {
            slot.Clear();
        }

        InvokeInventoryChanged();

        return true;
    }

    public void NextActiveSlot()
    {
        SetActiveIndex(_activeIndex + 1);
    }

    public void PreviousActiveSlot()
    {
        SetActiveIndex(_activeIndex - 1);
    }

    private void InvokeInventoryChanged()
    {
        if (InventoryChanged != null)
        {
            InventoryChanged.Invoke();
        }
    }

    private void SelectWeaponSlot(Item item, int slotIndex)
    {
        if (item.WeaponType == WeaponType.None)
        {
            return;
        }

        SetActiveIndex(slotIndex);
    }
}
