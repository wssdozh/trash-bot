using DG.Tweening;
using UnityEngine;

public sealed class PickupIdleMotion : PickupIdleBehaviour
{
    [Header("Настройки")]
    [SerializeField] private float _moveAmplitude = 0.15f;
    [SerializeField] private float _moveDurationSeconds = 1.2f;

    [SerializeField] private Vector3 _rotationDegrees = new Vector3(0f, 35f, 0f);
    [SerializeField] private float _rotationDurationSeconds = 1.6f;

    [SerializeField] private float _randomStartDelaySeconds = 0.25f;

    [Header("Высота")]
    [SerializeField] private float _baseHeightOffset = 0.35f;
    [SerializeField] private float _baseOffsetTransitionSeconds = 0.2f;

    private Tween _startDelayTween;
    private Tween _baseOffsetTween;
    private Tween _moveTween;
    private Tween _rotationTween;

    private Vector3 _defaultLocalPosition;
    private Vector3 _defaultLocalRotation;

    private Vector3 _baseLocalPosition;

    private void Awake()
    {
        _defaultLocalPosition = transform.localPosition;
        _defaultLocalRotation = transform.localEulerAngles;
    }

    protected override void OnIdleActivated()
    {
        StartMotion();
    }

    protected override void OnIdleDeactivated()
    {
        StopMotion();
    }

    private void StartMotion()
    {
        StopMotion();

        _baseLocalPosition = _defaultLocalPosition;

        if (_baseHeightOffset != 0f)

            _baseLocalPosition = new Vector3(_baseLocalPosition.x, _baseLocalPosition.y + _baseHeightOffset, _baseLocalPosition.z);


        float startDelaySeconds = 0f;

        if (_randomStartDelaySeconds > 0f)

            startDelaySeconds = Random.Range(0f, _randomStartDelaySeconds);


        _startDelayTween = DOVirtual.DelayedCall(startDelaySeconds, StartLoopTweens);
        _startDelayTween.SetId(this);
        _startDelayTween.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    private void StopMotion()
    {
        DOTween.Kill(this);

        _startDelayTween = null;
        _baseOffsetTween = null;
        _moveTween = null;
        _rotationTween = null;


        transform.localPosition = _defaultLocalPosition;
        transform.localEulerAngles = _defaultLocalRotation;
    }

    private void StartLoopTweens()
    {
        transform.localPosition = _defaultLocalPosition;
        transform.localEulerAngles = _defaultLocalRotation;

        if (_baseOffsetTransitionSeconds > 0f)
        {
            _baseOffsetTween = transform.DOLocalMove(_baseLocalPosition, _baseOffsetTransitionSeconds);
            _baseOffsetTween.SetEase(Ease.OutSine);
            _baseOffsetTween.SetId(this);
            _baseOffsetTween.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            _baseOffsetTween.OnComplete(StartLoopTweensAfterBaseReached);

            return;
        }


        transform.localPosition = _baseLocalPosition;

        StartLoopTweensAfterBaseReached();
    }

    private void StartLoopTweensAfterBaseReached()
    {
        Vector3 targetLocalPosition = _baseLocalPosition + Vector3.up * _moveAmplitude;
        Vector3 targetLocalRotation = _defaultLocalRotation + _rotationDegrees;

        if (_moveAmplitude != 0f)
        {
            _moveTween = transform.DOLocalMove(targetLocalPosition, _moveDurationSeconds);
            _moveTween.SetEase(Ease.InOutSine);
            _moveTween.SetLoops(-1, LoopType.Yoyo);
            _moveTween.SetId(this);
            _moveTween.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }


        if (_rotationDegrees != Vector3.zero)
        {
            _rotationTween = transform.DOLocalRotate(targetLocalRotation, _rotationDurationSeconds, RotateMode.FastBeyond360);
            _rotationTween.SetEase(Ease.InOutSine);
            _rotationTween.SetLoops(-1, LoopType.Yoyo);
            _rotationTween.SetId(this);
            _rotationTween.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }
    }
}
