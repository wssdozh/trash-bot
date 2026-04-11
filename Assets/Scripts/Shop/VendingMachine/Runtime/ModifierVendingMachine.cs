using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ModifierVendingMachine : Interactable
{
    [Header("Пул офферов")]
    [SerializeField] private ModifierOfferPool _offerPool;

    [Header("Карточки")]
    [SerializeField] private int _cardCount = 3;

    [Header("Редкость")]
    [SerializeField] private int _commonWeight = 5;
    [SerializeField] private int _rareWeight = 3;
    [SerializeField] private int _legendaryWeight = 2;

    [Header("Одноразовый")]
    [SerializeField] private bool _disableColliderOnPurchase = true;

    private ModifierOffer[] _rolledOffers;
    private bool _hasRolled;
    private bool _isPurchased;

    public event Action<ModifierVendingMachine, GameObject> InteractionRequested;

    public int OfferCount => _rolledOffers.Length;

    protected override void Awake()
    {
        base.Awake();

        if (_cardCount <= 0)
        {
            throw new InvalidOperationException(nameof(_cardCount));
        }

        if (_commonWeight <= 0)
        {
            throw new InvalidOperationException(nameof(_commonWeight));
        }

        if (_rareWeight <= 0)
        {
            throw new InvalidOperationException(nameof(_rareWeight));
        }

        if (_legendaryWeight <= 0)
        {
            throw new InvalidOperationException(nameof(_legendaryWeight));
        }

        _rolledOffers = new ModifierOffer[_cardCount];
    }

    public ModifierOffer GetOffer(int index)
    {
        if (index < 0 || index >= _rolledOffers.Length)
        {
            throw new InvalidOperationException(nameof(index));
        }

        return _rolledOffers[index];
    }

    public override string GetPrompt()
    {
        if (_isPurchased)
        {
            return string.Empty;
        }

        return _interactionName;
    }

    public override void Interact(GameObject interactor)
    {
        if (_isPurchased)
        {
            return;
        }

        RollOffersOnce(interactor);

        Action<ModifierVendingMachine, GameObject> interactionRequested = InteractionRequested;

        if (interactionRequested == null)
        {
            return;
        }

        interactionRequested.Invoke(this, interactor);
    }

    public void MarkPurchased()
    {
        if (_isPurchased)
        {
            return;
        }

        _isPurchased = true;

        Highlight(false);

        if (_disableColliderOnPurchase)
        {
            Collider collider = GetComponent<Collider>();

            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        enabled = false;
    }

    private void RollOffersOnce(GameObject buyer)
    {
        if (_hasRolled)
        {
            return;
        }

        _hasRolled = true;

        ModifierOffer[] pool = _offerPool.Offers;
        List<ModifierOffer> availableOffers = CreateAvailableOffers(pool, buyer);

        if (availableOffers.Count < _rolledOffers.Length)
        {
            availableOffers = CreateAllOffers(pool);
        }

        if (availableOffers.Count < _rolledOffers.Length)
        {
            throw new InvalidOperationException(nameof(_offerPool));
        }

        RollOffers(availableOffers);
    }

    private List<ModifierOffer> CreateAvailableOffers(ModifierOffer[] pool, GameObject buyer)
    {
        Inventory inventory = GetInventory(buyer);
        List<ModifierOffer> availableOffers = new List<ModifierOffer>();

        for (int i = 0; i < pool.Length; i++)
        {
            ModifierOffer offer = pool[i];

            if (offer == null)
            {
                continue;
            }

            if (inventory != null && offer.IsCompatible(inventory) == false)
            {
                continue;
            }

            availableOffers.Add(offer);
        }

        return availableOffers;
    }

    private List<ModifierOffer> CreateAllOffers(ModifierOffer[] pool)
    {
        List<ModifierOffer> offers = new List<ModifierOffer>();

        for (int i = 0; i < pool.Length; i++)
        {
            ModifierOffer offer = pool[i];

            if (offer == null)
            {
                continue;
            }

            offers.Add(offer);
        }

        return offers;
    }

    private Inventory GetInventory(GameObject buyer)
    {
        if (buyer == null)
        {
            return null;
        }

        return buyer.GetComponentInParent<Inventory>();
    }

    private void RollOffers(List<ModifierOffer> offers)
    {
        List<ModifierOffer> sourceOffers = new List<ModifierOffer>(offers);

        for (int cardIndex = 0; cardIndex < _rolledOffers.Length; cardIndex++)
        {
            int pickedIndex = GetPickedIndex(sourceOffers);

            _rolledOffers[cardIndex] = sourceOffers[pickedIndex];
            sourceOffers.RemoveAt(pickedIndex);
        }
    }

    private int GetPickedIndex(List<ModifierOffer> offers)
    {
        int totalWeight = 0;

        for (int i = 0; i < offers.Count; i++)
        {
            totalWeight += GetOfferWeight(offers[i]);
        }

        int rolledWeight = UnityEngine.Random.Range(0, totalWeight);
        int currentWeight = 0;

        for (int i = 0; i < offers.Count; i++)
        {
            currentWeight += GetOfferWeight(offers[i]);

            if (rolledWeight < currentWeight)
            {
                return i;
            }
        }

        return offers.Count - 1;
    }

    private int GetOfferWeight(ModifierOffer offer)
    {
        if (offer.Rarity == ModifierOfferRarity.Epic)
        {
            return _rareWeight;
        }

        if (offer.Rarity == ModifierOfferRarity.Legendary)
        {
            return _legendaryWeight;
        }

        return _commonWeight;
    }
}
