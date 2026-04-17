using System;
using System.Text;
using DG.Tweening;
using JunkyardBoss;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BossHealthOverlay : MonoBehaviour
{
    private const float SearchInterval = 0.35f;
    private const float SliderDuration = 0.2f;
    private const float ShowDuration = 0.32f;
    private const float HideDuration = 0.24f;
    private const float HiddenScale = 0.94f;
    private const float EnterOffsetY = 88f;
    private const float HideOffsetY = 30f;
    private const float PanelWidth = 760f;
    private const float PanelHeight = 92f;
    private const float BarHeight = 18f;

    private static Sprite s_uiSprite;
    private static bool s_uiSpriteResolved;

    private RectTransform _uiRoot;
    private TMP_FontAsset _fontAsset;
    private RectTransform _panelTransform;
    private CanvasGroup _canvasGroup;
    private Slider _slider;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _valueText;
    private Sequence _visibilitySequence;
    private Tween _sliderTween;
    private BossExcavator _boss;
    private Health _bossHealth;
    private RoomCombatLock _roomCombatLock;
    private Vector2 _shownAnchoredPosition;
    private float _searchTimer;
    private bool _isShown;

    public void Initialize(RectTransform uiRoot, TMP_FontAsset fontAsset)
    {
        if (uiRoot == null)
        {
            throw new InvalidOperationException(nameof(uiRoot));
        }

        if (fontAsset == null)
        {
            throw new InvalidOperationException(nameof(fontAsset));
        }

        _uiRoot = uiRoot;
        _fontAsset = fontAsset;
        BuildView();
        ApplyHiddenState();
    }

    private void OnEnable()
    {
        _searchTimer = 0f;
    }

    private void OnDisable()
    {
        UnbindBoss();
        KillVisibilitySequence();
        KillSliderTween();

        if (_panelTransform != null)
        {
            ApplyHiddenState();
        }
    }

    private void Update()
    {
        if (_panelTransform == null)
        {
            return;
        }

        TickBossBinding();

        if (ShouldShow() == false)
        {
            Hide();

            return;
        }

        Show();
    }

    private void BuildView()
    {
        if (_panelTransform != null)
        {
            return;
        }

        Sprite uiSprite = ResolveUiSprite();

        int uiLayer = _uiRoot.gameObject.layer;
        GameObject panelObject = CreateUiObject("Boss Health", _uiRoot, uiLayer);
        _panelTransform = panelObject.GetComponent<RectTransform>();
        _canvasGroup = panelObject.AddComponent<CanvasGroup>();
        _shownAnchoredPosition = new Vector2(0f, -16f);

        ConfigureRect(
            _panelTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            _shownAnchoredPosition,
            new Vector2(PanelWidth, PanelHeight));

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.sprite = uiSprite;
        panelImage.type = Image.Type.Simple;
        panelImage.color = new Color(0.03f, 0.04f, 0.05f, 0.94f);
        panelImage.raycastTarget = false;

        RectTransform accentTransform = CreateImageRect(
            "Accent",
            _panelTransform,
            uiLayer,
            uiSprite,
            new Color(0.82f, 0.33f, 0.18f, 1f));
        ConfigureRect(
            accentTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, 0f),
            new Vector2(0f, 4f));

        _titleText = CreateText("Title", _panelTransform, uiLayer, 30f, FontStyles.Bold);
        _titleText.alignment = TextAlignmentOptions.Left;
        _titleText.textWrappingMode = TextWrappingModes.NoWrap;
        _titleText.overflowMode = TextOverflowModes.Ellipsis;
        _titleText.text = "BOSS";
        ConfigureRect(
            _titleText.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0.7f, 1f),
            new Vector2(0f, 1f),
            new Vector2(22f, -14f),
            new Vector2(-150f, 30f));

        _valueText = CreateText("Value", _panelTransform, uiLayer, 22f, FontStyles.Bold);
        _valueText.alignment = TextAlignmentOptions.Right;
        _valueText.textWrappingMode = TextWrappingModes.NoWrap;
        _valueText.text = "0 / 0";
        ConfigureRect(
            _valueText.rectTransform,
            new Vector2(0.7f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-22f, -16f),
            new Vector2(-22f, 28f));

        GameObject barObject = CreateUiObject("Bar", _panelTransform, uiLayer);
        RectTransform barTransform = barObject.GetComponent<RectTransform>();
        ConfigureRect(
            barTransform,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 16f),
            new Vector2(-32f, BarHeight));

        Image barBackground = barObject.AddComponent<Image>();
        barBackground.sprite = uiSprite;
        barBackground.type = Image.Type.Simple;
        barBackground.color = new Color(0.12f, 0.14f, 0.18f, 1f);
        barBackground.raycastTarget = false;

        _slider = barObject.AddComponent<Slider>();
        _slider.interactable = false;
        _slider.minValue = 0f;
        _slider.maxValue = 1f;
        _slider.wholeNumbers = false;
        _slider.direction = Slider.Direction.LeftToRight;
        _slider.targetGraphic = barBackground;

        GameObject fillAreaObject = CreateUiObject("Fill Area", barTransform, uiLayer);
        RectTransform fillAreaTransform = fillAreaObject.GetComponent<RectTransform>();
        ConfigureRect(
            fillAreaTransform,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(-6f, -6f));

        GameObject fillObject = CreateUiObject("Fill", fillAreaTransform, uiLayer);
        RectTransform fillTransform = fillObject.GetComponent<RectTransform>();
        ConfigureRect(
            fillTransform,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero);

        Image fillImage = fillObject.AddComponent<Image>();
        fillImage.sprite = uiSprite;
        fillImage.type = Image.Type.Simple;
        fillImage.color = new Color(0.83f, 0.35f, 0.19f, 1f);
        fillImage.raycastTarget = false;

        _slider.fillRect = fillTransform;
        _slider.value = 1f;
    }

    private Sprite ResolveUiSprite()
    {
        if (s_uiSpriteResolved)
        {
            return s_uiSprite;
        }

        s_uiSpriteResolved = true;
        Texture2D whiteTexture = Texture2D.whiteTexture;

        if (whiteTexture == null)
        {
            throw new InvalidOperationException(nameof(whiteTexture));
        }

        s_uiSprite = Sprite.Create(
            whiteTexture,
            new Rect(0f, 0f, whiteTexture.width, whiteTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        s_uiSprite.name = "BossHealthOverlaySprite";
        s_uiSprite.hideFlags = HideFlags.HideAndDontSave;

        return s_uiSprite;
    }

    private void TickBossBinding()
    {
        if (HasValidBossBinding())
        {
            return;
        }

        UnbindBoss();
        _searchTimer -= Time.unscaledDeltaTime;

        if (_searchTimer > 0f)
        {
            return;
        }

        _searchTimer = SearchInterval;
        TryBindBoss();
    }

    private bool HasValidBossBinding()
    {
        if (_boss == null)
        {
            return false;
        }

        if (_boss.gameObject.activeInHierarchy == false)
        {
            return false;
        }

        if (_bossHealth == null)
        {
            return false;
        }

        return true;
    }

    private void TryBindBoss()
    {
        BossExcavator[] bosses = FindObjectsByType<BossExcavator>(FindObjectsSortMode.None);
        BossExcavator fallbackBoss = null;
        RoomCombatLock fallbackRoomCombatLock = null;
        int bossIndex = 0;

        while (bossIndex < bosses.Length)
        {
            BossExcavator boss = bosses[bossIndex];
            bossIndex += 1;

            if (boss == null)
            {
                continue;
            }

            if (boss.Health == null)
            {
                continue;
            }

            if (boss.gameObject.activeInHierarchy == false)
            {
                continue;
            }

            RoomCombatLock roomCombatLock = boss.GetComponentInParent<RoomCombatLock>();

            if (roomCombatLock != null && roomCombatLock.IsLocked)
            {
                BindBoss(boss, roomCombatLock);

                return;
            }

            if (fallbackBoss == null && boss.IsDead == false)
            {
                fallbackBoss = boss;
                fallbackRoomCombatLock = roomCombatLock;
            }
        }

        if (fallbackBoss == null)
        {
            return;
        }

        BindBoss(fallbackBoss, fallbackRoomCombatLock);
    }

    private void BindBoss(BossExcavator boss, RoomCombatLock roomCombatLock)
    {
        if (_boss == boss && _bossHealth == boss.Health)
        {
            _roomCombatLock = roomCombatLock;

            return;
        }

        UnbindBoss();
        _boss = boss;
        _bossHealth = boss.Health;
        _roomCombatLock = roomCombatLock;
        _bossHealth.Changed += OnBossHealthChanged;
        _bossHealth.Ended += OnBossHealthEnded;
        _titleText.text = FormatBossTitle(boss.name);
        UpdateHealthImmediate();
    }

    private void UnbindBoss()
    {
        if (_bossHealth != null)
        {
            _bossHealth.Changed -= OnBossHealthChanged;
            _bossHealth.Ended -= OnBossHealthEnded;
        }

        _boss = null;
        _bossHealth = null;
        _roomCombatLock = null;
    }

    private bool ShouldShow()
    {
        if (_boss == null)
        {
            return false;
        }

        if (_bossHealth == null)
        {
            return false;
        }

        if (_bossHealth.Value <= _bossHealth.MinValue)
        {
            return false;
        }

        if (_roomCombatLock != null && _roomCombatLock.IsLocked == false)
        {
            return false;
        }

        return true;
    }

    private void Show()
    {
        if (_isShown)
        {
            return;
        }

        _isShown = true;
        KillVisibilitySequence();
        _panelTransform.SetAsLastSibling();
        _canvasGroup.alpha = 0f;
        _panelTransform.anchoredPosition = GetEnterPosition();
        _panelTransform.localScale = new Vector3(HiddenScale, HiddenScale, 1f);

        _visibilitySequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(_panelTransform.gameObject)
            .Append(_canvasGroup.DOFade(1f, ShowDuration).SetEase(Ease.OutQuad))
            .Join(_panelTransform.DOAnchorPos(_shownAnchoredPosition, ShowDuration).SetEase(Ease.OutCubic))
            .Join(_panelTransform.DOScale(1f, ShowDuration).SetEase(Ease.OutBack));
    }

    private void Hide()
    {
        if (_isShown == false)
        {
            return;
        }

        _isShown = false;
        KillVisibilitySequence();

        _visibilitySequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(_panelTransform.gameObject)
            .Append(_canvasGroup.DOFade(0f, HideDuration).SetEase(Ease.InQuad))
            .Join(_panelTransform.DOAnchorPos(GetHidePosition(), HideDuration).SetEase(Ease.InCubic))
            .Join(_panelTransform.DOScale(HiddenScale, HideDuration).SetEase(Ease.InQuad))
            .OnComplete(ApplyHiddenState);
    }

    private void UpdateHealthImmediate()
    {
        if (_bossHealth == null)
        {
            return;
        }

        float normalizedValue = GetNormalizedValue();
        _slider.value = normalizedValue;
        _valueText.text = FormatHealthValue();
    }

    private void UpdateHealthAnimated()
    {
        if (_bossHealth == null)
        {
            return;
        }

        float normalizedValue = GetNormalizedValue();
        _valueText.text = FormatHealthValue();
        KillSliderTween();
        _sliderTween = _slider.DOValue(normalizedValue, SliderDuration).SetEase(Ease.OutQuad);
    }

    private float GetNormalizedValue()
    {
        if (_bossHealth.MaxValue <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(_bossHealth.Value / _bossHealth.MaxValue);
    }

    private string FormatHealthValue()
    {
        int currentValue = Mathf.CeilToInt(_bossHealth.Value);
        int maxValue = Mathf.CeilToInt(_bossHealth.MaxValue);

        return currentValue + " / " + maxValue;
    }

    private string FormatBossTitle(string sourceName)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
        {
            return "BOSS";
        }

        string cleanName = sourceName.Replace("(Clone)", string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(cleanName))
        {
            return "BOSS";
        }

        StringBuilder stringBuilder = new StringBuilder(cleanName.Length + 6);
        int charIndex = 0;

        while (charIndex < cleanName.Length)
        {
            char currentChar = cleanName[charIndex];

            if (charIndex > 0)
            {
                char previousChar = cleanName[charIndex - 1];

                if (char.IsUpper(currentChar) && char.IsLetter(previousChar) && char.IsUpper(previousChar) == false)
                {
                    stringBuilder.Append(' ');
                }
            }

            stringBuilder.Append(char.ToUpperInvariant(currentChar));
            charIndex += 1;
        }

        return stringBuilder.ToString();
    }

    private void ApplyHiddenState()
    {
        _canvasGroup.alpha = 0f;
        _panelTransform.anchoredPosition = GetEnterPosition();
        _panelTransform.localScale = new Vector3(HiddenScale, HiddenScale, 1f);
    }

    private Vector2 GetEnterPosition()
    {
        return _shownAnchoredPosition + new Vector2(0f, EnterOffsetY);
    }

    private Vector2 GetHidePosition()
    {
        return _shownAnchoredPosition + new Vector2(0f, HideOffsetY);
    }

    private void OnBossHealthChanged()
    {
        UpdateHealthAnimated();
    }

    private void OnBossHealthEnded()
    {
        UpdateHealthImmediate();
    }

    private void KillVisibilitySequence()
    {
        if (_visibilitySequence == null)
        {
            return;
        }

        if (_visibilitySequence.IsActive())
        {
            _visibilitySequence.Kill(false);
        }

        _visibilitySequence = null;
    }

    private void KillSliderTween()
    {
        if (_sliderTween == null)
        {
            return;
        }

        if (_sliderTween.IsActive())
        {
            _sliderTween.Kill(false);
        }

        _sliderTween = null;
    }

    private GameObject CreateUiObject(string objectName, Transform parentTransform, int layer)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        gameObject.layer = layer;
        gameObject.transform.SetParent(parentTransform, false);

        return gameObject;
    }

    private RectTransform CreateImageRect(string objectName, Transform parentTransform, int layer, Sprite sprite, Color color)
    {
        GameObject imageObject = CreateUiObject(objectName, parentTransform, layer);
        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;

        return imageObject.GetComponent<RectTransform>();
    }

    private TextMeshProUGUI CreateText(string objectName, Transform parentTransform, int layer, float fontSize, FontStyles fontStyle)
    {
        GameObject textObject = CreateUiObject(objectName, parentTransform, layer);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = _fontAsset;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = new Color(0.97f, 0.94f, 0.86f, 1f);
        text.raycastTarget = false;

        return text;
    }

    private void ConfigureRect(
        RectTransform rectTransform,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
    }
}
