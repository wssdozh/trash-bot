using System.Collections.Generic;
using UnityEngine;

public class InventoryView : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private InventorySlotView _slotViewPrefab;
    [SerializeField] private Transform _slotsParent;

    private readonly List<InventorySlotView> _slotViews = new List<InventorySlotView>();
    private bool _isBuilt;

    private void OnEnable()
    {
        if (_inventory != null)
        {
            _inventory.InventoryChanged += OnInventoryChanged;
            _inventory.ActiveIndexChanged += OnActiveIndexChanged;
        }
    }

    private void Start()
    {
        BuildIfNeeded();
        OnInventoryChanged();
        OnActiveIndexChanged(_inventory.ActiveIndex);
    }

    private void OnDisable()
    {
        if (_inventory != null)
        {
            _inventory.InventoryChanged -= OnInventoryChanged;
            _inventory.ActiveIndexChanged -= OnActiveIndexChanged;
        }
    }

    private void BuildIfNeeded()
    {
        if (_isBuilt)
        {
            return;
        }

        ClearSlotViews();

        int slotsCount = _inventory.Slots.Count;

        for (int i = 0; i < slotsCount; i++)
        {
            InventorySlot slot = _inventory.Slots[i];
            InventorySlotView slotView = Instantiate(_slotViewPrefab, _slotsParent);
            slotView.SetSlot(slot);
            _slotViews.Add(slotView);
        }

        _isBuilt = true;
    }

    private void ClearSlotViews()
    {
        for (int i = 0; i < _slotViews.Count; i++)
        {
            InventorySlotView slotView = _slotViews[i];

            if (slotView == null)
            {
                continue;
            }

            Destroy(slotView.gameObject);
        }

        _slotViews.Clear();
    }

    private void OnInventoryChanged()
    {
        if (_isBuilt == false)
        {
            BuildIfNeeded();
        }

        if (_slotViews.Count != _inventory.Slots.Count)
        {
            _isBuilt = false;
            BuildIfNeeded();
        }

        for (int i = 0; i < _slotViews.Count; i++)
        {
            InventorySlotView slotView = _slotViews[i];
            slotView.Refresh();
        }
    }

    private void OnActiveIndexChanged(int activeIndex)
    {
        if (_isBuilt == false)
        {
            BuildIfNeeded();
        }

        for (int i = 0; i < _slotViews.Count; i++)
        {
            bool isActive = i == activeIndex;
            _slotViews[i].SetActive(isActive);
        }
    }
}
