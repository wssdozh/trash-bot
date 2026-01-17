public sealed class PlayerActiveWeaponType
{
    private readonly Inventory _inventory;

    public PlayerActiveWeaponType(Inventory inventory)
    {
        _inventory = inventory;
    }

    public WeaponType Value
    {
        get
        {
            int activeIndex = _inventory.ActiveIndex;
            int slotsCount = _inventory.Slots.Count;

            if (activeIndex < 0)
            {
                return WeaponType.None;
            }

            if (activeIndex >= slotsCount)
            {
                return WeaponType.None;
            }

            InventorySlot slot = _inventory.Slots[activeIndex];

            if (slot.IsEmpty() == true)
            {
                return WeaponType.None;
            }

            return slot.Item.WeaponType;
        }
    }
}
