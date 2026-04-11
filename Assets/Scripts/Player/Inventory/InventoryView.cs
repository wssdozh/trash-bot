using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryView : MonoBehaviour
{
    private const string SlotViewPath = "Prefabs/UI/Cell";

    [SerializeField] private Inventory _inventory;
    [SerializeField] private InventorySlotView _slotViewPrefab;
    [SerializeField] private Transform _slotsParent;

    private readonly List<InventorySlotView> _slotViews = new List<InventorySlotView>();
    private int _boundSlotsCount = -1;

    private void Awake()
    {
        if (_inventory == null)
        {
            throw new InvalidOperationException(nameof(_inventory));
        }

        if (_slotsParent == null)
        {
            throw new InvalidOperationException(nameof(_slotsParent));
        }

        if (_slotViewPrefab == null)
        {
            GameObject slotViewPrefab = Resources.Load<GameObject>(SlotViewPath);

            if (slotViewPrefab == null)
            {
                throw new InvalidOperationException(nameof(_slotViewPrefab));
            }

            _slotViewPrefab = slotViewPrefab.GetComponent<InventorySlotView>();

            if (_slotViewPrefab == null)
            {
                throw new InvalidOperationException(nameof(_slotViewPrefab));
            }
        }
    }

    private void OnEnable()
    {
        _inventory.InventoryChanged += OnInventoryChanged;
        _inventory.ActiveIndexChanged += OnActiveIndexChanged;
    }

    private void Start()
    {
        EnsureSlotViews(true);
        OnInventoryChanged();
        OnActiveIndexChanged(_inventory.ActiveIndex);
    }

    private void OnDisable()
    {
        _inventory.InventoryChanged -= OnInventoryChanged;
        _inventory.ActiveIndexChanged -= OnActiveIndexChanged;
    }

    private void EnsureSlotViews(bool shouldRebindSlots)
    {
        int slotsCount = _inventory.Slots.Count;

        for (int index = 0; index < slotsCount; index++)
        {
            InventorySlotView slotView;

            if (index < _slotViews.Count)
            {
                slotView = _slotViews[index];
            }
            else
            {
                slotView = Instantiate(_slotViewPrefab, _slotsParent);
                _slotViews.Add(slotView);
            }

            slotView.gameObject.SetActive(true);

            if (shouldRebindSlots)
            {
                slotView.SetSlot(_inventory.Slots[index]);
            }
        }

        for (int index = slotsCount; index < _slotViews.Count; index++)
        {
            InventorySlotView slotView = _slotViews[index];
            slotView.gameObject.SetActive(false);
            slotView.SetActive(false);
        }

        _boundSlotsCount = slotsCount;
    }

    private void OnDestroy()
    {
        for (int index = 0; index < _slotViews.Count; index++)
        {
            InventorySlotView slotView = _slotViews[index];

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
        bool shouldRebindSlots = _boundSlotsCount != _inventory.Slots.Count;
        EnsureSlotViews(shouldRebindSlots);

        int slotsCount = _inventory.Slots.Count;

        for (int index = 0; index < slotsCount; index++)
        {
            InventorySlotView slotView = _slotViews[index];
            slotView.Refresh();
        }
    }

    private void OnActiveIndexChanged(int activeIndex)
    {
        EnsureSlotViews(false);

        int slotsCount = _inventory.Slots.Count;

        for (int index = 0; index < _slotViews.Count; index++)
        {
            bool isActive = index == activeIndex && index < slotsCount;
            _slotViews[index].SetActive(isActive);
        }
    }
}
