using System.Collections.Generic;
using UnityEngine;

public class InventoryView : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private InventorySlotView _slotViewPrefab;
    [SerializeField] private Transform _slotsParent;

    private List<InventorySlotView> _slotViews;

    private void Awake()
    {
        _slotViews = new List<InventorySlotView>();

        for (int i = 0; i < _inventory.Slots.Count; i++)
        {
            InventorySlot slot = _inventory.Slots[i];
            InventorySlotView slotView = Instantiate(_slotViewPrefab, _slotsParent);
            slotView.SetSlot(slot);
            _slotViews.Add(slotView);
        }

        _inventory.InventoryChanged += OnInventoryChanged;
        _inventory.ActiveIndexChanged += OnActiveIndexChanged;

        OnInventoryChanged();
        OnActiveIndexChanged(_inventory.ActiveIndex);
    }

    private void OnDestroy()
    {
        if (_inventory != null)
        {
            _inventory.InventoryChanged -= OnInventoryChanged;
            _inventory.ActiveIndexChanged -= OnActiveIndexChanged;
        }
    }

    private void OnInventoryChanged()
    {
        for (int i = 0; i < _slotViews.Count; i++)
        {
            InventorySlotView slotView = _slotViews[i];
            slotView.Refresh();
        }
    }

    private void OnActiveIndexChanged(int activeIndex)
    {
        for (int i = 0; i < _slotViews.Count; i++)
        {
            bool isActive = i == activeIndex;
            _slotViews[i].SetActive(isActive);
        }
    }
}
