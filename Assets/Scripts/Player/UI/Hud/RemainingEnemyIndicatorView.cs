using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RemainingEnemyIndicatorView : MonoBehaviour
{
    private const float RootSize = 34f;
    private const float FillSize = 24f;
    private const float DotSize = 10f;
    private const float ArrowWidth = 14f;
    private const float ArrowHeight = 16f;
    private const float HiddenScale = 0.9f;
    private const float VisibleScale = 1f;
    private const float ShowDuration = 0.14f;
    private const float BaseArrowOffsetY = 1f;
    private const float MoveSmoothTime = 0.07f;
    private const float RotateSmoothTime = 0.06f;

    private RectTransform _rectTransform;
    private Image _fillImage;
    private Image _ringImage;
    private Image _dotImage;
    private Image _arrowImage;
    private Color _fillBaseColor;
    private Color _ringBaseColor;
    private Color _dotBaseColor;
    private Color _arrowBaseColor;
    private Tween _showTween;
    private Vector2 _targetAnchoredPosition;
    private Vector2 _moveVelocity;
    private float _targetRotationZ;
    private float _rotationVelocity;
    private bool _isVisible;
    private bool _isPositionInitialized;

    private void OnDisable()
    {
        KillTween(_showTween);
        ResetVisualState();
        _isVisible = false;
        _isPositionInitialized = false;
        _moveVelocity = Vector2.zero;
        _rotationVelocity = 0f;
    }

    private void LateUpdate()
    {
        if (_isVisible == false)
        {
            return;
        }

        if (_isPositionInitialized == false)
        {
            return;
        }

        _rectTransform.anchoredPosition = Vector2.SmoothDamp(
            _rectTransform.anchoredPosition,
            _targetAnchoredPosition,
            ref _moveVelocity,
            MoveSmoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime);

        float currentRotationZ = _rectTransform.localEulerAngles.z;
        float nextRotationZ = Mathf.SmoothDampAngle(
            currentRotationZ,
            _targetRotationZ,
            ref _rotationVelocity,
            RotateSmoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime);

        _rectTransform.localRotation = Quaternion.Euler(0f, 0f, nextRotationZ);
    }

    public void Initialize(
        RectTransform parent,
        Sprite ringSprite,
        Sprite dotSprite,
        Sprite arrowSprite,
        Color fillColor,
        Color ringColor,
        Color dotColor,
        Color arrowColor)
    {
        if (parent == null)
        {
            throw new InvalidOperationException(nameof(parent));
        }

        if (ringSprite == null)
        {
            throw new InvalidOperationException(nameof(ringSprite));
        }

        if (dotSprite == null)
        {
            throw new InvalidOperationException(nameof(dotSprite));
        }

        if (arrowSprite == null)
        {
            throw new InvalidOperationException(nameof(arrowSprite));
        }

        if (_rectTransform != null)
        {
            return;
        }

        transform.SetParent(parent, false);
        _rectTransform = GetComponent<RectTransform>();

        if (_rectTransform == null)
        {
            throw new InvalidOperationException(nameof(_rectTransform));
        }

        _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _rectTransform.sizeDelta = new Vector2(RootSize, RootSize);
        _rectTransform.localScale = Vector3.one;
        _rectTransform.localRotation = Quaternion.identity;

        _fillImage = CreateImage("Fill", dotSprite, new Vector2(FillSize, FillSize), fillColor);
        _ringImage = gameObject.AddComponent<Image>();
        _ringImage.sprite = ringSprite;
        _ringImage.raycastTarget = false;
        _ringImage.color = ringColor;

        _dotImage = CreateImage("Dot", dotSprite, new Vector2(DotSize, DotSize), dotColor);
        _arrowImage = CreateImage("Arrow", arrowSprite, new Vector2(ArrowWidth, ArrowHeight), arrowColor);
        _arrowImage.rectTransform.anchoredPosition = new Vector2(0f, BaseArrowOffsetY);
        _fillBaseColor = _fillImage.color;
        _ringBaseColor = _ringImage.color;
        _dotBaseColor = _dotImage.color;
        _arrowBaseColor = _arrowImage.color;

        SetVisible(false);
    }

    public void ShowOnScreen(Vector2 anchoredPosition)
    {
        Show();
        SetTargetTransform(anchoredPosition, 0f);
        _dotImage.enabled = true;
        _arrowImage.enabled = false;
    }

    public void ShowOffScreen(Vector2 anchoredPosition, float angleDegrees)
    {
        Show();
        SetTargetTransform(anchoredPosition, 0f);
        _dotImage.enabled = true;
        _arrowImage.enabled = false;
    }

    public void SetVisible(bool isVisible)
    {
        if (gameObject.activeSelf == isVisible)
        {
            return;
        }

        gameObject.SetActive(isVisible);
    }

    private void Show()
    {
        if (_isVisible == false)
        {
            gameObject.SetActive(true);
            _isVisible = true;
            PlayShowAnimation();
        }
    }

    private void PlayShowAnimation()
    {
        KillTween(_showTween);
        _rectTransform.localScale = new Vector3(HiddenScale, HiddenScale, 1f);
        SetImageAlpha(_fillImage, 0f);
        SetImageAlpha(_ringImage, 0f);
        SetImageAlpha(_dotImage, 0f);
        SetImageAlpha(_arrowImage, 0f);

        _showTween = DOTween.Sequence()
            .SetUpdate(true)
            .Append(_rectTransform.DOScale(VisibleScale, ShowDuration).SetEase(Ease.OutCubic))
            .Join(_fillImage.DOFade(_fillBaseColor.a, ShowDuration).SetEase(Ease.OutQuad))
            .Join(_ringImage.DOFade(_ringBaseColor.a, ShowDuration).SetEase(Ease.OutQuad))
            .Join(_dotImage.DOFade(_dotBaseColor.a, ShowDuration).SetEase(Ease.OutQuad))
            .Join(_arrowImage.DOFade(_arrowBaseColor.a, ShowDuration).SetEase(Ease.OutQuad));
    }

    private void SetTargetTransform(Vector2 anchoredPosition, float rotationZ)
    {
        _targetAnchoredPosition = anchoredPosition;
        _targetRotationZ = rotationZ;

        if (_isPositionInitialized == false)
        {
            _rectTransform.anchoredPosition = anchoredPosition;
            _rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            _isPositionInitialized = true;
            _moveVelocity = Vector2.zero;
            _rotationVelocity = 0f;
        }
    }

    private Image CreateImage(string objectName, Sprite sprite, Vector2 sizeDelta, Color color)
    {
        GameObject childObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
        childObject.transform.SetParent(transform, false);
        RectTransform childRectTransform = childObject.GetComponent<RectTransform>();

        if (childRectTransform == null)
        {
            throw new InvalidOperationException(nameof(childRectTransform));
        }

        childRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        childRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        childRectTransform.pivot = new Vector2(0.5f, 0.5f);
        childRectTransform.sizeDelta = sizeDelta;
        childRectTransform.localScale = Vector3.one;
        childRectTransform.localRotation = Quaternion.identity;

        Image image = childObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        image.color = color;

        return image;
    }

    private void ResetVisualState()
    {
        if (_rectTransform == null)
        {
            return;
        }

        _rectTransform.localScale = new Vector3(VisibleScale, VisibleScale, 1f);
        _rectTransform.localRotation = Quaternion.identity;
        _rectTransform.anchoredPosition = _targetAnchoredPosition;

        if (_fillImage != null)
        {
            _fillImage.color = _fillBaseColor;
        }

        if (_ringImage != null)
        {
            _ringImage.color = _ringBaseColor;
        }

        if (_dotImage != null)
        {
            _dotImage.color = _dotBaseColor;
        }

        if (_arrowImage != null)
        {
            _arrowImage.color = _arrowBaseColor;
            _arrowImage.rectTransform.anchoredPosition = new Vector2(0f, BaseArrowOffsetY);
        }
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private void KillTween(Tween tween)
    {
        if (tween == null)
        {
            return;
        }

        if (tween.IsActive() == false)
        {
            return;
        }

        tween.Kill(false);
    }
}
