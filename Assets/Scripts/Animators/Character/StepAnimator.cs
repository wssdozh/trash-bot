using UnityEngine;

public class StepAnimator
{
    private static readonly int _stepLeftHash = Animator.StringToHash("StepLeft");
    private static readonly int _stepRightHash = Animator.StringToHash("StepRight");
    private static readonly int _stepForwardHash = Animator.StringToHash("StepForward");
    private static readonly int _stepBackwardHash = Animator.StringToHash("StepBackward");

    private enum StepDirection
    {
        None,
        Left,
        Right,
        Forward,
        Backward
    }

    private Animator _animator;
    private Transform _transform;
    private float _moveDirectionDeadZone;

    private Vector3 _previousPosition;
    private bool _hasPreviousPosition;
    private bool _isStepping;
    private StepDirection _currentStepDirection = StepDirection.None;

    public StepAnimator(Animator animator, Transform transform, float moveDirectionDeadZone)
    {
        _animator = animator;
        _transform = transform;
        _moveDirectionDeadZone = moveDirectionDeadZone;
        _hasPreviousPosition = false;
    }

    public void UpdateStepFromMovement(bool isMoving)
    {
        if (_hasPreviousPosition == false)
        {
            _previousPosition = _transform.position;
            _hasPreviousPosition = true;
            return;
        }

        Vector3 currentPosition = _transform.position;
        Vector3 deltaPosition = currentPosition - _previousPosition;
        _previousPosition = currentPosition;

        if (isMoving == false)
        {
            if (_isStepping == true)
            {
                StopStep();
            }

            return;
        }

        StepDirection desiredDirection = GetStepDirection(deltaPosition);

        if (desiredDirection == StepDirection.None)
        {
            if (_isStepping == true)
            {
                StopStep();
            }

            return;
        }

        if (_isStepping == false)
        {
            StartStep(desiredDirection);
            return;
        }

        if (desiredDirection != _currentStepDirection)
        {
            StopStep();
            StartStep(desiredDirection);
        }
    }

    public void TryStep(Vector3 worldMoveDirection)
    {
        if (_isStepping == true)
        {
            return;
        }

        StepDirection stepDirection = GetStepDirection(worldMoveDirection);

        if (stepDirection == StepDirection.None)
        {
            return;
        }

        StartStep(stepDirection);
    }

    public void StopStep()
    {
        _isStepping = false;
        _currentStepDirection = StepDirection.None;
        SetStepAnimatorBools(StepDirection.None);
    }

    private void StartStep(StepDirection stepDirection)
    {
        SetStepAnimatorBools(stepDirection);
        _isStepping = true;
        _currentStepDirection = stepDirection;
    }

    private StepDirection GetStepDirection(Vector3 worldMoveDirection)
    {
        worldMoveDirection.y = 0f;

        float deadZoneSqr = _moveDirectionDeadZone * _moveDirectionDeadZone;

        if (worldMoveDirection.sqrMagnitude <= deadZoneSqr)
        {
            return StepDirection.None;
        }

        worldMoveDirection.Normalize();

        Vector3 forward = _transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = _transform.right;
        right.y = 0f;
        right.Normalize();

        float forwardAmount = Vector3.Dot(worldMoveDirection, forward);
        float rightAmount = Vector3.Dot(worldMoveDirection, right);

        float absForwardAmount = Mathf.Abs(forwardAmount);
        float absRightAmount = Mathf.Abs(rightAmount);

        if (absForwardAmount < 0.0001f && absRightAmount < 0.0001f)
        {
            return StepDirection.None;
        }

        if (absForwardAmount >= absRightAmount)
        {
            if (forwardAmount > 0f)
            {
                return StepDirection.Forward;
            }

            return StepDirection.Backward;
        }

        if (rightAmount > 0f)
        {
            return StepDirection.Right;
        }

        return StepDirection.Left;
    }

    private void SetStepAnimatorBools(StepDirection stepDirection)
    {
        bool isLeft = stepDirection == StepDirection.Left;
        bool isRight = stepDirection == StepDirection.Right;
        bool isForward = stepDirection == StepDirection.Forward;
        bool isBackward = stepDirection == StepDirection.Backward;

        _animator.SetBool(_stepLeftHash, isLeft);
        _animator.SetBool(_stepRightHash, isRight);
        _animator.SetBool(_stepForwardHash, isForward);
        _animator.SetBool(_stepBackwardHash, isBackward);
    }
}
