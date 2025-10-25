using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HealthSmoothSliderIndicator : StatIndicatorBase<Health>
{
    [SerializeField] private Slider _slider;
    [SerializeField] private float _duration = 0.5f;

    private Tween _currentTween;

    protected override void Display()
    {
        float targetValue = Stat.Value / Stat.MaxValue;

        if (_currentTween != null && _currentTween.IsActive() == true)
        {
            _currentTween.Kill();
        }

        _currentTween = _slider.DOValue(targetValue, _duration).SetEase(Ease.OutQuad);
    }
}
