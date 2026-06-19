using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public sealed class CursorAnimator : MonoBehaviour
{
    private const string DefaultCursorResourcePath = "Textures/Cursor/crosshair_default";
    private const string DamageableCursorResourcePath = "Textures/Cursor/crosshair_damageable";
    private const float ColorCompareThreshold = 0.015f;
    private const float LuminanceRedWeight = 0.2126f;
    private const float LuminanceGreenWeight = 0.7152f;
    private const float LuminanceBlueWeight = 0.0722f;

    [Header("Visual")]
    [SerializeField] private RectTransform _visualRectTransform;
    [SerializeField] private Image _cursorImage;
    [SerializeField] private bool _useUnscaledTime = true;
    [SerializeField] private float _spriteTransitionDuration = 0.14f;
    [SerializeField] private float _colorTransitionDuration = 0.12f;
    [SerializeField] private Color _lightCursorColor = Color.white;
    [SerializeField] private Color _darkCursorColor = new Color(0.08f, 0.08f, 0.08f, 1f);

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
    private Tween _spriteTransitionTween;
    private Tween _colorTween;
    private Sprite _defaultCursorSprite;
    private Sprite _damageableCursorSprite;
    private Sprite _currentCursorSprite;
    private Color _currentCursorColor;

    private bool _isHeld;

    private void Awake()
    {
        if (_visualRectTransform == null)
        {
            throw new InvalidOperationException(nameof(_visualRectTransform));
        }

        if (_cursorImage == null)
        {
            _cursorImage = _visualRectTransform.GetComponent<Image>();
        }

        if (_cursorImage == null)
        {
            throw new InvalidOperationException(nameof(_cursorImage));
        }

        LoadCursorSprites();
        _baseScale = _visualRectTransform.localScale;
        _baseAnchoredPosition = _visualRectTransform.anchoredPosition;
        _baseLocalEulerAngles = _visualRectTransform.localEulerAngles;
        _currentCursorSprite = _defaultCursorSprite;
        _currentCursorColor = _lightCursorColor;
        _cursorImage.sprite = _currentCursorSprite;
        _cursorImage.color = _currentCursorColor;
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

    public void SetHoverVisual(bool hasDamageableTarget, Color surfaceColor)
    {
        Sprite targetSprite = hasDamageableTarget ? _damageableCursorSprite : _defaultCursorSprite;
        Color targetColor = GetContrastColor(surfaceColor);

        if (_currentCursorSprite != targetSprite)
        {
            PlaySpriteTransition(targetSprite, targetColor);

            return;
        }

        if (IsSameColor(_currentCursorColor, targetColor))
        {
            return;
        }

        _currentCursorColor = targetColor;
        KillTween(_colorTween);
        _colorTween = _cursorImage
            .DOColor(targetColor, _colorTransitionDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(_useUnscaledTime);
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
        KillTween(_spriteTransitionTween);
        KillTween(_colorTween);
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

    private void LoadCursorSprites()
    {
        _defaultCursorSprite = Resources.Load<Sprite>(DefaultCursorResourcePath);
        _damageableCursorSprite = Resources.Load<Sprite>(DamageableCursorResourcePath);

        if (_defaultCursorSprite == null)
        {
            throw new InvalidOperationException(DefaultCursorResourcePath);
        }

        if (_damageableCursorSprite == null)
        {
            throw new InvalidOperationException(DamageableCursorResourcePath);
        }
    }

    private Color GetContrastColor(Color surfaceColor)
    {
        float luminance = (surfaceColor.r * LuminanceRedWeight)
            + (surfaceColor.g * LuminanceGreenWeight)
            + (surfaceColor.b * LuminanceBlueWeight);
        float contrastFactor = Mathf.Clamp01(luminance);
        Color contrastColor = Color.Lerp(_lightCursorColor, _darkCursorColor, contrastFactor);
        contrastColor.a = _lightCursorColor.a;

        return contrastColor;
    }

    private void PlaySpriteTransition(Sprite targetSprite, Color targetColor)
    {
        _currentCursorSprite = targetSprite;
        _currentCursorColor = targetColor;

        KillTween(_spriteTransitionTween);
        KillTween(_colorTween);

        float halfDuration = _spriteTransitionDuration * 0.5f;
        Color transparentColor = _cursorImage.color;
        transparentColor.a = 0f;
        Color visibleTargetColor = targetColor;
        visibleTargetColor.a = _lightCursorColor.a;

        Sequence transitionSequence = DOTween.Sequence();
        transitionSequence.SetUpdate(_useUnscaledTime);
        transitionSequence.Append(_cursorImage.DOColor(transparentColor, halfDuration).SetEase(Ease.OutQuad));
        transitionSequence.AppendCallback(() =>
        {
            _cursorImage.sprite = targetSprite;
            _cursorImage.color = transparentColor;
        });
        transitionSequence.Append(_cursorImage.DOColor(visibleTargetColor, halfDuration).SetEase(Ease.OutQuad));

        _spriteTransitionTween = transitionSequence;
    }

    private bool IsSameColor(Color left, Color right)
    {
        float redDelta = Mathf.Abs(left.r - right.r);
        float greenDelta = Mathf.Abs(left.g - right.g);
        float blueDelta = Mathf.Abs(left.b - right.b);
        float alphaDelta = Mathf.Abs(left.a - right.a);

        return redDelta <= ColorCompareThreshold
            && greenDelta <= ColorCompareThreshold
            && blueDelta <= ColorCompareThreshold
            && alphaDelta <= ColorCompareThreshold;
    }
}
