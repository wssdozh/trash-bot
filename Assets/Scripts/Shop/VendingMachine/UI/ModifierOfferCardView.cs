using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class ModifierOfferCardView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image _background;
    [SerializeField] private Image _icon;
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _priceText;
    [SerializeField] private Text _descriptionText;
    [SerializeField] private Button _buyButton;
    [SerializeField] private Text _buyButtonText;

    [Header("\u0420\u0435\u0434\u043A\u043E\u0441\u0442\u044C")]
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

    public void Render(ModifierOffer offer, bool canBuy)
    {
        _background.sprite = GetCardSprite(offer.Rarity);
        _icon.sprite = offer.Icon;
        _titleText.text = offer.Title;
        _priceText.text = offer.Price.ToString();
        _descriptionText.text = BuildDescription(offer);

        _buyButton.interactable = canBuy;

        if (canBuy)
        {
            _buyButtonText.text = "\u041A\u0443\u043F\u0438\u0442\u044C";
        }
        else
        {
            _buyButtonText.text = "\u041D\u0435\u0442 \u0434\u0435\u043D\u0435\u0433";
        }
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

    private string BuildDescription(ModifierOffer offer)
    {
        WeaponModifier[] modifiers = offer.Modifiers;

        if (modifiers == null)
        {
            return string.Empty;
        }

        if (modifiers.Length == 0)
        {
            return string.Empty;
        }

        string description = string.Empty;

        for (int i = 0; i < modifiers.Length; i++)
        {
            WeaponModifier modifier = modifiers[i];

            if (modifier == null)
            {
                continue;
            }

            if (description.Length > 0)
            {
                description += "\n";
            }

            description += modifier.name;
        }

        return description;
    }
}

