using UnityEngine;

public interface IShopOffer
{
    string Title { get; }

    Sprite Icon { get; }

    ModifierOfferRarity Rarity { get; }

    int Price { get; }

    string Description { get; }
}
