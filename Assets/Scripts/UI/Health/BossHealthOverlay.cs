using System;
using DG.Tweening;
using JunkyardBoss;
using UnityEngine;
using UnityEngine.UI;

public sealed class BossHealthOverlay : MonoBehaviour
{
    private const float SearchInterval = 0.35f;
    private const float ShowDuration = 0.32f;
    private const float HideDuration = 0.24f;
    private const float HiddenScaleMultiplier = 0.94f;
    private const float VisibleScale = 1.85f;
    private const float EnterOffsetY = 88f;
    private const float HideOffsetY = 30f;

    private RectTransform _uiRoot;
    private RectTransform _indicatorTemplate;
    private RectTransform _panelTransform;
    private CanvasGroup _canvasGroup;
    private HealthSmoothSliderIndicator[] _sliderIndicators;
    private HealthTextIndicator[] _textIndicators;
    private BossSegmentedHealthIndicator[] _segmentedIndicators;
    private Sequence _visibilitySequence;
    private BossExcavator _boss;
    private Health _bossHealth;
    private RoomCombatLock _roomCombatLock;
    private Vector2 _shownAnchoredPosition;
    private float _searchTimer;
    private bool _isShown;

    public void Initialize(RectTransform uiRoot, RectTransform indicatorTemplate)
    {
        if (uiRoot == null)
        {
            throw new InvalidOperationException(nameof(uiRoot));
        }

        if (indicatorTemplate == null)
        {
            throw new InvalidOperationException(nameof(indicatorTemplate));
        }

        _uiRoot = uiRoot;
        _indicatorTemplate = indicatorTemplate;
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

        _panelTransform = Instantiate(_indicatorTemplate, _uiRoot);

        if (_panelTransform.gameObject.activeSelf == false)
        {
            _panelTransform.gameObject.SetActive(true);
        }

        _panelTransform.name = "Boss Health";
        _panelTransform.SetAsLastSibling();
        _canvasGroup = _panelTransform.gameObject.AddComponent<CanvasGroup>();
        _shownAnchoredPosition = new Vector2(0f, -18f);

        ConfigureRootRect();
        DisableRaycasts();
        CacheIndicators();
        ClearIndicators();
    }

    private void ConfigureRootRect()
    {
        Vector2 sizeDelta = _panelTransform.sizeDelta;
        _panelTransform.anchorMin = new Vector2(0.5f, 1f);
        _panelTransform.anchorMax = new Vector2(0.5f, 1f);
        _panelTransform.pivot = new Vector2(0.5f, 1f);
        _panelTransform.anchoredPosition = _shownAnchoredPosition;
        _panelTransform.sizeDelta = sizeDelta;
        _panelTransform.localRotation = Quaternion.identity;
    }

    private void DisableRaycasts()
    {
        Graphic[] graphics = _panelTransform.GetComponentsInChildren<Graphic>(true);
        int graphicIndex = 0;

        while (graphicIndex < graphics.Length)
        {
            Graphic graphic = graphics[graphicIndex];
            graphicIndex += 1;

            if (graphic == null)
            {
                continue;
            }

            graphic.raycastTarget = false;
        }

        Slider[] sliders = _panelTransform.GetComponentsInChildren<Slider>(true);
        int sliderIndex = 0;

        while (sliderIndex < sliders.Length)
        {
            Slider slider = sliders[sliderIndex];
            sliderIndex += 1;

            if (slider == null)
            {
                continue;
            }

            slider.interactable = false;
        }
    }

    private void CacheIndicators()
    {
        _sliderIndicators = _panelTransform.GetComponentsInChildren<HealthSmoothSliderIndicator>(true);
        _textIndicators = _panelTransform.GetComponentsInChildren<HealthTextIndicator>(true);
        _segmentedIndicators = _panelTransform.GetComponentsInChildren<BossSegmentedHealthIndicator>(true);
    }

