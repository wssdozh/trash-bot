using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class HealthSmoothSliderIndicator : StatIndicatorBase<Health>
{
    [SerializeField] private Slider _slider;
    [SerializeField] private float _duration = 0.5f;

    private Tween _currentTween;

    protected override void OnDisable()
    {
        base.OnDisable();
        KillCurrentTween();
    }

    protected override void Display()
    {
        float targetValue = Stat.Value / Stat.MaxValue;

        KillCurrentTween();

        _currentTween = _slider
            .DOValue(targetValue, _duration)
            .SetEase(Ease.OutQuad)
            .SetLink(_slider.gameObject);
    }

    private void KillCurrentTween()
    {
        if (_currentTween != null && _currentTween.IsActive())
        {
            _currentTween.Kill();
        }
    }
}
