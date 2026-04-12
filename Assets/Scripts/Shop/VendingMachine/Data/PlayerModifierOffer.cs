using UnityEngine;

[CreateAssetMenu(fileName = "PlayerModifierOffer", menuName = "Shop/Player Modifier Offer")]
public sealed class PlayerModifierOffer : ScriptableObject, IShopOffer
{
    [SerializeField] private string _title;
    [SerializeField] private string _description;
    [SerializeField] private Sprite _icon;
    [SerializeField] private ModifierOfferRarity _rarity = ModifierOfferRarity.Rare;
    [SerializeField] private int _price;
    [SerializeField] private PlayerModifier[] _modifiers;

    public string Title => _title;

    public string Description => _description;

    public Sprite Icon => _icon;

    public ModifierOfferRarity Rarity => _rarity;

    public int Price => _price;

    public PlayerModifier[] Modifiers => _modifiers;
}
