using System;
using UnityEngine;

public sealed class ModifierVendingMachine : Interactable
{
    [Header("Пул офферов")]
    [SerializeField] private ModifierOfferPool _offerPool;

    [Header("Карточки")]
    [SerializeField] private int _cardCount = 3;

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

        _rolledOffers = new ModifierOffer[_cardCount];

        RollOffersOnce();
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

        RollOffersOnce();

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

    private void RollOffersOnce()
    {
        if (_hasRolled)
        {
            return;
        }

        _hasRolled = true;

        ModifierOffer[] pool = _offerPool.Offers;

        if (pool.Length < _rolledOffers.Length)
        {
            throw new InvalidOperationException(nameof(_offerPool));
        }

        int[] indices = new int[pool.Length];

        for (int index = 0; index < indices.Length; index++)
        {
            indices[index] = index;
        }

        for (int cardIndex = 0; cardIndex < _rolledOffers.Length; cardIndex++)
        {
            int swapIndex = UnityEngine.Random.Range(cardIndex, indices.Length);

            int temp = indices[cardIndex];
            indices[cardIndex] = indices[swapIndex];
            indices[swapIndex] = temp;

            _rolledOffers[cardIndex] = pool[indices[cardIndex]];
        }
    }
}
