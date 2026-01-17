using UnityEngine;

public sealed class PlayerMoveStopDelay
{
    private readonly PlayerMoveApplier _moveApplier;
    private readonly float _moveStopDelaySeconds;

    private bool _isActive;
    private float _timerSeconds;

    public PlayerMoveStopDelay(PlayerMoveApplier moveApplier, float moveStopDelaySeconds)
    {
        _moveApplier = moveApplier;
        _moveStopDelaySeconds = moveStopDelaySeconds;

        _isActive = false;
        _timerSeconds = 0f;
    }

    public void OnInputMove(Vector2 moveVector)
    {
        if (moveVector == Vector2.zero)
        {
            _moveApplier.Apply(Vector2.zero);

            _isActive = true;
            _timerSeconds = 0f;

            _moveApplier.SetWorldMoveDirection(_moveApplier.LastWorldMoveDirection);
            _moveApplier.SetMoveState(true);

            return;
        }

        _isActive = false;

        _moveApplier.Apply(moveVector);
        _moveApplier.SetMoveState(true);
    }

    public void Tick(float deltaTime)
    {
        if (_isActive == false)
        {
            return;
        }

        if (_moveApplier.InputMoveVector != Vector2.zero)
        {
            _isActive = false;
            return;
        }

        if (_moveStopDelaySeconds <= 0f)
        {
            _isActive = false;
            _moveApplier.SetWorldMoveDirection(Vector3.zero);
            _moveApplier.SetMoveState(false);
            return;
        }

        _timerSeconds += deltaTime;

        if (_timerSeconds < _moveStopDelaySeconds)
        {
            _moveApplier.SetWorldMoveDirection(_moveApplier.LastWorldMoveDirection);
            _moveApplier.SetMoveState(true);
            return;
        }

        _isActive = false;
        _moveApplier.SetWorldMoveDirection(Vector3.zero);
        _moveApplier.SetMoveState(false);
    }

    public void Cancel()
    {
        _isActive = false;
        _timerSeconds = 0f;
    }
}
