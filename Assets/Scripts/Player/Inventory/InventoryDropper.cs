using UnityEngine;

public class InventoryDropper : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private Transform _dropOrigin;
    [SerializeField] private float _dropDistance = 1f;

    private void Awake()
    {
        if (_dropOrigin == null)
        {
            _dropOrigin = transform;
        }
    }

    public void DropOneFromActiveSlot()
    {
        DropFromActiveSlot(1);
    }

    public void DropAllFromActiveSlot()
    {
        int slotIndex = _inventory.ActiveIndex;
        InventorySlot slot = _inventory.Slots[slotIndex];

        if (slot.IsEmpty() == true)
        {
            return;
        }

        DropFromActiveSlot(slot.Amount);
    }

    private void DropFromActiveSlot(int amount)
    {
        int slotIndex = _inventory.ActiveIndex;
        InventorySlot slot = _inventory.Slots[slotIndex];

        if (slot.IsEmpty() == true)
        {
            return;
        }

        Item item = slot.Item;

        if (item == null)
        {
            return;
        }

        if (item.PickupSpawnerRef == null)
        {
            return;
        }

        PickupSpawner pickupSpawner = item.PickupSpawnerRef.Value;

        if (pickupSpawner == null)
        {
            return;
        }

        if (amount <= 0)
        {
            return;
        }

        if (item.IsStackable == true && amount > slot.Amount)
        {
            amount = slot.Amount;
        }

        if (item.IsStackable == false)
        {
            amount = 1;
        }

        Vector3 spawnPosition = _dropOrigin.position + _dropOrigin.forward * _dropDistance;

        bool removed = _inventory.TryRemoveFromSlot(slotIndex, amount);

        if (removed == false)
        {
            return;
        }

        BasePickup pickup = pickupSpawner.Spawn(spawnPosition);
        pickup.transform.SetParent(null, true);
        pickup.SetAmount(amount);

        HeldMode heldMode = pickup.GetComponent<HeldMode>();
        if (heldMode != null)
        {
            heldMode.SetHeld(false);
        }

        PickupReturner pickupReturner = pickup.GetComponent<PickupReturner>();
        if (pickupReturner != null)
        {
            pickupReturner.SetCanReturn(true);
        }
    }
}
