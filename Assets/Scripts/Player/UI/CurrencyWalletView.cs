using System;
using TMPro;
using UnityEngine;

public sealed class CurrencyWalletView : MonoBehaviour
{
    [SerializeField] private CurrencyWallet _wallet;
    [SerializeField] private TMP_Text _coinsText;

    private void Awake()
    {
        if (_wallet == null)
        {
            throw new InvalidOperationException(nameof(_wallet));
        }

        if (_coinsText == null)
        {
            throw new InvalidOperationException(nameof(_coinsText));
        }
    }

    private void OnEnable()
    {
        _wallet.CoinsChanged += OnCoinsChanged;
    }

    private void Start()
    {
        Refresh(_wallet.Coins);
    }

    private void OnDisable()
    {
        _wallet.CoinsChanged -= OnCoinsChanged;
    }

    private void OnCoinsChanged(int coins)
    {
        Refresh(coins);
    }

    private void Refresh(int coins)
    {
        _coinsText.text = "Coins " + coins.ToString();
    }
}
