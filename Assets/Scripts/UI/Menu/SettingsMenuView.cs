using UnityEngine;
using DG.Tweening;

public sealed class SettingsMenuView : BaseMenuView
{
    private enum SlideDirection
    {
        FromLeft,
        FromTop
    }

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _panel;

    [SerializeField] private SlideDirection _direction = SlideDirection.FromLeft;
    [SerializeField] private float _distance = 900.0f;

    [SerializeField] private float _fadeInDuration = 0.10f;
    [SerializeField] private float _fadeOutDuration = 0.08f;

    [SerializeField] private float _showDuration = 0.22f;
    [SerializeField] private float _hideDuration = 0.14f;

    [SerializeField] private float _scaleHidden = 0.92f;
    [SerializeField] private float _scaleShown = 1.0f;

    private Sequence _sequence;

    private Vector2 _shownPosition;

    public override bool IsAnimating { get; protected set; }
    public override bool IsOpen { get; protected set; }

    private void Awake()
    {
        _shownPosition = _panel.anchoredPosition;
        ApplyHidden();
        IsOpen = false;
    }

    public override void Show()
    {
        base.Show();

        KillSequence();

        IsAnimating = true;

        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;

        Vector2 hiddenPosition = GetHiddenPosition();

        _canvasGroup.alpha = 0.0f;
        _panel.anchoredPosition = hiddenPosition;
        _panel.localScale = new Vector3(_scaleHidden, _scaleHidden, 1.0f);

        _sequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(_canvasGroup.DOFade(1.0f, _fadeInDuration).SetEase(Ease.OutCubic))
            .Join(_panel.DOAnchorPos(_shownPosition, _showDuration).SetEase(Ease.OutCubic))
            .Join(_panel.DOScale(_scaleShown, _showDuration).SetEase(Ease.OutQuart))
            .OnComplete((TweenCallback)(() =>
            {
                _canvasGroup.alpha = 1.0f;
                _panel.anchoredPosition = _shownPosition;
                _panel.localScale = new Vector3(_scaleShown, _scaleShown, 1.0f);

                this.IsAnimating = false;
            }));
    }

    public override void Hide()
    {
        base.Hide();

        KillSequence();

        IsAnimating = true;

        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        Vector2 hiddenPosition = GetHiddenPosition();

        _sequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(_canvasGroup.DOFade(0.0f, _fadeOutDuration).SetEase(Ease.OutCubic))
            .Join(_panel.DOAnchorPos(hiddenPosition, _hideDuration).SetEase(Ease.OutCubic))
            .Join(_panel.DOScale(_scaleHidden, _hideDuration).SetEase(Ease.OutQuart))
            .OnComplete((TweenCallback)(() =>
            {
                ApplyHidden();

                this.IsAnimating = false;
            }));
    }

    private Vector2 GetHiddenPosition()
    {
        if (_direction == SlideDirection.FromLeft)
        {
            return _shownPosition + new Vector2(-_distance, 0.0f);
        }

        return _shownPosition + new Vector2(0.0f, _distance);
    }

    private void ApplyHidden()
    {
        _canvasGroup.alpha = 0.0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        _panel.anchoredPosition = GetHiddenPosition();
        _panel.localScale = new Vector3(_scaleHidden, _scaleHidden, 1.0f);
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
