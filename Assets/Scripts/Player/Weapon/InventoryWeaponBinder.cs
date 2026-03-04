using UnityEngine;

public class InventoryWeaponBinder : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private WeaponHolder _weaponHolder;

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

        if (activeSlot.IsEmpty())
        {
            _weaponHolder.Clear();

            return;
        }

        Item item = activeSlot.Item;

        if (item.WeaponType == WeaponType.None || item.Prefab == null)
        {
            _weaponHolder.Clear();

            return;
        }

        _weaponHolder.Equip(item.Prefab);
    }
}
