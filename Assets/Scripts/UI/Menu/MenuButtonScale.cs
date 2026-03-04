using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public sealed class MenuButtonScale : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform _rect;
    [SerializeField] private Button _button;

    [SerializeField] private Vector2 _scaleIdle = new Vector2(1.0f, 1.0f);
    [SerializeField] private Vector2 _scaleHover = new Vector2(1.06f, 1.12f);
    [SerializeField] private Vector2 _scalePressed = new Vector2(1.02f, 1.06f);

    [SerializeField] private float _durationHover = 0.14f;
    [SerializeField] private float _durationPress = 0.10f;
    [SerializeField] private float _durationRelease = 0.12f;

    private Tween _tween;

    private bool _inside;
    private bool _pressed;

    private void OnEnable()
    {
        _inside = false;
        _pressed = false;
        ApplyScale(_scaleIdle);
    }

    private void OnDisable()
    {
        KillTween();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _inside = true;
        ApplyState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _inside = false;
        _pressed = false;
        ApplyState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pressed = true;
        ApplyState();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _pressed = false;
        ApplyState();
    }

    private void ApplyState()
    {
        if (_button.interactable == false)
        {
            TweenScale(_scaleIdle, _durationRelease, Ease.OutCubic);
            return;
        }

        if (_pressed)
        {
            TweenScale(_scalePressed, _durationPress, Ease.OutQuart);
            return;
        }

        if (_inside)
        {
            TweenScale(_scaleHover, _durationHover, Ease.OutQuart);
            return;
        }

        TweenScale(_scaleIdle, _durationRelease, Ease.OutCubic);
    }

    private void TweenScale(Vector2 scale, float duration, Ease ease)
    {
        KillTween();

        Vector3 startScale = _rect.localScale;
        Vector2 startValue = new Vector2(startScale.x, startScale.y);

        _tween = DOTween
            .To(() => startValue, value =>
            {
                startValue = value;
                ApplyScale(value);
            }, scale, duration)
            .SetEase(ease)
            .SetUpdate(true);
    }

    private void ApplyScale(Vector2 scale)
    {
        _rect.localScale = new Vector3(scale.x, scale.y, 1.0f);
    }

    private void KillTween()
    {
        if (_tween != null && _tween.IsActive())
        {
            _tween.Kill(false);
        }

        _tween = null;
    }
}
