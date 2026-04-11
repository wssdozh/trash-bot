using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyMove : MonoBehaviour
{
    private const float ZeroThreshold = 0.0001f;
    private const float RunDotMin = 0.25f;
    private const float RunDotMax = 0.9f;

    [Header("Dependencies")]
    [SerializeField] private CharacterMover _characterMover;

    [Header("Settings")]
    [SerializeField] private float _steerSpeed = 180f;
    [SerializeField] private float _moveScale = 0.6f;
    [SerializeField] private float _runScaleFactor = 1.25f;
    [SerializeField] private float _moveGainSpeed = 5f;
    [SerializeField] private float _moveDropSpeed = 12f;

    private EnemyRotator _enemyRotator;
    private Vector3 _moveDirection;
    private Vector3 _targetDirection;
    private Vector2 _moveInput;
    private bool _isMoving;
    private bool _isRunRequested;
    private bool _isSprintApplied;
    private float _speedScale = 1f;

    public Vector3 MoveDirection => GetWorldMoveDirection();
    public float MoveAmount => _moveInput.magnitude;
    public bool IsRunning => _isSprintApplied;
    public bool IsRunRequested => _isRunRequested;

    private void Awake()
    {
        _enemyRotator = GetComponent<EnemyRotator>();

        if (_enemyRotator == null)
        {
            throw new InvalidOperationException(nameof(_enemyRotator));
        }

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

        if (_moveGainSpeed <= 0f)
        {
            throw new InvalidOperationException(nameof(_moveGainSpeed));
        }

        if (_moveDropSpeed <= 0f)
        {
            throw new InvalidOperationException(nameof(_moveDropSpeed));
        }
    }

    private void FixedUpdate()
    {
        UpdateMoveDirection();

        Vector2 targetMoveInput = Vector2.zero;
        bool isSprinting = false;

        if (_isMoving)
        {
            float moveScale = Mathf.Min(_moveScale * _speedScale, 1f);

            if (_isRunRequested)
            {
                moveScale = Mathf.Min(_moveScale * _runScaleFactor * _speedScale, 1f);
                targetMoveInput = GetRunInput(moveScale);
                isSprinting = targetMoveInput.sqrMagnitude > ZeroThreshold;
            }

            else
            {
                targetMoveInput = GetMoveInput(_moveDirection, moveScale);
            }
        }

        float inputSpeed = GetInputSpeed(targetMoveInput);
        _moveInput = Vector2.MoveTowards(
            _moveInput,
            targetMoveInput,
            inputSpeed * Time.fixedDeltaTime);
        _isSprintApplied = isSprinting;

        if (_moveInput.sqrMagnitude <= ZeroThreshold)
        {
            _moveInput = Vector2.zero;
            _isSprintApplied = false;
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

        _targetDirection = desiredDirection;

        if (_moveDirection.sqrMagnitude <= ZeroThreshold)
        {
            _moveDirection = desiredDirection;
        }

        _isMoving = true;
    }

    public void SetRun(bool isRunning)
    {
        _isRunRequested = isRunning;
    }

    public void SetSpeedScale(float speedScale)
    {
        if (speedScale <= 0f)
        {
            throw new InvalidOperationException(nameof(speedScale));
        }

        _speedScale = speedScale;
    }

    public void StopMove()
    {
        _isMoving = false;
        _isRunRequested = false;
        _isSprintApplied = false;
        _targetDirection = Vector3.zero;
        _characterMover.OnSprint(false);
    }

    public void ForceStop()
    {
        _isMoving = false;
        _isRunRequested = false;
        _isSprintApplied = false;
        _moveDirection = Vector3.zero;
        _targetDirection = Vector3.zero;
        _moveInput = Vector2.zero;
        _characterMover.OnSprint(false);
        _characterMover.ForceStop();
    }

    private void UpdateMoveDirection()
    {
        if (_isMoving == false)
        {
            _moveDirection = Vector3.zero;

            return;
        }

        if (_targetDirection.sqrMagnitude <= ZeroThreshold)
        {
            _moveDirection = Vector3.zero;

            return;
        }

        if (_moveDirection.sqrMagnitude <= ZeroThreshold)
        {
            _moveDirection = _targetDirection;

            return;
        }

        float maxRadiansDelta = _steerSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime;
        Vector3 nextDirection = Vector3.RotateTowards(_moveDirection, _targetDirection, maxRadiansDelta, 0f);
        nextDirection.y = 0f;

        if (nextDirection.sqrMagnitude > 1f)
        {
            nextDirection.Normalize();
        }

        _moveDirection = nextDirection;
    }

    private Vector2 GetRunInput(float moveScale)
    {
        Vector3 forwardDirection = _enemyRotator.ForwardDirection;
        float forwardDot = Vector3.Dot(forwardDirection, _moveDirection);

        if (forwardDot <= RunDotMin)
        {
            return Vector2.zero;
        }

        float dotScale = Mathf.InverseLerp(RunDotMin, RunDotMax, forwardDot);
        float speedScale = Mathf.SmoothStep(0f, 1f, dotScale);

        return GetMoveInput(forwardDirection, moveScale * speedScale);
    }

    private float GetInputSpeed(Vector2 targetMoveInput)
    {
        if (targetMoveInput.sqrMagnitude < _moveInput.sqrMagnitude)
        {
            return _moveDropSpeed;
        }

        if (targetMoveInput.sqrMagnitude <= ZeroThreshold)
        {
            return _moveDropSpeed;
        }

        if (_moveInput.sqrMagnitude <= ZeroThreshold)
        {
            return _moveGainSpeed;
        }

        float inputDot = Vector2.Dot(_moveInput.normalized, targetMoveInput.normalized);

        if (inputDot < 0f)
        {
            return _moveDropSpeed;
        }

        return _moveGainSpeed;
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

    private Vector3 GetWorldMoveDirection()
    {
        if (_moveInput.sqrMagnitude <= ZeroThreshold)
        {
            return Vector3.zero;
        }

        Vector3 moveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y);

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        return moveDirection;
    }
}
