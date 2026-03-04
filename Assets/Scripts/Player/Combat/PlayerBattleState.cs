public sealed class PlayerBattleState
{
    private readonly PlayerAnimator _animator;
    private readonly AnimatorSwitcher _animatorSwitcher;
    private readonly WeaponHolder _weaponHolder;
    private readonly PlayerActiveWeaponType _activeWeaponType;
    private readonly PlayerBattleTimer _battleTimer;
    private readonly float _timeBattle;

    public bool IsInBattle { get; private set; }

    public PlayerBattleState(
        PlayerAnimator animator,
        AnimatorSwitcher animatorSwitcher,
        WeaponHolder weaponHolder,
        PlayerActiveWeaponType activeWeaponType,
        float timeBattle)
    {
        _animator = animator;
        _animatorSwitcher = animatorSwitcher;
        _weaponHolder = weaponHolder;
        _activeWeaponType = activeWeaponType;
        _timeBattle = timeBattle;

        _battleTimer = new PlayerBattleTimer();
    }

    public bool Tick(float deltaTime)
    {
        return _battleTimer.Tick(deltaTime);
    }

    public void Touch()
    {
        if (IsInBattle == false)
        {
            IsInBattle = true;

            _animator.SetFight(true);
            _weaponHolder.SetHoldAllowed(true);

            _animatorSwitcher.SetBattleMode(true);
            SyncWeaponAnimatorIfNeeded();
        }

        _battleTimer.Restart(_timeBattle);
    }

    public void Exit()
    {
        _battleTimer.Stop();
        _weaponHolder.SetHoldAllowed(false);

        if (IsInBattle == false)
        {
            return;
        }

        IsInBattle = false;

        _animator.SetFight(false);
        _animatorSwitcher.SetBattleMode(false);
        _animatorSwitcher.SetWeaponType(WeaponType.None);
    }

    public void SyncWeaponAnimatorIfNeeded()
    {
        if (IsInBattle == false)
        {
            return;
        }

        if (_weaponHolder.IsSwitchLocked)
        {
            return;
        }

        WeaponType weaponType = _activeWeaponType.Value;
        _animatorSwitcher.SetWeaponType(weaponType);
    }

    public void UnlockWeaponSwitchAndRefreshAnimator()
    {
        _weaponHolder.SetSwitchLocked(false);
        SyncWeaponAnimatorIfNeeded();
    }

    public void SetSwitchLocked(bool isLocked)
    {
        _weaponHolder.SetSwitchLocked(isLocked);
    }

    public void SetHoldAllowed(bool isAllowed)
    {
        _weaponHolder.SetHoldAllowed(isAllowed);
    }
}
