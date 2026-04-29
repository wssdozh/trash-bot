using System;
using UnityEngine;

public abstract class BasePickup : MonoBehaviour
{
    [SerializeField] private Item _item;
    [SerializeField] private int _amount;

    protected bool _isPickedUp = false;

    public Item Item { get { return _item; } }
    public int Amount { get { return _amount; } }

    public event Action PickedUp;

    protected virtual void OnEnable()
    {
        _isPickedUp = false;
    }

    public virtual bool TryCollect(GameObject player, Inventory inventory)
    {
        if (_item == null)
        {
            return false;
        }

        if (inventory == null)
        {
            return false;
        }

        if (inventory.TryAddItem(_item, _amount) == false)
        {
            return false;
        }

        Pickup(player);

        return true;
    }

    public void Pickup(GameObject player)
    {
        if (_isPickedUp == false)
        {
            _isPickedUp = true;
            InvokePickedUp();
            OnPickup(player);
        }
    }

    public void SetAmount(int amount)
    {
        _amount = amount;
    }

    protected abstract void OnPickup(GameObject player);

    private void InvokePickedUp()
    {
        Action pickedUp = PickedUp;

        if (pickedUp != null)
        {
            pickedUp.Invoke();
        }
    }
}
