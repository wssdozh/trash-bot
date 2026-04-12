using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class ModifierOfferCardView : MonoBehaviour
{
    private const string CoinMark = "\u00A4 ";

    [Header("UI")]
    [SerializeField] private Image _background;
    [SerializeField] private Image _icon;
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _priceText;
    [SerializeField] private Text _descriptionText;
    [SerializeField] private Button _buyButton;
    [SerializeField] private Text _buyButtonText;

    [Header("Редкость")]
    [SerializeField] private Sprite _rareCardSprite;
    [SerializeField] private Sprite _epicCardSprite;
    [SerializeField] private Sprite _legendaryCardSprite;

    private int _index;

    public event Action<int> BuyClicked;

    private void OnEnable()
    {
        _buyButton.onClick.AddListener(OnBuyButtonClicked);
    }

    private void OnDisable()
    {
        _buyButton.onClick.RemoveListener(OnBuyButtonClicked);
    }

    public void SetIndex(int index)
    {
        _index = index;
    }

    public void Render(IShopOffer offer, bool canBuy)
    {
        _background.sprite = GetCardSprite(offer.Rarity);
        _icon.enabled = offer.Icon != null;
        _icon.sprite = offer.Icon;
        _titleText.text = offer.Title;
        _priceText.text = CoinMark + offer.Price.ToString();
        _descriptionText.text = offer.Description;

        _buyButton.interactable = canBuy;

        if (canBuy)
        {
            _buyButtonText.text = "Купить";

            return;
        }

        _buyButtonText.text = "Нет денег";
    }

    private void OnBuyButtonClicked()
    {
        Action<int> buyClicked = BuyClicked;

        if (buyClicked == null)
        {
            return;
        }

        buyClicked.Invoke(_index);
    }

    private Sprite GetCardSprite(ModifierOfferRarity rarity)
    {
        if (rarity == ModifierOfferRarity.Epic)
        {
            return _epicCardSprite;
        }

        if (rarity == ModifierOfferRarity.Legendary)
        {
            return _legendaryCardSprite;
        }

        return _rareCardSprite;
    }
}
