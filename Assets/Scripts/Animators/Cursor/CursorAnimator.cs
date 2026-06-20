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
    private const string TransitionImageName = "CursorTransitionImage";

    [Header("Visual")]
    [SerializeField] private RectTransform _visualRectTransform;
    [SerializeField] private Image _cursorImage;
    [SerializeField] private bool _useUnscaledTime = true;
    [SerializeField] private float _spriteTransitionDuration = 0.24f;
    [SerializeField] private Color _cursorColor = Color.white;

    [Header("Layering")]
    [SerializeField] private int _sortingOrder = 1000;

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
    private Image _transitionImage;
    private Sprite _defaultCursorSprite;
    private Sprite _damageableCursorSprite;
    private Sprite _currentCursorSprite;
    private Canvas _cursorCanvas;

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

        ConfigureLayering();
        LoadCursorSprites();
        CreateTransitionImage();
        _baseScale = _visualRectTransform.localScale;
        _baseAnchoredPosition = _visualRectTransform.anchoredPosition;
        _baseLocalEulerAngles = _visualRectTransform.localEulerAngles;
        _currentCursorSprite = _defaultCursorSprite;
        _cursorImage.sprite = _currentCursorSprite;
        _cursorImage.color = _cursorColor;
        _cursorImage.raycastTarget = false;
        HideTransitionImage();
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

    public void SetHoverVisual(bool hasDamageableTarget)
    {
        Sprite targetSprite = hasDamageableTarget ? _damageableCursorSprite : _defaultCursorSprite;

        if (_currentCursorSprite != targetSprite)
        {
            PlaySpriteTransition(targetSprite);
        }
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
        _cursorImage.sprite = _currentCursorSprite;
        _cursorImage.color = _cursorColor;
        HideTransitionImage();
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

    private void ConfigureLayering()
    {
        _cursorCanvas = _visualRectTransform.GetComponent<Canvas>();

        if (_cursorCanvas == null)
        {
            _cursorCanvas = _visualRectTransform.gameObject.AddComponent<Canvas>();
        }

        _cursorCanvas.overrideSorting = true;
        _cursorCanvas.sortingOrder = _sortingOrder;
    }

    private void CreateTransitionImage()
    {
        GameObject transitionObject = new GameObject(
            TransitionImageName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        transitionObject.transform.SetParent(_visualRectTransform, false);

        RectTransform transitionTransform = transitionObject.GetComponent<RectTransform>();
        transitionTransform.anchorMin = Vector2.zero;
        transitionTransform.anchorMax = Vector2.one;
        transitionTransform.offsetMin = Vector2.zero;
        transitionTransform.offsetMax = Vector2.zero;
        transitionTransform.pivot = new Vector2(0.5f, 0.5f);
        transitionTransform.localScale = Vector3.one;

        _transitionImage = transitionObject.GetComponent<Image>();
        _transitionImage.raycastTarget = false;
        _transitionImage.maskable = _cursorImage.maskable;
        _transitionImage.preserveAspect = _cursorImage.preserveAspect;
        _transitionImage.type = _cursorImage.type;
        _transitionImage.fillMethod = _cursorImage.fillMethod;
        _transitionImage.fillOrigin = _cursorImage.fillOrigin;
        _transitionImage.fillClockwise = _cursorImage.fillClockwise;
        _transitionImage.fillAmount = _cursorImage.fillAmount;
    }

    private void PlaySpriteTransition(Sprite targetSprite)
    {
        KillTween(_spriteTransitionTween);

        _currentCursorSprite = targetSprite;
        SyncTransitionImage(targetSprite);

        Color visibleColor = _cursorColor;
        Color transparentColor = _cursorColor;
        transparentColor.a = 0f;
        _cursorImage.color = visibleColor;
        _transitionImage.color = transparentColor;

        Sequence transitionSequence = DOTween.Sequence();
        transitionSequence.SetUpdate(_useUnscaledTime);
        transitionSequence.Join(_cursorImage.DOFade(0f, _spriteTransitionDuration).SetEase(Ease.InOutSine));
        transitionSequence.Join(_transitionImage
            .DOFade(_cursorColor.a, _spriteTransitionDuration)
            .SetEase(Ease.InOutSine));
        transitionSequence.OnComplete(() => ApplyTransitionSprite(targetSprite));

        _spriteTransitionTween = transitionSequence;
    }

    private void SyncTransitionImage(Sprite targetSprite)
    {
        _transitionImage.sprite = targetSprite;
        _transitionImage.type = _cursorImage.type;
        _transitionImage.fillMethod = _cursorImage.fillMethod;
        _transitionImage.fillOrigin = _cursorImage.fillOrigin;
        _transitionImage.fillClockwise = _cursorImage.fillClockwise;
        _transitionImage.fillAmount = _cursorImage.fillAmount;
    }

    private void ApplyTransitionSprite(Sprite targetSprite)
    {
        _cursorImage.sprite = targetSprite;
        _cursorImage.color = _cursorColor;
        HideTransitionImage();
    }

    private void HideTransitionImage()
    {
        if (_transitionImage == null)
        {
            return;
        }

        Color transparentColor = _cursorColor;
        transparentColor.a = 0f;
        _transitionImage.color = transparentColor;
    }
}
