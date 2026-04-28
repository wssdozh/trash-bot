using System;
using UnityEngine;

public class StepAnimator
{
    private readonly int _stepLeftHash = Animator.StringToHash("StepLeft");
    private readonly int _stepRightHash = Animator.StringToHash("StepRight");
    private readonly int _stepForwardHash = Animator.StringToHash("StepForward");
    private readonly int _stepBackwardHash = Animator.StringToHash("StepBackward");

    private readonly Animator _animator;
    private readonly Transform _transform;
    private readonly float _moveDirectionDeadZoneSqr;
    private readonly float _walkStepInterval;
    private readonly float _runStepInterval;
    private readonly Action<bool> _stepped;

    private StepDirection _currentStepDirection;
    private float _stepTimer;

    public StepAnimator(
        Animator animator,
        Transform transform,
        float moveDirectionDeadZone,
        float walkStepInterval,
        float runStepInterval,
        Action<bool> stepped)
    {
        _animator = animator;
        _transform = transform;
        _moveDirectionDeadZoneSqr = moveDirectionDeadZone * moveDirectionDeadZone;
        _walkStepInterval = walkStepInterval;
        _runStepInterval = runStepInterval;
        _stepped = stepped;
        _currentStepDirection = StepDirection.None;
        _stepTimer = 0f;
    }

    public void UpdateStepFromMoveDirection(
        bool isMoving,
        bool isSprinting,
        bool isStepSoundAllowed,
        Vector3 worldMoveDirection,
        float deltaTime)
    {
        if (isMoving == false)
        {
            if (_currentStepDirection != StepDirection.None)
            {
                StopStep();
            }

            _stepTimer = 0f;
            return;
        }

        StepDirection desiredDirection = GetStepDirection(worldMoveDirection);

        if (desiredDirection == StepDirection.None)
        {
            if (_currentStepDirection != StepDirection.None)
            {
                StopStep();
            }

            _stepTimer = 0f;
            return;
        }

        if (desiredDirection != _currentStepDirection)
        {
            StartStep(desiredDirection);
        }

        TickStep(isSprinting, isStepSoundAllowed, deltaTime);
    }

    public void StopStep()
    {
        SetStepBool(_currentStepDirection, false);
        _currentStepDirection = StepDirection.None;
        _stepTimer = 0f;
    }

    private void StartStep(StepDirection stepDirection)
    {
        SetStepBool(_currentStepDirection, false);
        SetStepBool(stepDirection, true);
        _currentStepDirection = stepDirection;
    }

    private void TickStep(bool isSprinting, bool isStepSoundAllowed, float deltaTime)
    {
        if (isStepSoundAllowed == false)
        {
            _stepTimer = 0f;
            return;
        }

        _stepTimer -= deltaTime;

        if (_stepTimer > 0f)
        {
            return;
        }

        _stepped?.Invoke(isSprinting);
        _stepTimer = GetStepInterval(isSprinting);
    }

    private float GetStepInterval(bool isSprinting)
    {
        if (isSprinting)
            return _runStepInterval;

        return _walkStepInterval;
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
