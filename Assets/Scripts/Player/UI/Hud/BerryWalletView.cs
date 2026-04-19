using System;
using TMPro;
using UnityEngine;

public sealed class BerryWalletView : MonoBehaviour
{
    [SerializeField] private BerryWallet _wallet;
    [SerializeField] private TMP_Text _berriesText;

    private void Awake()
    {
        if (_wallet == null)
        {
            throw new InvalidOperationException(nameof(_wallet));
        }

        if (_berriesText == null)
        {
            throw new InvalidOperationException(nameof(_berriesText));
        }
    }

    private void OnEnable()
    {
        _wallet.BerriesChanged += OnBerriesChanged;
    }

    private void Start()
    {
        Refresh(_wallet.Berries);
    }

    private void OnDisable()
    {
        _wallet.BerriesChanged -= OnBerriesChanged;
    }

    private void OnBerriesChanged(int berries)
    {
        Refresh(berries);
    }

    private void Refresh(int berries)
    {
        _berriesText.text = "Berries " + berries.ToString();
    }
}
