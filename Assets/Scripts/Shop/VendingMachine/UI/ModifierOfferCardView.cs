using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class ModifierOfferCardView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image _icon;
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _priceText;
    [SerializeField] private Text _descriptionText;
    [SerializeField] private Button _buyButton;
    [SerializeField] private Text _buyButtonText;

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
        _icon.sprite = offer.Icon;
        _titleText.text = offer.Title;
        _priceText.text = offer.Price.ToString();
        _descriptionText.text = BuildDescription(offer);

        _buyButton.interactable = canBuy;

        if (canBuy == true)
        {
            _buyButtonText.text = "Купить";
        }
        else
        {
            _buyButtonText.text = "Нет денег";
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
