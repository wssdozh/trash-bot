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
    private const float HiddenScale = 0.9f;
    private const float VisibleScale = 1f;
    private const float ShowDuration = 0.14f;
    private const float MoveSmoothTime = 0.07f;

    private RectTransform _rectTransform;
    private Image _fillImage;
    private Image _ringImage;
    private Image _dotImage;
    private Color _fillBaseColor;
    private Color _ringBaseColor;
    private Color _dotBaseColor;
    private Tween _showTween;
    private Vector2 _targetAnchoredPosition;
    private Vector2 _moveVelocity;
    private bool _isVisible;
    private bool _isPositionInitialized;

    private void OnDisable()
    {
        KillTween(_showTween);
        ResetVisualState();
        _isVisible = false;
        _isPositionInitialized = false;
        _moveVelocity = Vector2.zero;
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
    }

    public void Initialize(
        RectTransform parent,
        Sprite ringSprite,
        Sprite dotSprite,
        Color fillColor,
        Color ringColor,
        Color dotColor)
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
        _fillBaseColor = _fillImage.color;
        _ringBaseColor = _ringImage.color;
        _dotBaseColor = _dotImage.color;

        SetVisible(false);
    }

    public void ShowOnScreen(Vector2 anchoredPosition)
    {
        Show();
        SetTargetTransform(anchoredPosition);
        _dotImage.enabled = true;
    }

    public void ShowOffScreen(Vector2 anchoredPosition)
    {
        Show();
        SetTargetTransform(anchoredPosition);
        _dotImage.enabled = true;
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

        _showTween = DOTween.Sequence()
            .SetUpdate(true)
            .Append(_rectTransform.DOScale(VisibleScale, ShowDuration).SetEase(Ease.OutCubic))
            .Join(_fillImage.DOFade(_fillBaseColor.a, ShowDuration).SetEase(Ease.OutQuad))
            .Join(_ringImage.DOFade(_ringBaseColor.a, ShowDuration).SetEase(Ease.OutQuad))
            .Join(_dotImage.DOFade(_dotBaseColor.a, ShowDuration).SetEase(Ease.OutQuad));
    }

    private void SetTargetTransform(Vector2 anchoredPosition)
    {
        _targetAnchoredPosition = anchoredPosition;

        if (_isPositionInitialized == false)
        {
            _rectTransform.anchoredPosition = anchoredPosition;
            _rectTransform.localRotation = Quaternion.identity;
            _isPositionInitialized = true;
            _moveVelocity = Vector2.zero;
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
