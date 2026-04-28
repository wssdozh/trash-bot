using UnityEngine;

public sealed class PlayerMoveApplier
{
    private readonly CharacterMover _movement;
    private readonly PlayerAnimator _animator;

    public Vector2 InputMoveVector { get; private set; }
    public Vector2 AppliedMoveVector { get; private set; }
    public Vector3 LastWorldMoveDirection { get; private set; }

    public PlayerMoveApplier(CharacterMover movement, PlayerAnimator animator)
    {
        _movement = movement;
        _animator = animator;

        LastWorldMoveDirection = Vector3.zero;
        InputMoveVector = Vector2.zero;
        AppliedMoveVector = Vector2.zero;
    }

    public void SetInput(Vector2 moveVector)
    {
        InputMoveVector = moveVector;
    }

    public void Apply(Vector2 moveVector)
    {
        AppliedMoveVector = moveVector;

        _movement.OnMove(moveVector);

        Vector3 worldMoveDirection = ConvertMoveVectorToWorldDirection(moveVector);

        if (worldMoveDirection != Vector3.zero)
        {
            LastWorldMoveDirection = worldMoveDirection;
        }

        _animator.SetWorldMoveDirection(worldMoveDirection);
    }

    public void SetMoveState(bool isMoving)
    {
        _animator.SetMoveState(isMoving);
    }

    public void SetStepSoundAllowed(bool isAllowed)
    {
        _animator.SetStepSoundAllowed(isAllowed);
    }

    public void SetWorldMoveDirection(Vector3 worldMoveDirection)
    {
        _animator.SetWorldMoveDirection(worldMoveDirection);
    }

    private Vector3 ConvertMoveVectorToWorldDirection(Vector2 moveVector)
    {
        Vector3 worldMoveDirection = new Vector3(moveVector.x, 0f, moveVector.y);
        return worldMoveDirection;
    }
}
