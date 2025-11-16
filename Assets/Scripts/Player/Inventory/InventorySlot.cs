using System;
using UnityEngine;

[Serializable]
public class InventorySlot
{
    public Item Item;
    public int Amount;

    public bool IsEmpty()
    {
        if (Item == null)
        {
            return true;
        }

        if (Amount <= 0)
        {
            return true;
        }

        return false;
    }

    public bool CanStack(Item item)
    {
        if (Item == null)
        {
            return false;
        }

        if (Item != item)
        {
            return false;
        }

        if (Item.IsStackable == false)
        {
            return false;
        }

        if (Amount >= Item.MaxStack)
        {
            return false;
        }

        return true;
    }

    public void SetItem(Item item, int amount)
    {
        Item = item;
        Amount = amount;
    }

    public void Clear()
    {
        Item = null;
        Amount = 0;
    }
}
