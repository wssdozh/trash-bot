using UnityEngine;
using DG.Tweening;

public class ShakeFeedback : Feedback
{
    [Header("Зависимости")]
    [SerializeField] private Transform _rootTransform;
    [SerializeField] private Transform _shakeTransform;

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

    private Vector3 _initialLocalPosition;
    private Quaternion _initialLocalRotation;

    private void Awake()
    {
        _initialLocalPosition = _shakeTransform.localPosition;
        _initialLocalRotation = _shakeTransform.localRotation;
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

        _initialLocalPosition = _shakeTransform.localPosition;
        _initialLocalRotation = _shakeTransform.localRotation;

        _positionTween = _shakeTransform.DOShakePosition(
            _shakePositionDuration,
            _shakePositionStrength,
            _shakePositionVibrato
        );

        _rotationTween = _shakeTransform.DOShakeRotation(
            _shakeRotationDuration,
            _shakeRotationStrength,
            _shakeRotationVibrato
        );
    }

    public override void Stop()
    {
        if (_positionTween != null && _positionTween.IsActive())
        {
            _positionTween.Kill(true);
        }

        if (_rotationTween != null && _rotationTween.IsActive())
        {
            _rotationTween.Kill(true);
        }

        _shakeTransform.localPosition = _initialLocalPosition;
        _shakeTransform.localRotation = _initialLocalRotation;
    }
}