    private void ClearIndicators()
    {
        int sliderIndex = 0;

        while (sliderIndex < _sliderIndicators.Length)
        {
            HealthSmoothSliderIndicator sliderIndicator = _sliderIndicators[sliderIndex];
            sliderIndex += 1;

            if (sliderIndicator == null)
            {
                continue;
            }

            sliderIndicator.ClearStat();
        }

        int textIndex = 0;

        while (textIndex < _textIndicators.Length)
        {
            HealthTextIndicator textIndicator = _textIndicators[textIndex];
            textIndex += 1;

            if (textIndicator == null)
            {
                continue;
            }

            textIndicator.ClearStat();
        }

        int segmentedIndex = 0;

        while (segmentedIndex < _segmentedIndicators.Length)
        {
            BossSegmentedHealthIndicator segmentedIndicator = _segmentedIndicators[segmentedIndex];
            segmentedIndex += 1;

            if (segmentedIndicator == null)
            {
                continue;
            }

            segmentedIndicator.ClearBoss();
        }
    }

    private void BindIndicators(Health health)
    {
        int sliderIndex = 0;

        while (sliderIndex < _sliderIndicators.Length)
        {
            HealthSmoothSliderIndicator sliderIndicator = _sliderIndicators[sliderIndex];
            sliderIndex += 1;

            if (sliderIndicator == null)
            {
                continue;
            }

            sliderIndicator.SetStat(health);
        }

        int textIndex = 0;

        while (textIndex < _textIndicators.Length)
        {
            HealthTextIndicator textIndicator = _textIndicators[textIndex];
            textIndex += 1;

            if (textIndicator == null)
            {
                continue;
            }

            textIndicator.SetStat(health);
        }
    }

    private void BindSegmentedIndicators(BossExcavator boss)
    {
        int segmentedIndex = 0;

        while (segmentedIndex < _segmentedIndicators.Length)
        {
            BossSegmentedHealthIndicator segmentedIndicator = _segmentedIndicators[segmentedIndex];
            segmentedIndex += 1;

            if (segmentedIndicator == null)
            {
                continue;
            }

            segmentedIndicator.SetBoss(boss);
        }
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
        System.Collections.Generic.IReadOnlyList<BossExcavator> bosses = BossExcavator.Instances;
        BossExcavator fallbackBoss = null;
        RoomCombatLock fallbackRoomCombatLock = null;
        int bossIndex = 0;

        while (bossIndex < bosses.Count)
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
        _bossHealth.Ended += OnBossHealthEnded;
        BindIndicators(_bossHealth);
        BindSegmentedIndicators(_boss);
    }

    private void UnbindBoss()
    {
        if (_bossHealth != null)
        {
            _bossHealth.Ended -= OnBossHealthEnded;
        }

        ClearIndicators();
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
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
        _panelTransform.anchoredPosition = GetEnterPosition();
        _panelTransform.localScale = GetHiddenScale();

        _visibilitySequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(_panelTransform.gameObject)
            .Append(_canvasGroup.DOFade(1f, ShowDuration).SetEase(Ease.OutQuad))
            .Join(_panelTransform.DOAnchorPos(_shownAnchoredPosition, ShowDuration).SetEase(Ease.OutCubic))
            .Join(_panelTransform.DOScale(GetShownScale(), ShowDuration).SetEase(Ease.OutBack));
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
            .Join(_panelTransform.DOScale(GetHiddenScale(), HideDuration).SetEase(Ease.InQuad))
            .OnComplete(ApplyHiddenState);
    }

    private void ApplyHiddenState()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
        _panelTransform.anchoredPosition = GetEnterPosition();
        _panelTransform.localScale = GetHiddenScale();
    }

    private Vector2 GetEnterPosition()
    {
        return _shownAnchoredPosition + new Vector2(0f, EnterOffsetY);
    }

    private Vector2 GetHidePosition()
    {
        return _shownAnchoredPosition + new Vector2(0f, HideOffsetY);
    }

    private Vector3 GetShownScale()
    {
        return new Vector3(VisibleScale, VisibleScale, 1f);
    }

    private Vector3 GetHiddenScale()
    {
        float hiddenScale = VisibleScale * HiddenScaleMultiplier;

        return new Vector3(hiddenScale, hiddenScale, 1f);
    }

    private void OnBossHealthEnded()
    {
        Hide();
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
}
