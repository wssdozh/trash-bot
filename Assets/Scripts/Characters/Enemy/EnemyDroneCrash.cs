using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyDroneCrash : MonoBehaviour
{
    private const float ZeroThreshold = 0.0001f;

    [Header("Dependencies")]
    [SerializeField] private Transform _moveRoot;
    [SerializeField] private Rigidbody _rigidbody;

    [Header("Settings")]
    [SerializeField] private float _force = 2.2f;
    [SerializeField] private float _up = 0.12f;
    [SerializeField] private float _back = 0.32f;
    [SerializeField] private float _down = 0.06f;

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

        if (_force <= 0f)
        {
            throw new InvalidOperationException(nameof(_force));
        }

        if (_up < 0f)
        {
            throw new InvalidOperationException(nameof(_up));
        }

        if (_back < 0f)
        {
            throw new InvalidOperationException(nameof(_back));
        }

        if (_down < 0f)
        {
            throw new InvalidOperationException(nameof(_down));
        }
    }

    public void Crash(Vector3 moveVelocity, Vector3 moveDirection)
    {
        if (_isCrashed)
        {
            return;
        }

        Vector3 crashDirection = GetCrashDirection(moveDirection);
        Vector3 crashPoint = _rigidbody.position - (crashDirection * _back) - (Vector3.up * _down);
        Vector3 crashForceDirection = crashDirection + (Vector3.up * _up);

        if (crashForceDirection.sqrMagnitude <= ZeroThreshold)
        {
            crashForceDirection = Vector3.up;
        }

        crashForceDirection.Normalize();

        _isCrashed = true;
        _rigidbody.position = _moveRoot.position;
        _rigidbody.rotation = _moveRoot.rotation;
        _rigidbody.constraints = RigidbodyConstraints.None;
        _rigidbody.isKinematic = false;
        _rigidbody.useGravity = true;
        _rigidbody.linearVelocity = moveVelocity;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.WakeUp();
        _rigidbody.AddForceAtPosition(crashForceDirection * _force, crashPoint, ForceMode.Impulse);
    }

    private Vector3 GetCrashDirection(Vector3 moveDirection)
    {
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude <= ZeroThreshold)
        {
            moveDirection = _moveRoot.forward;
            moveDirection.y = 0f;
        }

        if (moveDirection.sqrMagnitude <= ZeroThreshold)
        {
            return Vector3.forward;
        }

        moveDirection.Normalize();

        return moveDirection;
    }
}
