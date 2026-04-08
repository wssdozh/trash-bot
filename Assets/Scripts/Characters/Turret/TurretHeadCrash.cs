using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TurretHeadCrash : MonoBehaviour
{
    private const float ZeroThreshold = 0.0001f;

    [Header("Dependencies")]
    [SerializeField] private Transform _moveRoot;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Collider _collider;

    [Header("Settings")]
    [SerializeField] private float _force = 2.4f;
    [SerializeField] private float _up = 0.45f;
    [SerializeField] private float _forward = 0.18f;
    [SerializeField] private float _spin = 4.5f;

    private Transform _startParent;
    private Vector3 _startLocalPosition;
    private Quaternion _startLocalRotation;
    private Vector3 _startLocalScale;
    private bool _isCrashed;

    private void Awake()
    {
        if (_moveRoot == null)
        {
            _moveRoot = transform;
        }

        if (_rigidbody == null)
        {
            throw new InvalidOperationException(nameof(_rigidbody));
        }

        if (_collider == null)
        {
            throw new InvalidOperationException(nameof(_collider));
        }

        if (_force <= 0f)
        {
            throw new InvalidOperationException(nameof(_force));
        }

        if (_up < 0f)
        {
            throw new InvalidOperationException(nameof(_up));
        }

        if (_forward < 0f)
        {
            throw new InvalidOperationException(nameof(_forward));
        }

        if (_spin < 0f)
        {
            throw new InvalidOperationException(nameof(_spin));
        }

        CacheStartState();
        ResetState();
    }

    private void OnEnable()
    {
        ResetState();
    }

    public void Crash()
    {
        if (_isCrashed)
        {
            return;
        }

        Vector3 crashDirection = GetCrashDirection();
        Vector3 crashForceDirection = crashDirection + (Vector3.up * _up);

        if (crashForceDirection.sqrMagnitude <= ZeroThreshold)
        {
            crashForceDirection = Vector3.up;
        }

        crashForceDirection.Normalize();

        Transform releaseParent = null;

        if (_startParent != null)
        {
            releaseParent = _startParent.parent;
        }

        _moveRoot.SetParent(releaseParent, true);
        _moveRoot.position += (crashDirection * _forward);
        _rigidbody.position = _moveRoot.position;
        _rigidbody.rotation = _moveRoot.rotation;
        _rigidbody.constraints = RigidbodyConstraints.None;
        _rigidbody.isKinematic = false;
        _rigidbody.useGravity = true;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _collider.enabled = true;
        _rigidbody.WakeUp();
        _rigidbody.AddForce(crashForceDirection * _force, ForceMode.Impulse);
        _rigidbody.AddTorque(GetSpinDirection(crashDirection) * _spin, ForceMode.Impulse);
        _isCrashed = true;
    }

    private void CacheStartState()
    {
        _startParent = _moveRoot.parent;
        _startLocalPosition = _moveRoot.localPosition;
        _startLocalRotation = _moveRoot.localRotation;
        _startLocalScale = _moveRoot.localScale;
    }

    private void ResetState()
    {
        _isCrashed = false;
        _moveRoot.SetParent(_startParent, false);
        _moveRoot.localPosition = _startLocalPosition;
        _moveRoot.localRotation = _startLocalRotation;
        _moveRoot.localScale = _startLocalScale;

        if (_rigidbody.isKinematic == false)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        _rigidbody.constraints = RigidbodyConstraints.FreezeAll;
        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        _collider.enabled = false;
    }

    private Vector3 GetCrashDirection()
    {
        Vector3 moveDirection = _moveRoot.forward;
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude <= ZeroThreshold)
        {
            return Vector3.forward;
        }

        moveDirection.Normalize();

        return moveDirection;
    }

    private Vector3 GetSpinDirection(Vector3 crashDirection)
    {
        Vector3 spinDirection = Vector3.Cross(Vector3.up, crashDirection) + Vector3.up;

        if (spinDirection.sqrMagnitude <= ZeroThreshold)
        {
            return new Vector3(1f, 1f, 0f);
        }

        spinDirection.Normalize();

        return spinDirection;
    }
}
