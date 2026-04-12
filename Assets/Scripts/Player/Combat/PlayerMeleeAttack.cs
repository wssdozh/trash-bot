public sealed class PlayerMeleeAttack
{
    private readonly Attacker _attacker;
    private readonly Stamina _stamina;
    private readonly WeaponModifierApplier _weaponModifierApplier;
    private readonly PlayerAnimator _animator;
    private readonly PlayerAnimationEvents _animationEvents;
    private readonly PlayerMovementGate _movementGate;
    private readonly PlayerBattleState _battleState;
    private float _attackStaminaCost;

    private bool _isAttackInProgress;
    private bool _isHitPending;
    private bool _isAttackBuffered;

    public PlayerMeleeAttack(
        Attacker attacker,
        Stamina stamina,
        WeaponModifierApplier weaponModifierApplier,
        PlayerAnimator animator,
        PlayerAnimationEvents animationEvents,
        PlayerMovementGate movementGate,
        PlayerBattleState battleState,
        float attackStaminaCost)
    {
        _attacker = attacker;
        _stamina = stamina;
        _weaponModifierApplier = weaponModifierApplier;
        _animator = animator;
        _animationEvents = animationEvents;
        _movementGate = movementGate;
        _battleState = battleState;
        _attackStaminaCost = attackStaminaCost;
    }

    public float AttackStaminaCost => _attackStaminaCost;

    public void SetAttackStaminaCost(float attackStaminaCost)
    {
        _attackStaminaCost = attackStaminaCost;
    }

    public bool StartAttack()
    {
        if (_isAttackInProgress)
        {
            if (_isAttackBuffered)
            {
                return false;
            }

            _isAttackBuffered = true;
            _battleState.Touch();

            return true;
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

    private void StartAttackCore()
    {
        _isAttackInProgress = true;
        _isHitPending = true;
        _isAttackBuffered = false;

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

        WeaponModifierContext weaponModifierContext = _weaponModifierApplier.BuildCurrentContext();

        if (_stamina.Value <= 0f)
        {
            return;
        }

        if (_attacker.PerformAttack(weaponModifierContext))
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

        if (_isAttackBuffered)
        {
            _isAttackBuffered = false;

            if (_stamina.Value > 0f)
            {
                _isHitPending = true;

                _animator.TriggerAttack();
                _battleState.Touch();

                return;
            }
        }

        _isAttackInProgress = false;
        _isHitPending = false;

        _animationEvents.Attacking -= OnAttackingFrame;
        _animationEvents.AttackEnded -= OnAttackEnded;

        _movementGate.AllowMovement();
        _battleState.UnlockWeaponSwitchAndRefreshAnimator();
    }

    private void ResetState()
    {
        if (_isAttackInProgress)
        {
            _animationEvents.Attacking -= OnAttackingFrame;
            _animationEvents.AttackEnded -= OnAttackEnded;
        }

        _isAttackInProgress = false;
        _isHitPending = false;
        _isAttackBuffered = false;
    }
}
