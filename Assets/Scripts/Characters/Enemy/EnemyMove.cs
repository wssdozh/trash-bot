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
    [SerializeField] private float _runScaleFactor = 1.25f;
    [SerializeField] private float _moveInputSpeed = 3f;

    private Vector3 _moveDirection;
    private Vector2 _moveInput;
    private bool _isMoving;
    private bool _isRunning;

    public Vector3 MoveDirection => _moveDirection;
    public float MoveAmount => _moveInput.magnitude;
    public bool IsRunning => _isRunning;

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

        if (_runScaleFactor < 1f)
        {
            throw new InvalidOperationException(nameof(_runScaleFactor));
        }

        if (_moveInputSpeed <= 0f)
        {
            throw new InvalidOperationException(nameof(_moveInputSpeed));
        }
    }

    private void FixedUpdate()
    {
        Vector2 targetMoveInput = Vector2.zero;
        bool isSprinting = false;

        if (_isMoving)
        {
            float moveScale = _moveScale;

            if (_isRunning)
            {
                moveScale = Mathf.Min(_moveScale * _runScaleFactor, 1f);
                isSprinting = true;
            }

            targetMoveInput = GetMoveInput(_moveDirection, moveScale);
        }

        _moveInput = Vector2.MoveTowards(
            _moveInput,
            targetMoveInput,
            _moveInputSpeed * Time.fixedDeltaTime);

        if (_moveInput.sqrMagnitude <= ZeroThreshold)
        {
            _moveInput = Vector2.zero;
            _characterMover.OnSprint(false);
            _characterMover.StopMove();

            return;
        }

        _characterMover.OnSprint(isSprinting);
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

        _moveDirection = desiredDirection;
        _isMoving = true;
    }

    public void SetRun(bool isRunning)
    {
        _isRunning = isRunning;
    }

    public void StopMove()
    {
        _isMoving = false;
        _isRunning = false;
        _characterMover.OnSprint(false);
    }

    public void ForceStop()
    {
        _isMoving = false;
        _isRunning = false;
        _moveDirection = Vector3.zero;
        _moveInput = Vector2.zero;
        _characterMover.OnSprint(false);
        _characterMover.ForceStop();
    }

    private Vector2 GetMoveInput(Vector3 direction)
    {
        return GetMoveInput(direction, _moveScale);
    }

    private Vector2 GetMoveInput(Vector3 direction, float moveScale)
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

        return new Vector2(flatDirection.x, flatDirection.z) * moveScale;
    }
}
