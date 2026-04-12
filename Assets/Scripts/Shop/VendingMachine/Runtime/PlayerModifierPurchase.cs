using System;
using UnityEngine;

public sealed class PlayerModifierPurchase : MonoBehaviour
{
    [SerializeField] private PlayerModifierShop _shop;

    public bool TryPurchase(int offerIndex, GameObject buyer)
    {
        if (buyer == null)
        {
            throw new InvalidOperationException(nameof(buyer));
        }

        PlayerModifierOffer offer = _shop.GetOffer(offerIndex);

        if (offer == null)
        {
            throw new InvalidOperationException(nameof(offer));
        }

        CurrencyWallet currencyWallet = buyer.GetComponentInParent<CurrencyWallet>();

        if (currencyWallet == null)
        {
            throw new InvalidOperationException(nameof(currencyWallet));
        }

        PlayerModifierStack playerModifierStack = buyer.GetComponentInParent<PlayerModifierStack>();

        if (playerModifierStack == null)
        {
            throw new InvalidOperationException(nameof(playerModifierStack));
        }

        if (currencyWallet.TrySpend(offer.Price) == false)
        {
            return false;
        }

        PlayerModifier[] modifiers = offer.Modifiers;

        if (modifiers != null)
        {
            for (int modifierIndex = 0; modifierIndex < modifiers.Length; modifierIndex++)
            {
                PlayerModifier modifier = modifiers[modifierIndex];

                if (modifier == null)
                {
                    continue;
                }

                playerModifierStack.Add(modifier);
            }
        }

        _shop.MarkPurchased();

        return true;
    }
}
