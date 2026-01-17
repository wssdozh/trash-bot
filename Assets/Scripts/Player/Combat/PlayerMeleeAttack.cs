public sealed class PlayerMeleeAttack
{
    private readonly Attacker _attacker;
    private readonly Stamina _stamina;
    private readonly PlayerAnimator _animator;
    private readonly PlayerAnimationEvents _animationEvents;
    private readonly PlayerMovementGate _movementGate;
    private readonly PlayerBattleState _battleState;
    private readonly float _attackStaminaCost;

    private bool _isAttackInProgress;
    private bool _isHitPending;
    private bool _isAttackBuffered;
    private bool _isBufferLocked;

    public PlayerMeleeAttack(
        Attacker attacker,
        Stamina stamina,
        PlayerAnimator animator,
        PlayerAnimationEvents animationEvents,
        PlayerMovementGate movementGate,
        PlayerBattleState battleState,
        float attackStaminaCost)
    {
        _attacker = attacker;
        _stamina = stamina;
        _animator = animator;
        _animationEvents = animationEvents;
        _movementGate = movementGate;
        _battleState = battleState;
        _attackStaminaCost = attackStaminaCost;
    }

    public bool StartAttack()
    {
        if (_isAttackInProgress == true)
        {
            return BufferAttack();
        }

        if (_stamina.Value <= 0f)
        {
            return false;
        }

        ResetState();
        StartAttackCore();

        return true;
    }

    public void CancelOnly()
    {
        ResetState();

        _movementGate.AllowMovement();
        _battleState.UnlockWeaponSwitchAndRefreshAnimator();
    }

    private bool BufferAttack()
    {
        if (_isBufferLocked == true)
        {
            return false;
        }

        if (_isAttackBuffered == true)
        {
            return false;
        }

        _isAttackBuffered = true;
        _isBufferLocked = true;

        _battleState.Touch();

        return true;
    }

    private bool TryStartChainedAttack()
    {
        if (_stamina.Value <= 0f)
        {
            return false;
        }

        StartAttackCore();
        return true;
    }

    private void StartAttackCore()
    {
        _isAttackInProgress = true;
        _isHitPending = true;

        _movementGate.BlockMovement();

        _animator.TriggerAttack();
        _battleState.Touch();

        _battleState.SetSwitchLocked(true);

        _animationEvents.Attacking += OnAttackingFrame;
        _animationEvents.AttackEnded += OnAttackEnded;
    }

    private void OnAttackingFrame()
    {
        if (_isAttackInProgress == false)
        {
            return;
        }

        if (_isHitPending == false)
        {
            return;
        }

        _isHitPending = false;

        if (_stamina.Value <= 0f)
        {
            return;
        }

        if (_attacker.PerformAttack() == true)
        {
            _stamina.Decrease(_attackStaminaCost);
        }
    }

    private void OnAttackEnded()
    {
        if (_isAttackInProgress == false)
        {
            return;
        }

        _isAttackInProgress = false;
        _isHitPending = false;

        _animationEvents.Attacking -= OnAttackingFrame;
        _animationEvents.AttackEnded -= OnAttackEnded;

        if (_isAttackBuffered == true)
        {
            _isAttackBuffered = false;

            if (TryStartChainedAttack() == true)
            {
                return;
            }
        }

        _isBufferLocked = false;

        _movementGate.AllowMovement();

        _battleState.UnlockWeaponSwitchAndRefreshAnimator();
    }

    private void ResetState()
    {
        if (_isAttackInProgress == true)
        {
            _animationEvents.Attacking -= OnAttackingFrame;
            _animationEvents.AttackEnded -= OnAttackEnded;
        }

        _isAttackInProgress = false;
        _isHitPending = false;
        _isAttackBuffered = false;
        _isBufferLocked = false;
    }
}
