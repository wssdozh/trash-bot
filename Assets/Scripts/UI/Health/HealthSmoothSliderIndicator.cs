using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HealthSmoothSliderIndicator : StatIndicatorBase<Health>  
{
    [SerializeField] private Slider _slider;
    [SerializeField] private float _duration = 0.5f; 

    protected override void Display()
    {
        float targetValue = Stat.Value / Stat.MaxValue;

        _slider.DOValue(targetValue, _duration).SetEase(Ease.OutQuad); 
    }
}
