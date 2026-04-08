using UnityEngine;

[CreateAssetMenu(fileName = "ModifierOffer", menuName = "Shop/Modifier Offer")]
public sealed class ModifierOffer : ScriptableObject
{
    [SerializeField] private string _title;
    [SerializeField] private Sprite _icon;
    [SerializeField] private Item _requiredItem;
    [SerializeField] private ModifierOfferRarity _rarity = ModifierOfferRarity.Rare;
    [SerializeField] private int _price;
    [SerializeField] private WeaponModifier[] _modifiers;

    public string Title => _title;

    public Sprite Icon => _icon;

    public Item RequiredItem => _requiredItem;

    public ModifierOfferRarity Rarity => _rarity;

    public int Price => _price;

    public WeaponModifier[] Modifiers => _modifiers;

    public bool IsCompatible(Inventory inventory)
    {
        if (_requiredItem == null)
        {
            return true;
        }

        if (inventory == null)
        {
            return false;
        }

        System.Collections.Generic.IReadOnlyList<InventorySlot> slots = inventory.Slots;

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];

            if (slot.IsEmpty())
            {
                continue;
            }

            if (slot.Item == _requiredItem)
            {
                return true;
            }
        }

        return false;
    }

    private void OnValidate()
    {
        if (_price < 0)
        {
            _price = 0;
        }
    }
}
