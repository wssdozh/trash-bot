using UnityEngine;

public sealed class PlayerSprint
{
    private readonly CharacterMover _movement;
    private readonly PlayerAnimator _animator;
    private readonly Stamina _stamina;
    private float _sprintStaminaCostPerSecond;

    public bool IsSprinting { get; private set; }

    public float StaminaCostPerSecond => _sprintStaminaCostPerSecond;

    public PlayerSprint(
        CharacterMover movement,
        PlayerAnimator animator,
        Stamina stamina,
        float sprintStaminaCostPerSecond)
    {
        _movement = movement;
        _animator = animator;
        _stamina = stamina;
        _sprintStaminaCostPerSecond = sprintStaminaCostPerSecond;

        IsSprinting = false;
    }

    public void SetCostPerSecond(float sprintStaminaCostPerSecond)
    {
        _sprintStaminaCostPerSecond = Mathf.Max(0f, sprintStaminaCostPerSecond);
    }

    public bool TryStart()
    {
        if (_stamina.Value <= 0f)
        {
            return false;
        }

        if (IsSprinting)
        {
            return true;
        }

        IsSprinting = true;
        _movement.OnSprint(true);
        _animator.SetSprintState(true);

        return true;
    }

    public void Stop()
    {
        if (IsSprinting == false)
        {
            return;
        }

        IsSprinting = false;
        _movement.OnSprint(false);
        _animator.SetSprintState(false);
    }

    public void Tick(float deltaTime)
    {
        if (IsSprinting == false)
        {
            return;
        }

        if (_stamina.Value <= _stamina.MinValue)
        {
            Stop();
            return;
        }

        _stamina.Decrease(_sprintStaminaCostPerSecond * deltaTime);
    }
}
