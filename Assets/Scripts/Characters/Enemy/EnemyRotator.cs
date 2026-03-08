using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyRotator : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Transform _rotationRoot;
    [SerializeField] private Rigidbody _rigidbody;

    [Header("Settings")]
    [SerializeField] private float _rotationSpeed = 300f;

    private void Awake()
    {
        if (_rotationSpeed <= 0f)
        {
            throw new InvalidOperationException(nameof(_rotationSpeed));
        }

        if (_rotationRoot == null)
        {
            _rotationRoot = transform;
        }

        if (_rotationRoot == transform && _rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        if (_rigidbody != null)
        {
            RigidbodyConstraints constraints = _rigidbody.constraints;
            constraints &= ~RigidbodyConstraints.FreezeRotationY;
            constraints |= RigidbodyConstraints.FreezeRotationX;
            constraints |= RigidbodyConstraints.FreezeRotationZ;
            _rigidbody.constraints = constraints;
        }
    }

    private void FixedUpdate()
    {
        if (_rigidbody == null)
        {
            return;
        }

        if (_rotationRoot != transform)
        {
            return;
        }

        _rigidbody.angularVelocity = Vector3.zero;
    }

    public void RotateToPoint(Vector3 targetPoint)
    {
        Vector3 direction = targetPoint - _rotationRoot.position;
        RotateToDirection(direction);
    }

    public void RotateToDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        float rotationStep = _rotationSpeed * Time.fixedDeltaTime;
        Quaternion nextRotation = Quaternion.RotateTowards(
            _rotationRoot.rotation,
            targetRotation,
            rotationStep);

        if (_rigidbody != null && _rotationRoot == transform)
        {
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.MoveRotation(nextRotation);

            return;
        }

        _rotationRoot.rotation = nextRotation;
    }

    public void SnapToDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);

        if (_rigidbody != null && _rotationRoot == transform)
        {
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.rotation = targetRotation;

            return;
        }

        _rotationRoot.rotation = targetRotation;
    }
}
