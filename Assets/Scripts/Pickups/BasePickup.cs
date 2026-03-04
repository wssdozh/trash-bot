using UnityEngine;

public abstract class BasePickup : MonoBehaviour
{
    [SerializeField] private Item _item;
    [SerializeField] private int _amount;

    protected bool _isPickedUp = false;

    public Item Item { get { return _item; } }
    public int Amount { get { return _amount; } }

    protected virtual void OnEnable()
    {
        _isPickedUp = false;
    }

    public void Pickup(GameObject player)
    {
        if (_isPickedUp == false)
        {
            _isPickedUp = true;
            OnPickup(player);
        }
    }

    public void SetAmount(int amount)
    {
        _amount = amount;
    }

    protected abstract void OnPickup(GameObject player);
}
