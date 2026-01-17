public sealed class PlayerJumpAction
{
    private readonly CharacterJump _jump;
    private readonly Stamina _stamina;
    private readonly float _jumpStaminaCost;

    public PlayerJumpAction(CharacterJump jump, Stamina stamina, float jumpStaminaCost)
    {
        _jump = jump;
        _stamina = stamina;
        _jumpStaminaCost = jumpStaminaCost;
    }

    public void TryJump(bool isMovementAllowed)
    {
        if (isMovementAllowed == false)
        {
            return;
        }

        if (_stamina.Value > _jumpStaminaCost)
        {
            _jump.OnJump();
            _stamina.Decrease(_jumpStaminaCost);
        }
    }
}
