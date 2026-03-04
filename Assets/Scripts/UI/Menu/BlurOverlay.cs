using UnityEngine;
using DG.Tweening;

public sealed class BlurOverlay : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _showDurationSeconds = 0.45f;
    [SerializeField] private float _hideDurationSeconds = 0.30f;
    [SerializeField] private AnimationCurve _showEaseCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
    [SerializeField] private AnimationCurve _hideEaseCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

    private Tween _tween;

    private void Awake()
    {
        SetState(0.0f);
    }

    public void Show()
    {
        if (gameObject.activeSelf == false)
        {
            gameObject.SetActive(true);
        }

        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;

        Animate(1.0f, _showDurationSeconds, _showEaseCurve);
    }

    public void Hide()
    {
        Animate(0.0f, _hideDurationSeconds, _hideEaseCurve);
    }

    private void Animate(float targetAlpha, float durationSeconds, AnimationCurve easeCurve)
    {
        if (_tween != null && _tween.IsActive())
        {
            _tween.Kill(false);
        }

        _tween = DOTween
            .To(() => _canvasGroup.alpha, value => _canvasGroup.alpha = value, targetAlpha, durationSeconds)
            .SetEase(easeCurve)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                SetState(targetAlpha);

                if (targetAlpha <= 0.0f)
                {
                    gameObject.SetActive(false);
                }
            });
    }

    private void SetState(float alpha)
    {
        _canvasGroup.alpha = alpha;

        if (alpha <= 0.0f)
        {
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            return;
        }

        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;
    }
}
