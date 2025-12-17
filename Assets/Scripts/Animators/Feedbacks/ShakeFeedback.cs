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
    private bool _shouldRestoreTransform;

    private void Awake()
    {
        _shouldRestoreTransform = _targetTransform != null;
        _resolvedTargetTransform = _shouldRestoreTransform == true ? _targetTransform : transform;

        if (_shouldRestoreTransform == true)
        {
            _initialLocalPosition = _resolvedTargetTransform.localPosition;
            _initialLocalRotation = _resolvedTargetTransform.localRotation;
        }
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

        if (_shouldRestoreTransform == true)
        {
            _initialLocalPosition = _resolvedTargetTransform.localPosition;
            _initialLocalRotation = _resolvedTargetTransform.localRotation;
        }

        _positionTween = _resolvedTargetTransform.DOShakePosition(
            _shakePositionDuration,
            _shakePositionStrength,
            _shakePositionVibrato
        );

        _rotationTween = _resolvedTargetTransform.DOShakeRotation(
            _shakeRotationDuration,
            _shakeRotationStrength,
            _shakeRotationVibrato
        );
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

        if (_shouldRestoreTransform == true)
        {
            _resolvedTargetTransform.localPosition = _initialLocalPosition;
            _resolvedTargetTransform.localRotation = _initialLocalRotation;
        }
    }
}
