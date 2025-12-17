using UnityEngine;
using DG.Tweening;

public class ShakeFeedback : Feedback
{
    [SerializeField] private Transform _targetTransform;

    [Header("Позиция")]
    [SerializeField] private float _shakePositionStrength = 0.3f;
    [SerializeField] private int _shakePositionVibrato = 12;
    [SerializeField] private float _shakePositionDuration = 0.25f;

    [Header("Ротация")]
    [SerializeField] private float _shakeRotationStrength = 15f;
    [SerializeField] private int _shakeRotationVibrato = 10;
    [SerializeField] private float _shakeRotationDuration = 0.25f;

    private Tween _positionTween;
    private Tween _rotationTween;

    private Transform _resolvedTargetTransform;
    private Vector3 _initialLocalPosition;
    private Quaternion _initialLocalRotation;

    private void Awake()
    {
        _resolvedTargetTransform = _targetTransform != null ? _targetTransform : transform;
        _initialLocalPosition = _resolvedTargetTransform.localPosition;
        _initialLocalRotation = _resolvedTargetTransform.localRotation;
    }

    private void OnDisable()
    {
        Stop();
    }

    private void OnDestroy()
    {
        Stop();
    }

    public override void Play()
    {
        Stop();

        _positionTween = _resolvedTargetTransform.DOShakePosition(_shakePositionDuration, _shakePositionStrength, _shakePositionVibrato);
        _rotationTween = _resolvedTargetTransform.DOShakeRotation(_shakeRotationDuration, _shakeRotationStrength, _shakeRotationVibrato);
    }

    public override void Stop()
    {
        if (_positionTween != null && _positionTween.IsActive() == true)
        {
            _positionTween.Kill(true);
        }

        if (_rotationTween != null && _rotationTween.IsActive() == true)
        {
            _rotationTween.Kill(true);
        }

        _resolvedTargetTransform.localPosition = _initialLocalPosition;
        _resolvedTargetTransform.localRotation = _initialLocalRotation;
    }
}
