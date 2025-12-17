using System;
using UnityEngine;

public class WeaponHolder : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private Transform _weaponSocket;
    [SerializeField] private string _targetTag = "Enemy";

    private PickupSpawner _pickupSpawner;
    private BasePickup _currentPickup;
    private FireExecutor _fireExecutor;

    public FireExecutor FireExecutor
    {
        get { return _fireExecutor; }
    }

    public event Action Changed;

    private void OnEnable()
    {
        _inventory.InventoryChanged += Refresh;
        _inventory.ActiveIndexChanged += OnActiveIndexChanged;
        Refresh();
    }

    private void OnDisable()
    {
        _inventory.InventoryChanged -= Refresh;
        _inventory.ActiveIndexChanged -= OnActiveIndexChanged;
    }

    private void OnActiveIndexChanged(int activeIndex)
    {
        Refresh();
    }

    private void Refresh()
    {
        InventorySlot activeSlot = _inventory.Slots[_inventory.ActiveIndex];

        if (activeSlot.IsEmpty() == true)
        {
            Clear();
            return;
        }

        Item item = activeSlot.Item;

        if (item.WeaponType == WeaponType.None)
        {
            Clear();
            return;
        }

        if (item.PickupSpawnerRef == null)
        {
            Clear();
            return;
        }

        PickupSpawner pickupSpawner = item.PickupSpawnerRef.Value;

        if (pickupSpawner == null)
        {
            Clear();
            return;
        }

        Set(pickupSpawner);
    }

    private void Set(PickupSpawner pickupSpawner)
    {
        if (_pickupSpawner == pickupSpawner && _currentPickup != null)
        {
            return;
        }

        Clear();

        _pickupSpawner = pickupSpawner;

        _currentPickup = _pickupSpawner.Spawn(_weaponSocket.position);
        _currentPickup.transform.SetParent(_weaponSocket, true);
        _currentPickup.transform.localPosition = Vector3.zero;
        _currentPickup.transform.localRotation = Quaternion.identity;

        _fireExecutor = _currentPickup.GetComponentInChildren<FireExecutor>();

        if (_fireExecutor != null)
        {
            _fireExecutor.SetTargetTag(_targetTag);
        }

        Changed?.Invoke();
    }

    private void Clear()
    {
        if (_currentPickup != null)
        {
            if (_pickupSpawner != null)
            {
                _pickupSpawner.Despawn(_currentPickup);
            }
            else
            {
                Destroy(_currentPickup.gameObject);
            }
        }

        _pickupSpawner = null;
        _currentPickup = null;
        _fireExecutor = null;

        Changed?.Invoke();
    }
}
