using System;
using UnityEngine;

public sealed class ModifierVendingMachinePurchase : MonoBehaviour
{
    [SerializeField] private ModifierVendingMachine _vendingMachine;

    public bool TryPurchase(int offerIndex, GameObject buyer)
    {
        if (buyer == null)
        {
            throw new InvalidOperationException(nameof(buyer));
        }

        ModifierOffer offer = _vendingMachine.GetOffer(offerIndex);

        if (offer == null)
        {
            throw new InvalidOperationException(nameof(offer));
        }

        CurrencyWallet currencyWallet = buyer.GetComponentInParent<CurrencyWallet>();

        if (currencyWallet == null)
        {
            throw new InvalidOperationException(nameof(currencyWallet));
        }

        WeaponModifierStack weaponModifierStack = buyer.GetComponentInParent<WeaponModifierStack>();

        if (weaponModifierStack == null)
        {
            throw new InvalidOperationException(nameof(weaponModifierStack));
        }

        if (currencyWallet.TrySpend(offer.Price) == false)
        {
            return false;
        }

        WeaponModifier[] modifiers = offer.Modifiers;

        if (modifiers != null)
        {
            for (int modifierIndex = 0; modifierIndex < modifiers.Length; modifierIndex++)
            {
                WeaponModifier modifier = modifiers[modifierIndex];

                if (modifier == null)
                {
                    continue;
                }

                weaponModifierStack.Add(modifier);
            }
        }

        _vendingMachine.MarkPurchased();

        return true;
    }
}
