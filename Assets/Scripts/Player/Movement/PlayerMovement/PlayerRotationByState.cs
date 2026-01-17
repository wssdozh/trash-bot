sealed class PlayerRotationByState
{
    private readonly CharacterRotator _rotator;
    private readonly PlayerMoveApplier _moveApplier;
    private readonly PlayerAttackMovementBlend _attackMovementBlend;

    public PlayerRotationByState(
        CharacterRotator rotator,
        PlayerMoveApplier moveApplier,
        PlayerAttackMovementBlend attackMovementBlend)
    {
        _rotator = rotator;
        _moveApplier = moveApplier;
        _attackMovementBlend = attackMovementBlend;
    }

    public void TickFixed(bool isInBattle, bool isMovementAllowed)
    {
        if (isInBattle == true)
        {
            _rotator.Rotate();
            return;
        }

        if (isMovementAllowed == false)
        {
            _rotator.RotateTowardsMovement(_moveApplier.AppliedMoveVector);
            return;
        }

        if (_attackMovementBlend.IsRecoverActive == true)
        {
            _rotator.RotateTowardsMovement(_moveApplier.AppliedMoveVector);
            return;
        }

        _rotator.RotateTowardsMovement(_moveApplier.InputMoveVector);
    }
}
