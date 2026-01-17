using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private InventoryDropper _inventoryDropper;
    [SerializeField] private CharacterEffects _effects;

    public void Scroll(Vector2 scrollValue)
    {
        if (scrollValue.y > 0f)
        {
            _inventory.PreviousActiveSlot();
        }

        if (scrollValue.y < 0f)
        {
            _inventory.NextActiveSlot();
        }
    }

    public void DropOne()
    {
        _inventoryDropper.DropOneFromActiveSlot();
    }

    public void DropAll()
    {
        _inventoryDropper.DropAllFromActiveSlot();
    }

    public void TryUseActiveItem()
    {
        InventorySlot activeSlot = _inventory.Slots[_inventory.ActiveIndex];

        if (activeSlot.IsEmpty() == true)
        {
            return;
        }

        if (activeSlot.Item.Effects.Count <= 0)
        {
            return;
        }

        UseItem(activeSlot.Item);
        _inventory.TryRemoveFromSlot(_inventory.ActiveIndex, 1);
    }

    private void UseItem(Item item)
    {
        for (int effectIndex = 0; effectIndex < item.Effects.Count; effectIndex++)
        {
            ItemEffect effect = item.Effects[effectIndex];
            effect.Apply(_effects);
        }
    }
}
