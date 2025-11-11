using UnityEngine;


public class LargeCarryInteractable : Interactable
{
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Vector3 _localPositionOffset;
    [SerializeField] private Vector3 _localEulerOffset;

    private bool _isCarried = false;
    private Transform _originalParent;

    protected override void Awake()
    {
        base.Awake();
    }

    public override string GetPrompt()
    {
        if (_isCarried == false)
        {
            return "Взять";
        }

        return "Опустить";
    }

    public override void Interact(GameObject interactor)
    {
        if (_isCarried == false)
        {
            Attach(interactor);
            return;
        }
        Detach();
    }

    private void Attach(GameObject interactor)
    {
        CarryAttachment carryAttachment = interactor.GetComponent<CarryAttachment>();

        _originalParent = transform.parent;

        Transform point = _interactionPoint != null ? _interactionPoint : transform;
        Transform target = carryAttachment.CarryPoint != null ? carryAttachment.CarryPoint : interactor.transform;

        Vector3 delta = target.position - point.position;
        transform.position += delta;

        transform.rotation = target.rotation;
        transform.SetParent(target, true);

        transform.localPosition += _localPositionOffset;
        transform.localRotation *= Quaternion.Euler(_localEulerOffset);

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.isKinematic = true;

        _isCarried = true;
    }

    private void Detach()
    {
        transform.SetParent(_originalParent, true);
        _rigidbody.isKinematic = false;
        _isCarried = false;
    }

    private void OnDisable()
    {
        if (_isCarried == false)
        {
            return;
        }
        Detach();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        float scale = 0.3f;

        Gizmos.DrawWireCube(transform.position + _localPositionOffset, new Vector3(scale, scale, scale));
    }
}
