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
        if (_inventory == null)
        {
            return;
        }

        int slotIndex = _inventory.ActiveIndex;
        InventorySlot slot = _inventory.Slots[slotIndex];

        if (slot.IsEmpty() == true)
        {
            return;
        }

        int amount = slot.Amount;

        DropFromActiveSlot(amount);
    }

    private void DropFromActiveSlot(int amount)
    {
        if (_inventory == null)
        {
            return;
        }

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

        PickupSpawner spawner = item.PickupSpawnerRef.Value;

        if (spawner == null)
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

        BasePickup pickup = spawner.Spawn(spawnPosition);
        pickup.SetAmount(amount);

        _inventory.TryRemoveFromSlot(slotIndex, amount);
    }
}
