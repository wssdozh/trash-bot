using UnityEngine;

public sealed class PlayerJumpAction
{
    private readonly CharacterJump _jump;
    private readonly Stamina _stamina;
    private float _jumpStaminaCost;

    public float StaminaCost => _jumpStaminaCost;

    public PlayerJumpAction(CharacterJump jump, Stamina stamina, float jumpStaminaCost)
    {
        _jump = jump;
        _stamina = stamina;
        _jumpStaminaCost = jumpStaminaCost;
    }

    public void SetCost(float jumpStaminaCost)
    {
        _jumpStaminaCost = Mathf.Max(0f, jumpStaminaCost);
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
