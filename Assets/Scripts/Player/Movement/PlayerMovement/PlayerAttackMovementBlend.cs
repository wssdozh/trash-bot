using UnityEngine;

public sealed class PlayerAttackMovementBlend
{
    private readonly PlayerMoveApplier _moveApplier;
    private readonly PlayerSprint _sprint;

    private readonly float _attackEnterSeconds;
    private readonly float _attackExitSeconds;
    private readonly float _attackMovementMultiplier;

    public bool IsRecoverActive { get; private set; }

    public PlayerAttackMovementBlend(
        PlayerMoveApplier moveApplier,
        PlayerSprint sprint,
        float attackEnterSeconds,
        float attackExitSeconds,
        float attackMovementMultiplier)
    {
        _moveApplier = moveApplier;
        _sprint = sprint;

        _attackEnterSeconds = attackEnterSeconds;
        _attackExitSeconds = attackExitSeconds;
        _attackMovementMultiplier = attackMovementMultiplier;

        IsRecoverActive = false;
    }

    public void StartRecover()
    {
        IsRecoverActive = true;
    }

    public void CancelRecover()
    {
        IsRecoverActive = false;
    }

    public void TickSlowdown(float deltaTime)
    {
        _sprint.Stop();

        Vector2 targetMoveVector = GetAttackTargetMoveVector(_moveApplier.InputMoveVector);
        float speed = GetSpeedFromSeconds(_attackEnterSeconds);

        Vector2 newMoveVector = MoveVectorTowards(_moveApplier.AppliedMoveVector, targetMoveVector, speed, deltaTime);
        _moveApplier.Apply(newMoveVector);
        _moveApplier.SetStepSoundAllowed(false);

        if (newMoveVector == Vector2.zero)
        {
            _moveApplier.SetMoveState(false);
            return;
        }

        _moveApplier.SetMoveState(true);
    }

    public void TickRecover(float deltaTime)
    {
        Vector2 targetMoveVector = _moveApplier.InputMoveVector;
        float speed = GetSpeedFromSeconds(_attackExitSeconds);

        Vector2 newMoveVector = MoveVectorTowards(_moveApplier.AppliedMoveVector, targetMoveVector, speed, deltaTime);
        _moveApplier.Apply(newMoveVector);
        _moveApplier.SetStepSoundAllowed(newMoveVector != Vector2.zero);

        if (newMoveVector == targetMoveVector)
        {
            IsRecoverActive = false;
        }

        if (newMoveVector == Vector2.zero)
        {
            _moveApplier.SetMoveState(false);
            return;
        }

        _moveApplier.SetMoveState(true);
    }

    private Vector2 GetAttackTargetMoveVector(Vector2 inputMoveVector)
    {
        if (inputMoveVector == Vector2.zero)
        {
            return Vector2.zero;
        }

        return inputMoveVector * _attackMovementMultiplier;
    }

    private Vector2 MoveVectorTowards(Vector2 current, Vector2 target, float speed, float deltaTime)
    {
        float delta = speed * deltaTime;

        float newX = Mathf.MoveTowards(current.x, target.x, delta);
        float newY = Mathf.MoveTowards(current.y, target.y, delta);

        Vector2 result = new Vector2(newX, newY);
        return result;
    }

    private float GetSpeedFromSeconds(float seconds)
    {
        if (seconds <= 0f)
        {
            return 1000f;
        }

        return 1f / seconds;
    }
}
