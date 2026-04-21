using System;
using TMPro;
using UnityEngine;

public sealed class PlayerModifierMenuView : MonoBehaviour
{
    private const string CoinMark = "\u00A4 ";
    private const float DefaultTimeScale = 1.0f;
    private const float TimeScaleThreshold = 0.0001f;

    [Header("UI")]
    [SerializeField] private GameObject _root;
    [SerializeField] private BlurOverlay _blurOverlay;
    [SerializeField] private PauseController _pauseController;
    [SerializeField] private TMP_Text _coinsText;
    [SerializeField] private ModifierOfferCardView[] _cardViews;

    [Header("Покупка")]
    [SerializeField] private PlayerModifierPurchase _purchase;

    [Header("Анимации")]
    [SerializeField] private ModifierVendingMachineMenuAnimator _animator;

    private PlayerModifierShop _shop;
    private GameObject _buyer;
    private bool _isBuyPending;
    private bool _isPauseOwned;

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

        if (_isPauseOwned)
        {
            _pauseController.ResumeTimeOnly();
            _isPauseOwned = false;
        }
    }

    public void HandleInteractionRequest(PlayerModifierShop shop, GameObject buyer)
    {
        if (IsOpen)
        {
            TryClose();

            return;
        }

        Show(shop, buyer);
    }

    public void Show(PlayerModifierShop shop, GameObject buyer)
    {
        if (shop == null)
        {
            throw new InvalidOperationException(nameof(shop));
        }

        if (buyer == null)
        {
            throw new InvalidOperationException(nameof(buyer));
        }

        EnsurePauseController();

        _shop = shop;
        _buyer = buyer;
        _isBuyPending = false;
        _isPauseOwned = _pauseController.IsPaused == false
            && Mathf.Abs(Time.timeScale - DefaultTimeScale) <= TimeScaleThreshold;

        if (_isPauseOwned)
        {
            _pauseController.PauseTimeOnly();
        }

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
        if (_isPauseOwned)
        {
            _pauseController.ResumeTimeOnly();
            _isPauseOwned = false;
        }

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

        _shop = null;
        _buyer = null;
        _isBuyPending = false;
        _isPauseOwned = false;
    }

    private void EnsurePauseController()
    {
        if (_pauseController != null)
        {
            return;
        }

        PauseController pauseController = PauseController.Instance;

        if (pauseController == null)
        {
            throw new InvalidOperationException(nameof(pauseController));
        }

        _pauseController = pauseController;
    }

    private void Refresh()
    {
        if (_shop == null)
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

        int cardCount = Mathf.Min(_cardViews.Length, _shop.OfferCount);

        for (int cardIndex = 0; cardIndex < cardCount; cardIndex++)
        {
            PlayerModifierOffer offer = _shop.GetOffer(cardIndex);

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
        if (_shop == null)
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
