using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyMove : MonoBehaviour
{
    private const float ZeroThreshold = 0.0001f;

    [Header("Dependencies")]
    [SerializeField] private CharacterMover _characterMover;

    [Header("Settings")]
    [SerializeField] private float _steerSpeed = 180f;
    [SerializeField] private float _moveScale = 0.6f;
    [SerializeField] private float _moveInputSpeed = 3f;

    private Vector3 _moveDirection;
    private Vector2 _moveInput;
    private bool _isMoving;

    public Vector3 MoveDirection => _moveDirection;
    public float MoveAmount => _moveInput.magnitude;

    private void Awake()
    {
        if (_characterMover == null)
        {
            throw new InvalidOperationException(nameof(_characterMover));
        }

        if (_steerSpeed <= 0f)
        {
            throw new InvalidOperationException(nameof(_steerSpeed));
        }

        if (_moveScale <= 0f)
        {
            throw new InvalidOperationException(nameof(_moveScale));
        }

        if (_moveScale > 1f)
        {
            throw new InvalidOperationException(nameof(_moveScale));
        }

        if (_moveInputSpeed <= 0f)
        {
            throw new InvalidOperationException(nameof(_moveInputSpeed));
        }
    }

    private void FixedUpdate()
    {
        Vector2 targetMoveInput = Vector2.zero;

        if (_isMoving)
        {
            targetMoveInput = GetMoveInput(_moveDirection);
        }

        _moveInput = Vector2.MoveTowards(
            _moveInput,
            targetMoveInput,
            _moveInputSpeed * Time.fixedDeltaTime);

        if (_moveInput.sqrMagnitude <= ZeroThreshold)
        {
            _moveInput = Vector2.zero;
            _characterMover.StopMove();

            return;
        }

        _characterMover.OnMove(_moveInput);
    }

    public void SetDirection(Vector3 moveDirection)
    {
        Vector3 desiredDirection = moveDirection;
        desiredDirection.y = 0f;

        if (desiredDirection.sqrMagnitude <= ZeroThreshold)
        {
            StopMove();

            return;
        }

        if (desiredDirection.sqrMagnitude > 1f)
        {
            desiredDirection.Normalize();
        }

        if (_isMoving == false || _moveDirection.sqrMagnitude <= ZeroThreshold)
        {
            _moveDirection = desiredDirection;
        }

        else
        {
            float maxRadiansDelta = _steerSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime;

            _moveDirection = Vector3.RotateTowards(
                _moveDirection,
                desiredDirection,
                maxRadiansDelta,
                0f);
        }

        if (_moveDirection.sqrMagnitude > 1f)
        {
            _moveDirection.Normalize();
        }

        _isMoving = true;
    }

    public void StopMove()
    {
        _isMoving = false;
    }

    public void ForceStop()
    {
        _isMoving = false;
        _moveDirection = Vector3.zero;
        _moveInput = Vector2.zero;
        _characterMover.ForceStop();
    }

    private Vector2 GetMoveInput(Vector3 direction)
    {
        Vector3 flatDirection = direction;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude <= ZeroThreshold)
        {
            return Vector2.zero;
        }

        if (flatDirection.sqrMagnitude > 1f)
        {
            flatDirection.Normalize();
        }

        return new Vector2(flatDirection.x, flatDirection.z) * _moveScale;
    }
}
