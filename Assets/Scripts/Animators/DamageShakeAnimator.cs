using UnityEngine;
using DG.Tweening;

public class DamageShakeAnimator : MonoBehaviour
{
    [SerializeField] private float _shakePositionStrength = 0.3f;
    [SerializeField] private int _shakePositionVibrato = 12;
    [SerializeField] private float _shakePositionDuration = 0.25f;

    [SerializeField] private float _shakeRotationStrength = 15f;
    [SerializeField] private int _shakeRotationVibrato = 10;
    [SerializeField] private float _shakeRotationDuration = 0.25f;

    private Tween _positionTween;
    private Tween _rotationTween;

    private void OnDestroy()
    {
        if (_positionTween != null && _positionTween.IsActive() == true)
        {
            _positionTween.Kill();
        }

        if (_rotationTween != null && _rotationTween.IsActive() == true)
        {
            _rotationTween.Kill();
        }
    }

    public void Shake()
    {
        if (_positionTween != null && _positionTween.IsActive() == true)
        {
            _positionTween.Kill();
        }

        if (_rotationTween != null && _rotationTween.IsActive() == true)
        {
            _rotationTween.Kill();
        }

        _positionTween = transform.DOShakePosition(_shakePositionDuration, _shakePositionStrength, _shakePositionVibrato);
        _rotationTween = transform.DOShakeRotation(_shakeRotationDuration, _shakeRotationStrength, _shakeRotationVibrato);
    }
}
