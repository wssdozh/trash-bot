using UnityEngine;
using System;

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

        if (slot.IsEmpty())
        {
            return;
        }

        DropFromActiveSlot(slot.Amount);
    }

    private void DropFromActiveSlot(int amount)
    {
        int slotIndex = _inventory.ActiveIndex;

        InventorySlot slot = _inventory.Slots[slotIndex];

        if (slot.IsEmpty())
        {
            return;
        }

        Item item = slot.Item;

        if (item == null)
        {
            throw new InvalidOperationException(nameof(item));
        }

        if (item.Prefab == null)
        {
            throw new InvalidOperationException(nameof(item.Prefab));
        }

        Spawner<BasePickup> pickupSpawner = SpawnerServiceLocator.Get<BasePickup>(item.Prefab.name);

        if (pickupSpawner == null)
        {
            throw new InvalidOperationException(nameof(pickupSpawner));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        if (item.IsStackable && amount > slot.Amount)
        {
            amount = slot.Amount;
        }

        if (item.IsStackable == false)
        {
            amount = 1;
        }

        Vector3 spawnPosition = _dropOrigin.position + _dropOrigin.forward * _dropDistance;

        if (_inventory.TryRemoveFromSlot(slotIndex, amount) == false)
        {
            throw new InvalidOperationException(nameof(_inventory));
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