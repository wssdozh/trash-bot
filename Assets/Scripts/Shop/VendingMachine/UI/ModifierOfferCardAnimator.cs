using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ModifierOfferCardAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("UI")]
    [SerializeField] private RectTransform _cardTransform;
    [SerializeField] private RectTransform _tiltTransform;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Button _buyButton;

    [Header("Reveal")]
    [SerializeField] private float _revealDurationSeconds = 0.24f;
    [SerializeField] private float _revealOffsetY = -18f;
    [SerializeField] private float _revealScale = 0.92f;

    [Header("Hide")]
    [SerializeField] private float _hideDurationSeconds = 0.16f;
    [SerializeField] private float _hideOffsetY = -10f;
    [SerializeField] private float _hideScale = 0.96f;

    [Header("Hover")]
    [SerializeField] private float _hoverScale = 1.03f;
    [SerializeField] private float _scaleSmoothSpeed = 10f;

    [Header("Tilt")]
    [SerializeField] private float _tiltMaxDegrees = 10f;
    [SerializeField] private float _tiltSmoothTimeSeconds = 0.08f;

    [Header("Click")]
    [SerializeField] private float _clickPunchScale = 0.06f;
    [SerializeField] private float _clickDurationSeconds = 0.14f;

    [Header("Time")]
    [SerializeField] private bool _ignoreTimeScale = true;

    private Vector2 _baseAnchoredPosition;

    private Sequence _revealSequence;
    private Sequence _hideSequence;
    private Tween _clickTween;

    private bool _isHover;

    private float _targetTiltX;
    private float _targetTiltY;

    private float _currentTiltX;
    private float _currentTiltY;

    private float _tiltVelocityX;
    private float _tiltVelocityY;

    private float _currentTiltScale = 1f;

    private void Awake()
    {
        if (_cardTransform == null)
        {
            _cardTransform = GetComponent<RectTransform>();
        }

        _baseAnchoredPosition = _cardTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        if (_buyButton != null)
        {
            _buyButton.onClick.AddListener(OnBuyClicked);
        }
    }

    private void OnDisable()
    {
        if (_buyButton != null)
        {
            _buyButton.onClick.RemoveListener(OnBuyClicked);
        }

        KillTweens();
        ResetTiltInstant();
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        if (_ignoreTimeScale == true)
        {
            deltaTime = Time.unscaledDeltaTime;
        }

        if (_isHover == false)
        {
            _targetTiltX = 0f;
            _targetTiltY = 0f;
        }

        _currentTiltX = Mathf.SmoothDamp(_currentTiltX, _targetTiltX, ref _tiltVelocityX, _tiltSmoothTimeSeconds, Mathf.Infinity, deltaTime);
        _currentTiltY = Mathf.SmoothDamp(_currentTiltY, _targetTiltY, ref _tiltVelocityY, _tiltSmoothTimeSeconds, Mathf.Infinity, deltaTime);

        if (_tiltTransform != null)
        {
            _tiltTransform.localRotation = Quaternion.Euler(_currentTiltX, _currentTiltY, 0f);
        }

        float targetScale = 1f;

        if (_isHover == true)
        {
            targetScale = _hoverScale;
        }

        _currentTiltScale = Mathf.Lerp(_currentTiltScale, targetScale, 1f - Mathf.Exp(-_scaleSmoothSpeed * deltaTime));

        if (_tiltTransform != null)
        {
            _tiltTransform.localScale = new Vector3(_currentTiltScale, _currentTiltScale, 1f);
        }
    }

    public void SetHiddenInstant()
    {
        _isHover = false;

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
        }

        if (_cardTransform != null)
        {
            _cardTransform.localScale = new Vector3(_revealScale, _revealScale, 1f);
            _cardTransform.anchoredPosition = _baseAnchoredPosition + new Vector2(0f, _revealOffsetY);
        }

        ResetTiltInstant();
    }

    public void PlayReveal(float delaySeconds)
    {
        KillRevealHide();

        _isHover = false;

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
        }

        if (_cardTransform != null)
        {
            _cardTransform.localScale = new Vector3(_revealScale, _revealScale, 1f);
            _cardTransform.anchoredPosition = _baseAnchoredPosition + new Vector2(0f, _revealOffsetY);
        }

        ResetTiltInstant();

        _revealSequence = DOTween.Sequence();
        _revealSequence.SetUpdate(_ignoreTimeScale);
        _revealSequence.SetDelay(delaySeconds);

        if (_canvasGroup != null)
        {
            _revealSequence.Join(_canvasGroup.DOFade(1f, _revealDurationSeconds).SetEase(Ease.OutCubic));
        }

        if (_cardTransform != null)
        {
            _revealSequence.Join(_cardTransform.DOScale(1f, _revealDurationSeconds).SetEase(Ease.OutCubic));
            _revealSequence.Join(_cardTransform.DOAnchorPos(_baseAnchoredPosition, _revealDurationSeconds).SetEase(Ease.OutCubic));
        }
    }

    public void PlayHide(float delaySeconds)
    {
        KillRevealHide();

        _isHover = false;

        Vector2 targetPosition = _baseAnchoredPosition + new Vector2(0f, _hideOffsetY);

        _hideSequence = DOTween.Sequence();
        _hideSequence.SetUpdate(_ignoreTimeScale);
        _hideSequence.SetDelay(delaySeconds);

        if (_canvasGroup != null)
        {
            _hideSequence.Join(_canvasGroup.DOFade(0f, _hideDurationSeconds).SetEase(Ease.InCubic));
        }

        if (_cardTransform != null)
        {
            _hideSequence.Join(_cardTransform.DOScale(_hideScale, _hideDurationSeconds).SetEase(Ease.InCubic));
            _hideSequence.Join(_cardTransform.DOAnchorPos(targetPosition, _hideDurationSeconds).SetEase(Ease.InCubic));
        }

        ResetTiltInstant();
    }

    public void PlayPurchased()
    {
        KillClick();

        if (_tiltTransform == null)
        {
            return;
        }

        _clickTween = _tiltTransform.DOPunchScale(new Vector3(_clickPunchScale, _clickPunchScale, 0f), _clickDurationSeconds, 8, 0.7f);
        _clickTween.SetUpdate(_ignoreTimeScale);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHover = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHover = false;
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (_isHover == false)
        {
            return;
        }

        if (_cardTransform == null)
        {
            return;
        }

        Vector2 localPoint;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_cardTransform, eventData.position, eventData.enterEventCamera, out localPoint) == false)
        {
            return;
        }

        Rect rect = _cardTransform.rect;

        if (rect.width <= 0.0001f)
        {
            return;
        }

        if (rect.height <= 0.0001f)
        {
            return;
        }

        float normalizedX = Mathf.Clamp(localPoint.x / (rect.width * 0.5f), -1f, 1f);
        float normalizedY = Mathf.Clamp(localPoint.y / (rect.height * 0.5f), -1f, 1f);

        _targetTiltX = -normalizedY * _tiltMaxDegrees;
        _targetTiltY = normalizedX * _tiltMaxDegrees;
    }

    private void OnBuyClicked()
    {
        PlayPurchased();
    }

    private void ResetTiltInstant()
    {
        _targetTiltX = 0f;
        _targetTiltY = 0f;

        _currentTiltX = 0f;
        _currentTiltY = 0f;

        _tiltVelocityX = 0f;
        _tiltVelocityY = 0f;

        _currentTiltScale = 1f;

        if (_tiltTransform != null)
        {
            _tiltTransform.localRotation = Quaternion.identity;
            _tiltTransform.localScale = new Vector3(1f, 1f, 1f);
        }
    }

    private void KillRevealHide()
    {
        if (_revealSequence != null)
        {
            _revealSequence.Kill();
            _revealSequence = null;
        }

        if (_hideSequence != null)
        {
            _hideSequence.Kill();
            _hideSequence = null;
        }
    }

    private void KillClick()
    {
        if (_clickTween != null)
        {
            _clickTween.Kill();
            _clickTween = null;
        }
    }

    private void KillTweens()
    {
        KillRevealHide();
        KillClick();
    }
}
