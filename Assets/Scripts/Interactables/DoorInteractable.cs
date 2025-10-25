using UnityEngine;
using DG.Tweening;

public class DoorInteractable : Interactable
{
    [Header("Настройки двери")]
    [SerializeField] private Transform _doorTransform;
    [SerializeField] private float _openAngle = 90f;
    [SerializeField] private float _openDuration = 0.7f;
    [SerializeField] private Ease _openEase = Ease.InOutSine;

    private bool _isOpen = false;
    private Quaternion _closedRotation;
    private Quaternion _openRotation;
    private Tween _currentTween;

    protected override void Awake()
    {
        base.Awake();

        if (_doorTransform == null)
            _doorTransform = transform;

        _closedRotation = _doorTransform.rotation;
        
        _openRotation = _doorTransform.rotation * Quaternion.Euler(0f, _openAngle, 0f);
    }

    public override string GetPrompt()
    {
        return _isOpen
            ? "Нажмите [E], чтобы закрыть дверь"
            : "Нажмите [E], чтобы открыть дверь";
    }

    public override void Interact(GameObject interactor)
    {
        _isOpen = _isOpen == false;

        _currentTween?.Kill();

        Quaternion targetRotation = _isOpen ? _openRotation : _closedRotation;

        _currentTween = _doorTransform.DORotateQuaternion(targetRotation, _openDuration)
            .SetEase(_openEase);
    }
}
