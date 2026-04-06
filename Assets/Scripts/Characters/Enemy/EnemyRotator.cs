using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyRotator : MonoBehaviour
{
    private const float ZeroThreshold = 0.0001f;

    [Header("Dependencies")]
    [SerializeField] private Transform _rotationRoot;
    [SerializeField] private Rigidbody _rigidbody;

    [Header("Settings")]
    [SerializeField] private float _rotationSpeed = 240f;

    public Vector3 ForwardDirection
    {
        get
        {
            Vector3 forwardDirection = _rotationRoot.forward;
            forwardDirection.y = 0f;

            if (forwardDirection.sqrMagnitude <= ZeroThreshold)
            {
                return Vector3.forward;
            }

            forwardDirection.Normalize();

            return forwardDirection;
        }
    }

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

        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        LockPhysicsRotation();
    }

    public void RotateToPoint(Vector3 targetPoint)
    {
        Vector3 direction = targetPoint - _rotationRoot.position;
        RotateToDirection(direction);
    }

    public void RotateToDirection(Vector3 direction)
    {
        RotateToDirection(direction, Time.fixedDeltaTime);
    }

    public void RotateToDirection(Vector3 direction, float deltaTime)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= ZeroThreshold)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        float rotationStep = _rotationSpeed * deltaTime;
        Quaternion nextRotation = Quaternion.RotateTowards(
            _rotationRoot.rotation,
            targetRotation,
            rotationStep);

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

        _rotationRoot.rotation = targetRotation;
    }

    private void LockPhysicsRotation()
    {
        if (_rigidbody == null)
        {
            return;
        }

        RigidbodyConstraints constraints = _rigidbody.constraints;
        constraints |= RigidbodyConstraints.FreezeRotationX;
        constraints |= RigidbodyConstraints.FreezeRotationY;
        constraints |= RigidbodyConstraints.FreezeRotationZ;
        _rigidbody.constraints = constraints;
    }
}
