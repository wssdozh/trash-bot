using System;
using TMPro;
using UnityEngine;

public sealed class ModifierVendingMachineMenuView : MonoBehaviour
{
    private const string CoinMark = "\u00A4 ";

    [Header("UI")]
    [SerializeField] private GameObject _root;
    [SerializeField] private BlurOverlay _blurOverlay;
    [SerializeField] private TMP_Text _coinsText;
    [SerializeField] private ModifierOfferCardView[] _cardViews;

    [Header("Покупка")]
    [SerializeField] private ModifierVendingMachinePurchase _purchase;

    [Header("Анимации")]
    [SerializeField] private ModifierVendingMachineMenuAnimator _animator;

    private ModifierVendingMachine _machine;
    private GameObject _buyer;
    private bool _isBuyPending;

    public bool IsOpen => _root.activeSelf;

    private void OnEnable()
    {
        for (int cardIndex = 0; cardIndex < _cardViews.Length; cardIndex++)
        {
            int index = cardIndex;

            _cardViews[cardIndex].SetIndex(index);
            _cardViews[cardIndex].BuyClicked += OnBuyClicked;
        }
    }

    private void OnDisable()
    {
        for (int cardIndex = 0; cardIndex < _cardViews.Length; cardIndex++)
        {
            _cardViews[cardIndex].BuyClicked -= OnBuyClicked;
        }
    }

    public void HandleInteractionRequest(ModifierVendingMachine machine, GameObject buyer)
    {
        if (IsOpen)
        {
            TryClose();

            return;
        }

        Show(machine, buyer);
    }

    public void Show(ModifierVendingMachine machine, GameObject buyer)
    {
        if (machine == null)
        {
            throw new InvalidOperationException(nameof(machine));
        }

        if (buyer == null)
        {
            throw new InvalidOperationException(nameof(buyer));
        }

        _machine = machine;
        _buyer = buyer;
        _isBuyPending = false;

        if (_blurOverlay != null)
        {
            Transform blurTransform = _blurOverlay.transform;
            Transform rootTransform = _root.transform;

            if (blurTransform.parent == rootTransform.parent)
            {
                blurTransform.SetAsFirstSibling();
                rootTransform.SetAsLastSibling();
            }

            _blurOverlay.Show();
        }

        Refresh();

        if (_animator != null)
        {
            _animator.PlayOpen();

            return;
        }

        _root.SetActive(true);
    }

    public bool TryClose()
    {
        if (IsOpen == false)
        {
            return false;
        }

        Hide();

        return true;
    }

    public void Hide()
    {
        if (_blurOverlay != null)
        {
            _blurOverlay.Hide();
        }

        if (_animator != null)
        {
            _animator.PlayClose(ClearContext);

            return;
        }

        _root.SetActive(false);

        ClearContext();
    }

    private void ClearContext()
    {
        _root.SetActive(false);

        _machine = null;
        _buyer = null;
        _isBuyPending = false;
    }

    private void Refresh()
    {
        if (_machine == null)
        {
            return;
        }

        if (_buyer == null)
        {
            return;
        }

        CurrencyWallet currencyWallet = _buyer.GetComponentInParent<CurrencyWallet>();

        if (currencyWallet == null)
        {
            throw new InvalidOperationException(nameof(currencyWallet));
        }

        if (_coinsText != null)
        {
            _coinsText.text = CoinMark + currencyWallet.Coins.ToString();
        }

        int cardCount = Mathf.Min(_cardViews.Length, _machine.OfferCount);

        for (int cardIndex = 0; cardIndex < cardCount; cardIndex++)
        {
            ModifierOffer offer = _machine.GetOffer(cardIndex);

            if (offer == null)
            {
                throw new InvalidOperationException(nameof(offer));
            }

            bool canBuy = currencyWallet.CanSpend(offer.Price);

            _cardViews[cardIndex].Render(offer, canBuy);
        }
    }

    private void OnBuyClicked(int index)
    {
        if (_machine == null)
        {
            return;
        }

        if (_buyer == null)
        {
            return;
        }

        if (_isBuyPending)
        {
            return;
        }

        _isBuyPending = true;

        bool purchased = _purchase.TryPurchase(index, _buyer);

        if (purchased == false)
        {
            _isBuyPending = false;
            Refresh();

            return;
        }

        Hide();
    }
}
