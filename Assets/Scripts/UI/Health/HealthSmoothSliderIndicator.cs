using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HealthSmoothSliderIndicator : HealthIndicatorBase<Health>
{
    [SerializeField] private Slider _slider;
    [SerializeField] private float _duration = 0.5f; 

    protected override void Display()
    {
        float targetValue = Health.Value / Health.MaxValue;

        _slider.DOValue(targetValue, _duration).SetEase(Ease.OutQuad); 
    }
}
