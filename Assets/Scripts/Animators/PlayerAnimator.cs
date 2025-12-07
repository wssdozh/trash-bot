using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private static readonly int MoveHash = Animator.StringToHash("Move");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int PointHash = Animator.StringToHash("Point");
    private static readonly int IsFightHash = Animator.StringToHash("IsFight");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int TakeDamageHash = Animator.StringToHash("TakeDamage");
    private static readonly int StepLeftHash = Animator.StringToHash("StepLeft");
    private static readonly int StepRightHash = Animator.StringToHash("StepRight");
    private static readonly int StepForwardHash = Animator.StringToHash("StepForward");
    private static readonly int StepBackwardHash = Animator.StringToHash("StepBackward");

    private enum StepDirection
    {
        None,
        Left,
        Right,
        Forward,
        Backward
    }

    [SerializeField] private Animator _animator;
    [SerializeField] private float _moveLerpSpeed = 5f;
    [SerializeField] private float _walkMoveValue = 0.5f;
    [SerializeField] private float _runMoveValue = 1f;
    [SerializeField] private float _turnLerpSpeed = 8f;
    [SerializeField] private float _moveDirectionDeadZone = 0.0001f;

    private float _currentMove;
    private float _targetMove;
    private bool _isMoving;
    private bool _isSprinting;

    private bool _isStepping;
    private StepDirection _currentStepDirection = StepDirection.None;

    private Vector3 _previousPosition;
    private bool _hasPreviousPosition;

    private void Awake()
    {
        _previousPosition = transform.position;
        _hasPreviousPosition = false;
    }

    private void Update()
    {
        UpdateMove();
        UpdateStepFromMovement();
    }

    public void SetMoveState(bool isMoving)
    {
        _isMoving = isMoving;
    }

    public void SetSprintState(bool isSprinting)
    {
        _isSprinting = isSprinting;
    }

    public void TriggerJump()
    {
        _animator.SetTrigger(JumpHash);
    }

    public void TriggerPoint()
    {
        _animator.SetTrigger(PointHash);
    }

    public void TriggerAttack()
    {
        _animator.SetTrigger(AttackHash);
    }

    public void TriggerTakeDamage()
    {
        _animator.SetTrigger(TakeDamageHash);
    }

    public void SetFight(bool isFight)
    {
        _animator.SetBool(IsFightHash, isFight);
    }

    private void UpdateMove()
    {
        if (_isMoving == false)
        {
            _targetMove = 0f;
        }
        else
        {
            if (_isSprinting == true)
            {
                _targetMove = _runMoveValue;
            }
            else
            {
                _targetMove = _walkMoveValue;
            }
        }

        _currentMove = Mathf.MoveTowards(_currentMove, _targetMove, _moveLerpSpeed * Time.deltaTime);
        _animator.SetFloat(MoveHash, _currentMove);
    }

    private void UpdateStepFromMovement()
    {
        if (_hasPreviousPosition == false)
        {
            _previousPosition = transform.position;
            _hasPreviousPosition = true;
            return;
        }

        Vector3 currentPosition = transform.position;
        Vector3 deltaPosition = currentPosition - _previousPosition;
        _previousPosition = currentPosition;

        deltaPosition.y = 0f;

        float deadZoneSqr = _moveDirectionDeadZone * _moveDirectionDeadZone;

        if (_isMoving == false || deltaPosition.sqrMagnitude <= deadZoneSqr)
        {
            if (_isStepping == true)
            {
                StopStep();
            }

            return;
        }

        Vector3 moveDirection = deltaPosition.normalized;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        float forwardAmount = Vector3.Dot(moveDirection, forward);
        float rightAmount = Vector3.Dot(moveDirection, right);

        float absForwardAmount = Mathf.Abs(forwardAmount);
        float absRightAmount = Mathf.Abs(rightAmount);

        if (absForwardAmount < 0.0001f && absRightAmount < 0.0001f)
        {
            if (_isStepping == true)
            {
                StopStep();
            }

            return;
        }

        StepDirection desiredDirection;

        if (absForwardAmount >= absRightAmount)
        {
            if (forwardAmount > 0f)
            {
                desiredDirection = StepDirection.Forward;
            }
            else
            {
                desiredDirection = StepDirection.Backward;
            }
        }
        else
        {
            if (rightAmount > 0f)
            {
                desiredDirection = StepDirection.Right;
            }
            else
            {
                desiredDirection = StepDirection.Left;
            }
        }

        if (_isStepping == false)
        {
            TryStep(moveDirection);
            return;
        }

        if (desiredDirection != _currentStepDirection)
        {
            StopStep();
            TryStep(moveDirection);
        }
    }

    public void TryStep(Vector3 worldMoveDirection)
    {
        if (_isStepping == true)
        {
            return;
        }

        worldMoveDirection.y = 0f;

        float deadZoneSqr = _moveDirectionDeadZone * _moveDirectionDeadZone;

        if (worldMoveDirection.sqrMagnitude <= deadZoneSqr)
        {
            return;
        }

        worldMoveDirection.Normalize();

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        float forwardAmount = Vector3.Dot(worldMoveDirection, forward);
        float rightAmount = Vector3.Dot(worldMoveDirection, right);

        float absForwardAmount = Mathf.Abs(forwardAmount);
        float absRightAmount = Mathf.Abs(rightAmount);

        StepDirection stepDirection = StepDirection.None;

        if (absForwardAmount >= absRightAmount)
        {
            if (forwardAmount > 0f)
            {
                stepDirection = StepDirection.Forward;
            }
            else
            {
                stepDirection = StepDirection.Backward;
            }
        }
        else
        {
            if (rightAmount > 0f)
            {
                stepDirection = StepDirection.Right;
            }
            else
            {
                stepDirection = StepDirection.Left;
            }
        }

        if (stepDirection == StepDirection.None)
        {
            return;
        }

        SetStepAnimatorBools(stepDirection);

        _isStepping = true;
        _currentStepDirection = stepDirection;
    }

    private void SetStepAnimatorBools(StepDirection stepDirection)
    {
        bool isLeft = stepDirection == StepDirection.Left;
        bool isRight = stepDirection == StepDirection.Right;
        bool isForward = stepDirection == StepDirection.Forward;
        bool isBackward = stepDirection == StepDirection.Backward;

        _animator.SetBool(StepLeftHash, isLeft);
        _animator.SetBool(StepRightHash, isRight);
        _animator.SetBool(StepForwardHash, isForward);
        _animator.SetBool(StepBackwardHash, isBackward);
    }

    public void StopStep()
    {
        _isStepping = false;
        _currentStepDirection = StepDirection.None;

        _animator.SetBool(StepLeftHash, false);
        _animator.SetBool(StepRightHash, false);
        _animator.SetBool(StepForwardHash, false);
        _animator.SetBool(StepBackwardHash, false);
    }
}
