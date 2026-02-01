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

    private Tween _startDelayTween;
    private Tween _moveTween;
    private Tween _rotationTween;

    private Vector3 _startLocalPosition;
    private Vector3 _startLocalRotation;

    private bool _hasCapturedStartState;

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
        _moveTween = null;
        _rotationTween = null;


        if (_hasCapturedStartState == true)
        {
            transform.localPosition = _startLocalPosition;
            transform.localEulerAngles = _startLocalRotation;
        }

        _hasCapturedStartState = false;
    }

    private void StartLoopTweens()
    {
        _startLocalPosition = transform.localPosition;
        _startLocalRotation = transform.localEulerAngles;

        _hasCapturedStartState = true;

        Vector3 targetLocalPosition = _startLocalPosition + new Vector3(0f, _moveAmplitude, 0f);
        Vector3 targetLocalRotation = _startLocalRotation + _rotationDegrees;

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
