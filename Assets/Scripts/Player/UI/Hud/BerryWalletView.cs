using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BerryWalletView : MonoBehaviour
{
    private const string CooldownImageName = "BerryCooldownFill";
    private const float ReadyCooldownProgress = 1f;
    private const float DimmedIconMultiplier = 0.35f;
    private const float DimmedIconAlphaMultiplier = 0.75f;

    [SerializeField] private BerryWallet _wallet;
    [SerializeField] private Image _cooldownSourceImage;
    [SerializeField] private TMP_Text _berriesText;

    private Image _cooldownImage;
    private Color _readyIconColor;
    private Color _dimmedIconColor;

    private void Awake()
    {
        if (_wallet == null)
        {
            throw new InvalidOperationException(nameof(_wallet));
        }

        if (_cooldownSourceImage == null)
        {
            throw new InvalidOperationException(nameof(_cooldownSourceImage));
        }

        if (_berriesText == null)
        {
            throw new InvalidOperationException(nameof(_berriesText));
        }

        _readyIconColor = _cooldownSourceImage.color;
        _dimmedIconColor = new Color(
            _readyIconColor.r * DimmedIconMultiplier,
            _readyIconColor.g * DimmedIconMultiplier,
            _readyIconColor.b * DimmedIconMultiplier,
            _readyIconColor.a * DimmedIconAlphaMultiplier);
        _cooldownImage = CreateCooldownImage();
    }

    private void OnEnable()
    {
        _wallet.BerriesChanged += OnBerriesChanged;
    }

    private void Start()
    {
        Refresh(_wallet.Berries);
        RefreshCooldown();
    }

    private void Update()
    {
        RefreshCooldown();
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
        _berriesText.text = berries.ToString();
    }

    private void RefreshCooldown()
    {
        float cooldownProgress01 = _wallet.CooldownProgress01;

        if (cooldownProgress01 >= ReadyCooldownProgress)
        {
            _cooldownSourceImage.color = _readyIconColor;
            _cooldownImage.enabled = false;
            _cooldownImage.fillAmount = ReadyCooldownProgress;

            return;
        }

        _cooldownSourceImage.color = _dimmedIconColor;
        _cooldownImage.enabled = true;
        _cooldownImage.fillAmount = cooldownProgress01;
    }

    private Image CreateCooldownImage()
    {
        GameObject cooldownObject = new GameObject(CooldownImageName, typeof(RectTransform), typeof(CanvasRenderer));
        cooldownObject.layer = _cooldownSourceImage.gameObject.layer;

        RectTransform cooldownRect = (RectTransform)cooldownObject.transform;
        cooldownRect.SetParent(_cooldownSourceImage.rectTransform, false);
        cooldownRect.anchorMin = Vector2.zero;
        cooldownRect.anchorMax = Vector2.one;
        cooldownRect.offsetMin = Vector2.zero;
        cooldownRect.offsetMax = Vector2.zero;
        cooldownRect.pivot = new Vector2(0.5f, 0.5f);

        Image cooldownImage = cooldownObject.AddComponent<Image>();
        cooldownImage.sprite = _cooldownSourceImage.sprite;
        cooldownImage.color = _readyIconColor;
        cooldownImage.raycastTarget = false;
        cooldownImage.preserveAspect = true;
        cooldownImage.type = Image.Type.Filled;
        cooldownImage.fillMethod = Image.FillMethod.Radial360;
        cooldownImage.fillOrigin = (int)Image.Origin360.Top;
        cooldownImage.fillClockwise = true;
        cooldownImage.fillAmount = ReadyCooldownProgress;
        cooldownImage.enabled = false;

        return cooldownImage;
    }
}
