using UnityEngine;
using DG.Tweening;

public class DamageShakeAnimator : MonoBehaviour
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

    private Vector3 _initialLocalPosition;
    private Quaternion _initialLocalRotation;

    private void OnEnable()
    {
        if (_targetTransform != null)
        {
            _initialLocalPosition = _targetTransform.localPosition;
            _initialLocalRotation = _targetTransform.localRotation;
        }
    }

    private void OnDisable()
    {
        bool isComplete = _targetTransform != null;

        KillTweens(isComplete);

        if (_targetTransform != null)
        {
            _targetTransform.localPosition = _initialLocalPosition;
            _targetTransform.localRotation = _initialLocalRotation;
        }
    }

    private void OnDestroy()
    {
        bool isComplete = _targetTransform != null;

        KillTweens(isComplete);

        if (_targetTransform != null)
        {
            _targetTransform.localPosition = _initialLocalPosition;
            _targetTransform.localRotation = _initialLocalRotation;
        }
    }

    public void Shake()
    {
        Transform targetTransform = _targetTransform != null ? _targetTransform : transform;
        bool isComplete = _targetTransform != null;

        KillTweens(isComplete);

        if (_targetTransform != null)
        {
            targetTransform.localPosition = _initialLocalPosition;
            targetTransform.localRotation = _initialLocalRotation;
        }

        _positionTween = targetTransform.DOShakePosition(_shakePositionDuration, _shakePositionStrength, _shakePositionVibrato);
        _rotationTween = targetTransform.DOShakeRotation(_shakeRotationDuration, _shakeRotationStrength, _shakeRotationVibrato);
    }

    private void KillTweens(bool isComplete)
    {
        if (_positionTween != null && _positionTween.IsActive() == true)
        {
            _positionTween.Kill(isComplete);
        }

        if (_rotationTween != null && _rotationTween.IsActive() == true)
        {
            _rotationTween.Kill(isComplete);
        }
    }
}
