using UnityEngine;

public class PickupTrigger : MonoBehaviour
{
    [SerializeField] private Collider _triggerCollider;
    [SerializeField] private Inventory _inventory;

    private void Awake()
    {
        _triggerCollider.isTrigger = true;

        if (_inventory == null)
        {
            _inventory = GetComponent<Inventory>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryPickup(other);
    }

    private void TryPickup(Collider other)
    {
        BasePickup itemPickup = other.GetComponent<BasePickup>();

        if (itemPickup == null)
        {
            return;
        }

        if (_inventory == null)
        {
            return;
        }

        bool added = _inventory.TryAddItem(itemPickup.Item, itemPickup.Amount);

        if (added == false)
        {
            return;
        }

        itemPickup.Pickup(gameObject);

    }
}
