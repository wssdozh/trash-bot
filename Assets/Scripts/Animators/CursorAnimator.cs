using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CursorAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform _visualRectTransform;
    [SerializeField] private bool _useUnscaledTime = true;

    [Header("Click")]
    [SerializeField] private float _clickDownScale = 0.85f;
    [SerializeField] private float _clickDownDuration = 0.05f;
    [SerializeField] private float _clickOvershootScale = 1.05f;
    [SerializeField] private float _clickOvershootDuration = 0.05f;
    [SerializeField] private float _clickReturnDuration = 0.08f;
    [SerializeField] private Ease _clickEaseDown = Ease.OutQuad;
    [SerializeField] private Ease _clickEaseOvershoot = Ease.OutQuad;
    [SerializeField] private Ease _clickEaseReturn = Ease.OutBack;

    [Header("Hold")]
    [SerializeField] private float _holdScale = 0.75f;
    [SerializeField] private float _holdReleaseDuration = 0.12f;
    [SerializeField] private float _holdJitterPositionStrength = 2.5f;
    [SerializeField] private float _holdJitterRotationStrength = 6f;
    [SerializeField] private float _holdJitterCycleDuration = 0.25f;
    [SerializeField] private int _holdJitterVibrato = 18;
    [SerializeField] private float _holdJitterRandomness = 90f;

    [Header("Optional (RMB / Scroll)")]
    [SerializeField] private float _secondaryClickRotationPunch = 14f;
    [SerializeField] private float _secondaryClickDuration = 0.12f;
    [SerializeField] private float _scrollScalePunch = 0.14f;
    [SerializeField] private float _scrollDuration = 0.10f;

    private Vector3 _baseScale;
    private Vector2 _baseAnchoredPosition;
    private Vector3 _baseLocalEulerAngles;

    private Sequence _clickSequence;
    private Tween _holdReturnScaleTween;
    private Tween _holdPositionJitterTween;
    private Tween _holdRotationJitterTween;

    private Tween _holdCandidateScaleTween;
    private Tween _secondaryClickTween;
    private Tween _scrollTween;

    private bool _isHeld;

    private void Awake()
    {
        if (_visualRectTransform == null)
        {
            _visualRectTransform = GetComponent<RectTransform>();
        }

        _baseScale = _visualRectTransform.localScale;
        _baseAnchoredPosition = _visualRectTransform.anchoredPosition;
        _baseLocalEulerAngles = _visualRectTransform.localEulerAngles;
    }

    private void OnDisable()
    {
        KillAllTweens();
        ApplyBaseTransform();
        _isHeld = false;
    }

    public void PlayClick()
    {
        if (_isHeld)
        {
            return;
        }

        KillTween(_holdCandidateScaleTween);
        KillTween(_holdReturnScaleTween);

        KillTween(_secondaryClickTween);
        KillTween(_scrollTween);
        KillTween(_clickSequence);

        _clickSequence = DOTween.Sequence();
        _clickSequence.SetUpdate(_useUnscaledTime);

        Vector3 downScale = MultiplyScale(_baseScale, _clickDownScale);
        Vector3 overshootScale = MultiplyScale(_baseScale, _clickOvershootScale);

        _clickSequence.Append(_visualRectTransform.DOScale(downScale, _clickDownDuration).SetEase(_clickEaseDown));
        _clickSequence.Append(_visualRectTransform.DOScale(overshootScale, _clickOvershootDuration).SetEase(_clickEaseOvershoot));
        _clickSequence.Append(_visualRectTransform.DOScale(_baseScale, _clickReturnDuration).SetEase(_clickEaseReturn));
    }

    public void BeginHoldCandidate(float thresholdSeconds)
    {
        if (_isHeld)
        {
            return;
        }

        KillTween(_clickSequence);
        KillTween(_secondaryClickTween);
        KillTween(_scrollTween);

        KillHoldJitterTweens();
        KillTween(_holdReturnScaleTween);
        KillTween(_holdCandidateScaleTween);

        Vector3 holdScale = MultiplyScale(_baseScale, _holdScale);

        _holdCandidateScaleTween = _visualRectTransform
            .DOScale(holdScale, thresholdSeconds)
            .SetEase(Ease.OutQuad)
            .SetUpdate(_useUnscaledTime);
    }

    public void CancelHoldCandidate()
    {
        if (_isHeld)
        {
            return;
        }

        KillTween(_holdCandidateScaleTween);
    }

    public void ConfirmHold()
    {
        if (_isHeld)
        {
            return;
        }

        _isHeld = true;

        KillTween(_clickSequence);
        KillTween(_secondaryClickTween);
        KillTween(_scrollTween);

        KillHoldJitterTweens();

        _holdPositionJitterTween = _visualRectTransform
            .DOShakeAnchorPos(
                _holdJitterCycleDuration,
                _holdJitterPositionStrength,
                _holdJitterVibrato,
                _holdJitterRandomness,
                false,
                false)
            .SetUpdate(_useUnscaledTime)
            .SetLoops(-1, LoopType.Restart);

        _holdRotationJitterTween = _visualRectTransform
            .DOShakeRotation(
                _holdJitterCycleDuration,
                new Vector3(0f, 0f, _holdJitterRotationStrength),
                _holdJitterVibrato,
                _holdJitterRandomness,
                false)
            .SetUpdate(_useUnscaledTime)
            .SetLoops(-1, LoopType.Restart);
    }

    public void EndHold()
    {
        if (_isHeld == false)
        {
            return;
        }

        _isHeld = false;

        KillTween(_holdCandidateScaleTween);
        KillHoldJitterTweens();
        KillTween(_holdReturnScaleTween);

        _visualRectTransform.anchoredPosition = _baseAnchoredPosition;
        _visualRectTransform.localEulerAngles = _baseLocalEulerAngles;

        _holdReturnScaleTween = _visualRectTransform
            .DOScale(_baseScale, _holdReleaseDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(_useUnscaledTime);
    }

    public void ResetToBase()
    {
        _isHeld = false;
        KillAllTweens();
        ApplyBaseTransform();
    }

    public void PlaySecondaryClick()
    {
        if (_isHeld)
        {
            return;
        }

        KillTween(_clickSequence);
        KillTween(_secondaryClickTween);

        _visualRectTransform.localEulerAngles = _baseLocalEulerAngles;

        _secondaryClickTween = _visualRectTransform
            .DOPunchRotation(new Vector3(0f, 0f, _secondaryClickRotationPunch), _secondaryClickDuration, 12, 0.7f)
            .SetUpdate(_useUnscaledTime);
    }

    public void PlayScroll(float direction)
    {
        if (_isHeld)
        {
            return;
        }

        KillTween(_clickSequence);
        KillTween(_secondaryClickTween);
        KillTween(_scrollTween);

        _visualRectTransform.localScale = _baseScale;

        float directionSign = Mathf.Sign(direction);
        Vector3 punch = new Vector3(0f, directionSign * _scrollScalePunch, 0f);

        _scrollTween = _visualRectTransform
            .DOPunchScale(punch, _scrollDuration, 10, 0.7f)
            .SetUpdate(_useUnscaledTime)
            .OnKill(ResetScaleToBase)
            .OnComplete(ResetScaleToBase);
    }

    private void ResetScaleToBase()
    {
        _visualRectTransform.localScale = _baseScale;
    }

    private void ApplyBaseTransform()
    {
        _visualRectTransform.localScale = _baseScale;
        _visualRectTransform.anchoredPosition = _baseAnchoredPosition;
        _visualRectTransform.localEulerAngles = _baseLocalEulerAngles;
    }

    private void KillAllTweens()
    {
        KillTween(_clickSequence);
        KillTween(_holdCandidateScaleTween);
        KillTween(_holdReturnScaleTween);
        KillHoldJitterTweens();
        KillTween(_secondaryClickTween);
        KillTween(_scrollTween);
    }

    private void KillHoldJitterTweens()
    {
        KillTween(_holdPositionJitterTween);
        KillTween(_holdRotationJitterTween);
    }

    private void KillTween(Tween tween)
    {
        if (tween != null && tween.IsActive())
        {
            tween.Kill(false);
        }
    }

    private Vector3 MultiplyScale(Vector3 scale, float factor)
    {
        return new Vector3(scale.x * factor, scale.y * factor, scale.z);
    }
}
