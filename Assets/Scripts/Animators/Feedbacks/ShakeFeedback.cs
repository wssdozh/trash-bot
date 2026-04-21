using System;
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
    private bool _requiresPhysicsSync;

    public override bool IsPlaying => IsShakeActive();

    private void Awake()
    {
        CacheInitialState();
        UpdatePhysicsSyncState();
    }

    private void OnDisable()
    {
        Stop();
    }

    private void OnDestroy()
    {
        Stop();
    }

    private void LateUpdate()
    {
        if (_requiresPhysicsSync == false)
        {
            return;
        }

        if (IsShakeActive() == false)
        {
            return;
        }

        Physics.SyncTransforms();
    }

    public override void Play()
    {
        if (_shakeTransform == null)
        {
            return;
        }

        Stop();

        CacheInitialState();

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
        if (_shakeTransform == null)
        {
            return;
        }

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

        SyncPhysics();
    }

    public void Initialize(Transform rootTransform, Transform shakeTransform)
    {
        _rootTransform = rootTransform;
        _shakeTransform = shakeTransform;

        CacheInitialState();
        UpdatePhysicsSyncState();
    }

    public void SetStrengthMultiplier(float strengthMultiplier)
    {
        if (strengthMultiplier < 0f)
        {
            throw new InvalidOperationException(nameof(strengthMultiplier));
        }

        _shakePositionStrength *= strengthMultiplier;
        _shakeRotationStrength *= strengthMultiplier;
    }

    private void CacheInitialState()
    {
        if (_shakeTransform == null)
        {
            return;
        }

        _initialLocalPosition = _shakeTransform.localPosition;
        _initialLocalRotation = _shakeTransform.localRotation;
    }

    private void UpdatePhysicsSyncState()
    {
        Transform physicsTransform = _rootTransform;

        if (physicsTransform == null)
        {
            physicsTransform = _shakeTransform;
        }

        _requiresPhysicsSync = false;

        if (physicsTransform == null)
        {
            return;
        }

        Collider collider = physicsTransform.GetComponentInParent<Collider>();
        Rigidbody rigidbody = physicsTransform.GetComponentInParent<Rigidbody>();

        if (collider != null || rigidbody != null)
        {
            _requiresPhysicsSync = true;
        }
    }

    private bool IsShakeActive()
    {
        bool hasPositionTween = _positionTween != null && _positionTween.IsActive() && _positionTween.IsPlaying();
        bool hasRotationTween = _rotationTween != null && _rotationTween.IsActive() && _rotationTween.IsPlaying();

        if (hasPositionTween)
        {
            return true;
        }

        return hasRotationTween;
    }

    private void SyncPhysics()
    {
        if (_requiresPhysicsSync == false)
        {
            return;
        }

        Physics.SyncTransforms();
    }
}
