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
    private float _moveDirectionDeadZoneSqr;

    private Vector3 _previousPosition;
    private bool _hasPreviousPosition;
    private StepDirection _currentStepDirection;

    public StepAnimator(Animator animator, Transform transform, float moveDirectionDeadZone)
    {
        _animator = animator;
        _transform = transform;
        _moveDirectionDeadZoneSqr = moveDirectionDeadZone * moveDirectionDeadZone;
        _hasPreviousPosition = false;
        _currentStepDirection = StepDirection.None;
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
            if (_currentStepDirection != StepDirection.None)
            {
                StopStep();
            }

            return;
        }

        StepDirection desiredDirection = GetStepDirection(deltaPosition);

        if (desiredDirection == StepDirection.None)
        {
            if (_currentStepDirection != StepDirection.None)
            {
                StopStep();
            }

            return;
        }

        if (desiredDirection != _currentStepDirection)
        {
            StartStep(desiredDirection);
        }
    }

    public void TryStep(Vector3 worldMoveDirection)
    {
        if (_currentStepDirection != StepDirection.None)
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
        SetStepBool(_currentStepDirection, false);
        _currentStepDirection = StepDirection.None;
    }

    private void StartStep(StepDirection stepDirection)
    {
        SetStepBool(_currentStepDirection, false);
        SetStepBool(stepDirection, true);
        _currentStepDirection = stepDirection;
    }

    private StepDirection GetStepDirection(Vector3 worldMoveDirection)
    {
        worldMoveDirection.y = 0f;

        if (worldMoveDirection.sqrMagnitude <= _moveDirectionDeadZoneSqr)
        {
            return StepDirection.None;
        }

        Vector3 forward = _transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward);

        float forwardAmount = Vector3.Dot(worldMoveDirection, forward);
        float rightAmount = Vector3.Dot(worldMoveDirection, right);

        if (Mathf.Abs(forwardAmount) >= Mathf.Abs(rightAmount))
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

    private void SetStepBool(StepDirection stepDirection, bool value)
    {
        if (stepDirection == StepDirection.Left)
        {
            _animator.SetBool(_stepLeftHash, value);
            return;
        }

        if (stepDirection == StepDirection.Right)
        {
            _animator.SetBool(_stepRightHash, value);
            return;
        }

        if (stepDirection == StepDirection.Forward)
        {
            _animator.SetBool(_stepForwardHash, value);
            return;
        }

        if (stepDirection == StepDirection.Backward)
        {
            _animator.SetBool(_stepBackwardHash, value);
        }
    }
}
