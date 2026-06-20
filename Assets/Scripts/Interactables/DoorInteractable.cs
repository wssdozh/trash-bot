using DG.Tweening;
using UnityEngine;

public class DoorInteractable : Interactable
{
    [Header("Настройки двери")]
    [SerializeField] private Transform _doorTransform;
    [SerializeField] private float _openAngle = 90f;
    [SerializeField] private float _openDuration = 0.7f;
    [SerializeField] private Ease _openEase = Ease.InOutSine;

    private bool _isOpen;
    private Quaternion _closedRotation;
    private Quaternion _openRotationPositive;
    private Quaternion _openRotationNegative;
    private Tween _currentTween;

    protected override void Awake()
    {
        base.Awake();

        _doorTransform = _doorTransform == null ? transform : _doorTransform;
        _closedRotation = _doorTransform.rotation;

        float openAngle = Mathf.Abs(_openAngle);

        _openRotationPositive = _closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        _openRotationNegative = _closedRotation * Quaternion.Euler(0f, -openAngle, 0f);
    }

    public override string GetPrompt()
    {
        return _isOpen
            ? "закрыть дверь"
            : "открыть дверь";
    }

    public override void Interact(GameObject interactor)
    {
        _isOpen = _isOpen == false;

        if (_currentTween != null)
        {
            _currentTween.Kill();
        }

        Quaternion targetRotation = _isOpen ? GetOpenRotation(interactor) : _closedRotation;

        _currentTween = _doorTransform.DORotateQuaternion(targetRotation, _openDuration)
            .SetEase(_openEase);
    }

    private Quaternion GetOpenRotation(GameObject interactor)
    {
        Vector3 toInteractor = interactor.transform.position - _doorTransform.position;
        toInteractor.y = 0f;

        Vector3 doorForward = _closedRotation * Vector3.forward;
        doorForward.y = 0f;

        float dot = Vector3.Dot(doorForward.normalized, toInteractor.normalized);

        return dot >= 0f ? _openRotationNegative : _openRotationPositive;
    }
}
