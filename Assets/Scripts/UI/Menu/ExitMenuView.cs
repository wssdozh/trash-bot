using UnityEngine;
using DG.Tweening;

class ExitMenuView : BaseMenuView
{
    [Header("Зависимости")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _panelTransform;

    [Header("Настройки анимации")]
    [Header("Настройки выхода")]
    [SerializeField] private float _slideDistance = 600.0f;
    [SerializeField] private float _fadeInDurationSeconds = 0.10f;
    [SerializeField] private float _slideInDurationSeconds = 0.18f;
    [SerializeField] private float _settleDurationSeconds = 0.06f;
    [SerializeField] private float _shownScale = 1.0f;

    [Header("Настройки скрытия")]
    [SerializeField] private float _fadeOutDurationSeconds = 0.08f;
    [SerializeField] private float _slideOutDurationSeconds = 0.12f;
    [SerializeField] private float _hiddenScale = 0.8f;


    private Sequence _sequence;
    private Vector2 _shownAnchoredPosition;

    public override bool IsOpen { get; protected set; }
    public override bool IsAnimating { get; protected set; }

    private void Awake()
    {
        _shownAnchoredPosition = _panelTransform.anchoredPosition;
        ApplyHiddenState();

        IsOpen = false;
    }

    public override void Show()
    {
        base.Show();

        KillSequence();

        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;

        Vector2 hiddenPosition = GetHiddenAnchoredPosition();

        _canvasGroup.alpha = 0.0f;
        _panelTransform.anchoredPosition = hiddenPosition;
        _panelTransform.localScale = new Vector3(_hiddenScale, _hiddenScale, 1.0f);

        _sequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(_canvasGroup.DOFade(1.0f, _fadeInDurationSeconds).SetEase(Ease.OutQuad))
            .Join(_panelTransform.DOAnchorPos(_shownAnchoredPosition, _slideInDurationSeconds).SetEase(Ease.OutCubic))
            .Join(_panelTransform.DOScale(_shownScale, _slideInDurationSeconds).SetEase(Ease.OutBack))
            .Append(_panelTransform.DOScale(_shownScale, _settleDurationSeconds).SetEase(Ease.OutQuad))
            .OnComplete(() =>
            {
                _panelTransform.anchoredPosition = _shownAnchoredPosition;
                _panelTransform.localScale = new Vector3(_shownScale, _shownScale, 1.0f);
                IsAnimating = false;
            }); 
    }

    public override void Hide()
    {
        base.Hide();

        KillSequence();

        IsAnimating = true;

        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        Vector2 hiddenPosition = GetHiddenAnchoredPosition();

        _sequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(_canvasGroup.DOFade(0.0f, _fadeOutDurationSeconds).SetEase(Ease.InQuad))
            .Join(_panelTransform.DOAnchorPos(hiddenPosition, _slideOutDurationSeconds).SetEase(Ease.InCubic))
            .Join(_panelTransform.DOScale(_hiddenScale, _slideOutDurationSeconds).SetEase(Ease.InCubic))
            .OnComplete(() =>
            {
                ApplyHiddenState();
                IsAnimating = false;
            });
    }

    private Vector2 GetHiddenAnchoredPosition()
    {
        return _shownAnchoredPosition + new Vector2(0.0f, _slideDistance);
    }

    private void ApplyHiddenState()
    {
        Vector2 hiddenPosition = GetHiddenAnchoredPosition();

        _canvasGroup.alpha = 0.0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        _panelTransform.anchoredPosition = hiddenPosition;
        _panelTransform.localScale = new Vector3(_hiddenScale, _hiddenScale, 1.0f);
    }

    private void KillSequence()
    {
        if (_sequence != null && _sequence.IsActive() == true)
        {
            _sequence.Kill(false);
        }

        _sequence = null;
    }
}